using Mci.Core.Application.Reporting;

namespace Mci.Api.Reporting;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reporting")
            .WithTags("Reporting");

        group.MapGet(
                "/metrics",
                async (
                    IReportingQueryService reportingQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var metrics = await reportingQueryService.GetMetricsAsync(cancellationToken);
                    return Results.Ok(metrics);
                })
            .WithName("GetReportingMetrics");

        group.MapGet(
                "/counties",
                async (
                    IReportingQueryService reportingQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var counties = await reportingQueryService.GetCountiesAsync(cancellationToken);
                    return Results.Ok(counties);
                })
            .WithName("GetReportingCounties");

        group.MapGet(
                "/current-observations",
                async (
                    string? metricCode,
                    string? countyFipsCode,
                    short? releaseYear,
                    IReportingQueryService reportingQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var query = new CurrentCountyMetricObservationQuery(
                        Normalize(metricCode),
                        Normalize(countyFipsCode),
                        releaseYear);

                    var observations = await reportingQueryService.GetCurrentCountyMetricObservationsAsync(
                        query,
                        cancellationToken);

                    return Results.Ok(observations);
                })
            .WithName("GetCurrentCountyMetricObservations");

        group.MapGet(
                "/current-observations/summary",
                async (
                    string? metricCode,
                    string? countyFipsCode,
                    short? releaseYear,
                    IReportingQueryService reportingQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var query = new CurrentCountyMetricObservationQuery(
                        Normalize(metricCode),
                        Normalize(countyFipsCode),
                        releaseYear);

                    var summaries = await reportingQueryService.GetCurrentCountyMetricObservationSummaryAsync(
                        query,
                        cancellationToken);

                    return Results.Ok(summaries);
                })
            .WithName("GetCurrentCountyMetricObservationSummary");

        return endpoints;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
