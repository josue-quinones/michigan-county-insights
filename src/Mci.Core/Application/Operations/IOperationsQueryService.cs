namespace Mci.Core.Application.Operations;

public interface IOperationsQueryService
{
    Task<IReadOnlyList<ImportRunSummaryDto>> GetImportRunsAsync(
        ImportRunQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportIssueDto>> GetImportIssuesAsync(
        ImportIssueQuery query,
        CancellationToken cancellationToken = default);
}
