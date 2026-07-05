namespace Mci.Core.Domain.Entities;

public sealed class CountyMetricObservation
{
    public long Id { get; set; }
    public int CountyId { get; set; }
    public int MetricDefinitionId { get; set; }
    public int DataReleaseId { get; set; }
    public Guid ImportRunId { get; set; }
    public decimal EstimateValue { get; set; }
    public decimal? MarginOfError { get; set; }
    public string CalculationVersion { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public County County { get; set; } = null!;
    public MetricDefinition MetricDefinition { get; set; } = null!;
    public DataRelease DataRelease { get; set; } = null!;
    public ImportRun ImportRun { get; set; } = null!;
}
