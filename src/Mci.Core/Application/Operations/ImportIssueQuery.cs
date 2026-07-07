namespace Mci.Core.Application.Operations;

public sealed record ImportIssueQuery(
    Guid? ImportRunId = null,
    string? Severity = null,
    string? Stage = null,
    int Limit = 100);
