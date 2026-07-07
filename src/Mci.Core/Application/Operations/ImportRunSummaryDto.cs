namespace Mci.Core.Application.Operations;

public sealed record ImportRunSummaryDto(
    Guid ImportRunId,
    int DataReleaseId,
    string SourceCode,
    string DatasetCode,
    short ReleaseYear,
    string DataReleaseDisplayName,
    string TriggerType,
    string Status,
    Guid? RetryOfImportRunId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int RecordsFetched,
    int RecordsStaged,
    int RecordsInserted,
    int RecordsRejected,
    int IssueCount,
    int ErrorCount,
    int WarningCount,
    string? ErrorSummary,
    string PipelineVersion);
