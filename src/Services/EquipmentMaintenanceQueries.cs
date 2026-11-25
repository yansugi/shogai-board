using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Services;

/// <summary>
/// ダッシュボード（トップ画面・閲覧専用画面）で共通して使う、計器メンテナンス予定の取得ロジック。
/// </summary>
public static class EquipmentMaintenanceQueries
{
    /// <summary>まだ終了していない計器メンテナンス（実施中・予定）を、予定開始日時の早い順で取得する。</summary>
    public static Task<List<EquipmentMaintenance>> GetUpcomingAsync(AppDbContext db)
    {
        var now = DateTime.Now;
        return db.EquipmentMaintenances
            .Include(m => m.Equipment)
            .Where(m => m.ScheduledEndAt >= now)
            .OrderBy(m => m.ScheduledStartAt)
            .ToListAsync();
    }

    /// <summary>直近24時間以内に終了した計器メンテナンスを、終了日時の新しい順で取得する（システムのメンテナンスと同じ保持期間）。</summary>
    public static Task<List<EquipmentMaintenance>> GetRecentlyEndedAsync(AppDbContext db)
    {
        var now = DateTime.Now;
        var retentionCutoff = now - MaintenanceQueries.EndedRetention;
        return db.EquipmentMaintenances
            .Include(m => m.Equipment)
            .Where(m => m.ScheduledEndAt < now && m.ScheduledEndAt >= retentionCutoff)
            .OrderByDescending(m => m.ScheduledEndAt)
            .ToListAsync();
    }
}
