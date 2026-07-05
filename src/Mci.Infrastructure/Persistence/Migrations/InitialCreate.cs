using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mci.Infrastructure.Persistence.Migrations;

/// <summary>
/// Initial SQL Server schema for Michigan County Insights V1.
///
/// This file includes migration discovery attributes so it can be applied as the
/// initial migration. Once the solution is compiled, scaffold or regenerate EF Core's
/// model snapshot before creating the next migration; do not hand-maintain a snapshot.
/// </summary>
[DbContext(typeof(MciDbContext))]
[Migration("202607050001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "mci_catalog");
        migrationBuilder.EnsureSchema(name: "mci_fact");
        migrationBuilder.EnsureSchema(name: "mci_ops");
        migrationBuilder.EnsureSchema(name: "mci_ref");
        migrationBuilder.EnsureSchema(name: "mci_reporting");
        migrationBuilder.EnsureSchema(name: "mci_stg");

        migrationBuilder.CreateTable(
            name: "County",
            schema: "mci_ref",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FipsCode = table.Column<string>(type: "char(5)", fixedLength: true, maxLength: 5, nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                StateFipsCode = table.Column<string>(type: "char(2)", fixedLength: true, maxLength: 2, nullable: false),
                StateCode = table.Column<string>(type: "char(2)", fixedLength: true, maxLength: 2, nullable: false),
                StateName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_County", x => x.Id);
                table.CheckConstraint("CK_County_FipsCode_Length", "LEN([FipsCode]) = 5");
            });

        migrationBuilder.CreateTable(
            name: "MetricDefinition",
            schema: "mci_catalog",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                DecimalPlaces = table.Column<byte>(type: "tinyint", nullable: false),
                CalculationType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                ComparisonGuidance = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RequiresDollarNormalization = table.Column<bool>(type: "bit", nullable: false),
                SupportsAdjacentReleaseComparison = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MetricDefinition", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DataRelease",
            schema: "mci_ops",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SourceCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                DatasetCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                ReleaseYear = table.Column<short>(type: "smallint", nullable: false),
                PeriodStartYear = table.Column<short>(type: "smallint", nullable: false),
                PeriodEndYear = table.Column<short>(type: "smallint", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataRelease", x => x.Id);
                table.CheckConstraint(
                    "CK_DataRelease_Period",
                    "[PeriodStartYear] <= [PeriodEndYear] AND [PeriodEndYear] = [ReleaseYear]");
            });

        migrationBuilder.CreateTable(
            name: "ImportRun",
            schema: "mci_ops",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DataReleaseId = table.Column<int>(type: "int", nullable: false),
                TriggerType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                RetryOfImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RecordsFetched = table.Column<int>(type: "int", nullable: false),
                RecordsStaged = table.Column<int>(type: "int", nullable: false),
                RecordsInserted = table.Column<int>(type: "int", nullable: false),
                RecordsRejected = table.Column<int>(type: "int", nullable: false),
                ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PipelineVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportRun", x => x.Id);
                table.CheckConstraint(
                    "CK_ImportRun_RecordCounts",
                    "[RecordsFetched] >= 0 AND [RecordsStaged] >= 0 AND [RecordsInserted] >= 0 AND [RecordsRejected] >= 0");
                table.CheckConstraint(
                    "CK_ImportRun_Completion",
                    "([Status] IN ('Queued', 'Running') AND [CompletedAtUtc] IS NULL) OR ([Status] IN ('Succeeded', 'SucceededWithWarnings', 'Failed') AND [CompletedAtUtc] IS NOT NULL)");
                table.UniqueConstraint(
                    "AK_ImportRun_DataReleaseId_Id",
                    x => new { x.DataReleaseId, x.Id });
                table.ForeignKey(
                    name: "FK_ImportRun_DataRelease_DataReleaseId",
                    column: x => x.DataReleaseId,
                    principalSchema: "mci_ops",
                    principalTable: "DataRelease",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ImportRun_ImportRun_RetryOfImportRunId",
                    column: x => x.RetryOfImportRunId,
                    principalSchema: "mci_ops",
                    principalTable: "ImportRun",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AcsCountyVariable",
            schema: "mci_stg",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CountyFipsCode = table.Column<string>(type: "char(5)", fixedLength: true, maxLength: 5, nullable: false),
                CountyNameRaw = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                SourceVariableCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                EstimateRaw = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                MarginOfErrorRaw = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                AnnotationRaw = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                SourceRowHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                StagedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AcsCountyVariable", x => x.Id);
                table.ForeignKey(
                    name: "FK_AcsCountyVariable_ImportRun_ImportRunId",
                    column: x => x.ImportRunId,
                    principalSchema: "mci_ops",
                    principalTable: "ImportRun",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ImportIssue",
            schema: "mci_ops",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Stage = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                Severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                IssueCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                CountyFipsCode = table.Column<string>(type: "char(5)", fixedLength: true, maxLength: 5, nullable: true),
                MetricCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                RawValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportIssue", x => x.Id);
                table.ForeignKey(
                    name: "FK_ImportIssue_ImportRun_ImportRunId",
                    column: x => x.ImportRunId,
                    principalSchema: "mci_ops",
                    principalTable: "ImportRun",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CountyMetricObservation",
            schema: "mci_fact",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CountyId = table.Column<int>(type: "int", nullable: false),
                MetricDefinitionId = table.Column<int>(type: "int", nullable: false),
                DataReleaseId = table.Column<int>(type: "int", nullable: false),
                ImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EstimateValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                MarginOfError = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                CalculationVersion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountyMetricObservation", x => x.Id);
                table.CheckConstraint(
                    "CK_CountyMetricObservation_MarginOfError",
                    "[MarginOfError] IS NULL OR [MarginOfError] >= 0");
                table.ForeignKey(
                    name: "FK_CountyMetricObservation_County_CountyId",
                    column: x => x.CountyId,
                    principalSchema: "mci_ref",
                    principalTable: "County",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CountyMetricObservation_DataRelease_DataReleaseId",
                    column: x => x.DataReleaseId,
                    principalSchema: "mci_ops",
                    principalTable: "DataRelease",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CountyMetricObservation_ImportRun_DataReleaseId_ImportRunId",
                    columns: x => new { x.DataReleaseId, x.ImportRunId },
                    principalSchema: "mci_ops",
                    principalTable: "ImportRun",
                    principalColumns: new[] { "DataReleaseId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CountyMetricObservation_MetricDefinition_MetricDefinitionId",
                    column: x => x.MetricDefinitionId,
                    principalSchema: "mci_catalog",
                    principalTable: "MetricDefinition",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_County_FipsCode",
            schema: "mci_ref",
            table: "County",
            column: "FipsCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MetricDefinition_Code",
            schema: "mci_catalog",
            table: "MetricDefinition",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DataRelease_SourceCode_DatasetCode_ReleaseYear",
            schema: "mci_ops",
            table: "DataRelease",
            columns: new[] { "SourceCode", "DatasetCode", "ReleaseYear" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DataRelease_SourceCode_DatasetCode",
            schema: "mci_ops",
            table: "DataRelease",
            columns: new[] { "SourceCode", "DatasetCode" },
            unique: true,
            filter: "[IsDefault] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_ImportRun_DataReleaseId_Status_CompletedAtUtc",
            schema: "mci_ops",
            table: "ImportRun",
            columns: new[] { "DataReleaseId", "Status", "CompletedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ImportRun_RetryOfImportRunId",
            schema: "mci_ops",
            table: "ImportRun",
            column: "RetryOfImportRunId");

        migrationBuilder.CreateIndex(
            name: "IX_AcsCountyVariable_ImportRunId_CountyFipsCode_SourceVariableCode",
            schema: "mci_stg",
            table: "AcsCountyVariable",
            columns: new[] { "ImportRunId", "CountyFipsCode", "SourceVariableCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ImportIssue_ImportRunId_Severity_Stage",
            schema: "mci_ops",
            table: "ImportIssue",
            columns: new[] { "ImportRunId", "Severity", "Stage" });

        migrationBuilder.CreateIndex(
            name: "IX_CountyMetricObservation_ImportRunId_CountyId_MetricDefinitionId",
            schema: "mci_fact",
            table: "CountyMetricObservation",
            columns: new[] { "ImportRunId", "CountyId", "MetricDefinitionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountyMetricObservation_CountyId_MetricDefinitionId_DataReleaseId",
            schema: "mci_fact",
            table: "CountyMetricObservation",
            columns: new[] { "CountyId", "MetricDefinitionId", "DataReleaseId" });

        migrationBuilder.CreateIndex(
            name: "IX_CountyMetricObservation_MetricDefinitionId_DataReleaseId_EstimateValue",
            schema: "mci_fact",
            table: "CountyMetricObservation",
            columns: new[] { "MetricDefinitionId", "DataReleaseId", "EstimateValue" });

        migrationBuilder.CreateIndex(
            name: "IX_CountyMetricObservation_DataReleaseId_ImportRunId",
            schema: "mci_fact",
            table: "CountyMetricObservation",
            columns: new[] { "DataReleaseId", "ImportRunId" });

        var seedDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);

        migrationBuilder.InsertData(
            schema: "mci_catalog",
            table: "MetricDefinition",
            columns: new[]
            {
                "Id", "Code", "DisplayName", "Description", "Category", "Unit", "DecimalPlaces",
                "CalculationType", "ComparisonGuidance", "RequiresDollarNormalization",
                "SupportsAdjacentReleaseComparison", "IsActive", "CreatedAtUtc"
            },
            columnTypes: new[]
            {
                "int", "varchar(100)", "nvarchar(150)", "nvarchar(500)", "nvarchar(50)", "varchar(30)",
                "tinyint", "varchar(30)", "nvarchar(500)", "bit", "bit", "bit", "datetime2"
            },
            values: new object[,]
            {
                { 1, "population", "Population", "Total population estimate for the county.", "Demographics", "Count", (byte)0, "DirectSource", "Show as a rolling ACS 5-Year estimate. Do not label adjacent release changes as year-over-year growth.", false, false, true, seedDate },
                { 2, "median_household_income", "Median Household Income", "Median household income in the past 12 months, reported by ACS.", "Economy", "Currency", (byte)0, "DirectSource", "Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.", true, false, true, seedDate },
                { 3, "per_capita_income", "Per Capita Income", "Per capita income in the past 12 months, reported by ACS.", "Economy", "Currency", (byte)0, "DirectSource", "Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.", true, false, true, seedDate },
                { 4, "poverty_rate", "Poverty Rate", "Share of the population for whom poverty status is determined that is below the poverty level.", "Economy", "Percentage", (byte)1, "Derived", "Compare counties within the same ACS release. Use non-overlapping periods for change comparisons.", false, false, true, seedDate },
                { 5, "labor_force_participation_rate", "Labor Force Participation Rate", "Share of the civilian population age 16 and over that is in the labor force.", "Economy", "Percentage", (byte)1, "Derived", "Compare counties within the same ACS release. Use non-overlapping periods for change comparisons.", false, false, true, seedDate },
                { 6, "bachelors_degree_or_higher_rate", "Bachelor's Degree or Higher", "Share of the population age 25 and over with a bachelor's, master's, professional, or doctorate degree.", "Education", "Percentage", (byte)1, "Derived", "Compare counties within the same ACS release. Use non-overlapping periods for change comparisons.", false, false, true, seedDate },
                { 7, "median_home_value", "Median Home Value", "Median value of owner-occupied housing units.", "Housing", "Currency", (byte)0, "DirectSource", "Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.", true, false, true, seedDate },
                { 8, "median_gross_rent", "Median Gross Rent", "Median gross rent for renter-occupied housing units paying cash rent.", "Housing", "Currency", (byte)0, "DirectSource", "Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.", true, false, true, seedDate }
            });

        migrationBuilder.InsertData(
            schema: "mci_ref",
            table: "County",
            columns: new[]
            {
                "Id", "FipsCode", "Name", "StateFipsCode", "StateCode", "StateName", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"
            },
            columnTypes: new[]
            {
                "int", "char(5)", "nvarchar(100)", "char(2)", "char(2)", "nvarchar(50)", "bit", "datetime2", "datetime2"
            },
            values: new object[,]
            {
                { 1, "26001", "Alcona", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 2, "26003", "Alger", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 3, "26005", "Allegan", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 4, "26007", "Alpena", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 5, "26009", "Antrim", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 6, "26011", "Arenac", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 7, "26013", "Baraga", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 8, "26015", "Barry", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 9, "26017", "Bay", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 10, "26019", "Benzie", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 11, "26021", "Berrien", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 12, "26023", "Branch", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 13, "26025", "Calhoun", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 14, "26027", "Cass", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 15, "26029", "Charlevoix", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 16, "26031", "Cheboygan", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 17, "26033", "Chippewa", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 18, "26035", "Clare", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 19, "26037", "Clinton", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 20, "26039", "Crawford", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 21, "26041", "Delta", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 22, "26043", "Dickinson", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 23, "26045", "Eaton", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 24, "26047", "Emmet", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 25, "26049", "Genesee", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 26, "26051", "Gladwin", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 27, "26053", "Gogebic", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 28, "26055", "Grand Traverse", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 29, "26057", "Gratiot", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 30, "26059", "Hillsdale", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 31, "26061", "Houghton", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 32, "26063", "Huron", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 33, "26065", "Ingham", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 34, "26067", "Ionia", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 35, "26069", "Iosco", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 36, "26071", "Iron", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 37, "26073", "Isabella", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 38, "26075", "Jackson", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 39, "26077", "Kalamazoo", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 40, "26079", "Kalkaska", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 41, "26081", "Kent", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 42, "26083", "Keweenaw", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 43, "26085", "Lake", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 44, "26087", "Lapeer", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 45, "26089", "Leelanau", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 46, "26091", "Lenawee", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 47, "26093", "Livingston", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 48, "26095", "Luce", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 49, "26097", "Mackinac", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 50, "26099", "Macomb", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 51, "26101", "Manistee", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 52, "26103", "Marquette", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 53, "26105", "Mason", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 54, "26107", "Mecosta", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 55, "26109", "Menominee", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 56, "26111", "Midland", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 57, "26113", "Missaukee", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 58, "26115", "Monroe", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 59, "26117", "Montcalm", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 60, "26119", "Montmorency", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 61, "26121", "Muskegon", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 62, "26123", "Newaygo", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 63, "26125", "Oakland", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 64, "26127", "Oceana", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 65, "26129", "Ogemaw", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 66, "26131", "Ontonagon", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 67, "26133", "Osceola", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 68, "26135", "Oscoda", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 69, "26137", "Otsego", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 70, "26139", "Ottawa", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 71, "26141", "Presque Isle", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 72, "26143", "Roscommon", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 73, "26145", "Saginaw", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 74, "26147", "St. Clair", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 75, "26149", "St. Joseph", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 76, "26151", "Sanilac", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 77, "26153", "Schoolcraft", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 78, "26155", "Shiawassee", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 79, "26157", "Tuscola", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 80, "26159", "Van Buren", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 81, "26161", "Washtenaw", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 82, "26163", "Wayne", "26", "MI", "Michigan", true, seedDate, seedDate },
                { 83, "26165", "Wexford", "26", "MI", "Michigan", true, seedDate, seedDate }
            });

        migrationBuilder.Sql(
            """
            CREATE OR ALTER VIEW [mci_reporting].[vw_CurrentCountyMetricObservation]
            AS
            WITH RankedSuccessfulImports AS
            (
                SELECT
                    [Id],
                    [DataReleaseId],
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY [DataReleaseId]
                        ORDER BY [CompletedAtUtc] DESC, [CreatedAtUtc] DESC
                    ) AS [ImportRank]
                FROM [mci_ops].[ImportRun]
                WHERE [Status] IN (N'Succeeded', N'SucceededWithWarnings')
            )
            SELECT
                observation.[Id] AS [ObservationId],
                county.[Id] AS [CountyId],
                county.[FipsCode] AS [CountyFipsCode],
                county.[Name] AS [CountyName],
                metric.[Id] AS [MetricDefinitionId],
                metric.[Code] AS [MetricCode],
                metric.[DisplayName] AS [MetricDisplayName],
                metric.[Category],
                metric.[Unit],
                metric.[DecimalPlaces],
                release.[Id] AS [DataReleaseId],
                release.[ReleaseYear],
                release.[PeriodStartYear],
                release.[PeriodEndYear],
                release.[DisplayName] AS [DataReleaseDisplayName],
                observation.[EstimateValue],
                observation.[MarginOfError],
                importRun.[Id] AS [ImportRunId],
                importRun.[CompletedAtUtc] AS [ImportedAtUtc]
            FROM [mci_fact].[CountyMetricObservation] AS observation
            INNER JOIN RankedSuccessfulImports AS rankedImport
                ON rankedImport.[Id] = observation.[ImportRunId]
               AND rankedImport.[ImportRank] = 1
            INNER JOIN [mci_ref].[County] AS county
                ON county.[Id] = observation.[CountyId]
            INNER JOIN [mci_catalog].[MetricDefinition] AS metric
                ON metric.[Id] = observation.[MetricDefinitionId]
            INNER JOIN [mci_ops].[DataRelease] AS release
                ON release.[Id] = observation.[DataReleaseId]
            INNER JOIN [mci_ops].[ImportRun] AS importRun
                ON importRun.[Id] = observation.[ImportRunId]
               AND importRun.[DataReleaseId] = observation.[DataReleaseId]
            WHERE county.[IsActive] = 1
              AND metric.[IsActive] = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS [mci_reporting].[vw_CurrentCountyMetricObservation];");

        migrationBuilder.DropTable(
            name: "AcsCountyVariable",
            schema: "mci_stg");

        migrationBuilder.DropTable(
            name: "CountyMetricObservation",
            schema: "mci_fact");

        migrationBuilder.DropTable(
            name: "ImportIssue",
            schema: "mci_ops");

        migrationBuilder.DropTable(
            name: "County",
            schema: "mci_ref");

        migrationBuilder.DropTable(
            name: "MetricDefinition",
            schema: "mci_catalog");

        migrationBuilder.DropTable(
            name: "ImportRun",
            schema: "mci_ops");

        migrationBuilder.DropTable(
            name: "DataRelease",
            schema: "mci_ops");

        migrationBuilder.Sql("DROP SCHEMA [mci_reporting];");
        migrationBuilder.Sql("DROP SCHEMA [mci_stg];");
        migrationBuilder.Sql("DROP SCHEMA [mci_fact];");
        migrationBuilder.Sql("DROP SCHEMA [mci_ref];");
        migrationBuilder.Sql("DROP SCHEMA [mci_catalog];");
        migrationBuilder.Sql("DROP SCHEMA [mci_ops];");
    }
}
