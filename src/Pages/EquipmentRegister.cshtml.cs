using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;
using ShogaiBoard.Services;

namespace ShogaiBoard.Pages;

/// <summary>
/// 計器・什器等の設備不具合の登録・編集画面。
/// 対象計器（対象計器管理で登録したマスター）を検索付きコンボボックスで選んで登録する。
/// ダッシュボードの「編集」リンクからIdを指定して開いた場合は、既存の不具合を編集する。
/// </summary>
public class EquipmentRegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public EquipmentRegisterModel(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>編集対象の不具合ID。ダッシュボードの「編集」リンクから遷移した場合のみ設定される。</summary>
    [BindProperty]
    public int? Id { get; set; }

    [BindProperty]
    public int EquipmentId { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    // InvariantGlobalization有効時にFlatpickrが日時を解釈できなくなるのを防ぐため、書式を固定する。
    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? OccurredAt { get; set; }

    /// <summary>「解消済み」にするかどうか。</summary>
    [BindProperty]
    public bool IsResolved { get; set; }

    [BindProperty]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>計器選択欄（検索付きコンボボックス）に渡す全計器の一覧。JSON文字列としてクライアントに渡す。</summary>
    public string EquipmentOptionsJson { get; set; } = "[]";

    /// <summary>登録フォームの初期表示。idが指定された場合は既存データを読み込んで編集モードにする。</summary>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            OccurredAt = DateTime.Now;
            await LoadEquipmentOptionsAsync();
            return Page();
        }

        var issue = await _db.EquipmentIssues.FindAsync(id.Value);
        if (issue is null)
        {
            return NotFound();
        }

        Id = issue.Id;
        EquipmentId = issue.EquipmentId;
        Description = issue.Description;
        OccurredAt = issue.OccurredAt;
        IsResolved = issue.ResolvedAt is not null;
        ResolvedAt = issue.ResolvedAt;
        await LoadEquipmentOptionsAsync();
        return Page();
    }

    /// <summary>設備不具合の登録・更新処理。バリデーション失敗時はフォームを再表示する。</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (EquipmentId <= 0 || !await _db.Equipments.AnyAsync(e => e.Id == EquipmentId))
        {
            ModelState.AddModelError(nameof(EquipmentId), "対象計器を選択してください。");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(nameof(Description), "不具合内容を入力してください。");
        }

        if (OccurredAt is null)
        {
            ModelState.AddModelError(nameof(OccurredAt), "発生日時を入力してください。");
        }

        if (IsResolved && OccurredAt is not null && ResolvedAt is not null && ResolvedAt < OccurredAt)
        {
            ModelState.AddModelError(nameof(ResolvedAt), "解消日時は発生日時より後を指定してください。");
        }

        if (!ModelState.IsValid)
        {
            await LoadEquipmentOptionsAsync();
            return Page();
        }

        // 解消済みで解消日時が未入力なら現在時刻を設定し、未解消なら解消日時は保存しない（矛盾防止）。
        DateTime? resolvedAt = IsResolved ? (ResolvedAt ?? DateTime.Now) : null;

        try
        {
            if (Id is not null)
            {
                var target = await _db.EquipmentIssues.FindAsync(Id.Value);
                if (target is null)
                {
                    return NotFound();
                }

                target.EquipmentId = EquipmentId;
                target.Description = Description.Trim();
                target.OccurredAt = OccurredAt!.Value;
                target.ResolvedAt = resolvedAt;
            }
            else
            {
                _db.EquipmentIssues.Add(new EquipmentIssue
                {
                    EquipmentId = EquipmentId,
                    Description = Description.Trim(),
                    OccurredAt = OccurredAt!.Value,
                    ResolvedAt = resolvedAt
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
