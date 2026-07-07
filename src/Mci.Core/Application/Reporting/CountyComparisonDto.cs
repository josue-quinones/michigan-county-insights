namespace Mci.Core.Application.Reporting;

/// <summary>
/// One metric compared across two counties. All numbers are formatting-neutral and
/// the difference is computed by the API (never the client). DifferenceKind is
/// "Absolute" for count/currency and "PercentagePoints" for percentage metrics.
/// </summary>
public sealed record CountyComparisonMetricDto(
    string Code,
    string DisplayName,
    string Category,
    string Unit,
    byte DecimalPlaces,
    decimal? LeftValue,
    decimal? LeftMarginOfError,
    decimal? RightValue,
    decimal? RightMarginOfError,
    decimal? Difference,
    string DifferenceKind,
    decimal? PercentDifference,
    bool IsAvailable);

/// <summary>Side-by-side comparison of two counties for a single data release.</summary>
public sealed record CountyComparisonDto(
    CountyReferenceDto LeftCounty,
    CountyReferenceDto RightCounty,
    ReleaseInfoDto Release,
    string DifferenceDirection,
    IReadOnlyList<CountyComparisonMetricDto> Metrics);
