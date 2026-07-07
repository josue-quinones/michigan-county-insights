using Mci.Core.Domain.Entities;
using Mci.Infrastructure.Persistence.Configurations;
using Mci.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Mci.Infrastructure.Persistence;

public sealed class MciDbContext(DbContextOptions<MciDbContext> options) : DbContext(options)
{
    public DbSet<County> Counties => Set<County>();
    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();
    public DbSet<DataRelease> DataReleases => Set<DataRelease>();
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    public DbSet<ImportIssue> ImportIssues => Set<ImportIssue>();
    public DbSet<AcsCountyVariable> AcsCountyVariables => Set<AcsCountyVariable>();
    public DbSet<CountyMetricObservation> CountyMetricObservations => Set<CountyMetricObservation>();

    public DbSet<CurrentCountyMetricObservation> CurrentCountyMetricObservations =>
        Set<CurrentCountyMetricObservation>();

    public DbSet<ClarityInsightSnapshot> ClarityInsightSnapshots => Set<ClarityInsightSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CountyConfiguration());
        modelBuilder.ApplyConfiguration(new MetricDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new DataReleaseConfiguration());
        modelBuilder.ApplyConfiguration(new ImportRunConfiguration());
        modelBuilder.ApplyConfiguration(new ImportIssueConfiguration());
        modelBuilder.ApplyConfiguration(new AcsCountyVariableConfiguration());
        modelBuilder.ApplyConfiguration(new CountyMetricObservationConfiguration());
        modelBuilder.ApplyConfiguration(new CurrentCountyMetricObservationConfiguration());
        modelBuilder.ApplyConfiguration(new ClarityInsightSnapshotConfiguration());

        modelBuilder.Entity<County>().HasData(MciReferenceData.Counties);
        modelBuilder.Entity<MetricDefinition>().HasData(MciReferenceData.MetricDefinitions);
        modelBuilder.Entity<DataRelease>().HasData(MciReferenceData.DataReleases);
    }
}
