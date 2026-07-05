using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class AcsCountyVariableConfiguration : IEntityTypeConfiguration<AcsCountyVariable>
{
    public void Configure(EntityTypeBuilder<AcsCountyVariable> builder)
    {
        builder.ToTable("AcsCountyVariable", MciSchemaNames.Staging);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountyFipsCode)
            .HasColumnType("char(5)")
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(x => x.CountyNameRaw).HasMaxLength(150).IsRequired();
        builder.Property(x => x.SourceVariableCode).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.EstimateRaw).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MarginOfErrorRaw).HasMaxLength(100);
        builder.Property(x => x.AnnotationRaw).HasMaxLength(250);
        builder.Property(x => x.SourceRowHash)
            .HasColumnType("char(64)")
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(x => x.StagedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.ImportRun)
            .WithMany(x => x.AcsCountyVariables)
            .HasForeignKey(x => x.ImportRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ImportRunId, x.CountyFipsCode, x.SourceVariableCode }).IsUnique();
    }
}
