using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Guests;
using MyCondo.Domain.Features.Security.ServiceProviders;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class AccessSessionConfiguration : IEntityTypeConfiguration<AccessSession>
{
    public void Configure(EntityTypeBuilder<AccessSession> builder)
    {
        builder.ToTable("access_sessions", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_access_sessions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AccessSessionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.AccessCategory).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.GuestProfileId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new GuestProfileId(value.Value) : (GuestProfileId?)null);

        builder.Property(x => x.VehicleId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new VehicleId(value.Value) : (VehicleId?)null);

        builder.Property(x => x.DomesticWorkerProfileId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new DomesticWorkerProfileId(value.Value) : (DomesticWorkerProfileId?)null);

        builder.Property(x => x.ServiceProviderProfileId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ServiceProviderProfileId(value.Value) : (ServiceProviderProfileId?)null);

        builder.Property(x => x.HostFlatId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new FlatId(value.Value) : (FlatId?)null);

        builder.Property(x => x.PurposeOfVisit).HasMaxLength(200);

        builder.Property(x => x.EntryGateId)
            .HasConversion(id => id.Value, value => new GateId(value))
            .IsRequired();
        builder.Property(x => x.EntryAtUtc).IsRequired();

        builder.Property(x => x.ExitGateId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new GateId(value.Value) : (GateId?)null);
        builder.Property(x => x.ExitAtUtc);

        builder.Property(x => x.CheckedInBy);
        builder.Property(x => x.CheckedOutBy);
        builder.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PassOrQrNumber).HasMaxLength(80);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OverrideReason).HasMaxLength(400);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.AccessCategory, x.Status })
            .HasDatabaseName("ix_access_sessions_tenant_id_category_status");

        // Data-integrity backstop for "one open access session per person/vehicle" (not just an
        // application-level check) — a partial unique index over only CheckedIn rows, so multiple
        // CheckedOut rows for the same guest/vehicle remain unrestricted (that's the visit history).
        // Note: this is also the only index on (TenantId, GuestProfileId)/(TenantId, VehicleId) —
        // EF Core merges multiple HasIndex calls over an identical property set into one index
        // definition (last call wins), so a separate unfiltered lookup index isn't achievable this
        // way; per-guest/per-vehicle history queries fall back to the category/status index above or
        // a sequential scan, acceptable at this slice's expected visit-history volume.
        builder.HasIndex(x => new { x.TenantId, x.GuestProfileId })
            .IsUnique()
            .HasFilter("status = 'CheckedIn' AND guest_profile_id IS NOT NULL")
            .HasDatabaseName("ux_access_sessions_tenant_id_guest_profile_id_open");

        builder.HasIndex(x => new { x.TenantId, x.VehicleId })
            .IsUnique()
            .HasFilter("status = 'CheckedIn' AND vehicle_id IS NOT NULL")
            .HasDatabaseName("ux_access_sessions_tenant_id_vehicle_id_open");

        builder.HasIndex(x => new { x.TenantId, x.DomesticWorkerProfileId })
            .IsUnique()
            .HasFilter("status = 'CheckedIn' AND domestic_worker_profile_id IS NOT NULL")
            .HasDatabaseName("ux_access_sessions_tenant_id_domestic_worker_id_open");

        builder.HasIndex(x => new { x.TenantId, x.ServiceProviderProfileId })
            .IsUnique()
            .HasFilter("status = 'CheckedIn' AND service_provider_profile_id IS NOT NULL")
            .HasDatabaseName("ux_access_sessions_tenant_id_service_provider_id_open");

        builder.Ignore(x => x.DomainEvents);
    }
}
