namespace Mci.Core.Domain.Entities;

public sealed class AcsCountyVariable
{
    public long Id { get; set; }
    public Guid ImportRunId { get; set; }
    public string CountyFipsCode { get; set; } = string.Empty;
    public string CountyNameRaw { get; set; } = string.Empty;
    public string SourceVariableCode { get; set; } = string.Empty;
    public string EstimateRaw { get; set; } = string.Empty;
    public string? MarginOfErrorRaw { get; set; }
    public string? AnnotationRaw { get; set; }
    public string SourceRowHash { get; set; } = string.Empty;
    public DateTime StagedAtUtc { get; set; }

    public ImportRun ImportRun { get; set; } = null!;
}
