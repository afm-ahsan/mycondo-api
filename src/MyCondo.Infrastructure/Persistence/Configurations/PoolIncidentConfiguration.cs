using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PoolIncidentConfiguration : IEntityTypeConfiguration<PoolIncident>
{
    public void Configure(EntityTypeBuilder<PoolIncident> builder)
    {
        builder.ToTable("pool_incidents", schema: "amenities");

        builder.HasKey(x => x.Id).HasName("pk_pool_incidents");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PoolIncidentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FacilityId)
            .HasConversion(id => id.Value, value => new FacilityId(value))
            .IsRequired();

        builder.Property(x => x.PoolSessionId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new PoolSessionId(value.Value) : (PoolSessionId?)null);

        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.ReportedBy);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ActionTaken).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.OccurredAtUtc })
            .HasDatabaseName("ix_pool_incidents_tenant_id_facility_id_occurred_at_utc");
    }
}
