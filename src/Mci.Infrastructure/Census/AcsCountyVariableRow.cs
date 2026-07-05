namespace Mci.Infrastructure.Census;

public sealed record AcsCountyVariableRow(
    string CountyFipsCode,
    string CountyName,
    IReadOnlyDictionary<string, string?> Variables);
