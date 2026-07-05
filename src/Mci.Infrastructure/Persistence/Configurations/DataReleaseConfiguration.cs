using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class DataReleaseConfiguration : IEntityTypeConfiguration<DataRelease>
{
    public void Configure(EntityTypeBuilder<DataRelease> builder)
    {
        builder.ToTable("DataRelease", MciSchemaNames.Ops, table =>
        {
            table.HasCheckConstraint(
                "CK_DataRelease_Period",
                "[PeriodStartYear] <= [PeriodEndYear] AND [PeriodEndYear] = [ReleaseYear]");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceCode).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.DatasetCode).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => new { x.SourceCode, x.DatasetCode, x.ReleaseYear }).IsUnique();
        builder.HasIndex(x => new { x.SourceCode, x.DatasetCode })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");
    }
}
