namespace Mci.Core.Domain.Entities;

/// <summary>
/// A daily snapshot of aggregated Microsoft Clarity project metrics, fetched from
/// the Clarity Data Export API. One row per capture date. The raw payload is kept
/// verbatim so new fields can be re-parsed later without another API call.
/// </summary>
public sealed class ClarityInsightSnapshot
{
    public int Id { get; set; }

    /// <summary>UTC date this snapshot was captured (unique; one capture per day).</summary>
    public DateOnly CaptureDate { get; set; }

    public DateTime CapturedAtUtc { get; set; }

    /// <summary>Rolling window the aggregate covers (Clarity supports 1-3 days).</summary>
    public byte NumOfDays { get; set; }

    public int? TotalSessionCount { get; set; }
    public int? TotalBotSessionCount { get; set; }
    public int? DistinctUserCount { get; set; }
    public decimal? PagesPerSession { get; set; }

    /// <summary>Verbatim JSON returned by the Clarity export API.</summary>
    public string RawPayload { get; set; } = string.Empty;
}
