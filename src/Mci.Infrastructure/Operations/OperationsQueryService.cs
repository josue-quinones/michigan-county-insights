using Mci.Core.Application.Operations;
using Mci.Core.Domain.Entities;
using Mci.Core.Domain.Enums;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Operations;

public sealed class OperationsQueryService : IOperationsQueryService
{
    private const int MaxImportRunLimit = 100;
    private const int MaxImportIssueLimit = 500;

    private readonly MciDbContext _dbContext;

    public OperationsQueryService(MciDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ImportRunSummaryDto>> GetImportRunsAsync(
        ImportRunQuery query,
        CancellationToken cancellationToken = default)
    {
        var importRuns = _dbContext.ImportRuns
            .AsNoTracking()
            .Include(run => run.DataRelease)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ImportRunStatus>(query.Status.Trim(), ignoreCase: true, out var status))
            {
                return Array.Empty<ImportRunSummaryDto>();
            }

            importRuns = importRuns.Where(run => run.Status == status);
        }

        if (query.ReleaseYear.HasValue)
        {
            importRuns = importRuns.Where(run => run.DataRelease.ReleaseYear == query.ReleaseYear.Value);
        }

        var issueCounts = _dbContext.ImportIssues
            .AsNoTracking()
            .GroupBy(issue => issue.ImportRunId)
            .Select(group => new
            {
                ImportRunId = group.Key,
                IssueCount = group.Count(),
                ErrorCount = group.Count(issue => issue.Severity == ImportIssueSeverity.Error),
                WarningCount = group.Count(issue => issue.Severity == ImportIssueSeverity.Warning),
            });

        var limit = NormalizeLimit(query.Limit, MaxImportRunLimit);

        return await importRuns
            .GroupJoin(
                issueCounts,
                run => run.Id,
                counts => counts.ImportRunId,
                (run, counts) => new
                {
                    ImportRun = run,
                    Counts = counts.FirstOrDefault(),
                })
            .OrderByDescending(row => row.ImportRun.StartedAtUtc)
            .Take(limit)
            .Select(row => new ImportRunSummaryDto(
                row.ImportRun.Id,
                row.ImportRun.DataReleaseId,
                row.ImportRun.DataRelease.SourceCode,
                row.ImportRun.DataRelease.DatasetCode,
                row.ImportRun.DataRelease.ReleaseYear,
                row.ImportRun.DataRelease.DisplayName,
                row.ImportRun.TriggerType.ToString(),
                row.ImportRun.Status.ToString(),
                row.ImportRun.RetryOfImportRunId,
                row.ImportRun.StartedAtUtc,
                row.ImportRun.CompletedAtUtc,
                row.ImportRun.RecordsFetched,
                row.ImportRun.RecordsStaged,
                row.ImportRun.RecordsInserted,
                row.ImportRun.RecordsRejected,
                row.Counts == null ? 0 : row.Counts.IssueCount,
                row.Counts == null ? 0 : row.Counts.ErrorCount,
                row.Counts == null ? 0 : row.Counts.WarningCount,
                row.ImportRun.ErrorSummary,
                row.ImportRun.PipelineVersion))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportIssueDto>> GetImportIssuesAsync(
        ImportIssueQuery query,
        CancellationToken cancellationToken = default)
    {
        var importIssues = _dbContext.ImportIssues
            .AsNoTracking()
            .AsQueryable();

        if (query.ImportRunId.HasValue)
        {
            importIssues = importIssues.Where(issue => issue.ImportRunId == query.ImportRunId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            if (!Enum.TryParse<ImportIssueSeverity>(query.Severity.Trim(), ignoreCase: true, out var severity))
            {
                return Array.Empty<ImportIssueDto>();
            }

            importIssues = importIssues.Where(issue => issue.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(query.Stage))
        {
            if (!Enum.TryParse<ImportIssueStage>(query.Stage.Trim(), ignoreCase: true, out var stage))
            {
                return Array.Empty<ImportIssueDto>();
            }

            importIssues = importIssues.Where(issue => issue.Stage == stage);
        }

        var limit = NormalizeLimit(query.Limit, MaxImportIssueLimit);

        return await importIssues
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ThenByDescending(issue => issue.Id)
            .Take(limit)
            .Select(issue => new ImportIssueDto(
                issue.Id,
                issue.ImportRunId,
                issue.Stage.ToString(),
                issue.Severity.ToString(),
                issue.IssueCode,
                issue.CountyFipsCode,
                issue.MetricCode,
                issue.RawValue,
                issue.Message,
                issue.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    private static int NormalizeLimit(int requestedLimit, int maxLimit)
    {
        if (requestedLimit <= 0)
        {
            return maxLimit;
        }

        return Math.Min(requestedLimit, maxLimit);
    }
}
