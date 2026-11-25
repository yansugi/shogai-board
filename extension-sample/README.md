# 社内ポータル連携用ブラウザ拡張機能（サンプル）

ShogaiBoardの`GET /api/incidents`・`GET /api/incidents/resolved`・`GET /api/maintenances`・`GET /api/equipment-issues`・`GET /api/equipment-maintenances`を参照し、社内ポータル等の別ページに「発生中の障害・直近24時間の復旧・予定されているメンテナンス・計器・什器等の不具合・計器・什器等のメンテナンス」をバナー表示するChrome拡張機能（Manifest V3）のサンプル。

> ⚠️ このフォルダー内のURL（`your-shogai-board-server.example`・`your-portal.example`）はすべて架空のものです。実際に使う際は、自分の環境のURLに書き換えてください。

## 構成
- [manifest.json](manifest.json)：拡張機能の定義。`host_permissions`にShogaiBoardのURL、`content_scripts.matches`に表示先ポータルのURLを指定する。
- [shogai-board-widget.js](shogai-board-widget.js)：本体スクリプト。background（service worker）とcontent scriptの両方から同じファイルを読み込ませ、実行環境（`document`の有無）で処理を振り分けている。

## なぜbackground/service worker経由にしているか
ShogaiBoardのAPIにはCORS設定を入れていない（イントラ限定でのAPI連携を想定した簡易仕様のため）。content script内で直接`fetch`すると、表示先ページのオリジンを基準としたCORS制限を受けてブロックされる。

Manifest V3のbackground（service worker）は、`host_permissions`が宣言されていればCORS制限を受けずに`fetch`できるため、実際の通信はbackground側で行い、content scriptとは`chrome.runtime.sendMessage`でやり取りする構成にしている。

## セットアップ手順
1. `shogai-board-widget.js`内の`API_BASE_URL`・`DISPLAY_URL`を、実際のShogaiBoardサーバーのURLに書き換える。
2. `manifest.json`の`host_permissions`（ShogaiBoardのURL）・`content_scripts.matches`（表示先ポータルのURL）も同様に書き換える。
3. `shogai-board-widget.js`内の`document.getElementById('dn-main')`の`"dn-main"`を、実際の表示先ページでバナーを差し込みたい要素のIDに書き換える。
4. Chromeで`chrome://extensions`を開き、「デベロッパーモード」をONにする。
5. 「パッケージ化されていない拡張機能を読み込む」から、このフォルダーを選択する。
6. 表示先ポータルのページを開き（既に開いていた場合は再読み込み）、バナーが表示されることを確認する。

## 表示仕様
ポータル上部を圧迫しないよう、**高さ約36pxの細い情報バー1本**に、カテゴリごとの色付きチップを横に並べて表示する（幅が足りない場合のみチップが折り返す）。複数のカテゴリが同時にあっても、色で見分けられる。該当が0件のカテゴリのチップは表示しない。並び順はダッシュボードと同じ（障害・不具合 → メンテナンス → 履歴）。

| 順 | チップ | 配色 |
|---|---|---|
| 1 | 障害（発生中） | 赤系の警告色（ShogaiBoard本体の警告バナーと同じ配色） |
| 2 | 計器等の不具合 | **黄色** |
| 3 | メンテナンス（システム） | 青系（ダッシュボードのメンテナンス欄と同じ系統） |
| 4 | 計器等のメンテナンス（実施中があれば件数を併記） | **緑** |
| 5 | 復旧（直近24時間） | 落ち着いた薄い青 |

- チップには件数と、先頭2件までの名前を表示する。3件目以降は「ほかN件」にまとめ、全件の名前はチップにマウスを乗せたときのツールチップで確認できる。
- 発生中の障害があるときは、バー全体を薄い赤にして下線を赤くし、気づきやすくする。
- すべてが0件の場合は、バー自体を表示しない。
- バーの右端に、ShogaiBoardの閲覧専用ダッシュボード（`/display`）への詳細リンクを1つ表示する。
- 計器・什器等のAPI（`/api/equipment-issues`・`/api/equipment-maintenances`）の取得に失敗した場合（計器用APIを持たない古いバージョンのサーバーなど）でも、システム側のチップは従来どおり表示する（失敗内容はブラウザのコンソールに警告として出力する）。
