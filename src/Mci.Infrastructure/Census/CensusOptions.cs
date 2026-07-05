namespace Mci.Infrastructure.Census;

public sealed class CensusOptions
{
    public const string SectionName = "Census";

    public string BaseUrl { get; init; } = "https://api.census.gov/data";

    public string? ApiKey { get; init; }
}
