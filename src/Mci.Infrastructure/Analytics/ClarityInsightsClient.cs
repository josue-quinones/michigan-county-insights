using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure.Analytics;

/// <summary>
/// Thin client over the Microsoft Clarity Data Export API. Returns the raw JSON
/// body; parsing is the service's responsibility so the payload can be stored
/// verbatim. The API is rate-limited (~10 requests per project per day), so
/// callers must not poll — one request per scheduled run.
/// </summary>
public sealed class ClarityInsightsClient
{
    private const string LiveInsightsPath = "export-data/api/v1/project-live-insights";

    private readonly HttpClient _httpClient;
    private readonly ClarityOptions _options;

    public ClarityInsightsClient(HttpClient httpClient, IOptions<ClarityOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetProjectLiveInsightsAsync(
        byte numOfDays,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            throw new InvalidOperationException(
                "Configuration value 'Clarity:ApiToken' is required for Clarity export requests.");
        }

        if (numOfDays is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numOfDays), numOfDays, "Clarity export supports a 1-3 day window.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{LiveInsightsPath}?numOfDays={numOfDays}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
