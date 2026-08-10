namespace ShogaiBoard.Models;

/// <summary>重要度・対応状況を画面表示用の日本語ラベル／バッジ用CSSクラスに変換する拡張メソッド。</summary>
public static class IncidentDisplayExtensions
{
    /// <summary>重要度を日本語ラベルに変換する。</summary>
    public static string ToLabel(this IncidentSeverity severity) => severity switch
    {
        IncidentSeverity.Critical => "緊急",
        IncidentSeverity.Major => "重要",
        IncidentSeverity.Minor => "軽微",
        _ => severity.ToString()
    };

    /// <summary>重要度に応じたバッジ用CSSクラスを返す。</summary>
    public static string ToBadgeClass(this IncidentSeverity severity) => severity switch
    {
        IncidentSeverity.Critical => "severity-badge severity-critical",
        IncidentSeverity.Major => "severity-badge severity-major",
        IncidentSeverity.Minor => "severity-badge severity-minor",
        _ => "severity-badge"
    };

    /// <summary>対応状況を日本語ラベルに変換する。</summary>
    public static string ToLabel(this IncidentStatus status) => status switch
    {
        IncidentStatus.Investigating => "調査中",
        IncidentStatus.InProgress => "対応中",
        IncidentStatus.Resolved => "復旧済み",
        _ => status.ToString()
    };

    /// <summary>対応状況に応じたバッジ用CSSクラスを返す。</summary>
    public static string ToBadgeClass(this IncidentStatus status) => status switch
    {
        IncidentStatus.Investigating => "status-badge status-investigating",
        IncidentStatus.InProgress => "status-badge status-inprogress",
        IncidentStatus.Resolved => "status-badge status-resolved",
        _ => "status-badge"
    };
}
