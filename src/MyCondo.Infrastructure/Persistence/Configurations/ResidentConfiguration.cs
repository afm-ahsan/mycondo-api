using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> builder)
    {
        builder.ToTable("residents", schema: "residents");

        builder.HasKey(x => x.Id).HasName("pk_residents");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ResidentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.ResidentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_residents_tenant_id_flat_id");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
