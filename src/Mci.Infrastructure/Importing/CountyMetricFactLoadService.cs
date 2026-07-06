using System.Globalization;
using Mci.Core.Domain.Entities;
using Mci.Core.Domain.Enums;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure.Importing;

public sealed class CountyMetricFactLoadService
{
    private const string SourceCode = "CENSUS";
    private const string DatasetCode = "ACS5_DETAILED";
    private const string DirectCalculationVersion = "direct-source-v1";
    private const string DerivedRateCalculationVersion = "derived-rate-v1";

    private readonly MciDbContext _dbContext;
    private readonly ImportOptions _options;

    public CountyMetricFactLoadService(
        MciDbContext dbContext,
        IOptions<ImportOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<CountyMetricFactLoadResult> LoadMetricsFromLatestValidatedStagingAsync(
        CancellationToken cancellationToken = default)
    {
        var importRun = await _dbContext.ImportRuns
            .Include(run => run.DataRelease)
            .Where(run =>
                run.DataRelease.SourceCode == SourceCode &&
                run.DataRelease.DatasetCode == DatasetCode &&
                run.DataRelease.ReleaseYear == _options.DefaultAcsReleaseYear &&
                run.DataRelease.IsDefault &&
                (run.Status == ImportRunStatus.Succeeded ||
                 run.Status == ImportRunStatus.SucceededWithWarnings))
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (importRun is null)
        {
            throw new InvalidOperationException(
                $"No successful validated staging import was found for default {SourceCode} {DatasetCode} {_options.DefaultAcsReleaseYear}.");
        }

        var existingMetricDefinitionIds = await _dbContext.CountyMetricObservations
            .Where(observation => observation.ImportRunId == importRun.Id)
            .Select(observation => observation.MetricDefinitionId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        var countiesByFipsCode = await _dbContext.Counties
            .AsNoTracking()
            .Where(county => county.IsActive)
            .ToDictionaryAsync(county => county.FipsCode, StringComparer.Ordinal, cancellationToken);

        var metricDefinitionsByCode = await _dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.IsActive)
            .ToDictionaryAsync(metric => metric.Code, StringComparer.Ordinal, cancellationToken);

        var sourceVariableCodes = AcsDirectMetricMappings.All
            .Select(mapping => mapping.SourceVariableCode)
            .Concat(AcsDerivedMetricMappings.SourceVariableCodes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var stagingRows = await _dbContext.AcsCountyVariables
            .AsNoTracking()
            .Where(row =>
                row.ImportRunId == importRun.Id &&
                sourceVariableCodes.Contains(row.SourceVariableCode))
            .ToArrayAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var importIssues = new List<ImportIssue>();
        var observations = new List<CountyMetricObservation>();

        AddDirectMetricObservations(
            importRun,
            stagingRows,
            countiesByFipsCode,
            metricDefinitionsByCode,
            existingMetricDefinitionIds,
            observations,
            importIssues,
            now);

        AddDerivedMetricObservations(
            importRun,
            stagingRows,
            countiesByFipsCode,
            metricDefinitionsByCode,
            existingMetricDefinitionIds,
            observations,
            importIssues,
            now);

        AddCompletenessIssues(importRun.Id, stagingRows, existingMetricDefinitionIds, metricDefinitionsByCode, importIssues, now);

        var errorCount = importIssues.Count(issue => issue.Severity == ImportIssueSeverity.Error);
        var warningCount = importIssues.Count(issue => issue.Severity == ImportIssueSeverity.Warning);

        _dbContext.ImportIssues.AddRange(importIssues);

        if (errorCount == 0)
        {
            _dbContext.CountyMetricObservations.AddRange(observations);
            importRun.Status = warningCount > 0 || importRun.Status == ImportRunStatus.SucceededWithWarnings
                ? ImportRunStatus.SucceededWithWarnings
                : ImportRunStatus.Succeeded;
        }
        else
        {
            importRun.RecordsRejected += errorCount;
            importRun.ErrorSummary = $"Fact loading failed with {errorCount} error(s).";
            importRun.Status = ImportRunStatus.Failed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var totalObservationCount = await _dbContext.CountyMetricObservations
            .CountAsync(observation => observation.ImportRunId == importRun.Id, cancellationToken);

        importRun.RecordsInserted = totalObservationCount;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CountyMetricFactLoadResult(
            importRun.Id,
            importRun.DataReleaseId,
            importRun.RecordsInserted,
            importIssues.Count,
            errorCount,
            warningCount,
            AlreadyLoaded: observations.Count == 0 && importIssues.Count == 0);
    }

    public Task<CountyMetricFactLoadResult> LoadDirectMetricsFromLatestValidatedStagingAsync(
        CancellationToken cancellationToken = default) =>
        LoadMetricsFromLatestValidatedStagingAsync(cancellationToken);

    private static void AddDirectMetricObservations(
        ImportRun importRun,
        IReadOnlyCollection<AcsCountyVariable> stagingRows,
        IReadOnlyDictionary<string, County> countiesByFipsCode,
        IReadOnlyDictionary<string, MetricDefinition> metricDefinitionsByCode,
        IReadOnlySet<int> existingMetricDefinitionIds,
        ICollection<CountyMetricObservation> observations,
        ICollection<ImportIssue> importIssues,
        DateTime createdAtUtc)
    {
        foreach (var mapping in AcsDirectMetricMappings.All)
        {
            if (!metricDefinitionsByCode.TryGetValue(mapping.MetricCode, out var metricDefinition))
            {
                importIssues.Add(CreateIssue(
                    importRun.Id,
                    ImportIssueSeverity.Error,
                    "MISSING_METRIC_DEFINITION",
                    null,
                    mapping.MetricCode,
                    null,
                    $"Metric definition {mapping.MetricCode} was not found or is inactive.",
                    createdAtUtc));

                continue;
            }

            if (existingMetricDefinitionIds.Contains(metricDefinition.Id))
            {
                continue;
            }

            var mappedRows = stagingRows
                .Where(row => row.SourceVariableCode == mapping.SourceVariableCode)
                .ToArray();

            foreach (var row in mappedRows)
            {
                if (!countiesByFipsCode.TryGetValue(row.CountyFipsCode, out var county))
                {
                    importIssues.Add(CreateIssue(
                        importRun.Id,
                        ImportIssueSeverity.Error,
                        "UNKNOWN_COUNTY_FOR_FACT_LOAD",
                        row.CountyFipsCode,
                        mapping.MetricCode,
                        null,
                        $"Cannot load metric {mapping.MetricCode}; county FIPS code {row.CountyFipsCode} was not found or is inactive.",
                        createdAtUtc));

                    continue;
                }

                if (!TryParseNonNegativeDecimal(row.EstimateRaw, out var estimateValue))
                {
                    importIssues.Add(CreateIssue(
                        importRun.Id,
                        ImportIssueSeverity.Error,
                        "INVALID_ESTIMATE_RAW",
                        row.CountyFipsCode,
                        mapping.MetricCode,
                        row.EstimateRaw,
                        $"Cannot load metric {mapping.MetricCode}; estimate raw value is not a non-negative number.",
                        createdAtUtc));

                    continue;
                }

                decimal? marginOfError = null;

                if (!string.IsNullOrWhiteSpace(row.MarginOfErrorRaw))
                {
                    if (TryParseNegativeCensusSentinel(row.MarginOfErrorRaw, out _))
                    {
                        importIssues.Add(CreateIssue(
                            importRun.Id,
                            ImportIssueSeverity.Warning,
                            "CENSUS_MARGIN_OF_ERROR_SENTINEL",
                            row.CountyFipsCode,
                            mapping.MetricCode,
                            row.MarginOfErrorRaw,
                            $"Metric {mapping.MetricCode} has Census margin-of-error sentinel value {row.MarginOfErrorRaw}; loading margin of error as null.",
                            createdAtUtc));
                    }
                    else if (TryParseNonNegativeDecimal(row.MarginOfErrorRaw, out var parsedMarginOfError))
                    {
                        marginOfError = parsedMarginOfError;
                    }
                    else
                    {
                        importIssues.Add(CreateIssue(
                            importRun.Id,
                            ImportIssueSeverity.Error,
                            "INVALID_MARGIN_OF_ERROR_RAW",
                            row.CountyFipsCode,
                            mapping.MetricCode,
                            row.MarginOfErrorRaw,
                            $"Cannot load metric {mapping.MetricCode}; margin-of-error raw value is not a non-negative number.",
                            createdAtUtc));

                        continue;
                    }
                }

                observations.Add(new CountyMetricObservation
                {
                    CountyId = county.Id,
                    MetricDefinitionId = metricDefinition.Id,
                    DataReleaseId = importRun.DataReleaseId,
                    ImportRunId = importRun.Id,
                    EstimateValue = estimateValue,
                    MarginOfError = marginOfError,
                    CalculationVersion = DirectCalculationVersion,
                    CreatedAtUtc = createdAtUtc,
                });
            }
        }
    }

    private static void AddDerivedMetricObservations(
        ImportRun importRun,
        IReadOnlyCollection<AcsCountyVariable> stagingRows,
        IReadOnlyDictionary<string, County> countiesByFipsCode,
        IReadOnlyDictionary<string, MetricDefinition> metricDefinitionsByCode,
        IReadOnlySet<int> existingMetricDefinitionIds,
        ICollection<CountyMetricObservation> observations,
        ICollection<ImportIssue> importIssues,
        DateTime createdAtUtc)
    {
        var stagedValues = stagingRows
            .ToDictionary(
                row => (row.CountyFipsCode, row.SourceVariableCode),
                row => row.EstimateRaw,
                StringTupleComparer.Ordinal);

        foreach (var mapping in AcsDerivedMetricMappings.All)
        {
            if (!metricDefinitionsByCode.TryGetValue(mapping.MetricCode, out var metricDefinition))
            {
                importIssues.Add(CreateIssue(
                    importRun.Id,
                    ImportIssueSeverity.Error,
                    "MISSING_METRIC_DEFINITION",
                    null,
                    mapping.MetricCode,
                    null,
                    $"Metric definition {mapping.MetricCode} was not found or is inactive.",
                    createdAtUtc));

                continue;
            }

            if (existingMetricDefinitionIds.Contains(metricDefinition.Id))
            {
                continue;
            }

            foreach (var county in countiesByFipsCode.Values)
            {
                if (!TryGetDerivedPercentage(
                        importRun.Id,
                        county.FipsCode,
                        mapping,
                        metricDefinition.DecimalPlaces,
                        stagedValues,
                        importIssues,
                        createdAtUtc,
                        out var percentageValue))
                {
                    continue;
                }

                observations.Add(new CountyMetricObservation
                {
                    CountyId = county.Id,
                    MetricDefinitionId = metricDefinition.Id,
                    DataReleaseId = importRun.DataReleaseId,
                    ImportRunId = importRun.Id,
                    EstimateValue = percentageValue,
                    MarginOfError = null,
                    CalculationVersion = DerivedRateCalculationVersion,
                    CreatedAtUtc = createdAtUtc,
                });
            }
        }
    }

    private static void AddCompletenessIssues(
        Guid importRunId,
        IReadOnlyCollection<AcsCountyVariable> stagingRows,
        IReadOnlySet<int> existingMetricDefinitionIds,
        IReadOnlyDictionary<string, MetricDefinition> metricDefinitionsByCode,
        ICollection<ImportIssue> importIssues,
        DateTime createdAtUtc)
    {
        const int expectedCountyCount = 83;

        foreach (var mapping in AcsDirectMetricMappings.All)
        {
            if (metricDefinitionsByCode.TryGetValue(mapping.MetricCode, out var metricDefinition) &&
                existingMetricDefinitionIds.Contains(metricDefinition.Id))
            {
                continue;
            }

            var mappedRows = stagingRows
                .Where(row => row.SourceVariableCode == mapping.SourceVariableCode)
                .ToArray();

            if (mappedRows.Length != expectedCountyCount)
            {
                importIssues.Add(CreateIssue(
                    importRunId,
                    ImportIssueSeverity.Error,
                    "DIRECT_FACT_SOURCE_ROW_COUNT_MISMATCH",
                    null,
                    mapping.MetricCode,
                    null,
                    $"Expected {expectedCountyCount} staged rows for source variable {mapping.SourceVariableCode} but found {mappedRows.Length}.",
                    createdAtUtc));
            }
        }

        foreach (var mapping in AcsDerivedMetricMappings.All)
        {
            if (metricDefinitionsByCode.TryGetValue(mapping.MetricCode, out var metricDefinition) &&
                existingMetricDefinitionIds.Contains(metricDefinition.Id))
            {
                continue;
            }

            foreach (var sourceVariableCode in mapping.NumeratorVariableCodes.Append(mapping.DenominatorVariableCode))
            {
                var mappedRows = stagingRows
                    .Where(row => row.SourceVariableCode == sourceVariableCode)
                    .ToArray();

                if (mappedRows.Length != expectedCountyCount)
                {
                    importIssues.Add(CreateIssue(
                        importRunId,
                        ImportIssueSeverity.Error,
                        "DERIVED_FACT_SOURCE_ROW_COUNT_MISMATCH",
                        null,
                        mapping.MetricCode,
                        null,
                        $"Expected {expectedCountyCount} staged rows for source variable {sourceVariableCode} but found {mappedRows.Length}.",
                        createdAtUtc));
                }
            }
        }
    }

    private static bool TryGetDerivedPercentage(
        Guid importRunId,
        string countyFipsCode,
        AcsDerivedMetricMapping mapping,
        byte decimalPlaces,
        IReadOnlyDictionary<(string CountyFipsCode, string SourceVariableCode), string> stagedValues,
        ICollection<ImportIssue> importIssues,
        DateTime createdAtUtc,
        out decimal percentageValue)
    {
        percentageValue = 0;

        if (!TryGetParsedSourceValue(
                importRunId,
                countyFipsCode,
                mapping.MetricCode,
                mapping.DenominatorVariableCode,
                stagedValues,
                importIssues,
                createdAtUtc,
                out var denominator))
        {
            return false;
        }

        if (denominator == 0)
        {
            importIssues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "DERIVED_DENOMINATOR_ZERO",
                countyFipsCode,
                mapping.MetricCode,
                "0",
                $"Cannot load metric {mapping.MetricCode}; denominator source variable {mapping.DenominatorVariableCode} is zero.",
                createdAtUtc));

            return false;
        }

        decimal numerator = 0;

        foreach (var numeratorVariableCode in mapping.NumeratorVariableCodes)
        {
            if (!TryGetParsedSourceValue(
                    importRunId,
                    countyFipsCode,
                    mapping.MetricCode,
                    numeratorVariableCode,
                    stagedValues,
                    importIssues,
                    createdAtUtc,
                    out var numeratorComponent))
            {
                return false;
            }

            numerator += numeratorComponent;
        }

        percentageValue = Math.Round((numerator / denominator) * 100, decimalPlaces, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryGetParsedSourceValue(
        Guid importRunId,
        string countyFipsCode,
        string metricCode,
        string sourceVariableCode,
        IReadOnlyDictionary<(string CountyFipsCode, string SourceVariableCode), string> stagedValues,
        ICollection<ImportIssue> importIssues,
        DateTime createdAtUtc,
        out decimal value)
    {
        value = 0;

        if (!stagedValues.TryGetValue((countyFipsCode, sourceVariableCode), out var rawValue))
        {
            importIssues.Add(CreateIssue(
                importRunId,
                ImportIssueSeverity.Error,
                "MISSING_DERIVED_SOURCE_VALUE",
                countyFipsCode,
                metricCode,
                null,
                $"Cannot load metric {metricCode}; source variable {sourceVariableCode} is missing.",
                createdAtUtc));

            return false;
        }

        if (TryParseNonNegativeDecimal(rawValue, out value))
        {
            return true;
        }

        importIssues.Add(CreateIssue(
            importRunId,
            ImportIssueSeverity.Error,
            "INVALID_DERIVED_SOURCE_VALUE",
            countyFipsCode,
            metricCode,
            rawValue,
            $"Cannot load metric {metricCode}; source variable {sourceVariableCode} is not a non-negative number.",
            createdAtUtc));

        return false;
    }

    private static bool TryParseNonNegativeDecimal(string? rawValue, out decimal value)
    {
        if (decimal.TryParse(
                rawValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value) &&
            value >= 0)
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParseNegativeCensusSentinel(string? rawValue, out decimal value)
    {
        if (decimal.TryParse(
                rawValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value) &&
            value < 0)
        {
            return true;
        }

        value = 0;
        return false;
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
        Stage = ImportIssueStage.Transform,
        Severity = severity,
        IssueCode = issueCode,
        CountyFipsCode = countyFipsCode,
        MetricCode = metricCode,
        RawValue = rawValue,
        Message = message,
        CreatedAtUtc = createdAtUtc,
    };

    private sealed class StringTupleComparer : IEqualityComparer<(string CountyFipsCode, string SourceVariableCode)>
    {
        public static readonly StringTupleComparer Ordinal = new();

        public bool Equals(
            (string CountyFipsCode, string SourceVariableCode) x,
            (string CountyFipsCode, string SourceVariableCode) y) =>
            string.Equals(x.CountyFipsCode, y.CountyFipsCode, StringComparison.Ordinal) &&
            string.Equals(x.SourceVariableCode, y.SourceVariableCode, StringComparison.Ordinal);

        public int GetHashCode((string CountyFipsCode, string SourceVariableCode) obj) =>
            HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.CountyFipsCode),
                StringComparer.Ordinal.GetHashCode(obj.SourceVariableCode));
    }
}
