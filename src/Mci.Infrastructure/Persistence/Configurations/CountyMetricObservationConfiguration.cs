using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class CountyMetricObservationConfiguration : IEntityTypeConfiguration<CountyMetricObservation>
{
    public void Configure(EntityTypeBuilder<CountyMetricObservation> builder)
    {
        builder.ToTable("CountyMetricObservation", MciSchemaNames.Fact, table =>
        {
            table.HasCheckConstraint(
                "CK_CountyMetricObservation_MarginOfError",
                "[MarginOfError] IS NULL OR [MarginOfError] >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EstimateValue).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.MarginOfError).HasPrecision(19, 4);
        builder.Property(x => x.CalculationVersion).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.County)
            .WithMany(x => x.Observations)
            .HasForeignKey(x => x.CountyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MetricDefinition)
            .WithMany(x => x.Observations)
            .HasForeignKey(x => x.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DataRelease)
            .WithMany(x => x.Observations)
            .HasForeignKey(x => x.DataReleaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ImportRun)
            .WithMany(x => x.Observations)
            .HasForeignKey(x => new { x.DataReleaseId, x.ImportRunId })
            .HasPrincipalKey(x => new { x.DataReleaseId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ImportRunId, x.CountyId, x.MetricDefinitionId }).IsUnique();
        builder.HasIndex(x => new { x.CountyId, x.MetricDefinitionId, x.DataReleaseId });
        builder.HasIndex(x => new { x.MetricDefinitionId, x.DataReleaseId, x.EstimateValue });
        builder.HasIndex(x => new { x.DataReleaseId, x.ImportRunId });
    }
}
