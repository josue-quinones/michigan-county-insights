namespace Mci.Core.Application.Reporting;

/// <summary>A single metric row on the county detail view. Formatting-neutral.</summary>
public sealed record CountyDetailMetricDto(
    string Code,
    string DisplayName,
    string Category,
    string Unit,
    byte DecimalPlaces,
    decimal? EstimateValue,
    decimal? MarginOfError,
    bool IsAvailable);

/// <summary>Full snapshot of one county for a single data release.</summary>
public sealed record CountyDetailDto(
    CountyReferenceDto County,
    ReleaseInfoDto Release,
    IReadOnlyList<CountyDetailMetricDto> Metrics,
    DateTime? LastSuccessfulImportAtUtc);
