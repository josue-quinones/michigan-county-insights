using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mci.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClarityInsightSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClarityInsightSnapshot",
                schema: "mci_ops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaptureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    NumOfDays = table.Column<byte>(type: "tinyint", nullable: false),
                    TotalSessionCount = table.Column<int>(type: "int", nullable: true),
                    TotalBotSessionCount = table.Column<int>(type: "int", nullable: true),
                    DistinctUserCount = table.Column<int>(type: "int", nullable: true),
                    PagesPerSession = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClarityInsightSnapshot", x => x.Id);
                    table.CheckConstraint("CK_ClarityInsightSnapshot_NumOfDays", "[NumOfDays] BETWEEN 1 AND 3");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClarityInsightSnapshot_CaptureDate",
                schema: "mci_ops",
                table: "ClarityInsightSnapshot",
                column: "CaptureDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClarityInsightSnapshot",
                schema: "mci_ops");
        }
    }
}
