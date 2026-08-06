using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class MeterAssignmentConfiguration : IEntityTypeConfiguration<MeterAssignment>
{
    public void Configure(EntityTypeBuilder<MeterAssignment> builder)
    {
        builder.ToTable("meter_assignments", schema: "utilities");

        builder.HasKey(x => x.Id).HasName("pk_meter_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new MeterAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MeterId)
            .HasConversion(id => id.Value, value => new MeterId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.AssignedFromUtc).IsRequired();
        builder.Property(x => x.AssignedToUtc);

        // NOTE: deliberately includes AssignedFromUtc so this index's property set differs from the
        // partial-unique index below — EF Core silently merges (last one wins) two HasIndex calls
        // declared over an identical property set, which bit Slice B's AccessSessionConfiguration.
        // This one serves "full assignment history for a meter" queries.
        builder.HasIndex(x => new { x.TenantId, x.MeterId, x.AssignedFromUtc })
            .HasDatabaseName("ix_meter_assignments_tenant_id_meter_id_assigned_from");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_meter_assignments_tenant_id_flat_id");

        // At most one open (AssignedToUtc IS NULL) assignment per meter — see MeterAssignment's doc
        // comment. Same partial-unique-index pattern as AttendanceRecord's open-record index.
        builder.HasIndex(x => new { x.TenantId, x.MeterId })
            .IsUnique()
            .HasFilter("assigned_to_utc IS NULL")
            .HasDatabaseName("ux_meter_assignments_tenant_id_meter_id_open");
    }
}
