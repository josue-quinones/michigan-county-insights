using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class CurrentCountyMetricObservationConfiguration
    : IEntityTypeConfiguration<CurrentCountyMetricObservation>
{
    public void Configure(EntityTypeBuilder<CurrentCountyMetricObservation> builder)
    {
        builder.HasNoKey();
        builder.ToView("vw_CurrentCountyMetricObservation", MciSchemaNames.Reporting);

        builder.Property(x => x.CountyFipsCode).HasColumnType("char(5)");
        builder.Property(x => x.CountyName).HasMaxLength(100);
        builder.Property(x => x.MetricCode).HasMaxLength(100);
        builder.Property(x => x.MetricDisplayName).HasMaxLength(150);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.Property(x => x.Unit).HasMaxLength(30);
        builder.Property(x => x.EstimateValue).HasPrecision(19, 4);
        builder.Property(x => x.MarginOfError).HasPrecision(19, 4);
        builder.Property(x => x.DataReleaseDisplayName).HasMaxLength(150);
    }
}
