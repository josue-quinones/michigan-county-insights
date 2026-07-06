namespace Mci.Core.Application.Reporting;

public interface IReportingQueryService
{
    Task<IReadOnlyList<CurrentCountyMetricObservationDto>> GetCurrentCountyMetricObservationsAsync(
        CurrentCountyMetricObservationQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportingMetricDto>> GetMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportingCountyDto>> GetCountiesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrentCountyMetricObservationSummaryDto>> GetCurrentCountyMetricObservationSummaryAsync(
        CurrentCountyMetricObservationQuery query,
        CancellationToken cancellationToken = default);
}
