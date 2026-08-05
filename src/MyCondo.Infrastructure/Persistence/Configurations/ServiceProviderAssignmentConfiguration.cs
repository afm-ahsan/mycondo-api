using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ServiceProviderAssignmentConfiguration : IEntityTypeConfiguration<ServiceProviderAssignment>
{
    public void Configure(EntityTypeBuilder<ServiceProviderAssignment> builder)
    {
        builder.ToTable("service_provider_assignments", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_service_provider_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceProviderAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ServiceProviderProfileId)
            .HasConversion(id => id.Value, value => new ServiceProviderProfileId(value))
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

        builder.HasIndex(x => new { x.TenantId, x.ServiceProviderProfileId })
            .HasDatabaseName("ix_service_provider_assignments_tenant_id_provider_id");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_service_provider_assignments_tenant_id_flat_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
