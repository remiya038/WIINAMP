# WINAMP // XP Edition

Windows 11 のブラウザーで動く、Windows XP 風の Winamp インスパイア音楽プレーヤーです。

`index.html` を Microsoft Edge または Chrome で開いてください。音楽ファイルをドラッグ＆ドロップするか、**ADD MEDIA** から選択すると再生できます。選択したファイルはネットワークへ送信されず、このブラウザー内だけで扱われます。

## Windows アプリ版

`WinampXp.exe` は専用ウィンドウで起動する Windows 11 向けアプリ版です。配布用ファイルは `publish\\WinampXp.exe` に作成されます。この `.exe` ひとつを配布できます。Edge WebView2 Runtime（通常の Windows 11 に標準搭載）が必要です。

## 主な機能

- MP3 / WAV / OGG / M4A など、ブラウザーが再生できるローカル音源
- プレイリスト、曲送り・戻し、シーク、音量、シャッフル、リピート
- Apple Musicの曲・アルバムURLの保存と、Apple Musicアプリ／既定ブラウザーでの起動
- SYSTEM MIX: Windowsの既定出力の音をリアルタイムに可視化。Apple MusicがWindowsのメディアセッションを公開している場合は曲情報も表示
- Windows XP の Luna を思わせる外観と疑似スペクトラム表示

アプリとして配布する場合は、次の段階で Electron または Tauri のラッパーを追加して `.exe` にパッケージ化できます。
