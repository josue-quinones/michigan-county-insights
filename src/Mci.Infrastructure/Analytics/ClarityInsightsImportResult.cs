namespace Mci.Infrastructure.Analytics;

/// <summary>Outcome of a Clarity insights fetch run.</summary>
public sealed record ClarityInsightsImportResult(
    bool Executed,
    bool AlreadyCaptured,
    string? SkipReason,
    int? SnapshotId,
    DateOnly CaptureDate,
    int? TotalSessionCount,
    int? DistinctUserCount);
