using Microsoft.AspNetCore.Mvc.RazorPages;
using ShogaiBoard.Data;
using ShogaiBoard.Models;
using ShogaiBoard.Services;

namespace ShogaiBoard.Pages;

/// <summary>
/// 閲覧専用ダッシュボード（/Display）。編集・削除ボタンやナビゲーションを一切持たず、
/// 障害状況を表示するだけの画面。庁内モニターへの常時投影や、社内ポータルへのiframe埋め込みなど、
/// 「操作させたくないが情報だけは見せたい」用途を想定している。
/// 表示内容はトップのダッシュボード（<see cref="IndexModel"/>）と同じ。
/// </summary>
public class DisplayModel : PageModel
{
    private readonly AppDbContext _db;

    public DisplayModel(AppDbContext db)
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

    /// <summary>発生中の障害のうち「緊急」の件数（警告バナー表示に使用）。復旧済みは含めない。</summary>
    public int CriticalCount => OngoingIncidents.Count(i => i.Severity == IncidentSeverity.Critical);

    public async Task OnGetAsync()
    {
        OngoingIncidents = await IncidentQueries.GetOngoingIncidentsAsync(_db);
        RecentlyResolvedIncidents = await IncidentQueries.GetRecentlyResolvedIncidentsAsync(_db);
        UpcomingMaintenances = await MaintenanceQueries.GetUpcomingMaintenancesAsync(_db);
        RecentlyEndedMaintenances = await MaintenanceQueries.GetRecentlyEndedMaintenancesAsync(_db);
    }
}
