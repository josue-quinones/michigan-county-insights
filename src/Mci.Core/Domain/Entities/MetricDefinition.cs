using Mci.Core.Domain.Enums;

namespace Mci.Core.Domain.Entities;

public sealed class MetricDefinition
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public MetricUnit Unit { get; set; }
    public byte DecimalPlaces { get; set; }
    public MetricCalculationType CalculationType { get; set; }
    public string ComparisonGuidance { get; set; } = string.Empty;
    public bool RequiresDollarNormalization { get; set; }
    public bool SupportsAdjacentReleaseComparison { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<CountyMetricObservation> Observations { get; set; } = new List<CountyMetricObservation>();
}
