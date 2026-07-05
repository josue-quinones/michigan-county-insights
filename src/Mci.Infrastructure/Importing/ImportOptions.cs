namespace Mci.Infrastructure.Importing;

public sealed class ImportOptions
{
    public const string SectionName = "Imports";

    public int DefaultAcsReleaseYear { get; init; } = 2024;
}
