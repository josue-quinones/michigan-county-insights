namespace Mci.Infrastructure.Importing;

public static class AcsDirectMetricMappings
{
    public static IReadOnlyList<AcsDirectMetricMapping> All { get; } =
    [
        new("population", "B01003_001E"),
        new("median_household_income", "B19013_001E"),
        new("per_capita_income", "B19301_001E"),
        new("median_home_value", "B25077_001E"),
        new("median_gross_rent", "B25064_001E"),
    ];
}
