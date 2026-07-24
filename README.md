# WIINAMP

Windows 11向けの、Windows XP Luna風デザインを採用したデスクトップ音楽プレーヤー／ビジュアライザーです。ローカル音源の再生に加え、Windowsのシステム出力を可視化し、Apple Musicが公開する曲情報とアルバムアートの表示、再生操作に対応します。

## ダウンロード

最新版は[Releases](https://github.com/remiya038/WIINAMP/releases)から `WIINAMP.exe` をダウンロードしてください。

## 動作環境

- Windows 11（64ビット）
- Microsoft Edge WebView2 Runtime
- SYSTEM MIXを使用する場合は、有効なWindowsの音声出力デバイス
- Apple Music連携を使用する場合は、Windows版Apple Music

WebView2 Runtimeは通常のWindows 11に含まれています。搭載されていない環境では、MicrosoftからWebView2 Runtimeをインストールしてください。

## 起動方法

1. `WIINAMP.exe` を任意のフォルダーへ保存します。
2. `WIINAMP.exe` を実行します。

自己完結型の単一ファイルなので、インストールや.NET Runtimeの追加導入は不要です。

現在のEXEにはコード署名がないため、Windows Defender SmartScreenが「認識されないアプリ」として警告する場合があります。ダウンロード元がこのGitHubリポジトリのReleasesであることを確認したうえで、実行するか判断してください。

## 主な機能

- Windows XP Luna風のウィンドウデザインとミニモード
- Windowsで再生中の音声に連動する60バンドの周波数ビジュアライザー
- Apple Musicの曲名、アーティスト、アルバム、アルバムアート、再生位置の表示
- Apple Musicの再生／一時停止／前後スキップ／停止操作
- ローカル音源（MP3、WAV、OGG、M4Aなど）の追加・再生
- プレイリスト、シーク、音量、シャッフル、リピート
- 常に手前に表示、ビジュアライザー感度、配色テーマの設定

ローカル音源やSYSTEM MIXの音声データを外部送信する機能はありません。

## SYSTEM MIXとApple Music連携

起動時はSYSTEM MIXが有効です。

- SYSTEM MIXは、PCから出力される音声をリアルタイムに解析して表示します。
- Apple MusicがWindowsのメディアセッション情報を公開している場合、曲情報・アルバムアート・再生時間・操作ボタンが連携します。
- 音声取得とApple Music連携は個別に初期化されるため、片方が利用できない環境でも利用可能な機能は継続します。
- 利用できない機能がある場合は、画面下部のステータスに表示されます。詳細はステータスへマウスポインターを合わせると確認できます。

## 制約

- Apple Music側の状態やWindowsの設定によっては、曲情報や操作が反映されない場合があります。
- SYSTEM MIXは、音声デバイスやドライバーの状態によって利用できない場合があります。
- プレイリストの内容はアプリ終了時に保存されません。
- 現在の配布EXEはコード署名されていません。

## 問題が起きた場合

- ビジュアライザーが動かない: SYSTEM MIXが有効か確認し、Windowsで音声が再生されていることを確認してください。
- Apple Musicの情報が表示されない: Apple Musicを起動して再生を開始し、数秒待ってください。
- 起動しない: Windows UpdateとMicrosoft Edge WebView2 Runtimeの更新を確認してください。
- その他の不具合: [Issues](https://github.com/remiya038/WIINAMP/issues)へ状況を報告してください。

## 開発と発行

必要なものは.NET 8 SDKです。

```powershell
dotnet build .\WinampXp.csproj -c Release
dotnet publish .\WinampXp.csproj -c Release -o .\publish
```

発行後の配布ファイルは `publish\WIINAMP.exe` です。

## ライセンス

ライセンスはまだ設定されていません。第三者への再配布や改変版の公開を行う前に、ライセンス条件を決定してください。
