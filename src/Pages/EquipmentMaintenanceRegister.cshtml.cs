using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;
using ShogaiBoard.Services;

namespace ShogaiBoard.Pages;

/// <summary>
/// 計器・什器等のメンテナンス予定の登録・編集画面。対象計器を選んで内容・予定開始/終了日時を登録する。
/// ダッシュボードの「編集」リンクからIdを指定して開いた場合は、既存の予定を編集する。
/// </summary>
public class EquipmentMaintenanceRegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public EquipmentMaintenanceRegisterModel(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>編集対象のID。ダッシュボードの「編集」リンクから遷移した場合のみ設定される。</summary>
    [BindProperty]
    public int? Id { get; set; }

    [BindProperty]
    public int EquipmentId { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    // InvariantGlobalization有効時にFlatpickrが日時を解釈できなくなるのを防ぐため、書式を固定する。
    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ScheduledStartAt { get; set; }

    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ScheduledEndAt { get; set; }

    /// <summary>計器選択欄（検索付きコンボボックス）に渡す全計器の一覧。JSON文字列としてクライアントに渡す。</summary>
    public string EquipmentOptionsJson { get; set; } = "[]";

    /// <summary>登録フォームの初期表示。idが指定された場合は既存の予定を読み込んで編集モードにする。</summary>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is not null)
        {
            var maintenance = await _db.EquipmentMaintenances.FindAsync(id.Value);
            if (maintenance is null)
            {
                return NotFound();
            }

            Id = maintenance.Id;
            EquipmentId = maintenance.EquipmentId;
            Description = maintenance.Description;
            ScheduledStartAt = maintenance.ScheduledStartAt;
            ScheduledEndAt = maintenance.ScheduledEndAt;
        }
        else
        {
            ScheduledStartAt = DateTime.Now;
        }

        await LoadEquipmentOptionsAsync();
        return Page();
    }

    /// <summary>計器メンテナンス予定の登録・更新処理。バリデーション失敗時はフォームを再表示する。</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (EquipmentId <= 0 || !await _db.Equipments.AnyAsync(e => e.Id == EquipmentId))
        {
            ModelState.AddModelError(nameof(EquipmentId), "対象計器を選択してください。");
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
            await LoadEquipmentOptionsAsync();
            return Page();
        }

        try
        {
            if (Id is not null)
            {
                var target = await _db.EquipmentMaintenances.FindAsync(Id.Value);
                if (target is null)
                {
                    return NotFound();
                }

                target.EquipmentId = EquipmentId;
                target.Description = Description.Trim();
                target.ScheduledStartAt = ScheduledStartAt!.Value;
                target.ScheduledEndAt = ScheduledEndAt!.Value;
            }
            else
            {
                _db.EquipmentMaintenances.Add(new EquipmentMaintenance
                {
                    EquipmentId = EquipmentId,
                    Description = Description.Trim(),
                    ScheduledStartAt = ScheduledStartAt!.Value,
                    ScheduledEndAt = ScheduledEndAt!.Value
                });
            }

            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "保存に失敗しました。時間をおいて再度お試しください。");
            await LoadEquipmentOptionsAsync();
            return Page();
        }

        return RedirectToPage("/Index");
    }

    /// <summary>対象計器マスターから選択肢一覧を読み込む。</summary>
    private async Task LoadEquipmentOptionsAsync()
    {
        EquipmentOptionsJson = await EquipmentOptionsProvider.GetOptionsJsonAsync(_db);
    }
}
