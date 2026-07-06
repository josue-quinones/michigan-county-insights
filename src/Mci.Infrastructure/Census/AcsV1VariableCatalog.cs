namespace Mci.Infrastructure.Census;

public static class AcsV1VariableCatalog
{
    public static IReadOnlyList<string> EstimateVariableCodes { get; } =
    [
        "B01003_001E",
        "B19013_001E",
        "B19301_001E",
        "B17001_001E",
        "B17001_002E",
        "B23025_001E",
        "B23025_002E",
        "B15003_001E",
        "B15003_022E",
        "B15003_023E",
        "B15003_024E",
        "B15003_025E",
        "B25077_001E",
        "B25064_001E",
    ];

    public static IReadOnlyList<string> SourceVariableCodes { get; } =
    [
        "B01003_001E",
        "B01003_001M",
        "B19013_001E",
        "B19013_001M",
        "B19301_001E",
        "B19301_001M",
        "B17001_001E",
        "B17001_001M",
        "B17001_002E",
        "B17001_002M",
        "B23025_001E",
        "B23025_001M",
        "B23025_002E",
        "B23025_002M",
        "B15003_001E",
        "B15003_001M",
        "B15003_022E",
        "B15003_022M",
        "B15003_023E",
        "B15003_023M",
        "B15003_024E",
        "B15003_024M",
        "B15003_025E",
        "B15003_025M",
        "B25077_001E",
        "B25077_001M",
        "B25064_001E",
        "B25064_001M",
    ];

    public static string ToMarginOfErrorVariableCode(string estimateVariableCode)
    {
        if (!estimateVariableCode.EndsWith('E'))
        {
            throw new ArgumentException("ACS estimate variable codes must end with 'E'.", nameof(estimateVariableCode));
        }

        return $"{estimateVariableCode[..^1]}M";
    }
}
