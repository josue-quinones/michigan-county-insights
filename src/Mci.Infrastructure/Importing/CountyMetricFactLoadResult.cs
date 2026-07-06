namespace Mci.Infrastructure.Importing;

public sealed record CountyMetricFactLoadResult(
    Guid ImportRunId,
    int DataReleaseId,
    int RecordsInserted,
    int IssueCount,
    int ErrorCount,
    int WarningCount,
    bool AlreadyLoaded);
