namespace Mci.Core.Application.Operations;

public sealed record ImportIssueDto(
    long Id,
    Guid ImportRunId,
    string Stage,
    string Severity,
    string IssueCode,
    string? CountyFipsCode,
    string? MetricCode,
    string? RawValue,
    string Message,
    DateTime CreatedAtUtc);
