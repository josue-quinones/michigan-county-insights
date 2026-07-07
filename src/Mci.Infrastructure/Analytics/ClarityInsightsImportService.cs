using System.Text.Json;
using Mci.Core.Domain.Entities;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure.Analytics;

/// <summary>
/// Fetches the daily aggregate from the Clarity Data Export API and stores one
/// snapshot per UTC day. Idempotent: if today's snapshot already exists it makes
/// no API call, which keeps the worker safely under Clarity's ~10 requests/day
/// limit even if it runs more than once.
/// </summary>
public sealed class ClarityInsightsImportService
{
    private const string TrafficMetricName = "Traffic";

    private readonly MciDbContext _dbContext;
    private readonly ClarityInsightsClient _client;
    private readonly ClarityOptions _options;
    private readonly ILogger<ClarityInsightsImportService> _logger;

    public ClarityInsightsImportService(
        MciDbContext dbContext,
        ClarityInsightsClient client,
        IOptions<ClarityOptions> options,
        ILogger<ClarityInsightsImportService> logger)
    {
        _dbContext = dbContext;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ClarityInsightsImportResult> FetchAndStoreDailyInsightsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            _logger.LogWarning("Clarity API token not configured; skipping insights fetch.");
            return new ClarityInsightsImportResult(
                Executed: false,
                AlreadyCaptured: false,
                SkipReason: "Clarity:ApiToken is not configured.",
                SnapshotId: null,
                CaptureDate: today,
                TotalSessionCount: null,
                DistinctUserCount: null);
        }

        var existing = await _dbContext.ClarityInsightSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(snapshot => snapshot.CaptureDate == today, cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation(
                "Clarity insights already captured for {CaptureDate}; skipping to respect the daily API limit.",
                today);
            return new ClarityInsightsImportResult(
                Executed: false,
                AlreadyCaptured: true,
                SkipReason: "A snapshot already exists for today.",
                SnapshotId: existing.Id,
                CaptureDate: today,
                TotalSessionCount: existing.TotalSessionCount,
                DistinctUserCount: existing.DistinctUserCount);
        }

        var numOfDays = _options.NumOfDays is >= 1 and <= 3 ? _options.NumOfDays : (byte)1;
        var rawPayload = await _client.GetProjectLiveInsightsAsync(numOfDays, cancellationToken);
        var traffic = ParseTraffic(rawPayload);

        var snapshot = new ClarityInsightSnapshot
        {
            CaptureDate = today,
            CapturedAtUtc = DateTime.UtcNow,
            NumOfDays = numOfDays,
            TotalSessionCount = traffic.TotalSessionCount,
            TotalBotSessionCount = traffic.TotalBotSessionCount,
            DistinctUserCount = traffic.DistinctUserCount,
            PagesPerSession = traffic.PagesPerSession,
            RawPayload = rawPayload,
        };

        _dbContext.ClarityInsightSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stored Clarity insights snapshot {SnapshotId} for {CaptureDate}: {Sessions} sessions, {Users} distinct users.",
            snapshot.Id,
            today,
            traffic.TotalSessionCount,
            traffic.DistinctUserCount);

        return new ClarityInsightsImportResult(
            Executed: true,
            AlreadyCaptured: false,
            SkipReason: null,
            SnapshotId: snapshot.Id,
            CaptureDate: today,
            TotalSessionCount: traffic.TotalSessionCount,
            DistinctUserCount: traffic.DistinctUserCount);
    }

    private readonly record struct TrafficMetrics(
        int? TotalSessionCount,
        int? TotalBotSessionCount,
        int? DistinctUserCount,
        decimal? PagesPerSession);

    /// <summary>
    /// Best-effort extraction of headline metrics from the "Traffic" metric. The
    /// full payload is always stored, so unknown/changed fields are recoverable;
    /// missing fields simply stay null (never zero).
    /// </summary>
    private static TrafficMetrics ParseTraffic(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return default;
            }

            foreach (var metric in document.RootElement.EnumerateArray())
            {
                if (!metric.TryGetProperty("metricName", out var metricName) ||
                    !string.Equals(metricName.GetString(), TrafficMetricName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!metric.TryGetProperty("information", out var information) ||
                    information.ValueKind != JsonValueKind.Array ||
                    information.GetArrayLength() == 0)
                {
                    return default;
                }

                var info = information[0];
                return new TrafficMetrics(
                    ReadInt(info, "totalSessionCount"),
                    ReadInt(info, "totalBotSessionCount"),
                    ReadInt(info, "distinctUserCount"),
                    ReadDecimal(info, "pagesPerSessionPercentage"));
            }
        }
        catch (JsonException)
        {
            // Payload is retained verbatim; headline metrics stay null.
        }

        return default;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }
}
