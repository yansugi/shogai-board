using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Services;

/// <summary>
/// ダッシュボード（トップ画面・閲覧専用画面）で共通して使う障害情報の取得ロジック。
/// 両画面で表示内容や「復旧済みを残す期間」がずれないよう、クエリを1箇所にまとめている。
/// </summary>
public static class IncidentQueries
{
    /// <summary>復旧済みの障害をダッシュボードに残しておく期間。</summary>
    public static readonly TimeSpan ResolvedRetention = TimeSpan.FromHours(24);

    /// <summary>現在発生中（未復旧）の障害を、重要度→発生日時の順で取得する。</summary>
    public static Task<List<Incident>> GetOngoingIncidentsAsync(AppDbContext db) =>
        db.Incidents
            .Include(i => i.System)
            .Where(i => i.Status != IncidentStatus.Resolved)
            .OrderBy(i => i.Severity)
            .ThenBy(i => i.OccurredAt)
            .ToListAsync();

    /// <summary>直近24時間以内に復旧した障害を、復旧日時の新しい順で取得する。</summary>
    public static Task<List<Incident>> GetRecentlyResolvedIncidentsAsync(AppDbContext db)
    {
        var retentionCutoff = DateTime.Now - ResolvedRetention;
        return db.Incidents
            .Include(i => i.System)
            .Where(i => i.Status == IncidentStatus.Resolved && i.ResolvedAt != null && i.ResolvedAt >= retentionCutoff)
            .OrderByDescending(i => i.ResolvedAt)
            .ToListAsync();
    }
}
