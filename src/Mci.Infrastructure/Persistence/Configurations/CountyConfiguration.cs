using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class CountyConfiguration : IEntityTypeConfiguration<County>
{
    public void Configure(EntityTypeBuilder<County> builder)
    {
        builder.ToTable("County", MciSchemaNames.Ref, table =>
        {
            table.HasCheckConstraint("CK_County_FipsCode_Length", "LEN([FipsCode]) = 5");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.FipsCode)
            .HasColumnType("char(5)")
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StateFipsCode)
            .HasColumnType("char(2)")
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(x => x.StateCode)
            .HasColumnType("char(2)")
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(x => x.StateName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.FipsCode).IsUnique();
    }
}
