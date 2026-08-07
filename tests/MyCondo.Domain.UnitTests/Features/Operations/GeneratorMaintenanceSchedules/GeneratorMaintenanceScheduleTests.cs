using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.UnitTests.Features.Operations.GeneratorMaintenanceSchedules;

public class GeneratorMaintenanceScheduleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();

    [Fact]
    public void Create_Throws_When_Neither_DueDate_Nor_DueReading_Given()
    {
        Action act = () => GeneratorMaintenanceSchedule.Create(TenantId, GeneratorId, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsDue_True_When_DueDate_Passed()
    {
        DateOnly dueDate = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1);
        GeneratorMaintenanceSchedule schedule = GeneratorMaintenanceSchedule.Create(TenantId, GeneratorId, dueDate, null, Now);

        schedule.IsDue(DateOnly.FromDateTime(Now.UtcDateTime), 0m).Should().BeTrue();
    }

    [Fact]
    public void IsDue_True_When_HourMeter_Reading_Reached()
    {
        GeneratorMaintenanceSchedule schedule = GeneratorMaintenanceSchedule.Create(TenantId, GeneratorId, null, 500m, Now);

        schedule.IsDue(DateOnly.FromDateTime(Now.UtcDateTime), 500m).Should().BeTrue();
        schedule.IsDue(DateOnly.FromDateTime(Now.UtcDateTime), 499m).Should().BeFalse();
    }

    [Fact]
    public void Reschedule_Updates_Due_Fields()
    {
        GeneratorMaintenanceSchedule schedule = GeneratorMaintenanceSchedule.Create(TenantId, GeneratorId, null, 500m, Now);

        schedule.Reschedule(null, 1000m);

        schedule.NextDueHourMeterReading.Should().Be(1000m);
    }

    [Fact]
    public void Deactivate_Makes_Schedule_Never_Due()
    {
        DateOnly pastDue = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1);
        GeneratorMaintenanceSchedule schedule = GeneratorMaintenanceSchedule.Create(TenantId, GeneratorId, pastDue, null, Now);
        schedule.Deactivate();

        schedule.IsDue(DateOnly.FromDateTime(Now.UtcDateTime), 0m).Should().BeFalse();
    }
}
