using Mci.Core.Domain.Enums;

namespace Mci.Core.Domain.Entities;

public sealed class ImportRun
{
    public Guid Id { get; set; }
    public int DataReleaseId { get; set; }
    public ImportTriggerType TriggerType { get; set; }
    public ImportRunStatus Status { get; set; }
    public Guid? RetryOfImportRunId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int RecordsFetched { get; set; }
    public int RecordsStaged { get; set; }
    public int RecordsInserted { get; set; }
    public int RecordsRejected { get; set; }
    public string? ErrorSummary { get; set; }
    public string PipelineVersion { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public DataRelease DataRelease { get; set; } = null!;
    public ImportRun? RetryOfImportRun { get; set; }
    public ICollection<ImportIssue> ImportIssues { get; set; } = new List<ImportIssue>();
    public ICollection<AcsCountyVariable> AcsCountyVariables { get; set; } = new List<AcsCountyVariable>();
    public ICollection<CountyMetricObservation> Observations { get; set; } = new List<CountyMetricObservation>();
}
