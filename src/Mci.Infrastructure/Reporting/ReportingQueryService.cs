using Mci.Core.Application.Reporting;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Reporting;

public sealed class ReportingQueryService : IReportingQueryService
{
    private readonly MciDbContext _dbContext;

    public ReportingQueryService(MciDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CurrentCountyMetricObservationDto>> GetCurrentCountyMetricObservationsAsync(
        CurrentCountyMetricObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        var observations = ApplyCurrentObservationFilters(
            _dbContext.CurrentCountyMetricObservations.AsNoTracking(),
            query);

        var metricGuidance = _dbContext.MetricDefinitions.AsNoTracking()
            .Select(metric => new
            {
                metric.Id,
                metric.ComparisonGuidance,
            });

        return await observations
            .Join(
                metricGuidance,
                observation => observation.MetricDefinitionId,
                metric => metric.Id,
                (observation, metric) => new
                {
                    Observation = observation,
                    metric.ComparisonGuidance,
                })
            .OrderBy(row => row.Observation.MetricCode)
            .ThenBy(row => row.Observation.CountyName)
            .Select(row => new CurrentCountyMetricObservationDto(
                row.Observation.ObservationId,
                row.Observation.CountyFipsCode,
                row.Observation.CountyName,
                row.Observation.MetricCode,
                row.Observation.MetricDisplayName,
                row.Observation.Category,
                row.Observation.Unit,
                row.Observation.DecimalPlaces,
                row.Observation.EstimateValue,
                row.Observation.MarginOfError,
                row.Observation.ReleaseYear,
                row.Observation.PeriodStartYear,
                row.Observation.PeriodEndYear,
                row.Observation.DataReleaseDisplayName,
                row.ComparisonGuidance,
                row.Observation.ImportedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportingMetricDto>> GetMetricsAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.IsActive)
            .OrderBy(metric => metric.Category)
            .ThenBy(metric => metric.DisplayName)
            .Select(metric => new ReportingMetricDto(
                metric.Code,
                metric.DisplayName,
                metric.Description,
                metric.Category,
                metric.Unit.ToString(),
                metric.DecimalPlaces,
                metric.CalculationType.ToString(),
                metric.ComparisonGuidance,
                metric.RequiresDollarNormalization,
                metric.SupportsAdjacentReleaseComparison))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportingCountyDto>> GetCountiesAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.Counties
            .AsNoTracking()
            .Where(county => county.IsActive)
            .OrderBy(county => county.Name)
            .Select(county => new ReportingCountyDto(
                county.FipsCode,
                county.Name,
                county.StateCode,
                county.StateName))
            .ToArrayAsync(cancellationToken);

    public async Task<ReportingCountyDto?> GetCountyAsync(
        string fipsCode,
        CancellationToken cancellationToken = default)
    {
        var fips = fipsCode.Trim();

        return await _dbContext.Counties
            .AsNoTracking()
            .Where(county => county.IsActive && county.FipsCode == fips)
            .Select(county => new ReportingCountyDto(
                county.FipsCode,
                county.Name,
                county.StateCode,
                county.StateName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrentCountyMetricObservationSummaryDto>> GetCurrentCountyMetricObservationSummaryAsync(
        CurrentCountyMetricObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        var observations = ApplyCurrentObservationFilters(
            _dbContext.CurrentCountyMetricObservations.AsNoTracking(),
            query);

        return await observations
            .GroupBy(observation => new
            {
                observation.MetricCode,
                observation.MetricDisplayName,
                observation.Category,
                observation.Unit,
                observation.DecimalPlaces,
                observation.ReleaseYear,
                observation.DataReleaseDisplayName,
            })
            .Select(group => new
            {
                group.Key.MetricCode,
                group.Key.MetricDisplayName,
                group.Key.Category,
                group.Key.Unit,
                group.Key.DecimalPlaces,
                group.Key.ReleaseYear,
                group.Key.DataReleaseDisplayName,
                ObservationCount = group.Count(),
                CountyCount = group.Select(observation => observation.CountyFipsCode).Distinct().Count(),
                MinimumEstimateValue = group.Min(observation => observation.EstimateValue),
                MaximumEstimateValue = group.Max(observation => observation.EstimateValue),
                ImportedAtUtc = group.Max(observation => observation.ImportedAtUtc),
            })
            .OrderBy(summary => summary.MetricCode)
            .Select(summary => new CurrentCountyMetricObservationSummaryDto(
                summary.MetricCode,
                summary.MetricDisplayName,
                summary.Category,
                summary.Unit,
                summary.DecimalPlaces,
                summary.ReleaseYear,
                summary.DataReleaseDisplayName,
                summary.ObservationCount,
                summary.CountyCount,
                summary.MinimumEstimateValue,
                summary.MaximumEstimateValue,
                summary.ImportedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    private static IQueryable<Core.Domain.Entities.CurrentCountyMetricObservation> ApplyCurrentObservationFilters(
        IQueryable<Core.Domain.Entities.CurrentCountyMetricObservation> observations,
        CurrentCountyMetricObservationQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.MetricCode))
        {
            observations = observations.Where(observation => observation.MetricCode == query.MetricCode);
        }

        if (!string.IsNullOrWhiteSpace(query.CountyFipsCode))
        {
            observations = observations.Where(observation => observation.CountyFipsCode == query.CountyFipsCode);
        }

        if (query.ReleaseYear.HasValue)
        {
            observations = observations.Where(observation => observation.ReleaseYear == query.ReleaseYear.Value);
        }

        return observations;
    }
}
