namespace Mci.Core.Application.Reporting;

public sealed record ReportingCountyDto(
    string FipsCode,
    string Name,
    string StateCode,
    string StateName);
