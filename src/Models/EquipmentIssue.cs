namespace ShogaiBoard.Models;

/// <summary>
/// 計器・什器等の設備の不具合1件を表す。
/// 障害（システム障害）と異なり、対象は対象システムではなく対象計器マスター（<see cref="TargetEquipment"/>）から選ぶ。
/// 状況は解消日時の有無で判定する（未入力＝発生中、入力済み＝解消済み）。
/// </summary>
public class EquipmentIssue
{
    public int Id { get; set; }

    /// <summary>対象計器のID（必須）。</summary>
    public int EquipmentId { get; set; }

    /// <summary>対象計器へのナビゲーションプロパティ。</summary>
    public TargetEquipment? Equipment { get; set; }

    /// <summary>不具合内容（必須）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>発生日時（必須）。</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>解消日時（実績。未解消の間はnull）。</summary>
    public DateTime? ResolvedAt { get; set; }
}
