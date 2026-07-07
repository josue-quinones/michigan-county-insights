using Mci.Core.Application.Reporting;
using Mci.Core.Domain.Entities;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Reporting;

public sealed class CountyInsightService : ICountyInsightService
{
    private readonly MciDbContext _dbContext;

    public CountyInsightService(MciDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CountyDetailDto?> GetCountyDetailAsync(
        string countyFips,
        short? releaseYear,
        CancellationToken cancellationToken = default)
    {
        var fips = Normalize(countyFips);

        var county = await FindCountyAsync(fips, cancellationToken);
        if (county is null)
        {
            return null;
        }

        var release = await ResolveReleaseAsync(releaseYear, cancellationToken);
        if (release is null)
        {
            return null;
        }

        var metrics = await GetActiveMetricsAsync(cancellationToken);
        var observations = await GetObservationsAsync(new[] { fips }, release.ReleaseYear, cancellationToken);
        var byMetric = observations.ToDictionary(o => o.MetricCode, StringComparer.OrdinalIgnoreCase);

        var metricRows = metrics
            .Select(metric =>
            {
                byMetric.TryGetValue(metric.Code, out var observation);
                return new CountyDetailMetricDto(
                    metric.Code,
                    metric.DisplayName,
                    metric.Category,
                    metric.Unit.ToString(),
                    metric.DecimalPlaces,
                    observation?.EstimateValue,
                    observation?.MarginOfError,
                    IsAvailable: observation is not null);
            })
            .ToArray();

        DateTime? lastImport = observations.Count > 0
            ? observations.Max(o => o.ImportedAtUtc)
            : null;

        return new CountyDetailDto(
            ToReference(county),
            ToReleaseInfo(release),
            metricRows,
            lastImport);
    }

    public async Task<CountyComparisonDto> CompareCountiesAsync(
        string leftCountyFips,
        string rightCountyFips,
        short? releaseYear,
        CancellationToken cancellationToken = default)
    {
        var leftFips = Normalize(leftCountyFips);
        var rightFips = Normalize(rightCountyFips);

        if (string.Equals(leftFips, rightFips, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A county cannot be compared with itself.");
        }

        var leftCounty = await FindCountyAsync(leftFips, cancellationToken)
            ?? throw new KeyNotFoundException($"County '{leftFips}' was not found.");
        var rightCounty = await FindCountyAsync(rightFips, cancellationToken)
            ?? throw new KeyNotFoundException($"County '{rightFips}' was not found.");

        var release = await ResolveReleaseAsync(releaseYear, cancellationToken)
            ?? throw new KeyNotFoundException("No data release is available.");

        var metrics = await GetActiveMetricsAsync(cancellationToken);
        var observations = await GetObservationsAsync(
            new[] { leftFips, rightFips },
            release.ReleaseYear,
            cancellationToken);

        var left = observations
            .Where(o => string.Equals(o.CountyFipsCode, leftFips, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(o => o.MetricCode, StringComparer.OrdinalIgnoreCase);
        var right = observations
            .Where(o => string.Equals(o.CountyFipsCode, rightFips, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(o => o.MetricCode, StringComparer.OrdinalIgnoreCase);

        var metricRows = metrics
            .Select(metric =>
            {
                left.TryGetValue(metric.Code, out var leftObs);
                right.TryGetValue(metric.Code, out var rightObs);

                var unit = metric.Unit.ToString();
                var result = MetricDifferenceCalculator.Compute(
                    unit,
                    leftObs?.EstimateValue,
                    rightObs?.EstimateValue);

                return new CountyComparisonMetricDto(
                    metric.Code,
                    metric.DisplayName,
                    metric.Category,
                    unit,
                    metric.DecimalPlaces,
                    leftObs?.EstimateValue,
                    leftObs?.MarginOfError,
                    rightObs?.EstimateValue,
                    rightObs?.MarginOfError,
                    result.Difference,
                    result.DifferenceKind,
                    result.PercentDifference,
                    result.IsAvailable);
            })
            .ToArray();

        return new CountyComparisonDto(
            ToReference(leftCounty),
            ToReference(rightCounty),
            ToReleaseInfo(release),
            "left_minus_right",
            metricRows);
    }

    private Task<County?> FindCountyAsync(string fips, CancellationToken cancellationToken) =>
        _dbContext.Counties
            .AsNoTracking()
            .Where(county => county.IsActive && county.FipsCode == fips)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<DataRelease?> ResolveReleaseAsync(short? releaseYear, CancellationToken cancellationToken)
    {
        if (releaseYear.HasValue)
        {
            var requested = await _dbContext.DataReleases
                .AsNoTracking()
                .FirstOrDefaultAsync(release => release.ReleaseYear == releaseYear.Value, cancellationToken);

            if (requested is not null)
            {
                return requested;
            }
        }

        return await _dbContext.DataReleases
            .AsNoTracking()
            .OrderByDescending(release => release.IsDefault)
            .ThenByDescending(release => release.ReleaseYear)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<List<MetricDefinition>> GetActiveMetricsAsync(CancellationToken cancellationToken) =>
        _dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.IsActive)
            .OrderBy(metric => metric.Category)
            .ThenBy(metric => metric.DisplayName)
            .ToListAsync(cancellationToken);

    private Task<List<CurrentCountyMetricObservation>> GetObservationsAsync(
        string[] fipsCodes,
        short releaseYear,
        CancellationToken cancellationToken) =>
        _dbContext.CurrentCountyMetricObservations
            .AsNoTracking()
            .Where(observation =>
                observation.ReleaseYear == releaseYear &&
                fipsCodes.Contains(observation.CountyFipsCode))
            .ToListAsync(cancellationToken);

    private static CountyReferenceDto ToReference(County county) =>
        new(county.FipsCode, county.Name, county.StateCode);

    private static ReleaseInfoDto ToReleaseInfo(DataRelease release) =>
        new(release.ReleaseYear, release.PeriodStartYear, release.PeriodEndYear, release.DisplayName);

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
