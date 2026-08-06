using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ResidentAccountConfiguration : IEntityTypeConfiguration<ResidentAccount>
{
    public void Configure(EntityTypeBuilder<ResidentAccount> builder)
    {
        builder.ToTable("resident_accounts", schema: "payments");

        builder.HasKey(x => x.Id).HasName("pk_resident_accounts");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ResidentAccountId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.OpenedAtUtc).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .IsUnique()
            .HasDatabaseName("ux_resident_accounts_tenant_id_flat_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
