using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure.Census;

public sealed class CensusAcsClient
{
    private const string MichiganStateFipsCode = "26";
    private readonly HttpClient _httpClient;
    private readonly CensusOptions _options;

    public CensusAcsClient(HttpClient httpClient, IOptions<CensusOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AcsCountyVariableRow>> GetMichiganCountyVariablesAsync(
        int releaseYear,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri(releaseYear, AcsV1VariableCatalog.SourceVariableCodes);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Census ACS 5-year endpoint was not found for release year {releaseYear}.");
        }

        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() < 2)
        {
            return [];
        }

        var headers = document.RootElement[0]
            .EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();

        var nameIndex = Array.IndexOf(headers, "NAME");
        var stateIndex = Array.IndexOf(headers, "state");
        var countyIndex = Array.IndexOf(headers, "county");

        if (nameIndex < 0 || stateIndex < 0 || countyIndex < 0)
        {
            throw new InvalidOperationException("Census response did not include NAME, state, and county columns.");
        }

        var variableIndexes = AcsV1VariableCatalog.SourceVariableCodes
            .Select(variableCode => (VariableCode: variableCode, Index: Array.IndexOf(headers, variableCode)))
            .Where(variable => variable.Index >= 0)
            .ToArray();

        var rows = new List<AcsCountyVariableRow>(document.RootElement.GetArrayLength() - 1);

        foreach (var row in document.RootElement.EnumerateArray().Skip(1))
        {
            var values = row.EnumerateArray().Select(value => value.GetString()).ToArray();
            var countyFipsCode = $"{values[stateIndex]}{values[countyIndex]}";
            var variables = variableIndexes.ToDictionary(
                variable => variable.VariableCode,
                variable => values[variable.Index]);

            rows.Add(new AcsCountyVariableRow(
                countyFipsCode,
                values[nameIndex] ?? string.Empty,
                variables));
        }

        return rows;
    }

    private string BuildRequestUri(int releaseYear, IReadOnlyList<string> variableCodes)
    {
        if (releaseYear <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseYear), releaseYear, "Release year must be positive.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Configuration value 'Census:ApiKey' is required for Census API requests.");
        }

        var queryParameters = new Dictionary<string, string>
        {
            ["get"] = $"NAME,{string.Join(',', variableCodes)}",
            ["for"] = "county:*",
            ["in"] = $"state:{MichiganStateFipsCode}",
        };

        queryParameters["key"] = _options.ApiKey;

        var queryString = string.Join(
            '&',
            queryParameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        return $"{releaseYear}/acs/acs5?{queryString}";
    }
}
