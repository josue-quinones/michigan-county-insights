using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Mci.Api.Tests;

public sealed class GraphQlEndpointTests : IClassFixture<GraphQlEndpointTests.ApiFactory>
{
    private readonly HttpClient _client;

    public GraphQlEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Graphql_endpoint_starts_and_serves_the_schema()
    {
        using var response = await PostAsync("{ __schema { queryType { name } } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(
            "ReportingGraphQlQueries",
            payload!.RootElement.GetProperty("data").GetProperty("__schema").GetProperty("queryType").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Comparing_a_county_with_itself_returns_a_graphql_error()
    {
        using var response = await PostAsync(
            "query { compareCounties(leftFips: \"26161\", rightFips: \"26161\") { differenceDirection } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Contains(
            payload!.RootElement.GetProperty("errors").EnumerateArray(),
            error => error.GetProperty("message").GetString()!.Contains("cannot be compared with itself"));
    }

    [Fact]
    public async Task County_rejects_a_blank_fips_code()
    {
        using var response = await PostAsync("query { county(fips: \"   \") { name } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Contains(
            payload!.RootElement.GetProperty("errors").EnumerateArray(),
            error => error.GetProperty("message").GetString()!.Contains("is required"));
    }

    [Fact]
    public async Task RankCounties_rejects_a_first_value_above_the_safe_maximum()
    {
        using var response = await PostAsync(
            "query { rankCounties(metricCode: \"median_household_income\", first: 500) { rank } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Contains(
            payload!.RootElement.GetProperty("errors").EnumerateArray(),
            error => error.GetProperty("message").GetString()!.Contains("must be between 1 and 50"));
    }

    [Fact]
    public async Task Counties_query_returns_data_from_local_database_when_connection_string_is_present()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING")))
        {
            return;
        }

        using var response = await PostAsync("query { counties { fipsCode name stateCode } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.True(payload!.RootElement.GetProperty("data").GetProperty("counties").GetArrayLength() > 0);
    }

    [Fact]
    public async Task CountyDetail_returns_the_expected_county_shape_from_local_database_when_connection_string_is_present()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING")))
        {
            return;
        }

        using var response = await PostAsync(
            "query { countyDetail(fips: \"26161\", releaseYear: 2024) { county { name fips } metrics { code isAvailable } } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var detail = payload!.RootElement.GetProperty("data").GetProperty("countyDetail");

        Assert.Equal("Washtenaw", detail.GetProperty("county").GetProperty("name").GetString());
    }

    [Fact]
    public async Task CompareCounties_handles_two_different_counties_from_local_database_when_connection_string_is_present()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING")))
        {
            return;
        }

        using var response = await PostAsync(
            "query { compareCounties(leftFips: \"26161\", rightFips: \"26081\", releaseYear: 2024) { differenceDirection leftCounty { name } rightCounty { name } } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var comparison = payload!.RootElement.GetProperty("data").GetProperty("compareCounties");

        Assert.Equal("left_minus_right", comparison.GetProperty("differenceDirection").GetString());
        Assert.Equal("Washtenaw", comparison.GetProperty("leftCounty").GetProperty("name").GetString());
        Assert.Equal("Kent", comparison.GetProperty("rightCounty").GetProperty("name").GetString());
    }

    [Fact]
    public async Task RankCounties_respects_the_first_limit_from_local_database_when_connection_string_is_present()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING")))
        {
            return;
        }

        using var response = await PostAsync(
            "query { rankCounties(metricCode: \"median_household_income\", releaseYear: 2024, first: 3) { rank } }");

        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(3, payload!.RootElement.GetProperty("data").GetProperty("rankCounties").GetArrayLength());
    }

    private Task<HttpResponseMessage> PostAsync(string query) =>
        _client.PostAsJsonAsync("/graphql", new { query });

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connectionString = Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING")
                ?? "Server=(local);Database=mci-test;Trusted_Connection=True;TrustServerCertificate=True;";

            builder.UseSetting("ConnectionStrings:MciDatabase", connectionString);
        }
    }
}
