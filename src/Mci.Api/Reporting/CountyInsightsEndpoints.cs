using Mci.Core.Application.Reporting;

namespace Mci.Api.Reporting;

public static class CountyInsightsEndpoints
{
    public static IEndpointRouteBuilder MapCountyInsightsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/counties/{fips}/detail",
                async (
                    string fips,
                    short? release,
                    ICountyInsightService countyInsightService,
                    CancellationToken cancellationToken) =>
                {
                    var detail = await countyInsightService.GetCountyDetailAsync(fips, release, cancellationToken);

                    return detail is null
                        ? Results.NotFound(new { message = $"County '{fips}' was not found." })
                        : Results.Ok(detail);
                })
            .WithName("GetCountyDetail")
            .WithTags("CountyInsights")
            .CacheOutput("Reporting");

        endpoints.MapGet(
                "/api/comparisons",
                async (
                    string? left,
                    string? right,
                    short? release,
                    ICountyInsightService countyInsightService,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                    {
                        return Results.BadRequest(
                            new { message = "Both 'left' and 'right' county FIPS codes are required." });
                    }

                    try
                    {
                        var comparison = await countyInsightService.CompareCountiesAsync(
                            left,
                            right,
                            release,
                            cancellationToken);

                        return Results.Ok(comparison);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { message = ex.Message });
                    }
                    catch (System.Collections.Generic.KeyNotFoundException ex)
                    {
                        return Results.NotFound(new { message = ex.Message });
                    }
                })
            .WithName("GetCountyComparison")
            .WithTags("CountyInsights")
            .CacheOutput("Reporting");

        return endpoints;
    }
}
