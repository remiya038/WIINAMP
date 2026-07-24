using NAudio.Wave;
using NAudio.Dsp;
using WindowsMediaController;
using Windows.Storage.Streams;
using System.Text.Json;

namespace WinampXp;

internal sealed class SystemMediaMonitor : IDisposable
{
    private WasapiLoopbackCapture? capture;
    private MediaManager? mediaManager;
    private readonly Action<string> send;
    private readonly System.Threading.Timer appleTimer;
    private bool enabled;
    private bool disposed;
    private int appleUpdateInProgress;
    private readonly SemaphoreSlim applePublishLock = new(1, 1);
    private const int FftLength = 1024;
    private readonly Complex[] fftBuffer = new Complex[FftLength];
    private int fftPosition;
    private string? artworkKey;
    private string? artworkDataUrl;

    public SystemMediaMonitor(Action<string> messageSender)
    {
        send = messageSender;
        appleTimer = new System.Threading.Timer(async _ => await UpdateAppleMusicAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Toggle() { if (enabled) Stop(); else Start(); }
    public void EnsureStarted() { if (!enabled) Start(); }

    /// <summary>Forwards transport commands to Apple Music's Windows media session.</summary>
    public async Task ControlAppleMusicAsync(string command)
    {
        var currentManager = mediaManager;
        if (currentManager is null) return;
        try
        {
            var session = currentManager.CurrentMediaSessions.Values.FirstOrDefault(s =>
                $"{s.Id} {s.ControlSession.SourceAppUserModelId}".Contains("apple", StringComparison.OrdinalIgnoreCase));
            if (session is null) return;

            switch (command)
            {
                case "toggle": await session.ControlSession.TryTogglePlayPauseAsync(); break;
                case "pause": await session.ControlSession.TryPauseAsync(); break;
                case "next": await session.ControlSession.TrySkipNextAsync(); break;
                case "previous": await session.ControlSession.TrySkipPreviousAsync(); break;
                case "stop": await session.ControlSession.TryStopAsync(); break;
            }
        }
        catch (Exception exception)
        {
            SendNotice($"Apple Music control failed: {FriendlyMessage(exception)}");
        }
    }

    private void Start()
    {
        if (enabled || disposed) return;

        var problems = new List<string>();
        TryStartAudioCapture(problems);
        TryStartMediaManager(problems);

        enabled = capture is not null || mediaManager is not null;
        if (mediaManager is not null) appleTimer.Change(0, 750);
        SendState(problems.Count == 0 ? null : string.Join(" / ", problems));
    }

    private void TryStartAudioCapture(List<string> problems)
    {
        try
        {
            capture = new WasapiLoopbackCapture();
            capture.DataAvailable += OnAudioData;
            capture.RecordingStopped += OnRecordingStopped;
            capture.StartRecording();
        }
        catch (Exception exception)
        {
            ReleaseAudioCapture();
            problems.Add($"Audio capture unavailable: {FriendlyMessage(exception)}");
        }
    }

    private void TryStartMediaManager(List<string> problems)
    {
        try
        {
            mediaManager = new MediaManager();
            mediaManager.OnAnyMediaPropertyChanged += OnAnyMediaPropertyChanged;
            mediaManager.Start();
            mediaManager.ForceUpdate();
            foreach (var session in mediaManager.CurrentMediaSessions.Values)
                _ = PublishAppleMediaAsync(session, forceArtworkRefresh: true);
        }
        catch (Exception exception)
        {
            ReleaseMediaManager();
            problems.Add($"Media session unavailable: {FriendlyMessage(exception)}");
        }
    }

    private void OnAnyMediaPropertyChanged(
        MediaManager.MediaSession session,
        Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties media)
    {
        _ = PublishAppleMediaAsync(session, forceArtworkRefresh: true);
    }

    private void Stop()
    {
        appleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ReleaseAudioCapture();
        ReleaseMediaManager();
        enabled = false;
        SendState();
    }

    private void ReleaseAudioCapture()
    {
        var previousCapture = capture;
        capture = null;
        if (previousCapture is null) return;

        previousCapture.DataAvailable -= OnAudioData;
        previousCapture.RecordingStopped -= OnRecordingStopped;
        try { previousCapture.StopRecording(); }
        catch { }
        previousCapture.Dispose();
    }

    private void ReleaseMediaManager()
    {
        var previousManager = mediaManager;
        mediaManager = null;
        if (previousManager is not null)
            previousManager.OnAnyMediaPropertyChanged -= OnAnyMediaPropertyChanged;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs eventArgs)
    {
        if (disposed || eventArgs.Exception is null) return;
        ReleaseAudioCapture();
        if (mediaManager is null) enabled = false;
        SendState($"Audio capture stopped: {FriendlyMessage(eventArgs.Exception)}");
    }

    private void OnAudioData(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;
        var samples = e.BytesRecorded / sizeof(float);
        var sum = 0d;
        var channels = Math.Max(1, capture?.WaveFormat.Channels ?? 2);
        for (var index = 0; index < samples; index += channels)
        {
            var value = BitConverter.ToSingle(e.Buffer, index * sizeof(float));
            sum += value * value;
            fftBuffer[fftPosition].X = (float)(value * FastFourierTransform.HammingWindow(fftPosition, FftLength));
            fftBuffer[fftPosition].Y = 0;
            fftPosition++;
            if (fftPosition == FftLength)
            {
                fftPosition = 0;
                FastFourierTransform.FFT(true, (int)Math.Log2(FftLength), fftBuffer);
                var bands = new double[60];
                for (var band = 0; band < bands.Length; band++)
                {
                    var start = Math.Max(1, (int)Math.Pow(2, band * 9.0 / bands.Length));
                    var end = Math.Min(FftLength / 2 - 1, (int)Math.Pow(2, (band + 1) * 9.0 / bands.Length));
                    var peak = 0d;
                    for (var bin = start; bin <= end; bin++) peak = Math.Max(peak, Math.Sqrt(fftBuffer[bin].X * fftBuffer[bin].X + fftBuffer[bin].Y * fftBuffer[bin].Y));
                    bands[band] = Math.Clamp(Math.Log10(1 + peak * 180) / 2.3, 0, 1);
                }
                Send(new { type = "frequencyBands", bands });
            }
        }
        var rms = Math.Sqrt(sum / Math.Max(1, samples / channels));
        var level = Math.Clamp(rms * 4.5, 0.0, 1.0);
        Send(new { type = "systemAudio", level });
    }

    private void Send(object value) => send(JsonSerializer.Serialize(value));
    private async Task PublishAppleMediaAsync(MediaManager.MediaSession session, bool forceArtworkRefresh = false)
    {
        await applePublishLock.WaitAsync();
        try
        {
            var source = $"{session.Id} {session.ControlSession.SourceAppUserModelId}";
            if (!source.Contains("apple", StringComparison.OrdinalIgnoreCase)) return;
            var media = await session.ControlSession.TryGetMediaPropertiesAsync();
            var playback = session.ControlSession.GetPlaybackInfo();
            var timeline = session.ControlSession.GetTimelineProperties();
            var duration = Math.Max(0, (timeline.EndTime - timeline.StartTime).TotalSeconds);
            var currentArtworkKey = $"{media.Title}\u001f{media.Artist}\u001f{media.AlbumTitle}";
            if (forceArtworkRefresh || artworkDataUrl is null || !string.Equals(artworkKey, currentArtworkKey, StringComparison.Ordinal))
            {
                artworkKey = currentArtworkKey;
                artworkDataUrl = await ReadArtworkAsync(media.Thumbnail);
            }

            Send(new { type = "appleMusic", title = media.Title, artist = media.Artist, album = media.AlbumTitle, artwork = artworkDataUrl, playing = playback.PlaybackStatus.ToString() == "Playing", elapsed = timeline.Position.TotalSeconds, duration });
        }
        catch { }
        finally
        {
            applePublishLock.Release();
        }
    }

    private static async Task<string?> ReadArtworkAsync(IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail is null) return null;

        using var stream = await thumbnail.OpenReadAsync();
        if (stream.Size is 0 or > 5 * 1024 * 1024) return null;

        using var reader = new DataReader(stream);
        await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[(int)stream.Size];
        reader.ReadBytes(bytes);
        var contentType = DetectImageContentType(bytes);
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string DetectImageContentType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return "image/gif";
        if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return "image/webp";
        return "image/jpeg";
    }
    private async Task UpdateAppleMusicAsync()
    {
        var currentManager = mediaManager;
        if (!enabled || currentManager is null || Interlocked.Exchange(ref appleUpdateInProgress, 1) != 0) return;
        try
        {
            foreach (var session in currentManager.CurrentMediaSessions.Values)
                await PublishAppleMediaAsync(session);
        }
        catch (Exception exception)
        {
            SendNotice($"Apple Music update failed: {FriendlyMessage(exception)}");
        }
        finally
        {
            Volatile.Write(ref appleUpdateInProgress, 0);
        }
    }
    private void SendState(string? message = null) => Send(new { type = "systemAudioState", enabled, message });
    private void SendNotice(string message) => Send(new { type = "systemAudioNotice", message });
    private static string FriendlyMessage(Exception exception) =>
        string.IsNullOrWhiteSpace(exception.Message) ? exception.GetType().Name : exception.Message;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        appleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ReleaseAudioCapture();
        ReleaseMediaManager();
        enabled = false;
        appleTimer.Dispose();
    }
}
