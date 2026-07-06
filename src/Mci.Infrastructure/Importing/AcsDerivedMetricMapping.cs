namespace Mci.Infrastructure.Importing;

public sealed record AcsDerivedMetricMapping(
    string MetricCode,
    IReadOnlyList<string> NumeratorVariableCodes,
    string DenominatorVariableCode);
