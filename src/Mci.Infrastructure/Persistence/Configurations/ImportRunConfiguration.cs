using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        builder.ToTable("ImportRun", MciSchemaNames.Ops, table =>
        {
            table.HasCheckConstraint(
                "CK_ImportRun_RecordCounts",
                "[RecordsFetched] >= 0 AND [RecordsStaged] >= 0 AND [RecordsInserted] >= 0 AND [RecordsRejected] >= 0");
            table.HasCheckConstraint(
                "CK_ImportRun_Completion",
                "([Status] IN ('Queued', 'Running') AND [CompletedAtUtc] IS NULL) OR ([Status] IN ('Succeeded', 'SucceededWithWarnings', 'Failed') AND [CompletedAtUtc] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TriggerType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.ErrorSummary).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PipelineVersion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.DataRelease)
            .WithMany(x => x.ImportRuns)
            .HasForeignKey(x => x.DataReleaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RetryOfImportRun)
            .WithMany()
            .HasForeignKey(x => x.RetryOfImportRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(x => new { x.DataReleaseId, x.Id });
        builder.HasIndex(x => new { x.DataReleaseId, x.Status, x.CompletedAtUtc });
        builder.HasIndex(x => x.RetryOfImportRunId);
    }
}
