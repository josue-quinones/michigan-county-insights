namespace Mci.Infrastructure.Importing;

public static class AcsDerivedMetricMappings
{
    public static IReadOnlyList<AcsDerivedMetricMapping> All { get; } =
    [
        new("poverty_rate", ["B17001_002E"], "B17001_001E"),
        new("labor_force_participation_rate", ["B23025_002E"], "B23025_001E"),
        new(
            "bachelors_degree_or_higher_rate",
            ["B15003_022E", "B15003_023E", "B15003_024E", "B15003_025E"],
            "B15003_001E"),
    ];

    public static IReadOnlyList<string> SourceVariableCodes { get; } = All
        .SelectMany(mapping => mapping.NumeratorVariableCodes.Append(mapping.DenominatorVariableCode))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}
