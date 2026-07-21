using NAudio.Wave;
using NAudio.Dsp;
using WindowsMediaController;
using System.Text.Json;

namespace WinampXp;

internal sealed class SystemMediaMonitor : IDisposable
{
    private WasapiLoopbackCapture? capture;
    private MediaManager? mediaManager;
    private readonly Action<string> send;
    private readonly System.Threading.Timer appleTimer;
    private bool enabled;
    private const int FftLength = 1024;
    private readonly Complex[] fftBuffer = new Complex[FftLength];
    private int fftPosition;

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
        if (mediaManager is null) return;
        try
        {
            var session = mediaManager.CurrentMediaSessions.Values.FirstOrDefault(s =>
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
        catch { }
    }

    private void Start()
    {
        capture = new WasapiLoopbackCapture();
        capture.DataAvailable += OnAudioData;
        capture.StartRecording();
        mediaManager = new MediaManager();
        mediaManager.OnAnyMediaPropertyChanged += (session, media) =>
        {
            _ = PublishAppleMediaAsync(session);
        };
        mediaManager.Start();
        mediaManager.ForceUpdate();
        foreach (var session in mediaManager.CurrentMediaSessions.Values) _ = PublishAppleMediaAsync(session);
        appleTimer.Change(0, 750);
        enabled = true;
        SendState();
    }

    private void Stop()
    {
        appleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        if (capture is not null)
        {
            capture.DataAvailable -= OnAudioData;
            capture.StopRecording();
            capture.Dispose();
            capture = null;
        }
        mediaManager = null;
        enabled = false;
        SendState();
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
    private async Task PublishAppleMediaAsync(MediaManager.MediaSession session)
    {
        try
        {
            var source = $"{session.Id} {session.ControlSession.SourceAppUserModelId}";
            if (!source.Contains("apple", StringComparison.OrdinalIgnoreCase)) return;
            var media = await session.ControlSession.TryGetMediaPropertiesAsync();
            var playback = session.ControlSession.GetPlaybackInfo();
            var timeline = session.ControlSession.GetTimelineProperties();
            var duration = Math.Max(0, (timeline.EndTime - timeline.StartTime).TotalSeconds);
            Send(new { type = "appleMusic", title = media.Title, artist = media.Artist, album = media.AlbumTitle, playing = playback.PlaybackStatus.ToString() == "Playing", elapsed = timeline.Position.TotalSeconds, duration });
        }
        catch { }
    }
    private async Task UpdateAppleMusicAsync()
    {
        if (!enabled || mediaManager is null) return;
        foreach (var session in mediaManager.CurrentMediaSessions.Values) await PublishAppleMediaAsync(session);
    }
    private void SendState() => Send(new { type = "systemAudioState", enabled });
    public void Dispose() { Stop(); appleTimer.Dispose(); }
}
