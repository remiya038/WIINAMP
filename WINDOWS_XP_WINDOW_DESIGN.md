# Windows XP風ウィンドウデザイン

Windows 11上で動くWebView、WinForms、WPFなどのデスクトップアプリに再利用できる、Windows XP（Luna）風のウィンドウ外観の設計仕様です。アプリ固有のコンテンツはこの外枠の内側へ置きます。

## 構成

```text
┌─ タイトルバー（29px）────────────────────── [−] [□] [×] ┐
│  アプリアイコン / アプリ名                                      │
├─ クライアント領域 ────────────────────────────────────────────┤
│  アプリ固有のUI                                                 │
└──────────────────────────────────────────────────────────────┘
```

タイトルバーはOS標準の枠線ではなく、アプリ側で描画する。ウィンドウの移動・最小化・閉じる操作はホスト（WinForms/WPF/Electron等）へイベントを渡して実行する。

## Luna Blue の基本トークン

| 要素 | 値 |
| --- | --- |
| タイトルバー | `linear-gradient(#5798ee, #2169cb 45%, #0a3d9a 51%, #164fac)` |
| ウィンドウ枠 | `#052468`、1px |
| 本文背景 | `#c4d7fb` |
| 標準ボタン | `linear-gradient(#fff, #b9d0f5)` |
| ボタン枠 | `#4d76bb` |
| タイトル文字 | 白、太字、1pxの濃紺シャドウ |
| 閉じるボタン | `linear-gradient(#f58f8f, #d63838 52%, #a91616)` |

Olive Green と Silver はタイトルバー・見出しのグラデーションのみ差し替え、余白・サイズ・ボタンの形状は共通にする。

## タイトルバーのルール

- 高さは29px。文字は Tahoma/Verdana の12px・太字を基準にする。
- 左右のパディングは8px。アプリ名は左、操作ボタン群は右に固定する。
- 操作ボタンは21px × 21px、間隔2px。角丸は2px程度に留め、白い1px枠＋濃い青の外側エッジでXP Lunaの小さな正方形にする。
- 記号は `−`、`□`、`×` を使う。アンダースコア `_` は下寄りに見えるため最小化記号に使わない。
- 操作記号は中央揃え、15pxを基準にする。最小化記号だけ18px程度でもよい。青ボタンは上が明るく下が濃いグラデーション、閉じるボタンは同じ構造の赤グラデーションにする。
- 閉じるボタンは赤系にし、最小化・最大化とは明確に区別する。
- 閉じるボタンにカーソルを置いたときも赤系の明るいグラデーションを使う。共通の青ボタン用ホバー色で上書きしない。
- 最大化を提供しないアプリでも位置を保つため `□` は表示して無効化できる。無効時は半透明・既定カーソルにする。
- タイトルバーのボタン以外を押してドラッグしたときだけ、ホストにウィンドウ移動を依頼する。

## レイアウトと可変パネル

- 外枠全体は `height: 100vh` の縦グリッドにし、タイトルバーを固定行、本文を可変行にする。
- 折りたたみ可能なパネルは、隠すときに中身だけを消すのではなく、ホスト側のクライアント高も縮める。
- 再表示へ戻す入口（`+`ボタン等）は必ず残し、縮小後も押せる位置に置く。
- コンテンツの高さを変更したら、可変領域だけが余白を吸収するようにする。操作バーやシークバーの直後に不要な余白を作らない。

## Web UI 用の最小CSS

```css
.xp-shell { height: 100vh; border: 1px solid #052468; background: #c4d7fb; }
.xp-titlebar {
  height: 29px; display: flex; align-items: center; padding: 0 8px;
  color: white; font: bold 12px Tahoma, Verdana, sans-serif;
  text-shadow: 1px 1px #00266e;
  background: linear-gradient(#5798ee, #2169cb 45%, #0a3d9a 51%, #164fac);
}
.xp-caption-buttons { margin-left: auto; display: flex; gap: 3px; }
.xp-caption-buttons button {
  width: 20px; height: 19px; padding: 0; display: grid; place-items: center;
  color: white; border: 1px solid #e0efff; border-radius: 3px;
  background: linear-gradient(#70a8ed, #1c5db8); font: 700 15px/1 Arial, sans-serif;
}
.xp-caption-buttons .close {
  border-color: #ffd2d2;
  background: linear-gradient(#f58f8f, #d63838 52%, #a91616);
}
```

## 実装時の確認項目

- 最小化記号・最大化記号・閉じる記号が視覚的に中央にある。
- 閉じるボタンだけ赤である。
- タスクバーにアプリ名とアイコンが表示される。
- コンテンツ領域を折りたたんでも、再表示ボタンが残る。
- 高DPI（125% / 150%）でもタイトルバーのボタンが欠けない。
