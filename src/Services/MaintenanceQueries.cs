using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Services;

/// <summary>
/// ダッシュボード（トップ画面・閲覧専用画面）で共通して使うメンテナンス予定の取得ロジック。
/// </summary>
public static class MaintenanceQueries
{
    /// <summary>終了したメンテナンスをダッシュボードに残しておく期間（障害の復旧済み表示と同じ長さに揃えている）。</summary>
    public static readonly TimeSpan EndedRetention = TimeSpan.FromHours(24);

    /// <summary>
    /// まだ終了していないメンテナンス予定（実施中・これから予定されているもの）を、
    /// 予定開始日時の早い順で取得する。
    /// </summary>
    public static Task<List<Maintenance>> GetUpcomingMaintenancesAsync(AppDbContext db)
    {
        var now = DateTime.Now;
        return db.Maintenances
            .Include(m => m.System)
            .Where(m => m.ScheduledEndAt >= now)
            .OrderBy(m => m.ScheduledStartAt)
            .ToListAsync();
    }

    /// <summary>
    /// 直近24時間以内に終了したメンテナンスを、終了日時の新しい順で取得する
    /// （終了直後にダッシュボードから即座に消えると見逃し確認がしづらいため、障害の復旧済み表示と同様にしばらく残す）。
    /// </summary>
    public static Task<List<Maintenance>> GetRecentlyEndedMaintenancesAsync(AppDbContext db)
    {
        var now = DateTime.Now;
        var retentionCutoff = now - EndedRetention;
        return db.Maintenances
            .Include(m => m.System)
            .Where(m => m.ScheduledEndAt < now && m.ScheduledEndAt >= retentionCutoff)
            .OrderByDescending(m => m.ScheduledEndAt)
            .ToListAsync();
    }
}
