using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Pages;

/// <summary>
/// 対象計器（計器・什器等）マスターの管理画面。追加・編集・削除を画面から自由に行える。
/// 対象システム管理と異なり、CSV一括登録やマスターパスワードは設けない。
/// </summary>
public class EquipmentsModel : PageModel
{
    private const int MaxNameLength = 100;
    private const int MaxLocationLength = 100;

    private readonly AppDbContext _db;

    public EquipmentsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<TargetEquipment> Equipments { get; set; } = new();

    [BindProperty]
    public string NewName { get; set; } = string.Empty;

    [BindProperty]
    public string? NewLocation { get; set; }

    /// <summary>対象計器一覧を表示順に読み込む。</summary>
    public async Task OnGetAsync()
    {
        await LoadEquipmentsAsync();
    }

    /// <summary>対象計器を新規追加する。並び順は現在の最大値の後ろに置く。</summary>
    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ValidateNameAndLocation(NewName, NewLocation, nameof(NewName)))
        {
            await LoadEquipmentsAsync();
            return Page();
        }

        try
        {
            var maxOrder = await _db.Equipments.Select(e => (int?)e.SortOrder).MaxAsync() ?? 0;
            _db.Equipments.Add(new TargetEquipment
            {
                Name = NewName.Trim(),
                Location = string.IsNullOrWhiteSpace(NewLocation) ? null : NewLocation.Trim(),
                SortOrder = maxOrder + 1
            });
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "保存に失敗しました。時間をおいて再度お試しください。");
            await LoadEquipmentsAsync();
            return Page();
        }

        return RedirectToPage();
    }

    /// <summary>計器名・設置場所を更新する。</summary>
    public async Task<IActionResult> OnPostUpdateAsync(int id, string name, string? location)
    {
        var equipment = await _db.Equipments.FindAsync(id);
        if (equipment is null)
        {
            return NotFound();
        }

        // 追加フォーム用のバインド値の未入力による無関係な検証エラーは除き、この更新に関するエラーだけを表示する。
        ModelState.Clear();
        if (!ValidateNameAndLocation(name, location, string.Empty))
        {
            await LoadEquipmentsAsync();
            return Page();
        }

        try
        {
            equipment.Name = name.Trim();
            equipment.Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "保存に失敗しました。時間をおいて再度お試しください。");
            await LoadEquipmentsAsync();
            return Page();
        }

        return RedirectToPage();
    }

    /// <summary>対象計器を削除する。紐づく不具合情報（履歴含む）も併せて削除される（DBのカスケード削除）。</summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var equipment = await _db.Equipments.FindAsync(id);
        if (equipment is null)
        {
            return NotFound();
        }

        try
        {
            _db.Equipments.Remove(equipment);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "削除に失敗しました。時間をおいて再度お試しください。");
            await LoadEquipmentsAsync();
            return Page();
        }

        return RedirectToPage();
    }

    /// <summary>計器名（必須・文字数上限）と設置場所（文字数上限）を検証し、問題があればModelStateにエラーを追加する。</summary>
    private bool ValidateNameAndLocation(string? name, string? location, string nameKey)
    {
        var valid = true;

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameKey, "計器名を入力してください。");
            valid = false;
        }
        else if (name.Trim().Length > MaxNameLength)
        {
            ModelState.AddModelError(nameKey, $"計器名は{MaxNameLength}文字以内で入力してください。");
            valid = false;
        }

        if (!string.IsNullOrWhiteSpace(location) && location.Trim().Length > MaxLocationLength)
        {
            ModelState.AddModelError(string.Empty, $"設置場所は{MaxLocationLength}文字以内で入力してください。");
            valid = false;
        }

        return valid;
    }

    private async Task LoadEquipmentsAsync()
    {
        Equipments = await _db.Equipments.OrderBy(e => e.SortOrder).ToListAsync();
    }
}
