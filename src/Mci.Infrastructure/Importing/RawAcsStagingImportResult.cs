namespace Mci.Infrastructure.Importing;

public sealed record RawAcsStagingImportResult(
    Guid ImportRunId,
    int DataReleaseId,
    int RecordsFetched,
    int RecordsStaged,
    int IssueCount,
    int ErrorCount,
    int WarningCount);
