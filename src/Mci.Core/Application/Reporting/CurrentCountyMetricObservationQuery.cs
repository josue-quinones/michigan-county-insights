namespace Mci.Core.Application.Reporting;

public sealed record CurrentCountyMetricObservationQuery(
    string? MetricCode = null,
    string? CountyFipsCode = null,
    short? ReleaseYear = null);
