using Mci.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mci.Infrastructure.Persistence.Configurations;

public sealed class ClarityInsightSnapshotConfiguration : IEntityTypeConfiguration<ClarityInsightSnapshot>
{
    public void Configure(EntityTypeBuilder<ClarityInsightSnapshot> builder)
    {
        builder.ToTable("ClarityInsightSnapshot", MciSchemaNames.Ops, table =>
        {
            table.HasCheckConstraint(
                "CK_ClarityInsightSnapshot_NumOfDays",
                "[NumOfDays] BETWEEN 1 AND 3");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CaptureDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.CapturedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.NumOfDays).IsRequired();
        builder.Property(x => x.PagesPerSession).HasColumnType("decimal(9,2)");
        builder.Property(x => x.RawPayload).HasColumnType("nvarchar(max)").IsRequired();

        // One snapshot per capture day keeps the fetch idempotent and enforces the
        // "at most one call per day" contract at the database level.
        builder.HasIndex(x => x.CaptureDate).IsUnique();
    }
}
