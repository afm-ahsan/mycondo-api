using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", schema: "tenancy");

        builder.HasKey(x => x.Id).HasName("pk_tenants");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(63);
        builder.Property(x => x.Code).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.PrimaryAdministratorUserId);
        builder.Property(x => x.PrimaryAdministratorFullName).HasMaxLength(200);
        builder.Property(x => x.PrimaryAdministratorEmail).HasMaxLength(320);

        builder.Property(x => x.CreatedAtUtc);
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ux_tenants_slug");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("code IS NOT NULL")
            .HasDatabaseName("ux_tenants_code");

        builder.Ignore(x => x.DomainEvents);
    }
}
