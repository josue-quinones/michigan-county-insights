using Mci.Infrastructure.Census;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure.Tests;

public sealed class CensusAcsClientTests
{
    [Fact]
    public async Task Fetches_2024_michigan_county_rows_when_enabled()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MCI_TEST_CENSUS"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("MCI_TEST_CENSUS_API_KEY")
            ?? Environment.GetEnvironmentVariable("CENSUS_API_KEY")
            ?? Environment.GetEnvironmentVariable("Census__ApiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.census.gov/data/", UriKind.Absolute),
        };

        var client = new CensusAcsClient(
            httpClient,
            Options.Create(new CensusOptions
            {
                BaseUrl = "https://api.census.gov/data",
                ApiKey = apiKey,
            }));

        var rows = await client.GetMichiganCountyVariablesAsync(2024);

        Assert.Equal(83, rows.Count);
        Assert.Contains(rows, row => row.CountyFipsCode == "26161" && row.CountyName.Contains("Washtenaw"));
        Assert.All(rows, row => Assert.Equal(AcsV1VariableCatalog.SourceVariableCodes.Count, row.Variables.Count));
    }
}
