using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PoolSessionConfiguration : IEntityTypeConfiguration<PoolSession>
{
    public void Configure(EntityTypeBuilder<PoolSession> builder)
    {
        builder.ToTable("pool_sessions", schema: "amenities");

        builder.HasKey(x => x.Id).HasName("pk_pool_sessions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PoolSessionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FacilityId)
            .HasConversion(id => id.Value, value => new FacilityId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.PersonType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.AgeCategory).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.AccompaniedBySessionId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new PoolSessionId(value.Value) : (PoolSessionId?)null);

        builder.Property(x => x.EntryAtUtc).IsRequired();
        builder.Property(x => x.ExitAtUtc);
        builder.Property(x => x.GuestFeeAmount).HasPrecision(18, 2);
        builder.Property(x => x.SafetyAcknowledgedAtUtc);
        builder.Property(x => x.CheckedInBy);
        builder.Property(x => x.CheckedOutBy);
        builder.Property(x => x.OverrideReason).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        // Live capacity count: "how many are currently checked in at this facility".
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ExitAtUtc })
            .HasDatabaseName("ix_pool_sessions_tenant_id_facility_id_exit_at_utc");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_pool_sessions_tenant_id_flat_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
