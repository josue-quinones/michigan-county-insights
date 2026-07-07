namespace Mci.Core.Application.Operations;

public sealed record ImportRunQuery(
    string? Status = null,
    short? ReleaseYear = null,
    int Limit = 25);
