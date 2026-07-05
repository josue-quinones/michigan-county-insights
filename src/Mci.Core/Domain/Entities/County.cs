namespace Mci.Core.Domain.Entities;

public sealed class County
{
    public int Id { get; set; }
    public string FipsCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StateFipsCode { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<CountyMetricObservation> Observations { get; set; } = new List<CountyMetricObservation>();
}
