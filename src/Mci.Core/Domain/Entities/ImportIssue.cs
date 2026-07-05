using Mci.Core.Domain.Enums;

namespace Mci.Core.Domain.Entities;

public sealed class ImportIssue
{
    public long Id { get; set; }
    public Guid ImportRunId { get; set; }
    public ImportIssueStage Stage { get; set; }
    public ImportIssueSeverity Severity { get; set; }
    public string IssueCode { get; set; } = string.Empty;
    public string? CountyFipsCode { get; set; }
    public string? MetricCode { get; set; }
    public string? RawValue { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ImportRun ImportRun { get; set; } = null!;
}
