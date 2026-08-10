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

    document.querySelectorAll(".edit-incident-link").forEach(function (link) {
        link.target = "_blank";
    });
})();
