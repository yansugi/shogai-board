namespace ShogaiBoard.Models;

/// <summary>
/// 事前に計画されたメンテナンス（保守作業）の予定1件を表す。
/// 障害と異なり緊急対応ではなく計画的な作業のため、重要度や対応状況（調査中/対応中/復旧済み）の概念は持たない。
/// 「予定」「実施中」「終了」の状況は、予定開始・終了日時と現在時刻から動的に判定する（手動でのステータス更新は不要）。
/// </summary>
public class Maintenance
{
    public int Id { get; set; }

    /// <summary>対象システムのID（必須）。</summary>
    public int SystemId { get; set; }

    /// <summary>対象システムへのナビゲーションプロパティ。</summary>
    public TargetSystem? System { get; set; }

    /// <summary>メンテナンス内容（必須）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>影響範囲・対象業務（任意）。</summary>
    public string? AffectedScope { get; set; }

    /// <summary>予定開始日時（必須）。</summary>
    public DateTime ScheduledStartAt { get; set; }

    /// <summary>予定終了日時（必須）。</summary>
    public DateTime ScheduledEndAt { get; set; }
}
