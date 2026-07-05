using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class ImportIssueConfiguration : IEntityTypeConfiguration<ImportIssue>
{
    public void Configure(EntityTypeBuilder<ImportIssue> builder)
    {
        builder.ToTable("ImportIssue", MciSchemaNames.Ops);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Stage)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.IssueCode).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.CountyFipsCode)
            .HasColumnType("char(5)")
            .IsUnicode(false)
            .IsFixedLength();
        builder.Property(x => x.MetricCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.RawValue).HasMaxLength(100);
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.ImportRun)
            .WithMany(x => x.ImportIssues)
            .HasForeignKey(x => x.ImportRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ImportRunId, x.Severity, x.Stage });
    }
}
