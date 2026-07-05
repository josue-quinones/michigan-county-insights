namespace Mci.Core.Domain.Entities;

/// <summary>
/// Keyless projection backed by mci_reporting.vw_CurrentCountyMetricObservation.
/// </summary>
public sealed class CurrentCountyMetricObservation
{
    public long ObservationId { get; set; }
    public int CountyId { get; set; }
    public string CountyFipsCode { get; set; } = string.Empty;
    public string CountyName { get; set; } = string.Empty;
    public int MetricDefinitionId { get; set; }
    public string MetricCode { get; set; } = string.Empty;
    public string MetricDisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public byte DecimalPlaces { get; set; }
    public int DataReleaseId { get; set; }
    public short ReleaseYear { get; set; }
    public short PeriodStartYear { get; set; }
    public short PeriodEndYear { get; set; }
    public string DataReleaseDisplayName { get; set; } = string.Empty;
    public decimal EstimateValue { get; set; }
    public decimal? MarginOfError { get; set; }
    public Guid ImportRunId { get; set; }
    public DateTime ImportedAtUtc { get; set; }
}
