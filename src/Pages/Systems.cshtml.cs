using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

namespace ShogaiBoard.Pages;

/// <summary>
/// 対象システムマスターの管理画面。追加・編集・削除を画面から自由に行える。
/// </summary>
public class SystemsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public SystemsModel(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public List<TargetSystem> Systems { get; set; } = new();

    [BindProperty]
    public string NewName { get; set; } = string.Empty;

    [BindProperty]
    public string? NewOwnerSection { get; set; }

    /// <summary>CSVインポート用にアップロードされたファイル。</summary>
    [BindProperty]
    public IFormFile? CsvFile { get; set; }

    /// <summary>CSVインポート実行時に入力するマスターパスワード。</summary>
    [BindProperty]
    public string? ImportMasterPassword { get; set; }

    /// <summary>CSVインポートの結果メッセージ（成功件数など）。リダイレクト後の表示用に一度だけ保持する。</summary>
    [TempData]
    public string? ImportResultMessage { get; set; }

    /// <summary>CSVインポートでスキップされた行の詳細。リダイレクト後の表示用に一度だけ保持する。</summary>
    [TempData]
    public string? ImportErrorDetail { get; set; }

    /// <summary>対象システム一覧を表示順に読み込む。</summary>
    public async Task OnGetAsync()
    {
        await LoadSystemsAsync();
    }

    /// <summary>対象システムを新規追加する。並び順は現在の最大値の後ろに置く。</summary>
    public async Task<IActionResult> OnPostAddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            ModelState.AddModelError(nameof(NewName), "システム名を入力してください。");
            await LoadSystemsAsync();
            return Page();
        }

        var maxOrder = await _db.Systems.Select(s => (int?)s.SortOrder).MaxAsync() ?? 0;
        _db.Systems.Add(new TargetSystem
        {
            Name = NewName.Trim(),
            OwnerSection = string.IsNullOrWhiteSpace(NewOwnerSection) ? null : NewOwnerSection.Trim(),
            SortOrder = maxOrder + 1
        });
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    /// <summary>
    /// システム名・管轄部署を更新する。
    /// CSVインポートで追加されたシステム（IsImported）は、誤ってシステム名を書き換えられないよう
    /// システム名の変更を受け付けない（画面側も読み取り専用にしているが、サーバー側でも念のため無視する）。管轄部署は変更可能。
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync(int id, string name, string? ownerSection)
    {
        var system = await _db.Systems.FindAsync(id);
        if (system is null)
        {
            return NotFound();
        }

        if (!system.IsImported)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(string.Empty, "システム名を入力してください。");
                await LoadSystemsAsync();
                return Page();
            }

            system.Name = name.Trim();
        }

        system.OwnerSection = string.IsNullOrWhiteSpace(ownerSection) ? null : ownerSection.Trim();
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    /// <summary>
    /// 対象システムを削除する。紐づく障害情報も併せて削除する。
    /// CSVインポートで追加されたシステム（IsImported）のみ、マスターパスワードを知っている者だけが
    /// 削除できるように事前照合する。各自が手動で追加したシステムはパスワードなしで削除できる。
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id, string? masterPassword)
    {
        var system = await _db.Systems.FindAsync(id);
        if (system is null)
        {
            return NotFound();
        }

        if (system.IsImported && !IsMasterPasswordValid(masterPassword))
        {
            // このハンドラーでは使わないNewName等の未入力による無関係な検証エラーを除き、
            // パスワード誤りのエラーだけを表示する。
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "マスターパスワードが正しくないため、削除できませんでした。");
            await LoadSystemsAsync();
            return Page();
        }

        var relatedIncidents = await _db.Incidents.Where(i => i.SystemId == id).ToListAsync();
        if (relatedIncidents.Count > 0)
        {
            _db.Incidents.RemoveRange(relatedIncidents);
        }

        _db.Systems.Remove(system);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    /// <summary>
    /// CSVファイルから対象システムを一括登録・更新する。
    /// 1行目はヘッダーとして読み飛ばし、1列目：システム名（必須）、2列目：管轄部署（任意）として扱う。
    /// 既存のシステム名と一致する行は管轄部署を更新し、一致しない行は末尾に新規追加する（新規追加分はIsImported=trueとなる）。
    /// 一括登録の悪用・誤操作を防ぐため、マスターパスワードを知っている者のみが実行できる。
    /// </summary>
    public async Task<IActionResult> OnPostImportCsvAsync()
    {
        if (!IsMasterPasswordValid(ImportMasterPassword))
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "マスターパスワードが正しくないため、CSVを取り込めませんでした。");
            await LoadSystemsAsync();
            return Page();
        }

        if (CsvFile is null || CsvFile.Length == 0)
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "CSVファイルを選択してください。");
            await LoadSystemsAsync();
            return Page();
        }

        byte[] rawBytes;
        using (var memoryStream = new MemoryStream())
        {
            await CsvFile.CopyToAsync(memoryStream);
            rawBytes = memoryStream.ToArray();
        }

        var lines = DecodeCsvBytes(rawBytes)
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count <= 1)
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "CSVにデータ行がありません（1行目はヘッダーとして扱われます）。");
            await LoadSystemsAsync();
            return Page();
        }

        var existingSystems = await _db.Systems.ToListAsync();
        var nextSortOrder = (existingSystems.Count > 0 ? existingSystems.Max(s => s.SortOrder) : 0) + 1;

        var addedCount = 0;
        var updatedCount = 0;
        var errorLines = new List<string>();

        for (var i = 1; i < lines.Count; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            var name = fields.Count > 0 ? fields[0].Trim() : string.Empty;
            var ownerSection = fields.Count > 1 ? fields[1].Trim() : string.Empty;
            var lineNumber = i + 1;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorLines.Add($"{lineNumber}行目：システム名が空です。");
                continue;
            }
            if (name.Length > 50)
            {
                errorLines.Add($"{lineNumber}行目：システム名は50文字以内で入力してください。");
                continue;
            }
            if (ownerSection.Length > 50)
            {
                errorLines.Add($"{lineNumber}行目：管轄部署は50文字以内で入力してください。");
                continue;
            }

            var ownerSectionValue = string.IsNullOrWhiteSpace(ownerSection) ? null : ownerSection;
            var existing = existingSystems.FirstOrDefault(s => s.Name == name);
            if (existing is not null)
            {
                existing.OwnerSection = ownerSectionValue;
                updatedCount++;
            }
            else
            {
                var newSystem = new TargetSystem
                {
                    Name = name,
                    OwnerSection = ownerSectionValue,
                    SortOrder = nextSortOrder++,
                    IsImported = true
                };
                _db.Systems.Add(newSystem);
                existingSystems.Add(newSystem);
                addedCount++;
            }
        }

        await _db.SaveChangesAsync();

        ImportResultMessage = $"CSVの取り込みが完了しました（追加：{addedCount}件、更新：{updatedCount}件、スキップ：{errorLines.Count}件）。";
        if (errorLines.Count > 0)
        {
            ImportErrorDetail = string.Join("\n", errorLines);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// CSVのバイト列を文字列に変換する。UTF-8（BOM有無問わず）を優先し、
    /// デコードに失敗した場合はExcel由来のCSVで多いShift_JISとして扱う。
    /// </summary>
    private static string DecodeCsvBytes(byte[] bytes)
    {
        var hasUtf8Bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        if (hasUtf8Bom)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        try
        {
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            var shiftJis = Encoding.GetEncoding("shift_jis");
            return shiftJis.GetString(bytes);
        }
    }

    /// <summary>CSVの1行をカンマ区切りのフィールドに分割する（ダブルクォート囲み・エスケープに対応）。</summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    /// <summary>入力されたマスターパスワードが、設定値（appsettings.jsonのSystemManagement:MasterPassword）と一致するか照合する。</summary>
    private bool IsMasterPasswordValid(string? inputPassword)
    {
        var configuredPassword = _configuration["SystemManagement:MasterPassword"];
        return !string.IsNullOrEmpty(configuredPassword) && inputPassword == configuredPassword;
    }

    private async Task LoadSystemsAsync()
    {
        Systems = await _db.Systems.OrderBy(s => s.SortOrder).ToListAsync();
    }
}
