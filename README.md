# 障害情報ボード

システム／サービス単位の障害発生状況・事前に予定されているメンテナンス情報をイントラネット上で共有するWebアプリ。要件の詳細は [requirements.md](requirements.md) を参照。

> ⚠️ **閉域のイントラネット限定での利用を前提としています。インターネットに公開しないでください。**
> 認証機能を持たないため（[既知の制約](#既知の制約仕様として意図したもの)参照）、インターネット上に公開すると対象システム名や障害情報を誰でも閲覧・書き換えできてしまいます。
>
> また、[appsettings.json](src/appsettings.json)の`SystemManagement:MasterPassword`は初期値（`changeme123`）のままです。**デプロイ前に必ず独自の値へ変更してください。**

## 構成
- **フレームワーク**: ASP.NET Core 8.0（Razor Pages）
- **DB**: SQLite（`shogai-board.db`、アプリ実行フォルダー直下に自動生成）
- **ソースコード**: [src/](src/)
- **API**: `GET /api/incidents`・`GET /api/maintenances`（現在発生中の障害・まだ終了していないメンテナンス予定の一覧をJSONで返す。Slack/Teams等の連携用。詳細は[API仕様](#api仕様)を参照）
- **閲覧専用ダッシュボード**: `/Display`（編集・削除ボタンやナビゲーションを持たない、表示専用の画面。庁内モニターへの常時投影やiframe埋め込み用途）

姉妹アプリの[部署不在ボード（Inainja）](../inainja)と同じ技術構成・配置方針を踏襲している。

## API仕様

### `GET /api/incidents`
現在発生中（未復旧）の障害情報一覧を返す。Slack/Teams等の社内ツールから定期ポーリングして通知する用途を想定。

| 項目 | 内容 |
|---|---|
| 認証 | なし（画面と同様、イントラ限定のため） |
| クエリパラメーター | なし |
| 対象データ | 対応状況（`status`）が「復旧済み」以外の障害情報すべて |
| ソート順 | 重要度（緊急→重要→軽微）昇順、同じ重要度内では発生日時（`occurredAt`）昇順 |
| レスポンス | `200 OK`、`Content-Type: application/json`。発生中の障害がなければ空配列`[]` |

**レスポンスの各要素**

| フィールド | 型 | 説明 |
|---|---|---|
| `system` | string | 対象システム名 |
| `description` | string | 障害内容 |
| `severity` | string | 重要度（`Critical`＝緊急、`Major`＝重要、`Minor`＝軽微） |
| `status` | string | 対応状況（`Investigating`＝調査中、`InProgress`＝対応中） |
| `affectedScope` | string \| null | 影響範囲・対象業務（未入力の場合は`null`） |
| `occurredAt` | string（ISO 8601日時） | 発生日時 |
| `estimatedRecoveryAt` | string（ISO 8601日時）\| null | 復旧予定時刻（見込み、未入力の場合は`null`） |

**レスポンス例**
```json
[
  {
    "system": "住民票交付システム",
    "description": "オンライン交付が利用できない状態です。",
    "severity": "Critical",
    "status": "Investigating",
    "affectedScope": "住民票のコンビニ交付のみ",
    "occurredAt": "2026-08-10T09:00:00",
    "estimatedRecoveryAt": null
  }
]
```

**実装箇所**: [Program.cs](src/Program.cs)（Razor Pagesの画面とは別に、Minimal APIとして直接定義）。

### `GET /api/maintenances`
まだ終了していない（実施中・これから予定されている）メンテナンス予定一覧を返す。用途は`/api/incidents`と同様。

| 項目 | 内容 |
|---|---|
| 認証 | なし（画面と同様、イントラ限定のため） |
| クエリパラメーター | なし |
| 対象データ | 予定終了日時が現在時刻以降のメンテナンス予定すべて |
| ソート順 | 予定開始日時（`scheduledStartAt`）昇順 |
| レスポンス | `200 OK`、`Content-Type: application/json`。該当がなければ空配列`[]` |

**レスポンスの各要素**

| フィールド | 型 | 説明 |
|---|---|---|
| `system` | string | 対象システム名 |
| `description` | string | メンテナンス内容 |
| `status` | string | 状況（`Scheduled`＝予定、`InProgress`＝実施中。現在時刻から算出） |
| `affectedScope` | string \| null | 影響範囲・対象業務（未入力の場合は`null`） |
| `scheduledStartAt` | string（ISO 8601日時） | 予定開始日時 |
| `scheduledEndAt` | string（ISO 8601日時） | 予定終了日時 |

**レスポンス例**
```json
[
  {
    "system": "庁内メール",
    "description": "サーバー機器の定期点検のため、一時的にサービスを停止します。",
    "status": "Scheduled",
    "affectedScope": null,
    "scheduledStartAt": "2026-08-20T09:00:00",
    "scheduledEndAt": "2026-08-20T12:00:00"
  }
]
```

## 推奨環境
- **サーバーOS**: Debian/Ubuntu系またはRHEL/CentOS/Alma系のLinux。
- **ブラウザ（利用者側）**: Chrome・Edge・Firefox等の最新版を想定。日時選択にFlatpickrを使用しているため、更新の古いブラウザやIEには対応しない。
- **サーバースペックの目安**: 庁内イントラの数システム〜数十システム規模での利用を想定した軽量な構成のため、CPU 1〜2コア・メモリ1GB程度でも十分動作する。
- **ストレージ容量の目安**: 通常デプロイ（`-r linux-x64`指定）の実行ファイル一式は約16MB程度（別途.NET Runtimeのインストールが必要）。自己完結型デプロイの場合は`.NET Runtime`同梱で約150MB程度。DB（SQLite）はテキストデータのみで、画像等の添付は扱わないため、長期運用してもDBファイル自体は数MB〜数十MB程度に収まる見込み（不在ボードと異なり、復旧済みの障害も履歴として残す設計のため、件数次第でやや増える）。合計でも1GB程度のストレージがあれば余裕を持って運用できる。

## 必要なソフトウェア（Linuxサーバー側）
- **ASP.NET Core 8.0 Runtime**（Kestrelでアプリ本体を実行するために必要）
  - Debian/Ubuntu系の例（[Microsoftのパッケージリポジトリ](https://learn.microsoft.com/dotnet/core/install/linux)を登録した上で）:
    ```
    sudo apt-get update
    sudo apt-get install -y aspnetcore-runtime-8.0
    ```
  - RHEL/CentOS/Alma系の例:
    ```
    sudo dnf install -y aspnetcore-runtime-8.0
    ```
- **Nginx**（Kestrelの手前に置くリバースプロキシ。80/443番ポートで待ち受け、内部でKestrelに転送する）
  ```
  sudo apt-get install -y nginx    # または sudo dnf install -y nginx
  ```
- アプリ専用の実行ユーザー（例：`shogai-board`）。rootでは実行しない。
  ```
  sudo useradd --system --no-create-home --shell /usr/sbin/nologin shogai-board
  ```

> **サーバーがインターネットに接続できない場合**は、以下の手順は使えません（apt/dnfでのパッケージ取得が前提のため）。[オフライン（インターネット非接続）サーバーへの配置手順](#オフラインインターネット非接続サーバーへの配置手順)を参照してください。

## ビルド・発行手順（開発機で実行）
```
cd src
dotnet publish -c Release -r linux-x64 --self-contained false -o ../publish
```
`-r linux-x64`でLinux向けのランタイム識別子を指定することで、SQLiteのネイティブライブラリがWindows/macOS等の他OS分までバンドルされるのを防ぎ、発行サイズを抑えられる（RID未指定だと約47MBになるところ、約16MBまで削減できる）。`--self-contained false`のため、これは自己完結型デプロイではなく、サーバー側に別途ASP.NET Core Runtimeのインストールが必要な点は変わらない（[後述](#必要なソフトウェアlinuxサーバー側)）。

`publish` フォルダーの中身一式を、そのままLinuxサーバーにコピーする（`scp`やファイル共有等で転送する）。

## Linuxサーバーへの配置手順
1. `publish` フォルダーの中身を、サーバー上の任意のフォルダー（例：`/var/www/shogai-board`）にコピーする。
2. 配置フォルダーの所有者を実行ユーザーに変更する。
   ```
   sudo chown -R shogai-board:shogai-board /var/www/shogai-board
   ```
   SQLiteのDBファイル（`shogai-board.db`）がこのフォルダーの下に自動生成されるため、実行ユーザーに書き込み権限が必要。
3. [deploy/shogai-board.service](deploy/shogai-board.service) を参考に、`/etc/systemd/system/shogai-board.service` を作成する（`WorkingDirectory`・`ExecStart`のパスを配置先に合わせて調整する）。
   ```
   sudo systemctl daemon-reload
   sudo systemctl enable --now shogai-board
   sudo systemctl status shogai-board
   ```
4. [deploy/nginx-shogai-board.conf](deploy/nginx-shogai-board.conf) を参考に、Nginxのリバースプロキシ設定を作成する（`server_name`をイントラ内のホスト名に合わせる）。
   ```
   sudo cp deploy/nginx-shogai-board.conf /etc/nginx/sites-available/shogai-board.conf
   sudo ln -s /etc/nginx/sites-available/shogai-board.conf /etc/nginx/sites-enabled/
   sudo nginx -t && sudo systemctl reload nginx
   ```
5. ブラウザで `http://<サーバー名>/` にアクセスし、ダッシュボードが表示されることを確認する。

## オフライン（インターネット非接続）サーバーへの配置手順
ここでの「オフライン」は、**サーバーが導入時にインターネットへ出ていけるかどうか**（apt等でパッケージを取得できるかどうか）の話であり、アプリを外部に公開するかどうかとは別の軸。イントラ限定公開という前提は変わらない。むしろサーバー自体がインターネットに一切繋がらない構成は、その前提をもっとも確実に満たす形と言える。

サーバーがインターネットに接続できない環境向けの手順。通常の配置手順（apt/dnfでのASP.NET Core Runtime・Nginxのインストールが前提）とは異なり、以下の2点を変更する。
- **自己完結型デプロイ**を使い、.NET Runtimeをアプリに同梱する（サーバー側でのRuntimeインストールが不要になる）。
- **Nginxを使わず、Kestrelが直接80番ポートで待ち受ける**（TLSを使わないイントラ限定公開のため、リバースプロキシは必須ではない。オフライン転送するパッケージも1つ減らせる）。

### 1. インターネットに接続できる開発機で自己完結型ビルドを作成する
```
cd src
dotnet publish -c Release -r linux-x64 --self-contained true -o ../publish-offline
```
`publish-offline` フォルダーの中に、Linuxネイティブの実行ファイル `ShogaiBoard`（拡張子なし）と`.NET Runtime`一式がまとめて出力される。この1フォルダーだけでサーバー側にRuntimeがなくても動作する。

### 2. `publish-offline` フォルダーをサーバーへ転送する
USBメモリや社内ファイル共有等、インターネットを経由しない方法で転送する。**転送後は必ずファイル数を比較するなどして、転送漏れ・破損がないか確認すること**（USB経由の転送ではI/Oエラーで一部ファイルだけ静かに欠けることがある）。

### 3. サーバー側のセットアップ
```
sudo useradd --system --no-create-home --shell /usr/sbin/nologin shogai-board
sudo mkdir -p /var/www/shogai-board
sudo cp -r /path/to/publish-offline/. /var/www/shogai-board/
sudo chown -R shogai-board:shogai-board /var/www/shogai-board
sudo chmod +x /var/www/shogai-board/ShogaiBoard
```
80番ポートは通常root権限が必要だが、以下のコマンドで実行ユーザーのまま待ち受けられるようにする（`setcap` コマンドがない場合は `libcap2-bin` パッケージが必要）。
```
sudo setcap 'cap_net_bind_service=+ep' /var/www/shogai-board/ShogaiBoard
```

### 4. systemdサービス化
[deploy/shogai-board-offline.service](deploy/shogai-board-offline.service) を参考に、`/etc/systemd/system/shogai-board.service` を作成する（`WorkingDirectory`・`ExecStart`のパスを配置先に合わせて調整する）。
```
sudo systemctl daemon-reload
sudo systemctl enable --now shogai-board
sudo systemctl status shogai-board
```

### 5. 動作確認
```
curl http://127.0.0.1/
```
社内LAN内の別端末から `http://<サーバーのIPアドレス>/` にアクセスし、ダッシュボードが表示されることを確認する。

### 更新時の手順
DBファイル（`shogai-board.db`）を上書き消去しないよう注意する。
```
sudo systemctl stop shogai-board
sudo cp /var/www/shogai-board/shogai-board.db /tmp/shogai-board.db.bak   # DBを退避
sudo rm -rf /var/www/shogai-board/*
sudo cp -r /path/to/new-publish-offline/. /var/www/shogai-board/
sudo cp /tmp/shogai-board.db.bak /var/www/shogai-board/shogai-board.db   # DBを戻す
sudo chmod +x /var/www/shogai-board/ShogaiBoard   # USB転送等で実行属性が落ちることがあるため、コピーのたびに再設定する
sudo chown -R shogai-board:shogai-board /var/www/shogai-board
sudo setcap 'cap_net_bind_service=+ep' /var/www/shogai-board/ShogaiBoard   # 実行ファイルを上書きするたび再設定が必要
sudo systemctl start shogai-board
```

### オフライン環境特有の注意点
- **ICU未インストール環境での起動失敗**: ICU（国際化ライブラリ）が入っていないサーバーで自己完結型デプロイを起動すると、`Couldn't find a valid ICU package installed on the system` というエラーで起動に失敗することがある。本アプリはカルチャ依存の文字列処理をしていないため、[src/ShogaiBoard.csproj](src/ShogaiBoard.csproj)で`<InvariantGlobalization>true</InvariantGlobalization>`を設定済み（ICUなしで動作する）。
- **時刻同期**: インターネット上のNTPサーバーに繋がらないため、サーバーの時刻がずれていないか確認する（`timedatectl`）。このアプリは障害の発生・復旧時刻の記録に現在時刻を使うため、時刻ずれが直接記録の正確性に影響する。社内にNTPサーバーがあれば設定しておくと安心。
- **Nginxを使いたい場合**: 複数アプリを1台のサーバーに同居させたい等の理由でNginxを使いたい場合は、インターネットに接続できる同バージョンのDebian環境で `sudo apt-get install --download-only -y nginx` を実行し、`/var/cache/apt/archives/` にできる`.deb`ファイル一式をオフラインサーバーへ転送、`sudo dpkg -i *.deb` でインストールする。その場合は上記の`setcap`手順は不要にし、`ASPNETCORE_URLS`を`http://127.0.0.1:5243`に戻した上で、[deploy/nginx-shogai-board.conf](deploy/nginx-shogai-board.conf)を使う（通常の配置手順と同様）。

## 保守メモ
- **初回起動時**、およびマイグレーション追加後の起動時に、DBのテーブルは自動的に作成・更新される（`Program.cs` で `db.Database.Migrate()` を実行しているため）。手動でのDB初期化作業は不要。
- **データモデルを変更した場合**は、開発機で以下を実行してマイグレーションを追加し、`src/Migrations` 以下のファイルごと発行・配置する。
  ```
  cd src
  dotnet ef migrations add <変更内容が分かる名前>
  ```
  `dotnet-ef` コマンドがない場合は `dotnet tool install --global dotnet-ef --version 8.0.10` でインストールする。
- **DBのバックアップ**: `shogai-board.db` ファイルをそのままコピーするだけでよい（アプリを止めなくても、SQLiteはファイルロックにより読み取り中の破損は防がれる。確実を期す場合は業務時間外にコピーする）。
- **ログ**: `journalctl -u shogai-board -f` でsystemdが管理する標準出力・エラーログをリアルタイムに確認できる。
- **再発行時の反映手順**: `dotnet publish` の成果物を配置先に上書きコピーした後、`sudo systemctl restart shogai-board` でアプリを再起動する。
- **アプリの起動確認**: `sudo systemctl status shogai-board` で稼働状況を、`curl http://127.0.0.1:5243/` でKestrel自体への到達性を確認できる（Nginx経由の到達性とは切り分けて調査できる）。

## 既知の制約（仕様として意図したもの）
- 認証なし。イントラネット内から誰でも閲覧・登録・対象システムマスターの編集が可能。ただし、対象システムマスター管理画面のCSV一括取り込みと、CSVで取り込まれたシステムの削除・システム名の変更のみマスターパスワードで保護（手動で追加したシステムは誰でも削除・改名可能）。
- 1つの対象システムに対して複数件の障害情報を並行して登録できる（不在ボードと異なり「システム＋日付で1件のみ」のような制約はない）。
- 障害の解消は、対応状況を「復旧済み」に変更することで行う。復旧予定時刻を過ぎても自動では復旧済みにならない（実際の復旧確認が必要なため）。
- 「復旧済み」に変更した障害は、復旧日時から24時間はダッシュボードに「直近24時間に復旧した障害」として別枠で残り、24時間経過後に自動的に一覧表示から外れる（復旧直後にダッシュボードから即座に消えると見逃し確認がしづらいための措置）。
- 復旧済みの障害情報は履歴として保持され、自動削除されない（監査・振り返り用途を考慮）。24時間経過後はダッシュボードに表示されないが、誤登録等で不要になった情報は手動で削除できる。
- メンテナンス予定は障害情報とは別テーブルで管理し、重要度・対応状況の概念を持たない。状況（予定／実施中／終了）は登録された予定日時と現在時刻から都度算出するため、手動でのステータス更新は不要。
- 予定終了日時を過ぎたメンテナンスは、障害の「復旧済み」と同様、終了日時から24時間はダッシュボードに別枠で残り、24時間経過後に自動的に一覧表示から外れる（物理削除はされず、履歴として残る）。

詳細な機能要件・非機能要件は [requirements.md](requirements.md) を参照。

## ライセンス
[MIT License](LICENSE)
