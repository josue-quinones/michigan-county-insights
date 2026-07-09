namespace Mci.Core.Application.Reporting;

/// <summary>
/// Reporting service for the county detail and comparison views. Owns release
/// resolution, reading clean observations, and unit-aware difference calculation.
/// </summary>
public interface ICountyInsightService
{
    /// <summary>
    /// Returns a county's metric snapshot for the given (or default) release,
    /// or null when the FIPS code is unknown.
    /// </summary>
    Task<CountyDetailDto?> GetCountyDetailAsync(
        string countyFips,
        short? releaseYear,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two distinct counties for the same release. Throws
    /// <see cref="ArgumentException"/> for a same-county comparison and
    /// <see cref="KeyNotFoundException"/> when either county is unknown.
    /// </summary>
    Task<CountyComparisonDto> CompareCountiesAsync(
        string leftCountyFips,
        string rightCountyFips,
        short? releaseYear,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranks counties by a single metric for the given (or default) release, highest
    /// estimate first, limited to <paramref name="first"/> rows. Throws
    /// <see cref="KeyNotFoundException"/> when the metric code or release is unknown.
    /// </summary>
    Task<IReadOnlyList<CountyRankingRowDto>> RankCountiesAsync(
        string metricCode,
        short? releaseYear,
        int first,
        CancellationToken cancellationToken = default);
}
