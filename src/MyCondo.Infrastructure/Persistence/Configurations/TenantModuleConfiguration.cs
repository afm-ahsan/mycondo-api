using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class TenantModuleConfiguration : IEntityTypeConfiguration<TenantModule>
{
    public void Configure(EntityTypeBuilder<TenantModule> builder)
    {
        builder.ToTable("tenant_modules", schema: "tenancy");

        builder.HasKey(x => x.Id).HasName("pk_tenant_modules");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TenantModuleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ModuleKey).IsRequired().HasMaxLength(50);
        builder.Property(x => x.EnabledAtUtc).IsRequired();
        builder.Property(x => x.EnabledBy);

        builder.HasIndex(x => new { x.TenantId, x.ModuleKey })
            .IsUnique()
            .HasDatabaseName("ux_tenant_modules_tenant_module");

        // No DB-level FK to tenancy.tenants — matches this schema's established convention of
        // application-layer-only referential integrity (see Architecture_Decision_Register.md's
        // note on tenant_id having no FK anywhere else in the schema).
    }
}
