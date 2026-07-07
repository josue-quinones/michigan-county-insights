namespace Mci.Infrastructure.Analytics;

public sealed class ClarityOptions
{
    public const string SectionName = "Clarity";

    public string BaseUrl { get; init; } = "https://www.clarity.ms";

    /// <summary>Data Export API token (Clarity → Settings → Data Export).</summary>
    public string? ApiToken { get; init; }

    /// <summary>Rolling aggregate window in days. Clarity accepts 1, 2, or 3.</summary>
    public byte NumOfDays { get; init; } = 1;
}
