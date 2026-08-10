namespace ShogaiBoard.Models;

/// <summary>
/// 障害情報の対象となるシステム／サービスのマスター。
/// 例：住民票交付システム、庁内メール、基幹ネットワーク等。
/// </summary>
public class TargetSystem
{
    public int Id { get; set; }

    /// <summary>システム名（必須）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>管轄部署等の補足情報（任意）。</summary>
    public string? OwnerSection { get; set; }

    /// <summary>一覧での表示順（小さい順に表示）。</summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// CSVインポートで新規追加されたシステムかどうか。
    /// trueの場合、削除・システム名の変更にはマスターパスワードが必要（誤って一括削除・改名されるのを防ぐため）。
    /// 手動で追加したシステムはfalseのままとし、誰でも削除・改名できる。
    /// </summary>
    public bool IsImported { get; set; }
}
