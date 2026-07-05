using System.Data.Common;
using Mci.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Tests;

public sealed class DatabaseFoundationTests
{
    [Fact]
    public async Task Local_database_has_expected_foundation_when_connection_string_is_present()
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
        await dbContext.Database.OpenConnectionAsync();

        Assert.Equal(83, await dbContext.Counties.CountAsync());
        Assert.Equal(8, await dbContext.MetricDefinitions.CountAsync());
        Assert.Equal(1, await dbContext.DataReleases.CountAsync());
        Assert.Equal(0, await ScalarAsync<int>(
            dbContext.Database.GetDbConnection(),
            """
            SELECT COUNT(*)
            FROM (
                SELECT [SourceCode], [DatasetCode]
                FROM [mci_ops].[DataRelease]
                WHERE [IsDefault] = 1
                GROUP BY [SourceCode], [DatasetCode]
                HAVING COUNT(*) > 1
            ) AS duplicateDefaults;
            """));

        Assert.Equal(6, await ScalarAsync<int>(
            dbContext.Database.GetDbConnection(),
            """
            SELECT COUNT(*)
            FROM sys.schemas
            WHERE [name] IN ('mci_ref', 'mci_catalog', 'mci_ops', 'mci_stg', 'mci_fact', 'mci_reporting');
            """));

        Assert.Equal(1, await ScalarAsync<int>(
            dbContext.Database.GetDbConnection(),
            """
            SELECT COUNT(*)
            FROM sys.views AS viewInfo
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = viewInfo.schema_id
            WHERE schemaInfo.[name] = 'mci_reporting'
              AND viewInfo.[name] = 'vw_CurrentCountyMetricObservation';
            """));
    }

    private static async Task<T> ScalarAsync<T>(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return Assert.IsType<T>(value);
    }
}
