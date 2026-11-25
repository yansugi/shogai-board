using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;

namespace ShogaiBoard.Services;

/// <summary>
/// 設備不具合登録画面の対象計器選択欄（検索付きコンボボックス）に渡す選択肢一覧を組み立てる。
/// </summary>
public static class EquipmentOptionsProvider
{
    /// <summary>
    /// 全計器を表示順に並べ、JSON文字列として返す。
    /// 設置場所がある場合は「計器名（設置場所）」の形式で表示テキストを組み立てる。
    /// </summary>
    public static async Task<string> GetOptionsJsonAsync(AppDbContext db)
    {
        var list = await db.Equipments
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        var options = list
            .Select(e => new
            {
                id = e.Id,
                text = string.IsNullOrWhiteSpace(e.Location) ? e.Name : $"{e.Name}（{e.Location}）"
            })
            .ToList();

        return System.Text.Json.JsonSerializer.Serialize(options);
    }
}
