using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShogaiBoard.Data;
using ShogaiBoard.Models;
using ShogaiBoard.Services;

namespace ShogaiBoard.Pages;

/// <summary>
/// メンテナンス予定の登録・編集画面。対象システムを選んでメンテナンス内容・予定開始/終了日時等を登録する。
/// 障害と異なり重要度・対応状況の概念はなく、状況（予定/実施中）は日時から自動的に判定される。
/// ダッシュボードの「編集」リンクからIdを指定して開いた場合は、既存のメンテナンス予定を編集する。
/// </summary>
public class MaintenanceRegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public MaintenanceRegisterModel(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>編集対象のメンテナンスID。ダッシュボードの「編集」リンクから遷移した場合のみ設定される。</summary>
    [BindProperty]
    public int? Id { get; set; }

    [BindProperty]
    public int SystemId { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? AffectedScope { get; set; }

    // InvariantGlobalization有効時、カルチャ依存の既定書式で出力されるとFlatpickr（Y-m-d H:i形式を期待）が
    // 解釈できず日時が壊れるため、書式を明示的に固定する。
    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ScheduledStartAt { get; set; }

    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ScheduledEndAt { get; set; }

    /// <summary>システム選択欄（検索付きコンボボックス）に渡す全システムの一覧。JSON文字列としてクライアントに渡す。</summary>
    public string SystemOptionsJson { get; set; } = "[]";

    /// <summary>
    /// 登録フォームの初期表示。idが指定された場合は既存のメンテナンス予定を読み込んで編集モードにする。
    /// 未指定の場合は新規登録として、予定開始日時の初期値を現在日時にする。
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is not null)
        {
            var maintenance = await _db.Maintenances.FindAsync(id.Value);
            if (maintenance is null)
            {
                return NotFound();
            }

            Id = maintenance.Id;
            SystemId = maintenance.SystemId;
            Description = maintenance.Description;
            AffectedScope = maintenance.AffectedScope;
            ScheduledStartAt = maintenance.ScheduledStartAt;
            ScheduledEndAt = maintenance.ScheduledEndAt;
            await LoadSystemOptionsAsync();
            return Page();
        }

        ScheduledStartAt = DateTime.Now;
        await LoadSystemOptionsAsync();
        return Page();
    }

    /// <summary>メンテナンス予定の登録処理。バリデーション失敗時はフォームを再表示する。</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (SystemId <= 0)
        {
            ModelState.AddModelError(nameof(SystemId), "対象システムを選択してください。");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(nameof(Description), "メンテナンス内容を入力してください。");
        }

        if (ScheduledStartAt is null)
        {
            ModelState.AddModelError(nameof(ScheduledStartAt), "予定開始日時を入力してください。");
        }

        if (ScheduledEndAt is null)
        {
            ModelState.AddModelError(nameof(ScheduledEndAt), "予定終了日時を入力してください。");
        }

        if (ScheduledStartAt is not null && ScheduledEndAt is not null && ScheduledEndAt <= ScheduledStartAt)
        {
            ModelState.AddModelError(nameof(ScheduledEndAt), "予定終了日時は予定開始日時より後を指定してください。");
        }

        if (!ModelState.IsValid)
        {
            await LoadSystemOptionsAsync();
            return Page();
        }

        if (Id is not null)
        {
            // 編集モード：ダッシュボードから指定された既存レコードを更新する。
            var target = await _db.Maintenances.FindAsync(Id.Value);
            if (target is null)
            {
                return NotFound();
            }

            target.SystemId = SystemId;
            target.Description = Description.Trim();
            target.AffectedScope = string.IsNullOrWhiteSpace(AffectedScope) ? null : AffectedScope.Trim();
            target.ScheduledStartAt = ScheduledStartAt!.Value;
            target.ScheduledEndAt = ScheduledEndAt!.Value;
        }
        else
        {
            _db.Maintenances.Add(new Maintenance
            {
                SystemId = SystemId,
                Description = Description.Trim(),
                AffectedScope = string.IsNullOrWhiteSpace(AffectedScope) ? null : AffectedScope.Trim(),
                ScheduledStartAt = ScheduledStartAt!.Value,
                ScheduledEndAt = ScheduledEndAt!.Value
            });
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Index");
    }

    /// <summary>対象システムマスターから選択肢一覧を読み込む。</summary>
    private async Task LoadSystemOptionsAsync()
    {
        SystemOptionsJson = await SystemOptionsProvider.GetOptionsJsonAsync(_db);
    }
}
