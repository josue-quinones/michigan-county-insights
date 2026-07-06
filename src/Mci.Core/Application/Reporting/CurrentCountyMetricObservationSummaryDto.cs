namespace Mci.Core.Application.Reporting;

public sealed record CurrentCountyMetricObservationSummaryDto(
    string MetricCode,
    string MetricDisplayName,
    string Category,
    string Unit,
    byte DecimalPlaces,
    short ReleaseYear,
    string DataReleaseDisplayName,
    int ObservationCount,
    int CountyCount,
    decimal MinimumEstimateValue,
    decimal MaximumEstimateValue,
    DateTime ImportedAtUtc);
