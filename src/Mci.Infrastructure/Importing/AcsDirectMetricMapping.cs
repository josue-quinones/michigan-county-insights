namespace Mci.Infrastructure.Importing;

public sealed record AcsDirectMetricMapping(
    string MetricCode,
    string SourceVariableCode);
