using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class MetricDefinitionConfiguration : IEntityTypeConfiguration<MetricDefinition>
{
    public void Configure(EntityTypeBuilder<MetricDefinition> builder)
    {
        builder.ToTable("MetricDefinition", MciSchemaNames.Catalog);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Unit)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.DecimalPlaces).IsRequired();
        builder.Property(x => x.CalculationType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.ComparisonGuidance).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
