using System.ComponentModel;
using Mci.Core.Application.Reporting;
using ModelContextProtocol.Server;

namespace Mci.McpServer.Tools;

[McpServerToolType]
public sealed class ReportingTools(IReportingQueryService reportingQueryService)
{
    [McpServerTool]
    [Description("Get the Michigan counties available in Michigan County Insights.")]
    public Task<IReadOnlyList<ReportingCountyDto>> GetCounties(
        CancellationToken cancellationToken) =>
        reportingQueryService.GetCountiesAsync(cancellationToken);

    [McpServerTool]
    [Description("Get the metric catalog available for county-level reporting.")]
    public Task<IReadOnlyList<ReportingMetricDto>> GetMetrics(
        CancellationToken cancellationToken) =>
        reportingQueryService.GetMetricsAsync(cancellationToken);

    [McpServerTool]
    [Description("Get current county metric observations, optionally filtered by metric code, county FIPS code, and ACS release year.")]
    public Task<IReadOnlyList<CurrentCountyMetricObservationDto>> GetCurrentCountyMetricObservations(
        [Description("Optional metric code, such as population or median_household_income.")]
        string? metricCode,
        [Description("Optional five-character Michigan county FIPS code, such as 26161.")]
        string? countyFipsCode,
        [Description("Optional ACS release year, such as 2024.")]
        short? releaseYear,
        CancellationToken cancellationToken)
    {
        var query = new CurrentCountyMetricObservationQuery(
            Normalize(metricCode),
            Normalize(countyFipsCode),
            releaseYear);

        return reportingQueryService.GetCurrentCountyMetricObservationsAsync(query, cancellationToken);
    }

    [McpServerTool]
    [Description("Compare counties for one metric within the same current ACS release. This returns existing observations only and does not calculate new metrics.")]
    public async Task<IReadOnlyList<CurrentCountyMetricObservationDto>> CompareCountiesCurrentObservations(
        [Description("Required metric code to compare, such as population or median_household_income.")]
        string metricCode,
        [Description("Optional ACS release year. If omitted, the default current release rows are used.")]
        short? releaseYear,
        [Description("Optional county FIPS codes to include. If omitted, all Michigan counties are returned.")]
        string[]? countyFipsCodes,
        CancellationToken cancellationToken)
    {
        var normalizedMetricCode = Normalize(metricCode)
            ?? throw new ArgumentException("A metric code is required.", nameof(metricCode));

        var query = new CurrentCountyMetricObservationQuery(
            normalizedMetricCode,
            CountyFipsCode: null,
            releaseYear);

        var observations = await reportingQueryService.GetCurrentCountyMetricObservationsAsync(
            query,
            cancellationToken);

        var countyFilter = countyFipsCodes?
            .Select(Normalize)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (countyFilter is { Count: > 0 })
        {
            observations = observations
                .Where(observation => countyFilter.Contains(observation.CountyFipsCode))
                .ToList();
        }

        return observations
            .OrderByDescending(observation => observation.EstimateValue)
            .ThenBy(observation => observation.CountyName)
            .ToList();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
