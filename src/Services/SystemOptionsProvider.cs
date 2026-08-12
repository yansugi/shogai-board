using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;

namespace ShogaiBoard.Services;

/// <summary>
/// 対象システム選択欄（検索付きコンボボックス）に渡す選択肢一覧を組み立てる。
/// 障害登録・メンテナンス登録の両画面で共通して使う。
/// </summary>
public static class SystemOptionsProvider
{
    /// <summary>
    /// 全システムを表示順に並べ、JSON文字列として返す（クライアント側のコンボボックスにそのまま渡す）。
    /// 管轄部署がある場合は「システム名（管轄部署）」の形式で表示テキストを組み立てる。
    /// </summary>
    public static async Task<string> GetOptionsJsonAsync(AppDbContext db)
    {
        var systems = await db.Systems
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        var options = systems
            .Select(s => new
            {
                id = s.Id,
                text = string.IsNullOrWhiteSpace(s.OwnerSection) ? s.Name : $"{s.Name}（{s.OwnerSection}）"
            })
            .ToList();

        return System.Text.Json.JsonSerializer.Serialize(options);
    }
}
