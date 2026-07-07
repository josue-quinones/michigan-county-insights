using Mci.Infrastructure.Persistence;
using Mci.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Tests;

public sealed class CountyInsightServiceTests
{
    [Fact]
    public async Task Comparing_a_county_with_itself_is_rejected()
    {
        // The same-county guard runs before any database access, so a non-connecting
        // context is sufficient to exercise it.
        var options = new DbContextOptionsBuilder<MciDbContext>()
            .UseSqlServer("Server=(local);Database=mci-test;Trusted_Connection=True;")
            .Options;

        await using var dbContext = new MciDbContext(options);
        var service = new CountyInsightService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CompareCountiesAsync("26161", "26161", releaseYear: 2024));
    }

    [Fact]
    public async Task Returns_county_detail_with_all_metrics_from_local_database_when_connection_string_is_present()
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
        var service = new CountyInsightService(dbContext);

        var detail = await service.GetCountyDetailAsync("26161", releaseYear: 2024);

        Assert.NotNull(detail);
        Assert.Equal("Washtenaw", detail!.County.Name);
        Assert.Equal("MI", detail.County.StateCode);
        Assert.Equal(8, detail.Metrics.Count);
        Assert.All(detail.Metrics, metric => Assert.True(metric.IsAvailable));
    }

    [Fact]
    public async Task Unknown_fips_returns_null_detail_when_connection_string_is_present()
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
        var service = new CountyInsightService(dbContext);

        var detail = await service.GetCountyDetailAsync("00000", releaseYear: 2024);

        Assert.Null(detail);
    }

    [Fact]
    public async Task Compares_two_counties_with_computed_differences_when_connection_string_is_present()
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
        var service = new CountyInsightService(dbContext);

        var comparison = await service.CompareCountiesAsync("26161", "26081", releaseYear: 2024);

        Assert.Equal("left_minus_right", comparison.DifferenceDirection);
        Assert.Equal("Washtenaw", comparison.LeftCounty.Name);
        Assert.Equal("Kent", comparison.RightCounty.Name);
        Assert.Equal(8, comparison.Metrics.Count);
        Assert.Contains(comparison.Metrics, metric => metric.IsAvailable && metric.Difference is not null);
    }
}
