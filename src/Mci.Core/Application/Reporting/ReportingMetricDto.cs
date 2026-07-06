namespace Mci.Core.Application.Reporting;

public sealed record ReportingMetricDto(
    string Code,
    string DisplayName,
    string Description,
    string Category,
    string Unit,
    byte DecimalPlaces,
    string CalculationType,
    string ComparisonGuidance,
    bool RequiresDollarNormalization,
    bool SupportsAdjacentReleaseComparison);
