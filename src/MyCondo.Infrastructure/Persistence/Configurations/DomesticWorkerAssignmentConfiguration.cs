using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class DomesticWorkerAssignmentConfiguration : IEntityTypeConfiguration<DomesticWorkerAssignment>
{
    public void Configure(EntityTypeBuilder<DomesticWorkerAssignment> builder)
    {
        builder.ToTable("domestic_worker_assignments", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_domestic_worker_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DomesticWorkerAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.DomesticWorkerProfileId)
            .HasConversion(id => id.Value, value => new DomesticWorkerProfileId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.ApprovedByResident).IsRequired();
        builder.Property(x => x.ValidFromUtc).IsRequired();
        builder.Property(x => x.ValidToUtc);
        builder.Property(x => x.AllowedDays).HasConversion<int>().IsRequired();
        builder.Property(x => x.AllowedStartTime);
        builder.Property(x => x.AllowedEndTime);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.DomesticWorkerProfileId })
            .HasDatabaseName("ix_domestic_worker_assignments_tenant_id_worker_id");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_domestic_worker_assignments_tenant_id_flat_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
