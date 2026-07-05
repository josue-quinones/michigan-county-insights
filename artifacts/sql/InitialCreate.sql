IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF SCHEMA_ID(N'mci_catalog') IS NULL EXEC(N'CREATE SCHEMA [mci_catalog];');

IF SCHEMA_ID(N'mci_fact') IS NULL EXEC(N'CREATE SCHEMA [mci_fact];');

IF SCHEMA_ID(N'mci_ops') IS NULL EXEC(N'CREATE SCHEMA [mci_ops];');

IF SCHEMA_ID(N'mci_ref') IS NULL EXEC(N'CREATE SCHEMA [mci_ref];');

IF SCHEMA_ID(N'mci_reporting') IS NULL EXEC(N'CREATE SCHEMA [mci_reporting];');

IF SCHEMA_ID(N'mci_stg') IS NULL EXEC(N'CREATE SCHEMA [mci_stg];');

CREATE TABLE [mci_ref].[County] (
    [Id] int NOT NULL IDENTITY,
    [FipsCode] char(5) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [StateFipsCode] char(2) NOT NULL,
    [StateCode] char(2) NOT NULL,
    [StateName] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_County] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_County_FipsCode_Length] CHECK (LEN([FipsCode]) = 5)
);

CREATE TABLE [mci_catalog].[MetricDefinition] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(100) NOT NULL,
    [DisplayName] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Category] nvarchar(50) NOT NULL,
    [Unit] varchar(30) NOT NULL,
    [DecimalPlaces] tinyint NOT NULL,
    [CalculationType] varchar(30) NOT NULL,
    [ComparisonGuidance] nvarchar(500) NOT NULL,
    [RequiresDollarNormalization] bit NOT NULL,
    [SupportsAdjacentReleaseComparison] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_MetricDefinition] PRIMARY KEY ([Id])
);

CREATE TABLE [mci_ops].[DataRelease] (
    [Id] int NOT NULL IDENTITY,
    [SourceCode] varchar(50) NOT NULL,
    [DatasetCode] varchar(100) NOT NULL,
    [ReleaseYear] smallint NOT NULL,
    [PeriodStartYear] smallint NOT NULL,
    [PeriodEndYear] smallint NOT NULL,
    [DisplayName] nvarchar(150) NOT NULL,
    [IsDefault] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_DataRelease] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_DataRelease_Period] CHECK ([PeriodStartYear] <= [PeriodEndYear] AND [PeriodEndYear] = [ReleaseYear])
);

CREATE TABLE [mci_ops].[ImportRun] (
    [Id] uniqueidentifier NOT NULL,
    [DataReleaseId] int NOT NULL,
    [TriggerType] varchar(30) NOT NULL,
    [Status] varchar(30) NOT NULL,
    [RetryOfImportRunId] uniqueidentifier NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [RecordsFetched] int NOT NULL,
    [RecordsStaged] int NOT NULL,
    [RecordsInserted] int NOT NULL,
    [RecordsRejected] int NOT NULL,
    [ErrorSummary] nvarchar(max) NULL,
    [PipelineVersion] nvarchar(100) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ImportRun] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_ImportRun_DataReleaseId_Id] UNIQUE ([DataReleaseId], [Id]),
    CONSTRAINT [CK_ImportRun_RecordCounts] CHECK ([RecordsFetched] >= 0 AND [RecordsStaged] >= 0 AND [RecordsInserted] >= 0 AND [RecordsRejected] >= 0),
    CONSTRAINT [CK_ImportRun_Completion] CHECK (([Status] IN ('Queued', 'Running') AND [CompletedAtUtc] IS NULL) OR ([Status] IN ('Succeeded', 'SucceededWithWarnings', 'Failed') AND [CompletedAtUtc] IS NOT NULL)),
    CONSTRAINT [FK_ImportRun_DataRelease_DataReleaseId] FOREIGN KEY ([DataReleaseId]) REFERENCES [mci_ops].[DataRelease] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ImportRun_ImportRun_RetryOfImportRunId] FOREIGN KEY ([RetryOfImportRunId]) REFERENCES [mci_ops].[ImportRun] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [mci_stg].[AcsCountyVariable] (
    [Id] bigint NOT NULL IDENTITY,
    [ImportRunId] uniqueidentifier NOT NULL,
    [CountyFipsCode] char(5) NOT NULL,
    [CountyNameRaw] nvarchar(150) NOT NULL,
    [SourceVariableCode] varchar(30) NOT NULL,
    [EstimateRaw] nvarchar(100) NOT NULL,
    [MarginOfErrorRaw] nvarchar(100) NULL,
    [AnnotationRaw] nvarchar(250) NULL,
    [SourceRowHash] char(64) NOT NULL,
    [StagedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AcsCountyVariable] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AcsCountyVariable_ImportRun_ImportRunId] FOREIGN KEY ([ImportRunId]) REFERENCES [mci_ops].[ImportRun] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [mci_ops].[ImportIssue] (
    [Id] bigint NOT NULL IDENTITY,
    [ImportRunId] uniqueidentifier NOT NULL,
    [Stage] varchar(30) NOT NULL,
    [Severity] varchar(20) NOT NULL,
    [IssueCode] varchar(100) NOT NULL,
    [CountyFipsCode] char(5) NULL,
    [MetricCode] varchar(100) NULL,
    [RawValue] nvarchar(100) NULL,
    [Message] nvarchar(1000) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ImportIssue] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImportIssue_ImportRun_ImportRunId] FOREIGN KEY ([ImportRunId]) REFERENCES [mci_ops].[ImportRun] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [mci_fact].[CountyMetricObservation] (
    [Id] bigint NOT NULL IDENTITY,
    [CountyId] int NOT NULL,
    [MetricDefinitionId] int NOT NULL,
    [DataReleaseId] int NOT NULL,
    [ImportRunId] uniqueidentifier NOT NULL,
    [EstimateValue] decimal(19,4) NOT NULL,
    [MarginOfError] decimal(19,4) NULL,
    [CalculationVersion] varchar(50) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_CountyMetricObservation] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_CountyMetricObservation_MarginOfError] CHECK ([MarginOfError] IS NULL OR [MarginOfError] >= 0),
    CONSTRAINT [FK_CountyMetricObservation_County_CountyId] FOREIGN KEY ([CountyId]) REFERENCES [mci_ref].[County] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CountyMetricObservation_DataRelease_DataReleaseId] FOREIGN KEY ([DataReleaseId]) REFERENCES [mci_ops].[DataRelease] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CountyMetricObservation_ImportRun_DataReleaseId_ImportRunId] FOREIGN KEY ([DataReleaseId], [ImportRunId]) REFERENCES [mci_ops].[ImportRun] ([DataReleaseId], [Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CountyMetricObservation_MetricDefinition_MetricDefinitionId] FOREIGN KEY ([MetricDefinitionId]) REFERENCES [mci_catalog].[MetricDefinition] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_County_FipsCode] ON [mci_ref].[County] ([FipsCode]) WHERE [FipsCode] IS NOT NULL;

CREATE UNIQUE INDEX [IX_MetricDefinition_Code] ON [mci_catalog].[MetricDefinition] ([Code]) WHERE [Code] IS NOT NULL;

CREATE UNIQUE INDEX [IX_DataRelease_SourceCode_DatasetCode_ReleaseYear] ON [mci_ops].[DataRelease] ([SourceCode], [DatasetCode], [ReleaseYear]) WHERE [SourceCode] IS NOT NULL AND [DatasetCode] IS NOT NULL AND [ReleaseYear] IS NOT NULL;

CREATE UNIQUE INDEX [IX_DataRelease_SourceCode_DatasetCode] ON [mci_ops].[DataRelease] ([SourceCode], [DatasetCode]) WHERE [IsDefault] = 1;

CREATE INDEX [IX_ImportRun_DataReleaseId_Status_CompletedAtUtc] ON [mci_ops].[ImportRun] ([DataReleaseId], [Status], [CompletedAtUtc]);

CREATE INDEX [IX_ImportRun_RetryOfImportRunId] ON [mci_ops].[ImportRun] ([RetryOfImportRunId]);

CREATE UNIQUE INDEX [IX_AcsCountyVariable_ImportRunId_CountyFipsCode_SourceVariableCode] ON [mci_stg].[AcsCountyVariable] ([ImportRunId], [CountyFipsCode], [SourceVariableCode]) WHERE [ImportRunId] IS NOT NULL AND [CountyFipsCode] IS NOT NULL AND [SourceVariableCode] IS NOT NULL;

CREATE INDEX [IX_ImportIssue_ImportRunId_Severity_Stage] ON [mci_ops].[ImportIssue] ([ImportRunId], [Severity], [Stage]);

CREATE UNIQUE INDEX [IX_CountyMetricObservation_ImportRunId_CountyId_MetricDefinitionId] ON [mci_fact].[CountyMetricObservation] ([ImportRunId], [CountyId], [MetricDefinitionId]) WHERE [ImportRunId] IS NOT NULL AND [CountyId] IS NOT NULL AND [MetricDefinitionId] IS NOT NULL;

CREATE INDEX [IX_CountyMetricObservation_CountyId_MetricDefinitionId_DataReleaseId] ON [mci_fact].[CountyMetricObservation] ([CountyId], [MetricDefinitionId], [DataReleaseId]);

CREATE INDEX [IX_CountyMetricObservation_MetricDefinitionId_DataReleaseId_EstimateValue] ON [mci_fact].[CountyMetricObservation] ([MetricDefinitionId], [DataReleaseId], [EstimateValue]);

CREATE INDEX [IX_CountyMetricObservation_DataReleaseId_ImportRunId] ON [mci_fact].[CountyMetricObservation] ([DataReleaseId], [ImportRunId]);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DisplayName', N'Description', N'Category', N'Unit', N'DecimalPlaces', N'CalculationType', N'ComparisonGuidance', N'RequiresDollarNormalization', N'SupportsAdjacentReleaseComparison', N'IsActive', N'CreatedAtUtc') AND [object_id] = OBJECT_ID(N'[mci_catalog].[MetricDefinition]'))
    SET IDENTITY_INSERT [mci_catalog].[MetricDefinition] ON;
INSERT INTO [mci_catalog].[MetricDefinition] ([Id], [Code], [DisplayName], [Description], [Category], [Unit], [DecimalPlaces], [CalculationType], [ComparisonGuidance], [RequiresDollarNormalization], [SupportsAdjacentReleaseComparison], [IsActive], [CreatedAtUtc])
VALUES (1, 'population', N'Population', N'Total population estimate for the county.', N'Demographics', 'Count', CAST(0 AS tinyint), 'DirectSource', N'Show as a rolling ACS 5-Year estimate. Do not label adjacent release changes as year-over-year growth.', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(2, 'median_household_income', N'Median Household Income', N'Median household income in the past 12 months, reported by ACS.', N'Economy', 'Currency', CAST(0 AS tinyint), 'DirectSource', N'Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(3, 'per_capita_income', N'Per Capita Income', N'Per capita income in the past 12 months, reported by ACS.', N'Economy', 'Currency', CAST(0 AS tinyint), 'DirectSource', N'Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(4, 'poverty_rate', N'Poverty Rate', N'Share of the population for whom poverty status is determined that is below the poverty level.', N'Economy', 'Percentage', CAST(1 AS tinyint), 'Derived', N'Compare counties within the same ACS release. Use non-overlapping periods for change comparisons.', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(5, 'labor_force_participation_rate', N'Labor Force Participation Rate', N'Share of the civilian population age 16 and over that is in the labor force.', N'Economy', 'Percentage', CAST(1 AS tinyint), 'Derived', N'Compare counties within the same ACS release. Use non-overlapping periods for change comparisons.', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(6, 'bachelors_degree_or_higher_rate', N'Bachelor''s Degree or Higher', N'Share of the population age 25 and over with a bachelor''s, master''s, professional, or doctorate degree.', N'Education', 'Percentage', CAST(1 AS tinyint), 'Derived', N'Compare counties within the same ACS release. Use non-overlapping periods for change comparisons.', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(7, 'median_home_value', N'Median Home Value', N'Median value of owner-occupied housing units.', N'Housing', 'Currency', CAST(0 AS tinyint), 'DirectSource', N'Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z'),
(8, 'median_gross_rent', N'Median Gross Rent', N'Median gross rent for renter-occupied housing units paying cash rent.', N'Housing', 'Currency', CAST(0 AS tinyint), 'DirectSource', N'Show release periods clearly. Cross-period dollar comparisons are not inflation-adjusted in V1.', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DisplayName', N'Description', N'Category', N'Unit', N'DecimalPlaces', N'CalculationType', N'ComparisonGuidance', N'RequiresDollarNormalization', N'SupportsAdjacentReleaseComparison', N'IsActive', N'CreatedAtUtc') AND [object_id] = OBJECT_ID(N'[mci_catalog].[MetricDefinition]'))
    SET IDENTITY_INSERT [mci_catalog].[MetricDefinition] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'FipsCode', N'Name', N'StateFipsCode', N'StateCode', N'StateName', N'IsActive', N'CreatedAtUtc', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[mci_ref].[County]'))
    SET IDENTITY_INSERT [mci_ref].[County] ON;
INSERT INTO [mci_ref].[County] ([Id], [FipsCode], [Name], [StateFipsCode], [StateCode], [StateName], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
VALUES (1, '26001', N'Alcona', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(2, '26003', N'Alger', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(3, '26005', N'Allegan', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(4, '26007', N'Alpena', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(5, '26009', N'Antrim', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(6, '26011', N'Arenac', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(7, '26013', N'Baraga', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(8, '26015', N'Barry', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(9, '26017', N'Bay', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(10, '26019', N'Benzie', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(11, '26021', N'Berrien', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(12, '26023', N'Branch', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(13, '26025', N'Calhoun', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(14, '26027', N'Cass', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(15, '26029', N'Charlevoix', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(16, '26031', N'Cheboygan', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(17, '26033', N'Chippewa', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(18, '26035', N'Clare', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(19, '26037', N'Clinton', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(20, '26039', N'Crawford', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(21, '26041', N'Delta', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(22, '26043', N'Dickinson', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(23, '26045', N'Eaton', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(24, '26047', N'Emmet', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(25, '26049', N'Genesee', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(26, '26051', N'Gladwin', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(27, '26053', N'Gogebic', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(28, '26055', N'Grand Traverse', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(29, '26057', N'Gratiot', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(30, '26059', N'Hillsdale', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(31, '26061', N'Houghton', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(32, '26063', N'Huron', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(33, '26065', N'Ingham', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(34, '26067', N'Ionia', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(35, '26069', N'Iosco', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(36, '26071', N'Iron', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(37, '26073', N'Isabella', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(38, '26075', N'Jackson', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(39, '26077', N'Kalamazoo', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(40, '26079', N'Kalkaska', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(41, '26081', N'Kent', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(42, '26083', N'Keweenaw', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z');
INSERT INTO [mci_ref].[County] ([Id], [FipsCode], [Name], [StateFipsCode], [StateCode], [StateName], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
VALUES (43, '26085', N'Lake', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(44, '26087', N'Lapeer', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(45, '26089', N'Leelanau', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(46, '26091', N'Lenawee', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(47, '26093', N'Livingston', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(48, '26095', N'Luce', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(49, '26097', N'Mackinac', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(50, '26099', N'Macomb', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(51, '26101', N'Manistee', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(52, '26103', N'Marquette', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(53, '26105', N'Mason', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(54, '26107', N'Mecosta', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(55, '26109', N'Menominee', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(56, '26111', N'Midland', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(57, '26113', N'Missaukee', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(58, '26115', N'Monroe', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(59, '26117', N'Montcalm', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(60, '26119', N'Montmorency', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(61, '26121', N'Muskegon', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(62, '26123', N'Newaygo', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(63, '26125', N'Oakland', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(64, '26127', N'Oceana', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(65, '26129', N'Ogemaw', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(66, '26131', N'Ontonagon', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(67, '26133', N'Osceola', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(68, '26135', N'Oscoda', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(69, '26137', N'Otsego', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(70, '26139', N'Ottawa', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(71, '26141', N'Presque Isle', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(72, '26143', N'Roscommon', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(73, '26145', N'Saginaw', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(74, '26147', N'St. Clair', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(75, '26149', N'St. Joseph', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(76, '26151', N'Sanilac', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(77, '26153', N'Schoolcraft', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(78, '26155', N'Shiawassee', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(79, '26157', N'Tuscola', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(80, '26159', N'Van Buren', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(81, '26161', N'Washtenaw', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(82, '26163', N'Wayne', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z'),
(83, '26165', N'Wexford', '26', 'MI', N'Michigan', CAST(1 AS bit), '2026-07-05T00:00:00.0000000Z', '2026-07-05T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'FipsCode', N'Name', N'StateFipsCode', N'StateCode', N'StateName', N'IsActive', N'CreatedAtUtc', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[mci_ref].[County]'))
    SET IDENTITY_INSERT [mci_ref].[County] OFF;

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'202607050001_InitialCreate', N'9.0.6');

COMMIT;
GO

