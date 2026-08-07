using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class OccupancyRegistrationStatusHistoryConfiguration
    : IEntityTypeConfiguration<OccupancyRegistrationStatusHistory>
{
    public void Configure(EntityTypeBuilder<OccupancyRegistrationStatusHistory> builder)
    {
        builder.ToTable("occupancy_registration_status_histories", schema: "leasing");

        builder.HasKey(x => x.Id).HasName("pk_occupancy_registration_status_histories");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationStatusHistoryId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccupancyRegistrationId)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationId(value))
            .IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(25);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(25).IsRequired();
        builder.Property(x => x.ChangedBy);
        builder.Property(x => x.ChangedAtUtc).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.OccupancyRegistrationId, x.ChangedAtUtc })
            .HasDatabaseName("ix_occ_reg_status_histories_tenant_id_occ_reg_id_changed_at_utc");
    }
}
