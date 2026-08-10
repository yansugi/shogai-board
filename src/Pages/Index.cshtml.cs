using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Pages;

/// <summary>
/// トップ画面（ダッシュボード）。現在発生中（未復旧）の障害のみを一覧表示する。
/// 復旧済みの障害は自動削除せず、履歴としてDBに残す（一覧表示からは外れる）。
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Incident> OngoingIncidents { get; set; } = new();

    /// <summary>現在発生中（未復旧）の障害一覧を、重要度→発生日時の順で取得する。</summary>
    public async Task OnGetAsync()
    {
        OngoingIncidents = await _db.Incidents
            .Include(i => i.System)
            .Where(i => i.Status != IncidentStatus.Resolved)
            .OrderBy(i => i.Severity)
            .ThenBy(i => i.OccurredAt)
            .ToListAsync();
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
}
