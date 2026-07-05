using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mci.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Seed2024AcsDataRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "mci_ops",
                table: "DataRelease",
                columns: new[] { "Id", "CreatedAtUtc", "DatasetCode", "DisplayName", "IsDefault", "PeriodEndYear", "PeriodStartYear", "ReleaseYear", "SourceCode" },
                values: new object[] { 1, new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Utc), "ACS5_DETAILED", "2020-2024 ACS 5-Year", true, (short)2024, (short)2020, (short)2024, "CENSUS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "mci_ops",
                table: "DataRelease",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
