namespace ShogaiBoard.Models;

/// <summary>障害への対応状況。</summary>
public enum IncidentStatus
{
    /// <summary>調査中：発生を確認し、原因を調査している段階。</summary>
    Investigating,

    /// <summary>対応中：原因が判明し、復旧作業を行っている段階。</summary>
    InProgress,

    /// <summary>復旧済み：障害が解消した状態。</summary>
    Resolved
}
