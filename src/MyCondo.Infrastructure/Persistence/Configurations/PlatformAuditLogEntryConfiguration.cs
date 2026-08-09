using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.PlatformAudit;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PlatformAuditLogEntryConfiguration : IEntityTypeConfiguration<PlatformAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<PlatformAuditLogEntry> builder)
    {
        builder.ToTable("platform_audit_log", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_platform_audit_log");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PlatformAuditLogEntryId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasMaxLength(120);
        builder.Property(x => x.TargetType).HasMaxLength(120);
        builder.Property(x => x.TargetId).HasMaxLength(120);
        builder.Property(x => x.Metadata).HasColumnType("jsonb");
        builder.Property(x => x.CorrelationId).HasMaxLength(64);

        builder.HasIndex(x => x.OccurredAtUtc).HasDatabaseName("ix_platform_audit_log_occurred_at_utc");
        builder.HasIndex(x => x.ActorPlatformUserId).HasDatabaseName("ix_platform_audit_log_actor_platform_user_id");
    }
}
