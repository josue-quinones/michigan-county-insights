using Mci.Core.Application.Operations;

namespace Mci.Api.Operations;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operations")
            .WithTags("Operations");

        group.MapGet(
                "/import-runs",
                async (
                    string? status,
                    short? releaseYear,
                    int? limit,
                    IOperationsQueryService operationsQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var query = new ImportRunQuery(
                        Normalize(status),
                        releaseYear,
                        limit ?? 25);

                    var importRuns = await operationsQueryService.GetImportRunsAsync(query, cancellationToken);
                    return Results.Ok(importRuns);
                })
            .WithName("GetImportRuns");

        group.MapGet(
                "/import-issues",
                async (
                    Guid? importRunId,
                    string? severity,
                    string? stage,
                    int? limit,
                    IOperationsQueryService operationsQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var query = new ImportIssueQuery(
                        importRunId,
                        Normalize(severity),
                        Normalize(stage),
                        limit ?? 100);

                    var importIssues = await operationsQueryService.GetImportIssuesAsync(query, cancellationToken);
                    return Results.Ok(importIssues);
                })
            .WithName("GetImportIssues");

        return endpoints;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
