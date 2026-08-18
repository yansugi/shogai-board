# 社内ポータル連携用ブラウザ拡張機能（サンプル）

ShogaiBoardの`GET /api/incidents`・`GET /api/incidents/resolved`・`GET /api/maintenances`を参照し、社内ポータル等の別ページに「発生中の障害・直近24時間の復旧・予定されているメンテナンス」をバナー表示するChrome拡張機能（Manifest V3）のサンプル。

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
- 現在発生中の障害が1件以上ある場合：赤系の警告色（ShogaiBoard本体の警告バナーと同じ配色）
- 発生中の障害が無く、復旧済み・メンテナンス予定のみの場合：落ち着いた青系
- 障害・復旧・メンテナンスがいずれも0件の場合：バナー自体を表示しない
- バナー右側に、ShogaiBoardの閲覧専用ダッシュボード（`/display`）への詳細リンクを表示する
