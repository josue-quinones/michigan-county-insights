namespace Mci.Core.Application.Reporting;

/// <summary>Lightweight county identity for detail/comparison responses.</summary>
public sealed record CountyReferenceDto(
    string Fips,
    string Name,
    string StateCode);

/// <summary>Release period metadata shared by detail and comparison responses.</summary>
public sealed record ReleaseInfoDto(
    short Year,
    short PeriodStartYear,
    short PeriodEndYear,
    string DisplayName);
