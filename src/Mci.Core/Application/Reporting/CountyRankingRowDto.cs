namespace Mci.Core.Application.Reporting;

/// <summary>One county's standing within a single-metric ranking. Rank is 1-based
/// and reflects position within the requested (already-limited) result set.</summary>
public sealed record CountyRankingRowDto(
    int Rank,
    CountyReferenceDto County,
    MetricReferenceDto Metric,
    ReleaseInfoDto Release,
    decimal EstimateValue,
    decimal? MarginOfError);
