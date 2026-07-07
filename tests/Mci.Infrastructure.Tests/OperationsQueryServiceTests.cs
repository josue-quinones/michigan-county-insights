using Mci.Core.Application.Operations;
using Mci.Infrastructure.Operations;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Tests;

public sealed class OperationsQueryServiceTests
{
    [Fact]
    public async Task Gets_import_runs_and_issues_from_local_database_when_connection_string_is_present()
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
        var service = new OperationsQueryService(dbContext);

        var importRuns = await service.GetImportRunsAsync(new ImportRunQuery(ReleaseYear: 2024));
        var importIssues = await service.GetImportIssuesAsync(new ImportIssueQuery(Limit: 10));

        Assert.NotEmpty(importRuns);
        Assert.All(importRuns, run => Assert.Equal(2024, run.ReleaseYear));
        Assert.True(importIssues.Count <= 10);
    }
}
