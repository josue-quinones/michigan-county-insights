using System.Security.Cryptography;
using System.Text;
using Mci.Core.Domain.Entities;
using Mci.Core.Domain.Enums;
using Mci.Infrastructure.Census;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure.Importing;

public sealed class RawAcsStagingImportService
{
    private const string SourceCode = "CENSUS";
    private const string DatasetCode = "ACS5_DETAILED";
    private const string PipelineVersion = "raw-acs-staging-v1";
    private const int ExpectedMichiganCountyCount = 83;

    private readonly MciDbContext _dbContext;
    private readonly CensusAcsClient _censusClient;
    private readonly ImportOptions _options;

    public RawAcsStagingImportService(
        MciDbContext dbContext,
        CensusAcsClient censusClient,
        IOptions<ImportOptions> options)
    {
        _dbContext = dbContext;
        _censusClient = censusClient;
        _options = options.Value;
    }

    public async Task<RawAcsStagingImportResult> ImportDefaultAcsReleaseAsync(
        ImportTriggerType triggerType = ImportTriggerType.Manual,
        CancellationToken cancellationToken = default)
    {
        var dataRelease = await _dbContext.DataReleases.SingleOrDefaultAsync(
            release =>
                release.SourceCode == SourceCode &&
                release.DatasetCode == DatasetCode &&
                release.ReleaseYear == _options.DefaultAcsReleaseYear &&
                release.IsDefault,
            cancellationToken);

        if (dataRelease is null)
        {
            throw new InvalidOperationException(
                $"Default {SourceCode} {DatasetCode} data release for {_options.DefaultAcsReleaseYear} was not found.");
        }

        var now = DateTime.UtcNow;
        var importRun = new ImportRun
        {
            Id = Guid.NewGuid(),
            DataReleaseId = dataRelease.Id,
            TriggerType = triggerType,
            Status = ImportRunStatus.Running,
            StartedAtUtc = now,
            PipelineVersion = PipelineVersion,
            CreatedAtUtc = now,
        };

        _dbContext.ImportRuns.Add(importRun);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var censusRows = await _censusClient.GetMichiganCountyVariablesAsync(
                dataRelease.ReleaseYear,
                cancellationToken);

            importRun.RecordsFetched = censusRows.Count;

            var stagedAtUtc = DateTime.UtcNow;
            var stagingRows = censusRows
                .SelectMany(row => AcsV1VariableCatalog.EstimateVariableCodes.Select(variableCode =>
                {
                    row.Variables.TryGetValue(variableCode, out var estimateRaw);
                    row.Variables.TryGetValue(
                        AcsV1VariableCatalog.ToMarginOfErrorVariableCode(variableCode),
                        out var marginOfErrorRaw);

                    return new AcsCountyVariable
                    {
                        ImportRunId = importRun.Id,
                        CountyFipsCode = row.CountyFipsCode,
                        CountyNameRaw = row.CountyName,
                        SourceVariableCode = variableCode,
                        EstimateRaw = estimateRaw ?? string.Empty,
                        MarginOfErrorRaw = marginOfErrorRaw,
                        SourceRowHash = ComputeSourceRowHash(
                            row.CountyFipsCode,
                            row.CountyName,
                            variableCode,
                            estimateRaw,
                            marginOfErrorRaw),
                        StagedAtUtc = stagedAtUtc,
                    };
                }))
                .ToArray();

            importRun.RecordsStaged = stagingRows.Length;

            var knownCountyFipsCodes = await _dbContext.Counties
                .AsNoTracking()
                .Where(county => county.IsActive)
                .Select(county => county.FipsCode)
                .ToHashSetAsync(cancellationToken);

            var importIssues = ValidateStagedRows(
                importRun.Id,
                censusRows.Count,
                stagingRows,
                knownCountyFipsCodes,
                DateTime.UtcNow);

            _dbContext.AcsCountyVariables.AddRange(stagingRows);
            _dbContext.ImportIssues.AddRange(importIssues);

            var errorCount = importIssues.Count(issue => issue.Severity == ImportIssueSeverity.Error);
            var warningCount = importIssues.Count(issue => issue.Severity == ImportIssueSeverity.Warning);

            importRun.Status = errorCount > 0
                ? ImportRunStatus.Failed
                : warningCount > 0
                    ? ImportRunStatus.SucceededWithWarnings
                    : ImportRunStatus.Succeeded;
            importRun.CompletedAtUtc = DateTime.UtcNow;
            importRun.RecordsRejected = errorCount;
            importRun.ErrorSummary = errorCount > 0
                ? $"Raw ACS staging validation failed with {errorCount} error(s)."
                : null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RawAcsStagingImportResult(
                importRun.Id,
                dataRelease.Id,
                importRun.RecordsFetched,
                importRun.RecordsStaged,
                importIssues.Count,
                errorCount,
                warningCount);
        }
        catch (Exception ex)
        {
            _dbContext.ChangeTracker.Clear();

            var failedImportRun = await _dbContext.ImportRuns.SingleOrDefaultAsync(
                run => run.Id == importRun.Id,
                CancellationToken.None);

            if (failedImportRun is not null)
            {
                failedImportRun.Status = ImportRunStatus.Failed;
                failedImportRun.CompletedAtUtc = DateTime.UtcNow;
                failedImportRun.ErrorSummary = ex.Message;
                failedImportRun.RecordsFetched = importRun.RecordsFetched;
                failedImportRun.RecordsStaged = importRun.RecordsStaged;
                failedImportRun.RecordsRejected = importRun.RecordsRejected;

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }

            throw;
        }
    }

    private static string ComputeSourceRowHash(
        string countyFipsCode,
        string countyName,
        string sourceVariableCode,
        string? estimateRaw,
        string? marginOfErrorRaw)
    {
        var value = string.Join(
            '|',
            countyFipsCode,
            countyName,
            sourceVariableCode,
            estimateRaw ?? string.Empty,
            marginOfErrorRaw ?? string.Empty);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static List<ImportIssue> ValidateStagedRows(
        Guid importRunId,
        int fetchedCountyCount,
        IReadOnlyCollection<AcsCountyVariable> stagingRows,
        IReadOnlySet<string> knownCountyFipsCodes,
        DateTime createdAtUtc)
    {
        var issues = new List<ImportIssue>();
        var expectedVariableCount = AcsV1VariableCatalog.EstimateVariableCodes.Count;
        var expectedStagingRowCount = ExpectedMichiganCountyCount * expectedVariableCount;
        var distinctCountyCount = stagingRows.Select(row => row.CountyFipsCode).Distinct(StringComparer.Ordinal).Count();
        var distinctVariableCount = stagingRows.Select(row => row.SourceVariableCode).Distinct(StringComparer.Ordinal).Count();

        if (fetchedCountyCount != ExpectedMichiganCountyCount)
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "ACS_COUNTY_COUNT_MISMATCH",
                null,
                null,
                null,
                $"Expected {ExpectedMichiganCountyCount} Michigan county rows from Census but fetched {fetchedCountyCount}.",
                createdAtUtc));
        }

        if (distinctCountyCount != ExpectedMichiganCountyCount)
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "STAGING_COUNTY_COUNT_MISMATCH",
                null,
                null,
                null,
                $"Expected {ExpectedMichiganCountyCount} distinct staged county FIPS codes but found {distinctCountyCount}.",
                createdAtUtc));
        }

        if (distinctVariableCount != expectedVariableCount)
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "STAGING_VARIABLE_COUNT_MISMATCH",
                null,
                null,
                null,
                $"Expected {expectedVariableCount} distinct staged ACS estimate variables but found {distinctVariableCount}.",
                createdAtUtc));
        }

        if (stagingRows.Count != expectedStagingRowCount)
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "STAGING_ROW_COUNT_MISMATCH",
                null,
                null,
                null,
                $"Expected {expectedStagingRowCount} raw staging rows but found {stagingRows.Count}.",
                createdAtUtc));
        }

        foreach (var unknownCountyFipsCode in stagingRows
                     .Select(row => row.CountyFipsCode)
                     .Distinct(StringComparer.Ordinal)
                     .Where(countyFipsCode => !knownCountyFipsCodes.Contains(countyFipsCode))
                     .Order(StringComparer.Ordinal))
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "UNKNOWN_COUNTY",
                unknownCountyFipsCode,
                null,
                null,
                $"Staged ACS row references unknown or inactive county FIPS code {unknownCountyFipsCode}.",
                createdAtUtc));
        }

        foreach (var missingCountyFipsCode in knownCountyFipsCodes
                     .Where(countyFipsCode => stagingRows.All(row => row.CountyFipsCode != countyFipsCode))
                     .Order(StringComparer.Ordinal))
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "MISSING_COUNTY",
                missingCountyFipsCode,
                null,
                null,
                $"No staged ACS rows were found for expected Michigan county FIPS code {missingCountyFipsCode}.",
                createdAtUtc));
        }

        var stagedVariableCodes = stagingRows
            .Select(row => row.SourceVariableCode)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var missingVariableCode in AcsV1VariableCatalog.EstimateVariableCodes
                     .Where(variableCode => !stagedVariableCodes.Contains(variableCode)))
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "MISSING_VARIABLE",
                null,
                missingVariableCode,
                null,
                $"No staged ACS rows were found for expected source variable {missingVariableCode}.",
                createdAtUtc));
        }

        foreach (var row in stagingRows.Where(row => string.IsNullOrWhiteSpace(row.EstimateRaw)))
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "MISSING_ESTIMATE_RAW",
                row.CountyFipsCode,
                row.SourceVariableCode,
                row.EstimateRaw,
                $"Staged ACS row is missing raw estimate value for {row.SourceVariableCode}.",
                createdAtUtc));
        }

        foreach (var row in stagingRows.Where(row => string.IsNullOrWhiteSpace(row.MarginOfErrorRaw)))
        {
            issues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Warning,
                "MISSING_MARGIN_OF_ERROR_RAW",
                row.CountyFipsCode,
                row.SourceVariableCode,
                row.MarginOfErrorRaw,
                $"Staged ACS row is missing raw margin-of-error value for {row.SourceVariableCode}.",
                createdAtUtc));
        }

        return issues;
    }

    private static ImportIssue CreateIssue(
        Guid importRunId,
        ImportIssueSeverity severity,
        string issueCode,
        string? countyFipsCode,
        string? metricCode,
        string? rawValue,
        string message,
        DateTime createdAtUtc) => new()
    {
        ImportRunId = importRunId,
        Stage = ImportIssueStage.Validate,
        Severity = severity,
        IssueCode = issueCode,
        CountyFipsCode = countyFipsCode,
        MetricCode = metricCode,
        RawValue = rawValue,
        Message = message,
        CreatedAtUtc = createdAtUtc,
    };
}
