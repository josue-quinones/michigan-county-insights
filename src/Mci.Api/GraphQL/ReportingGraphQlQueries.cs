using HotChocolate;
using Mci.Core.Application.Reporting;

namespace Mci.Api.GraphQL;

public sealed class ReportingGraphQlQueries
{
    public Task<IReadOnlyList<CurrentCountyMetricObservationDto>> CurrentObservations(
        string? metricCode,
        string? countyFipsCode,
        short? releaseYear,
        [Service] IReportingQueryService reportingQueryService,
        CancellationToken cancellationToken)
    {
        var query = new CurrentCountyMetricObservationQuery(
            Normalize(metricCode),
            Normalize(countyFipsCode),
            releaseYear);

        return reportingQueryService.GetCurrentCountyMetricObservationsAsync(query, cancellationToken);
    }

    public Task<IReadOnlyList<ReportingMetricDto>> Metrics(
        [Service] IReportingQueryService reportingQueryService,
        CancellationToken cancellationToken) =>
        reportingQueryService.GetMetricsAsync(cancellationToken);

    public Task<IReadOnlyList<ReportingCountyDto>> Counties(
        [Service] IReportingQueryService reportingQueryService,
        CancellationToken cancellationToken) =>
        reportingQueryService.GetCountiesAsync(cancellationToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
