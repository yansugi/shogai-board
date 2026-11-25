// ShogaiBoard 障害・不具合・メンテナンス情報バー（サンプル拡張機能）
//
// content script（DOM操作）とbackground/service worker（CORSを受けないfetch）の
// 両方からこの同じファイルを読み込ませ、実行環境で処理を振り分ける。
// service workerにはdocumentが存在しないため、これを判定に使う。
//
// 使い方：
// 1. API_BASE_URL・DISPLAY_URLを実際のShogaiBoardサーバーのURLに書き換える
// 2. manifest.jsonのhost_permissions・content_scripts.matchesも実際のURLに合わせて書き換える
// 3. 表示先ページの実際のDOM構造に合わせて、containerの取得方法（下記の "dn-main" の部分）を調整する

// 実際のShogaiBoardサーバーのURLに書き換えること。
const API_BASE_URL = "https://your-shogai-board-server.example";
const DISPLAY_URL = "https://your-shogai-board-server.example/display";

if (typeof document === "undefined") {
  // ============ background（service worker）側の処理 ============

  /**
   * 指定パスのAPIを取得してJSON配列を返す。
   * @param {string} path APIのパス（例："/api/incidents"）
   * @returns {Promise<Array>} 取得したJSON配列
   * @throws {Error} HTTPエラー、またはJSONとして解釈できなかった場合
   */
  async function fetchJson(path) {
    const res = await fetch(`${API_BASE_URL}${path}`);
    if (!res.ok) {
      throw new Error(`${path} が ${res.status} を返しました`);
    }
    return res.json();
  }

  /**
   * 計器・什器等のAPIを取得する。失敗してもシステム側のバナー表示を妨げないよう、
   * 例外は投げずに空配列とエラーメッセージを返す
   * （計器用APIを持たない古いバージョンのサーバーに接続した場合などを想定）。
   * @param {string} path APIのパス
   * @returns {Promise<{items: Array, error: string|null}>}
   */
  async function fetchEquipmentJson(path) {
    try {
      return { items: await fetchJson(path), error: null };
    } catch (error) {
      return { items: [], error: error.message };
    }
  }

  // content scriptからのメッセージを受け取り、代わりにAPIを取得して返す。
  // host_permissionsが宣言されたservice workerからのfetchはCORS制限を受けない。
  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type !== "FETCH_SHOGAI_BOARD_STATUS") {
      return false;
    }

    (async () => {
      try {
        // システムの障害・メンテナンス（失敗した場合は全体をエラーとする）
        const [incidents, resolvedIncidents, maintenances] = await Promise.all([
          fetchJson("/api/incidents"),
          fetchJson("/api/incidents/resolved"),
          fetchJson("/api/maintenances"),
        ]);

        // 計器・什器等の不具合・メンテナンス（失敗してもシステム側の表示は続ける）
        const [equipmentIssuesResult, equipmentMaintenancesResult] = await Promise.all([
          fetchEquipmentJson("/api/equipment-issues"),
          fetchEquipmentJson("/api/equipment-maintenances"),
        ]);

        sendResponse({
          ok: true,
          incidents,
          resolvedIncidents,
          maintenances,
          equipmentIssues: equipmentIssuesResult.items,
          equipmentMaintenances: equipmentMaintenancesResult.items,
          equipmentErrors: [equipmentIssuesResult.error, equipmentMaintenancesResult.error].filter(Boolean),
        });
      } catch (error) {
        sendResponse({ ok: false, error: error.message });
      }
    })();

    // 非同期でsendResponseを呼ぶために true を返す（Manifest V3のお作法）。
    return true;
  });
} else {
  // ============ content script側の処理 ============

  // 名前を並べるときに表示する最大件数（これを超えた分は「ほかN件」にまとめ、バーが長くなりすぎないようにする）
  const MAX_NAMES = 2;

  /**
   * 名前の一覧を、重複を除いて先頭MAX_NAMES件まで「、」でつなぐ。超えた分は「ほかN件」にまとめる。
   * @param {string[]} names 名前の配列
   * @returns {string} 例："A、B ほか1件"
   */
  function summarizeNames(names) {
    const unique = [...new Set(names)];
    const shown = unique.slice(0, MAX_NAMES).join("、");
    return unique.length > MAX_NAMES ? `${shown} ほか${unique.length - MAX_NAMES}件` : shown;
  }

  /**
   * カテゴリ1つ分のチップ（色付きの小さなラベル）を作る。
   * 表示は短くし、全件のフルテキストはマウスを乗せたときのツールチップ（title）に入れる。
   * @param {string} text チップに表示する短いテキスト
   * @param {string} fullText ツールチップに表示する全件のテキスト
   * @param {{background: string, color: string}} theme 背景色と文字色
   * @returns {HTMLElement} 作成したチップ要素
   */
  function createChip(text, fullText, theme) {
    const chip = document.createElement("span");
    chip.textContent = text;
    chip.title = fullText;
    chip.style.display = "inline-block";
    chip.style.padding = "3px 12px";
    chip.style.borderRadius = "999px";
    chip.style.fontSize = "13px";
    chip.style.fontWeight = "bold";
    chip.style.lineHeight = "1.5";
    chip.style.whiteSpace = "nowrap";
    chip.style.background = theme.background;
    chip.style.color = theme.color;
    return chip;
  }

  /**
   * チップを1行に並べた、細い情報バーを作る。右端に閲覧専用ダッシュボードへのリンクを1つだけ付ける。
   * 幅が足りない場合のみチップが折り返す。
   * @param {HTMLElement[]} chips 並べるチップ
   * @param {boolean} hasIncident 発生中の障害があるか（あれば、バー全体を薄い赤で強調する）
   * @returns {HTMLElement} 作成したバー要素
   */
  function createBar(chips, hasIncident) {
    const bar = document.createElement("div");
    bar.style.display = "flex";
    bar.style.flexWrap = "wrap";
    bar.style.alignItems = "center";
    bar.style.gap = "6px 8px";
    bar.style.minHeight = "36px";
    bar.style.padding = "4px 12px";
    bar.style.boxSizing = "border-box";
    bar.style.background = hasIncident ? "#fef2f2" : "#f8fafc";
    bar.style.borderBottom = hasIncident ? "2px solid #dc2626" : "1px solid #e2e8f0";

    chips.forEach((chip) => bar.appendChild(chip));

    // 閲覧専用ダッシュボード（/display）へのリンクを、バーの右端に1つだけ追加する。
    const link = document.createElement("a");
    link.href = DISPLAY_URL;
    link.textContent = "詳細を見る ▶";
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.style.marginLeft = "auto";
    link.style.fontSize = "13px";
    link.style.fontWeight = "bold";
    link.style.color = hasIncident ? "#b91c1c" : "#475569";
    link.style.textDecoration = "underline";
    link.style.whiteSpace = "nowrap";
    bar.appendChild(link);

    return bar;
  }

  // 各チップの配色（カテゴリごとに色を分け、複数のカテゴリが同時にあっても見分けられるようにする）
  const THEME_INCIDENT = { background: "linear-gradient(135deg, #dc2626, #b91c1c)", color: "#ffffff" }; // 発生中の障害（赤）
  const THEME_EQUIPMENT_ISSUE = { background: "#fef08a", color: "#854d0e" }; // 計器・什器等の不具合（黄色）
  const THEME_MAINTENANCE = { background: "#dbeafe", color: "#1d4ed8" }; // システムのメンテナンス（青。ダッシュボードのメンテナンス欄と同じ系統）
  const THEME_EQUIPMENT_MAINTENANCE = { background: "#bbf7d0", color: "#166534" }; // 計器・什器等のメンテナンス（緑）
  const THEME_RESOLVED = { background: "#e0f2fe", color: "#0369a1" }; // 直近24時間に復旧した障害（落ち着いた薄い青）

  // backgroundにAPI取得を依頼し、結果を受け取ってdn-mainの先頭に情報バーを追加する。
  chrome.runtime.sendMessage({ type: "FETCH_SHOGAI_BOARD_STATUS" }, (response) => {
    if (chrome.runtime.lastError) {
      console.error("background との通信に失敗しました:", chrome.runtime.lastError.message);
      return;
    }
    if (!response || !response.ok) {
      console.error("ShogaiBoard情報の取得に失敗しました:", response && response.error);
      return;
    }

    // 計器・什器等のAPIだけ失敗した場合は、システム側の表示は続けつつコンソールに残す。
    if (response.equipmentErrors && response.equipmentErrors.length > 0) {
      console.warn("計器・什器等の情報を取得できませんでした:", response.equipmentErrors.join(" / "));
    }

    const { incidents, resolvedIncidents, maintenances, equipmentIssues, equipmentMaintenances } = response;

    // ① カテゴリごとに1つずつチップを組み立てる。
    // 並び順はダッシュボードと同じ（障害・不具合 → メンテナンス → 履歴）。該当が0件のカテゴリは出さない。
    const chips = [];

    // 発生中の障害（赤）
    if (incidents.length > 0) {
      const names = incidents.map((i) => i.system);
      chips.push(createChip(
        `障害 ${incidents.length}件（${summarizeNames(names)}）`,
        `現在発生中の障害：${incidents.length}件（${names.join("、")}）`,
        THEME_INCIDENT
      ));
    }

    // 計器・什器等の不具合（黄色）
    if (equipmentIssues.length > 0) {
      const names = equipmentIssues.map((e) => e.equipmentName);
      chips.push(createChip(
        `計器等の不具合 ${equipmentIssues.length}件（${summarizeNames(names)}）`,
        `計器・什器等の不具合：${equipmentIssues.length}件（${[...new Set(names)].join("、")}）`,
        THEME_EQUIPMENT_ISSUE
      ));
    }

    // システムのメンテナンス（青）
    if (maintenances.length > 0) {
      const names = maintenances.map((m) => m.system);
      chips.push(createChip(
        `メンテナンス ${maintenances.length}件（${summarizeNames(names)}）`,
        `予定されているメンテナンス：${maintenances.length}件（${names.join("、")}）`,
        THEME_MAINTENANCE
      ));
    }

    // 計器・什器等のメンテナンス（緑）。実施中のものがあれば件数を併記する。
    if (equipmentMaintenances.length > 0) {
      const names = equipmentMaintenances.map((m) => m.equipmentName);
      const inProgressCount = equipmentMaintenances.filter((m) => m.status === "InProgress").length;
      const inProgressNote = inProgressCount > 0 ? `・実施中${inProgressCount}` : "";
      chips.push(createChip(
        `計器等のメンテナンス ${equipmentMaintenances.length}件${inProgressNote}（${summarizeNames(names)}）`,
        `計器・什器等のメンテナンス：${equipmentMaintenances.length}件${inProgressNote ? `（うち実施中 ${inProgressCount}件）` : ""}（${[...new Set(names)].join("、")}）`,
        THEME_EQUIPMENT_MAINTENANCE
      ));
    }

    // 直近24時間に復旧した障害（落ち着いた薄い青）
    if (resolvedIncidents.length > 0) {
      const names = resolvedIncidents.map((i) => i.system);
      chips.push(createChip(
        `復旧 ${resolvedIncidents.length}件（${summarizeNames(names)}）`,
        `直近24時間に復旧した障害：${resolvedIncidents.length}件（${names.join("、")}）`,
        THEME_RESOLVED
      ));
    }

    // 表示するものが何も無ければ、バー自体を出さない。
    if (chips.length === 0) {
      return;
    }

    // ② コンテナ要素を取得
    const container = document.getElementById("dn-main");
    if (!container) {
      console.error('id="dn-main" の要素が見つかりません');
      return;
    }

    // ③ 最初の子要素として追加する。
    container.prepend(createBar(chips, incidents.length > 0));
  });
}
