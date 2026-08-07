using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class OccupancyRegistrationWorkerAssignmentConfiguration
    : IEntityTypeConfiguration<OccupancyRegistrationWorkerAssignment>
{
    public void Configure(EntityTypeBuilder<OccupancyRegistrationWorkerAssignment> builder)
    {
        builder.ToTable("occupancy_registration_worker_assignments", schema: "leasing");

        builder.HasKey(x => x.Id).HasName("pk_occupancy_registration_worker_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationWorkerAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccupancyRegistrationId)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationId(value))
            .IsRequired();
        builder.Property(x => x.DomesticWorkerProfileId)
            .HasConversion(id => id.Value, value => new DomesticWorkerProfileId(value))
            .IsRequired();
        builder.Property(x => x.AssignedAtUtc).IsRequired();
        builder.Property(x => x.EndedAtUtc);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OccupancyRegistrationId })
            .HasDatabaseName("ix_occ_reg_worker_assignments_tenant_id_occ_reg_id");
    }
}
