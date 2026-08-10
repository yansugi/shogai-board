namespace ShogaiBoard.Models;

/// <summary>障害の重要度。</summary>
public enum IncidentSeverity
{
    /// <summary>緊急：業務停止級の重大な障害。</summary>
    Critical,

    /// <summary>重要：一部機能に影響がある障害。</summary>
    Major,

    /// <summary>軽微：影響が限定的な障害。</summary>
    Minor
}
