namespace ShogaiBoard.Models;

/// <summary>
/// 障害情報1件を表す。対象システムに対して複数件登録できる（システムを跨いだ再発・複数系統の障害にも対応）。
/// </summary>
public class Incident
{
    public int Id { get; set; }

    /// <summary>対象システムのID（必須）。</summary>
    public int SystemId { get; set; }

    /// <summary>対象システムへのナビゲーションプロパティ。</summary>
    public TargetSystem? System { get; set; }

    /// <summary>障害内容（必須）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>重要度。</summary>
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Major;

    /// <summary>対応状況。</summary>
    public IncidentStatus Status { get; set; } = IncidentStatus.Investigating;

    /// <summary>影響範囲・対象業務（任意）。</summary>
    public string? AffectedScope { get; set; }

    /// <summary>発生日時（必須）。</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>復旧予定時刻（見込み、任意）。</summary>
    public DateTime? EstimatedRecoveryAt { get; set; }

    /// <summary>復旧日時（実績）。ステータスが復旧済みになったときに設定される。</summary>
    public DateTime? ResolvedAt { get; set; }
}
