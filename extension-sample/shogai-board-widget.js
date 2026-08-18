// ShogaiBoard 障害・メンテナンス情報バナー（サンプル拡張機能）
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
const API_BASE_URL = 'https://your-shogai-board-server.example';
const DISPLAY_URL = 'https://your-shogai-board-server.example/display';

if (typeof document === 'undefined') {
    // ============ background（service worker）側の処理 ============
    // content scriptからのメッセージを受け取り、代わりにAPIを取得して返す。
    // host_permissionsが宣言されたservice workerからのfetchはCORS制限を受けない
    // （content script側で直接fetchすると、ページのCORS制限を受けてブロックされる）。
    chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
        if (message.type !== 'FETCH_SHOGAI_BOARD_STATUS') {
            return false;
        }

        (async () => {
            try {
                const [incidentsRes, resolvedRes, maintenancesRes] = await Promise.all([
                    fetch(`${API_BASE_URL}/api/incidents`),
                    fetch(`${API_BASE_URL}/api/incidents/resolved`),
                    fetch(`${API_BASE_URL}/api/maintenances`),
                ]);

                if (!incidentsRes.ok) {
                    throw new Error(`/api/incidents が ${incidentsRes.status} を返しました`);
                }
                if (!resolvedRes.ok) {
                    throw new Error(`/api/incidents/resolved が ${resolvedRes.status} を返しました`);
                }
                if (!maintenancesRes.ok) {
                    throw new Error(`/api/maintenances が ${maintenancesRes.status} を返しました`);
                }

                const incidents = await incidentsRes.json();
                const resolvedIncidents = await resolvedRes.json();
                const maintenances = await maintenancesRes.json();
                sendResponse({ ok: true, incidents, resolvedIncidents, maintenances });
            } catch (error) {
                sendResponse({ ok: false, error: error.message });
            }
        })();

        // 非同期でsendResponseを呼ぶために true を返す（Manifest V3のお作法）。
        return true;
    });
} else {
    // ============ content script側の処理 ============
    // backgroundにAPI取得を依頼し、結果を受け取ってポータル画面の先頭にバナーを追加する。
    chrome.runtime.sendMessage({ type: 'FETCH_SHOGAI_BOARD_STATUS' }, (response) => {
        if (chrome.runtime.lastError) {
            console.error('background との通信に失敗しました:', chrome.runtime.lastError.message);
            return;
        }
        if (!response || !response.ok) {
            console.error('ShogaiBoard情報の取得に失敗しました:', response && response.error);
            return;
        }

        const { incidents, resolvedIncidents, maintenances } = response;

        // 障害・復旧・メンテナンスのいずれも無ければ、何も表示しない。
        if (incidents.length === 0 && resolvedIncidents.length === 0 && maintenances.length === 0) {
            return;
        }

        // ① バナーを差し込むコンテナ要素を取得する。
        // "dn-main" は表示先ポータルの実際のDOM構造に合わせて書き換えること。
        const container = document.getElementById('dn-main');
        if (!container) {
            console.error('コンテナ要素が見つかりません');
            return;
        }

        // 表示するテキストを組み立てる。
        const messages = [];
        if (incidents.length > 0) {
            messages.push(`現在発生中の障害：${incidents.length}件（${incidents.map(i => i.system).join('、')}）`);
        }
        if (resolvedIncidents.length > 0) {
            messages.push(`直近24時間に復旧した障害：${resolvedIncidents.length}件（${resolvedIncidents.map(i => i.system).join('、')}）`);
        }
        if (maintenances.length > 0) {
            messages.push(`予定されているメンテナンス：${maintenances.length}件（${maintenances.map(m => m.system).join('、')}）`);
        }

        // 発生中の障害がある場合のみ目立つ赤系にする（復旧済み・メンテナンスのみなら落ち着いた青系）。
        const hasOngoingIncident = incidents.length > 0;

        // ② 新しい div 要素を作成する。
        const newDiv = document.createElement('div');
        newDiv.style.display = 'flex';
        newDiv.style.alignItems = 'center';
        newDiv.style.justifyContent = 'space-between';
        newDiv.style.height = '50px';
        newDiv.style.padding = '0 16px';
        newDiv.style.fontWeight = 'bold';
        newDiv.style.boxSizing = 'border-box';

        if (hasOngoingIncident) {
            // 発生中の障害がある場合は目立つ赤系の警告色にする（ShogaiBoard本体の警告バナーと同じ配色）。
            newDiv.style.background = 'linear-gradient(135deg, #dc2626, #b91c1c)';
            newDiv.style.color = '#ffffff';
        } else {
            // 復旧済み・メンテナンスのみの場合は落ち着いた青系にする。
            newDiv.style.background = '#e0f2fe';
            newDiv.style.color = '#0369a1';
        }

        const textSpan = document.createElement('span');
        textSpan.textContent = messages.join('　/　');
        newDiv.appendChild(textSpan);

        // 閲覧専用ダッシュボード（/display）へのリンクを追加する。
        const link = document.createElement('a');
        link.href = DISPLAY_URL;
        link.textContent = '詳細を見る ▶';
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        link.style.color = hasOngoingIncident ? '#ffffff' : '#0369a1';
        link.style.textDecoration = 'underline';
        link.style.marginLeft = '12px';
        link.style.whiteSpace = 'nowrap';
        newDiv.appendChild(link);

        // ③ 最初の子要素として追加する。
        container.prepend(newDiv);
    });
}
