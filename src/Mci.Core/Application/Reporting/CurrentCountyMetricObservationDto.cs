namespace Mci.Core.Application.Reporting;

public sealed record CurrentCountyMetricObservationDto(
    long ObservationId,
    string CountyFipsCode,
    string CountyName,
    string MetricCode,
    string MetricDisplayName,
    string Category,
    string Unit,
    byte DecimalPlaces,
    decimal EstimateValue,
    decimal? MarginOfError,
    short ReleaseYear,
    short PeriodStartYear,
    short PeriodEndYear,
    string DataReleaseDisplayName,
    string ComparisonGuidance,
    DateTime ImportedAtUtc);
