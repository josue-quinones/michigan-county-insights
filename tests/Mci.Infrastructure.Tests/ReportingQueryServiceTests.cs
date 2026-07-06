using Mci.Core.Application.Reporting;
using Mci.Infrastructure.Persistence;
using Mci.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Tests;

public sealed class ReportingQueryServiceTests
{
    [Fact]
    public async Task Gets_current_observations_from_local_database_when_connection_string_is_present()
    {
        var connectionString = Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<MciDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var dbContext = new MciDbContext(options);
        var service = new ReportingQueryService(dbContext);

        var observations = await service.GetCurrentCountyMetricObservationsAsync(
            new CurrentCountyMetricObservationQuery(
                MetricCode: "population",
                CountyFipsCode: "26161",
                ReleaseYear: 2024));

        var observation = Assert.Single(observations);
        Assert.Equal("Washtenaw", observation.CountyName);
        Assert.Equal("population", observation.MetricCode);
        Assert.Equal("2020-2024 ACS 5-Year", observation.DataReleaseDisplayName);
        Assert.False(string.IsNullOrWhiteSpace(observation.ComparisonGuidance));
    }

    [Fact]
    public async Task Gets_reporting_filter_data_and_summary_from_local_database_when_connection_string_is_present()
    {
        var connectionString = Environment.GetEnvironmentVariable("MCI_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<MciDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var dbContext = new MciDbContext(options);
        var service = new ReportingQueryService(dbContext);

        var metrics = await service.GetMetricsAsync();
        var counties = await service.GetCountiesAsync();
        var summary = await service.GetCurrentCountyMetricObservationSummaryAsync(
            new CurrentCountyMetricObservationQuery(ReleaseYear: 2024));

        Assert.Equal(8, metrics.Count);
        Assert.Equal(83, counties.Count);
        Assert.Equal(8, summary.Count);
        Assert.All(summary, item => Assert.Equal(83, item.CountyCount));
    }
}
