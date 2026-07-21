# WIINAMP

Windows 11向けの、Windows XP Luna風デザインを採用したデスクトップ音楽ビジュアライザーです。Apple Musicの再生情報とWindowsのシステム音声を表示できます。

## ダウンロード

最新版は[Releases](https://github.com/remiya038/WIINAMP/releases)から `WIINAMP.exe` をダウンロードしてください。

## 動作環境

- Windows 11（64bit）
- Microsoft Edge WebView2 Runtime
  - 通常はWindows 11にあらかじめ含まれています。
- Apple Music連携を使用する場合は、Windows版Apple Musicが必要です。

## 起動方法

1. Releasesから `WIINAMP.exe` をダウンロードします。
2. 任意のフォルダへ保存します。
3. `WIINAMP.exe` を実行します。

インストールは不要です。

## 主な機能

- Windows XP Luna風のウィンドウデザイン
- Windowsで再生中の音声に連動する60バンドの周波数ビジュアライザー
- Apple Musicの曲名、アーティスト、アルバム、再生位置の表示
- Apple Musicの再生／一時停止／前後スキップ操作
- ローカル音源（MP3、WAV、OGG、M4Aなど）の追加・再生
- プレイリスト表示の開閉
- 常に手前に表示、ビジュアライザー感度、配色テーマの設定

## SYSTEM MIX と Apple Music連携

起動時はSYSTEM MIXが有効です。

- SYSTEM MIXは、PCから出力される音声をリアルタイムに解析して表示します。
- Apple MusicがWindowsのメディアセッション情報を公開している場合、曲名・再生時間・操作ボタンが連携します。
- Apple Music側の状態やWindowsの設定によっては、曲情報や操作が反映されないことがあります。
- 音声データを録音・保存・外部送信する機能はありません。

## Windowsの警告について

初回配布版はコード署名を行っていないため、Windows Defender SmartScreenに「認識されないアプリ」として警告されることがあります。

ダウンロード元がこのGitHubリポジトリのReleasesであることを確認したうえで、実行するか判断してください。今後、コード署名および配布実績の蓄積により警告が減る場合があります。

## 問題が起きた場合

- ビジュアライザーが動かない: SYSTEM MIXが有効か確認し、Windowsで音声が再生されていることを確認してください。
- Apple Musicの情報が表示されない: Apple Musicを起動して再生を開始し、数秒待ってください。
- 起動しない: Windows UpdateとMicrosoft Edge WebView2 Runtimeの更新を確認してください。

## 開発・フィードバック

不具合報告や改善提案は、[Issues](https://github.com/remiya038/WIINAMP/issues)へお願いします。
