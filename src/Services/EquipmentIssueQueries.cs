using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Services;

/// <summary>
/// ダッシュボード（トップ画面・閲覧専用画面）で共通して使う、設備不具合の取得ロジック。
/// </summary>
public static class EquipmentIssueQueries
{
    /// <summary>解消した不具合をダッシュボードに残しておく期間（障害の復旧済み表示と同じ長さに揃えている）。</summary>
    public static readonly TimeSpan ResolvedRetention = TimeSpan.FromHours(24);

    /// <summary>未解消（発生中）の設備不具合を、発生日時の早い順で取得する。</summary>
    public static Task<List<EquipmentIssue>> GetOngoingAsync(AppDbContext db)
    {
        return db.EquipmentIssues
            .Include(e => e.Equipment)
            .Where(e => e.ResolvedAt == null)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();
    }

    /// <summary>直近24時間以内に解消した設備不具合を、解消日時の新しい順で取得する。</summary>
    public static Task<List<EquipmentIssue>> GetRecentlyResolvedAsync(AppDbContext db)
    {
        var retentionCutoff = DateTime.Now - ResolvedRetention;
        return db.EquipmentIssues
            .Include(e => e.Equipment)
            .Where(e => e.ResolvedAt != null && e.ResolvedAt >= retentionCutoff)
            .OrderByDescending(e => e.ResolvedAt)
            .ToListAsync();
    }
}
