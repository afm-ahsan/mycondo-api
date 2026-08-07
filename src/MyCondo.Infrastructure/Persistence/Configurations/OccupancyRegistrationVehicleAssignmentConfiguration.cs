using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class OccupancyRegistrationVehicleAssignmentConfiguration
    : IEntityTypeConfiguration<OccupancyRegistrationVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<OccupancyRegistrationVehicleAssignment> builder)
    {
        builder.ToTable("occupancy_registration_vehicle_assignments", schema: "leasing");

        builder.HasKey(x => x.Id).HasName("pk_occupancy_registration_vehicle_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationVehicleAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccupancyRegistrationId)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationId(value))
            .IsRequired();
        builder.Property(x => x.VehicleId)
            .HasConversion(id => id.Value, value => new VehicleId(value))
            .IsRequired();
        builder.Property(x => x.AssignedAtUtc).IsRequired();
        builder.Property(x => x.EndedAtUtc);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OccupancyRegistrationId })
            .HasDatabaseName("ix_occ_reg_vehicle_assignments_tenant_id_occ_reg_id");
    }
}
