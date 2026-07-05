namespace Mci.Core.Domain.Entities;

public sealed class DataRelease
{
    public int Id { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string DatasetCode { get; set; } = string.Empty;
    public short ReleaseYear { get; set; }
    public short PeriodStartYear { get; set; }
    public short PeriodEndYear { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<ImportRun> ImportRuns { get; set; } = new List<ImportRun>();
    public ICollection<CountyMetricObservation> Observations { get; set; } = new List<CountyMetricObservation>();
}
