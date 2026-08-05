using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ServiceProviderProfileConfiguration : IEntityTypeConfiguration<ServiceProviderProfile>
{
    public void Configure(EntityTypeBuilder<ServiceProviderProfile> builder)
    {
        builder.ToTable("service_provider_profiles", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_service_provider_profiles");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceProviderProfileId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ServiceDescription).HasMaxLength(200);
        builder.Property(x => x.IdentityDocumentType).HasMaxLength(40);
        builder.Property(x => x.IdentityDocumentNumber).HasMaxLength(60);
        builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.StatusReason).HasMaxLength(400);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.Phone })
            .HasDatabaseName("ix_service_provider_profiles_tenant_id_phone");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
