namespace ShogaiBoard.Models;

/// <summary>
/// 登録画面の共通ヘッダー（種別バッジ・タイトル・タブ）の表示内容。
/// 「障害・不具合登録」と「メンテナンス登録」の2グループがあり、それぞれ中でタブを切り替えて登録先を選ぶ。
/// </summary>
/// <param name="Group">グループ。"incident"（障害・不具合）または"maintenance"（メンテナンス）。</param>
/// <param name="ActiveTab">現在表示中のタブ。"system"（システム）または"equipment"（計器・什器等）。</param>
/// <param name="Title">画面のタイトル。</param>
/// <param name="Subtitle">タイトル下の説明文。</param>
/// <param name="IsEdit">編集モードかどうか。編集中は入力内容を失わないよう、他のタブへは切り替えられない。</param>
public record RegisterHeaderViewModel(string Group, string ActiveTab, string Title, string Subtitle, bool IsEdit);
