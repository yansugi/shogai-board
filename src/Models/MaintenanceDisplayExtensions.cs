namespace ShogaiBoard.Models;

/// <summary>メンテナンス予定の状況（予定/実施中/終了）を画面表示用に変換する拡張メソッド。</summary>
public static class MaintenanceDisplayExtensions
{
    /// <summary>現在時刻を基準に、メンテナンスが終了しているかどうかを判定する。</summary>
    public static bool IsEnded(this Maintenance maintenance, DateTime now) =>
        now >= maintenance.ScheduledEndAt;

    /// <summary>現在時刻を基準に、メンテナンスが実施中かどうかを判定する。</summary>
    public static bool IsInProgress(this Maintenance maintenance, DateTime now) =>
        maintenance.ScheduledStartAt <= now && now < maintenance.ScheduledEndAt;

    /// <summary>状況の日本語ラベル（予定／実施中／終了）を返す。</summary>
    public static string ToStatusLabel(this Maintenance maintenance, DateTime now) =>
        maintenance.IsEnded(now) ? "終了" : maintenance.IsInProgress(now) ? "実施中" : "予定";

    /// <summary>状況に応じたバッジ用CSSクラスを返す。</summary>
    public static string ToStatusBadgeClass(this Maintenance maintenance, DateTime now) =>
        maintenance.IsEnded(now) ? "status-badge status-maintenance-ended"
        : maintenance.IsInProgress(now) ? "status-badge status-maintenance-inprogress"
        : "status-badge status-maintenance-scheduled";
}
