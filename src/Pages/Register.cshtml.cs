using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Pages;

/// <summary>
/// 障害情報の登録・編集画面。対象システムを選んで障害内容・重要度・対応状況・発生日時等を登録する。
/// 不在ボードと異なり、同一システムに対して複数件の障害を並行して登録できる（システム跨ぎの複合障害等にも対応）。
/// ダッシュボードの「編集」リンクからIdを指定して開いた場合は、既存の障害情報を編集する。
/// </summary>
public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public RegisterModel(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>編集対象の障害情報ID。ダッシュボードの「編集」リンクから遷移した場合のみ設定される。</summary>
    [BindProperty]
    public int? Id { get; set; }

    [BindProperty]
    public int SystemId { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Major;

    [BindProperty]
    public IncidentStatus Status { get; set; } = IncidentStatus.Investigating;

    [BindProperty]
    public string? AffectedScope { get; set; }

    // InvariantGlobalization有効時、カルチャ依存の既定書式で出力されるとFlatpickr（Y-m-d H:i形式を期待）が
    // 解釈できず日時が壊れるため、書式を明示的に固定する。
    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? OccurredAt { get; set; }

    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? EstimatedRecoveryAt { get; set; }

    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>システム選択欄（検索付きコンボボックス）に渡す全システムの一覧。JSON文字列としてクライアントに渡す。</summary>
    public string SystemOptionsJson { get; set; } = "[]";

    /// <summary>
    /// 登録フォームの初期表示。idが指定された場合は既存の障害情報を読み込んで編集モードにする。
    /// 未指定の場合は新規登録として、発生日時の初期値を現在日時にする。
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is not null)
        {
            var incident = await _db.Incidents.FindAsync(id.Value);
            if (incident is null)
            {
                return NotFound();
            }

            Id = incident.Id;
            SystemId = incident.SystemId;
            Description = incident.Description;
            Severity = incident.Severity;
            Status = incident.Status;
            AffectedScope = incident.AffectedScope;
            OccurredAt = incident.OccurredAt;
            EstimatedRecoveryAt = incident.EstimatedRecoveryAt;
            ResolvedAt = incident.ResolvedAt;
            await LoadSystemOptionsAsync();
            return Page();
        }

        OccurredAt = DateTime.Now;
        await LoadSystemOptionsAsync();
        return Page();
    }

    /// <summary>障害情報の登録処理。バリデーション失敗時はフォームを再表示する。</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (SystemId <= 0)
        {
            ModelState.AddModelError(nameof(SystemId), "対象システムを選択してください。");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(nameof(Description), "障害内容を入力してください。");
        }

        if (OccurredAt is null)
        {
            ModelState.AddModelError(nameof(OccurredAt), "発生日時を入力してください。");
        }

        if (OccurredAt is not null && EstimatedRecoveryAt is not null && EstimatedRecoveryAt < OccurredAt)
        {
            ModelState.AddModelError(nameof(EstimatedRecoveryAt), "復旧予定時刻は発生日時より後を指定してください。");
        }

        if (OccurredAt is not null && ResolvedAt is not null && ResolvedAt < OccurredAt)
        {
            ModelState.AddModelError(nameof(ResolvedAt), "復旧日時は発生日時より後を指定してください。");
        }

        if (!ModelState.IsValid)
        {
            await LoadSystemOptionsAsync();
            return Page();
        }

        // ステータスが「復旧済み」なのに復旧日時が未入力の場合は、現在時刻を復旧日時として自動設定する。
        // 逆に「復旧済み」以外のステータスでは、復旧日時が入力されていても未復旧の状態と矛盾するためクリアする。
        if (Status == IncidentStatus.Resolved)
        {
            ResolvedAt ??= DateTime.Now;
        }
        else
        {
            ResolvedAt = null;
        }

        if (Id is not null)
        {
            // 編集モード：ダッシュボードから指定された既存レコードを更新する。
            var target = await _db.Incidents.FindAsync(Id.Value);
            if (target is null)
            {
                return NotFound();
            }

            target.SystemId = SystemId;
            target.Description = Description.Trim();
            target.Severity = Severity;
            target.Status = Status;
            target.AffectedScope = string.IsNullOrWhiteSpace(AffectedScope) ? null : AffectedScope.Trim();
            target.OccurredAt = OccurredAt!.Value;
            target.EstimatedRecoveryAt = EstimatedRecoveryAt;
            target.ResolvedAt = ResolvedAt;
        }
        else
        {
            _db.Incidents.Add(new Incident
            {
                SystemId = SystemId,
                Description = Description.Trim(),
                Severity = Severity,
                Status = Status,
                AffectedScope = string.IsNullOrWhiteSpace(AffectedScope) ? null : AffectedScope.Trim(),
                OccurredAt = OccurredAt!.Value,
                EstimatedRecoveryAt = EstimatedRecoveryAt,
                ResolvedAt = ResolvedAt
            });
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Index");
    }

    /// <summary>対象システムマスターから選択肢一覧を読み込む。管轄部署がある場合はシステム名に併記する。</summary>
    private async Task LoadSystemOptionsAsync()
    {
        var systems = await _db.Systems
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        var options = systems
            .Select(s => new
            {
                id = s.Id,
                text = string.IsNullOrWhiteSpace(s.OwnerSection) ? s.Name : $"{s.Name}（{s.OwnerSection}）"
            })
            .ToList();

        SystemOptionsJson = System.Text.Json.JsonSerializer.Serialize(options);
    }
}
