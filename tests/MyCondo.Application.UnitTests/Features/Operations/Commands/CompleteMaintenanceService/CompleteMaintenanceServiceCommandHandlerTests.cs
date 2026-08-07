using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.Commands.CompleteMaintenanceService;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;
using MyCondo.Domain.Features.Operations.Generators;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Operations.Commands.CompleteMaintenanceService;

public class CompleteMaintenanceServiceCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IGeneratorServiceRecordRepository _serviceRecords = Substitute.For<IGeneratorServiceRecordRepository>();
    private readonly IGeneratorMaintenanceScheduleRepository _schedules = Substitute.For<IGeneratorMaintenanceScheduleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CompleteMaintenanceServiceCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
    }

    private CompleteMaintenanceServiceCommandHandler CreateHandler() => new(
        _serviceRecords, _schedules, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CompleteMaintenanceServiceCommandHandler>>());

    [Fact]
    public async Task Handle_Records_Service_And_Reschedules_In_One_Call()
    {
        GeneratorMaintenanceSchedule schedule = GeneratorMaintenanceSchedule.Create(TenantId, GeneratorId, null, 500m, Now);
        _schedules.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        DateOnly nextDueDate = DateOnly.FromDateTime(Now.UtcDateTime).AddMonths(3);

        CompleteMaintenanceServiceCommand command = new(
            schedule.Id.Value, Now, "Replaced oil filter", 800m, nextDueDate, 1000m);

        GeneratorServiceRecordDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Description.Should().Be("Replaced oil filter");
        schedule.NextDueDate.Should().Be(nextDueDate);
        schedule.NextDueHourMeterReading.Should().Be(1000m);
        _serviceRecords.Received(1).Add(Arg.Any<GeneratorServiceRecord>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
