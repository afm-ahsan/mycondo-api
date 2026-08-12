using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Commands.DeactivateBuilding;

public class DeactivateBuildingCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeactivateBuildingCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private DeactivateBuildingCommandHandler CreateHandler() => new(
        _buildings, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<DeactivateBuildingCommandHandler>>());

    [Fact]
    public async Task Deactivates_A_Building_In_Callers_Tenant()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        await CreateHandler().Handle(new DeactivateBuildingCommand(building.Id.Value), CancellationToken.None);

        building.DeletedAtUtc.Should().Be(NowUtc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Building_Belongs_To_A_Different_Tenant()
    {
        Building building = Building.Create(OtherTenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new DeactivateBuildingCommand(building.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
