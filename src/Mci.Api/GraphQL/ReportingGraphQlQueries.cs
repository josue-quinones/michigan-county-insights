using HotChocolate;
using Mci.Core.Application.Reporting;

namespace Mci.Api.GraphQL;

public sealed class ReportingGraphQlQueries
{
    private const int MaxRankingSize = 50;

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

    public Task<ReportingCountyDto?> County(
        string fips,
        [Service] IReportingQueryService reportingQueryService,
        CancellationToken cancellationToken)
    {
        var normalized = RequireValue(fips, nameof(fips));
        return reportingQueryService.GetCountyAsync(normalized, cancellationToken);
    }

    public Task<CountyDetailDto?> CountyDetail(
        string fips,
        short? releaseYear,
        [Service] ICountyInsightService countyInsightService,
        CancellationToken cancellationToken)
    {
        var normalized = RequireValue(fips, nameof(fips));
        return countyInsightService.GetCountyDetailAsync(normalized, releaseYear, cancellationToken);
    }

    public async Task<CountyComparisonDto> CompareCounties(
        string leftFips,
        string rightFips,
        short? releaseYear,
        [Service] ICountyInsightService countyInsightService,
        CancellationToken cancellationToken)
    {
        var left = RequireValue(leftFips, nameof(leftFips));
        var right = RequireValue(rightFips, nameof(rightFips));

        try
        {
            return await countyInsightService.CompareCountiesAsync(left, right, releaseYear, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw new GraphQLException(ex.Message);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    public async Task<IReadOnlyList<CountyRankingRowDto>> RankCounties(
        string metricCode,
        short? releaseYear,
        [Service] ICountyInsightService countyInsightService,
        CancellationToken cancellationToken,
        int first = 10)
    {
        var code = RequireValue(metricCode, nameof(metricCode));

        if (first is < 1 or > MaxRankingSize)
        {
            throw new GraphQLException($"'{nameof(first)}' must be between 1 and {MaxRankingSize}.");
        }

        try
        {
            return await countyInsightService.RankCountiesAsync(code, releaseYear, first, cancellationToken);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RequireValue(string? value, string paramName)
    {
        var normalized = Normalize(value);
        return normalized ?? throw new GraphQLException($"'{paramName}' is required.");
    }
}
