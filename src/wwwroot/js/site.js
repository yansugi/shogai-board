// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ナビゲーションバーの更新ボタン：クリックした瞬間にアイコンを回転させつつ、ページを再読み込みする。
(function () {
    var refreshBtn = document.getElementById("refreshPageBtn");
    if (!refreshBtn) {
        return;
    }

    refreshBtn.addEventListener("click", function () {
        refreshBtn.classList.add("is-refreshing");
        location.reload();
    });
})();

// このページがiframeで埋め込まれている（社内ポータル等に組み込まれている）場合のみ、
// 障害登録画面へ遷移するリンク（ナビゲーションバーの「障害を登録する」ボタン、
// ダッシュボード各行の「編集」リンク）を新しいタブで開くようにする。
// 狭いiframe内でそのまま画面遷移すると、iframe内に障害登録画面が窮屈に表示されてしまうため。
// iframeでない通常表示の場合は、これまで通り同じタブ内で遷移する。
(function () {
    var isInIframe;
    try {
        isInIframe = window.self !== window.top;
    } catch {
        // クロスオリジンのiframeではwindow.topへのアクセス自体が例外になることがあるが、
        // その場合も「別ドメインに埋め込まれている＝iframe内」とみなしてよい。
        isInIframe = true;
    }

    if (!isInIframe) {
        return;
    }

    // ページによって存在しない要素もあるため、それぞれ見つかったものだけ処理する。
    var registerBtn = document.getElementById("navbarRegisterBtn");
    if (registerBtn) {
        registerBtn.target = "_blank";
    }

    document.querySelectorAll(".edit-incident-link, .edit-maintenance-link").forEach(function (link) {
        link.target = "_blank";
    });
})();

// 障害登録・メンテナンス登録の両画面で使う共通のフォーム部品。
window.ShogaiBoardForms = window.ShogaiBoardForms || {};

// 対象システム選択欄：全システムを対象に検索しながら選べるコンボボックスにする。
window.ShogaiBoardForms.setupSystemCombobox = function (searchInputId, hiddenInputId, listId, systemOptions) {
    const searchInput = document.getElementById(searchInputId);
    const hiddenInput = document.getElementById(hiddenInputId);
    const optionsList = document.getElementById(listId);

    // Enterキーで確定できるよう、直近の絞り込み結果と矢印キーでの選択位置を保持しておく。
    let currentFiltered = [];
    let activeIndex = -1;

    function closeOptionsList() {
        optionsList.classList.remove("show");
    }

    function selectSystem(option) {
        hiddenInput.value = option.id;
        searchInput.value = option.text;
        closeOptionsList();
    }

    function highlightActiveItem() {
        const items = optionsList.querySelectorAll(".system-option-item");
        items.forEach(function (el, idx) {
            if (idx === activeIndex) {
                el.classList.add("active");
                el.scrollIntoView({ block: "nearest" });
            } else {
                el.classList.remove("active");
            }
        });
    }

    function renderOptionsList(filterText) {
        const keyword = (filterText || "").trim().toLowerCase();
        const filtered = systemOptions.filter(function (opt) {
            return opt.text.toLowerCase().includes(keyword);
        });
        currentFiltered = filtered;
        activeIndex = filtered.length > 0 ? 0 : -1;

        optionsList.innerHTML = "";
        if (filtered.length === 0) {
            const empty = document.createElement("div");
            empty.className = "system-option-empty";
            empty.textContent = "該当するシステムがありません";
            optionsList.appendChild(empty);
        } else {
            filtered.forEach(function (opt, idx) {
                const item = document.createElement("button");
                item.type = "button";
                item.className = "system-option-item";
                item.textContent = opt.text;
                // clickではなくmousedownで処理し、input側のblurより先に選択を確定させる。
                item.addEventListener("mousedown", function (e) {
                    e.preventDefault();
                    selectSystem(opt);
                });
                item.addEventListener("mouseenter", function () {
                    activeIndex = idx;
                    highlightActiveItem();
                });
                optionsList.appendChild(item);
            });
            highlightActiveItem();
        }
        optionsList.classList.add("show");
    }

    searchInput.addEventListener("focus", function () {
        renderOptionsList(searchInput.value);
    });
    searchInput.addEventListener("input", function () {
        hiddenInput.value = "0";
        renderOptionsList(searchInput.value);
    });
    searchInput.addEventListener("blur", function () {
        setTimeout(closeOptionsList, 150);
    });
    searchInput.addEventListener("keydown", function (e) {
        if (e.key === "ArrowDown") {
            e.preventDefault();
            if (currentFiltered.length === 0) {
                return;
            }
            activeIndex = Math.min(activeIndex + 1, currentFiltered.length - 1);
            highlightActiveItem();
            return;
        }
        if (e.key === "ArrowUp") {
            e.preventDefault();
            if (currentFiltered.length === 0) {
                return;
            }
            activeIndex = Math.max(activeIndex - 1, 0);
            highlightActiveItem();
            return;
        }
        if (e.key === "Enter") {
            e.preventDefault();
            if (activeIndex >= 0 && activeIndex < currentFiltered.length) {
                selectSystem(currentFiltered[activeIndex]);
            }
        }
    });
    document.addEventListener("click", function (e) {
        if (!e.target.closest(".system-combobox")) {
            closeOptionsList();
        }
    });

    // 編集時など、既に選択済みのシステムがある場合は検索欄にシステム名を表示しておく。
    const initialId = parseInt(hiddenInput.value, 10);
    if (initialId) {
        const initialOption = systemOptions.find(function (opt) { return opt.id === initialId; });
        if (initialOption) {
            searchInput.value = initialOption.text;
        }
    }
};

// 発生日時・復旧予定時刻・メンテナンス予定日時等は、いずれも日付＋時刻を1つの欄で入力できるようにする。
window.ShogaiBoardForms.setupDateTimePicker = function (id) {
    const el = document.getElementById(id);
    return flatpickr(el, {
        enableTime: true,
        time_24hr: true,
        dateFormat: "Y-m-d H:i",
        defaultDate: el.value || undefined
    });
};
