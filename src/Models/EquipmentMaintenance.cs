namespace ShogaiBoard.Models;

/// <summary>
/// 計器・什器等の事前に計画されたメンテナンス（保守・点検作業）の予定1件を表す。
/// システムのメンテナンス（<see cref="Maintenance"/>）と同様に、重要度や対応状況の概念は持たず、
/// 「予定」「実施中」「終了」の状況は予定開始・終了日時と現在時刻から動的に判定する。
/// </summary>
public class EquipmentMaintenance
{
    public int Id { get; set; }

    /// <summary>対象計器のID（必須）。</summary>
    public int EquipmentId { get; set; }

    /// <summary>対象計器へのナビゲーションプロパティ。</summary>
    public TargetEquipment? Equipment { get; set; }

    /// <summary>メンテナンス内容（必須）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>予定開始日時（必須）。</summary>
    public DateTime ScheduledStartAt { get; set; }

    /// <summary>予定終了日時（必須）。</summary>
    public DateTime ScheduledEndAt { get; set; }
}

/// <summary>計器メンテナンス予定の状況（予定/実施中/終了）を画面表示用に変換する拡張メソッド。</summary>
public static class EquipmentMaintenanceDisplayExtensions
{
    /// <summary>現在時刻を基準に、メンテナンスが終了しているかどうかを判定する。</summary>
    public static bool IsEnded(this EquipmentMaintenance m, DateTime now) => now >= m.ScheduledEndAt;

    /// <summary>現在時刻を基準に、メンテナンスが実施中かどうかを判定する。</summary>
    public static bool IsInProgress(this EquipmentMaintenance m, DateTime now) =>
        m.ScheduledStartAt <= now && now < m.ScheduledEndAt;

    /// <summary>状況の日本語ラベル（予定／実施中／終了）を返す。</summary>
    public static string ToStatusLabel(this EquipmentMaintenance m, DateTime now) =>
        m.IsEnded(now) ? "終了" : m.IsInProgress(now) ? "実施中" : "予定";

    /// <summary>状況に応じたバッジ用CSSクラスを返す（システムのメンテナンスと同じ配色）。</summary>
    public static string ToStatusBadgeClass(this EquipmentMaintenance m, DateTime now) =>
        m.IsEnded(now) ? "status-badge status-maintenance-ended"
        : m.IsInProgress(now) ? "status-badge status-maintenance-inprogress"
        : "status-badge status-maintenance-scheduled";
}
