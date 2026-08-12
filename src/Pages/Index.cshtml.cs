using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShogaiBoard.Data;
using ShogaiBoard.Models;
using ShogaiBoard.Services;

namespace ShogaiBoard.Pages;

/// <summary>
/// トップ画面（ダッシュボード）。現在発生中（未復旧）の障害に加え、直近24時間以内に復旧した障害も
/// 「復旧済み」として一覧表示する（復旧直後にダッシュボードから即座に消えてしまうと、
/// 見逃し確認がしづらいため）。24時間を過ぎるとダッシュボードからは自動的に外れるが、
/// 復旧済みの障害は自動削除せず、履歴としてDBには残り続ける。
/// 編集・削除ができる管理用の画面。閲覧専用にしたい場合は<see cref="DisplayModel"/>（/Display）を使う。
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>現在発生中（未復旧）の障害。</summary>
    public List<Incident> OngoingIncidents { get; set; } = new();

    /// <summary>直近24時間以内に復旧済みになった障害（新しく復旧したものから順）。</summary>
    public List<Incident> RecentlyResolvedIncidents { get; set; } = new();

    /// <summary>まだ終了していないメンテナンス予定（実施中・これから予定されているもの。予定開始日時の早い順）。</summary>
    public List<Maintenance> UpcomingMaintenances { get; set; } = new();

    /// <summary>直近24時間以内に終了したメンテナンス（終了日時の新しいものから順）。</summary>
    public List<Maintenance> RecentlyEndedMaintenances { get; set; } = new();

    /// <summary>発生中の障害のうち「緊急」の件数（ダッシュボード上部の警告バナー表示に使用）。復旧済みは含めない。</summary>
    public int CriticalCount => OngoingIncidents.Count(i => i.Severity == IncidentSeverity.Critical);

    /// <summary>
    /// 現在発生中（未復旧）の障害を重要度→発生日時の順で、
    /// 直近24時間以内に復旧した障害を復旧日時の新しい順で、
    /// まだ終了していないメンテナンス予定を予定開始日時の早い順で、
    /// 直近24時間以内に終了したメンテナンスを終了日時の新しい順で取得する。
    /// </summary>
    public async Task OnGetAsync()
    {
        OngoingIncidents = await IncidentQueries.GetOngoingIncidentsAsync(_db);
        RecentlyResolvedIncidents = await IncidentQueries.GetRecentlyResolvedIncidentsAsync(_db);
        UpcomingMaintenances = await MaintenanceQueries.GetUpcomingMaintenancesAsync(_db);
        RecentlyEndedMaintenances = await MaintenanceQueries.GetRecentlyEndedMaintenancesAsync(_db);
    }

    /// <summary>誤登録などで掲示が不要になった障害情報を手動で削除する。</summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        _db.Incidents.Remove(incident);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    /// <summary>不要になったメンテナンス予定を手動で削除する（予定の取り消し等）。</summary>
    public async Task<IActionResult> OnPostDeleteMaintenanceAsync(int id)
    {
        var maintenance = await _db.Maintenances.FindAsync(id);
        if (maintenance is null)
        {
            return NotFound();
        }

        _db.Maintenances.Remove(maintenance);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }
}
