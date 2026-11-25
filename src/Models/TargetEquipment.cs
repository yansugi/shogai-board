namespace ShogaiBoard.Models;

/// <summary>
/// 不具合情報の対象となる計器・什器等のマスター。
/// 例：窓口の番号発券機、会議室Aのプロジェクター等。
/// </summary>
public class TargetEquipment
{
    public int Id { get; set; }

    /// <summary>計器・什器等の名称（必須）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>設置場所（任意）。</summary>
    public string? Location { get; set; }

    /// <summary>一覧での表示順（小さい順に表示）。</summary>
    public int SortOrder { get; set; }
}
