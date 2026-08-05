using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", schema: "payroll");

        builder.HasKey(x => x.Id).HasName("pk_attendance_records");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AttendanceRecordId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.StaffMemberId)
            .HasConversion(id => id.Value, value => new StaffMemberId(value))
            .IsRequired();
        builder.Property(x => x.WorkDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.ScheduledStartUtc);
        builder.Property(x => x.ScheduledEndUtc);
        builder.Property(x => x.CheckInUtc).IsRequired();
        builder.Property(x => x.CheckOutUtc);
        builder.Property(x => x.WorkLocation).HasMaxLength(200);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CorrectionRequested).IsRequired();
        builder.Property(x => x.CorrectionReason).HasMaxLength(400);
        builder.Property(x => x.ApprovedBy);
        builder.Property(x => x.ApprovedAtUtc);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.StaffMemberId, x.WorkDate })
            .HasDatabaseName("ix_attendance_records_tenant_id_staff_member_id_work_date");

        // Data-integrity backstop for "one open attendance record per staff member" (not just an
        // application-level check) — mirrors AccessSession's partial-unique-index pattern.
        builder.HasIndex(x => new { x.TenantId, x.StaffMemberId })
            .IsUnique()
            .HasFilter("check_out_utc IS NULL")
            .HasDatabaseName("ux_attendance_records_tenant_id_staff_member_id_open");

        builder.Ignore(x => x.IsLateArrival);
        builder.Ignore(x => x.IsEarlyDeparture);
        builder.Ignore(x => x.OvertimeMinutes);
        builder.Ignore(x => x.DomainEvents);
    }
}
