using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.Commands.UpdateBuilding;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Commands.UpdateBuilding;

public class UpdateBuildingCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public UpdateBuildingCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private UpdateBuildingCommandHandler CreateHandler() => new(
        _buildings, _unitOfWork, _currentUser, Substitute.For<ILogger<UpdateBuildingCommandHandler>>());

    [Fact]
    public async Task Updates_A_Building_In_Callers_Tenant()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        BuildingDto result = await CreateHandler().Handle(
            new UpdateBuildingCommand(building.Id.Value, "Tower A Renamed", "TA2", "New Address"), CancellationToken.None);

        result.Name.Should().Be("Tower A Renamed");
        result.Code.Should().Be("TA2");
        result.Address.Should().Be("New Address");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_Keeping_The_Same_Name_And_Code()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);
        _buildings.GetByNameAsync(TenantId, "Tower A", Arg.Any<CancellationToken>()).Returns(building);
        _buildings.GetByCodeAsync(TenantId, "TA", Arg.Any<CancellationToken>()).Returns(building);

        BuildingDto result = await CreateHandler().Handle(
            new UpdateBuildingCommand(building.Id.Value, "Tower A", "TA", "New Address"), CancellationToken.None);

        result.Address.Should().Be("New Address");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Name_Belongs_To_Another_Building()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        Building other = Building.Create(TenantId, "Tower B", "TB", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);
        _buildings.GetByNameAsync(TenantId, "Tower B", Arg.Any<CancellationToken>()).Returns(other);

        Func<Task> act = async () => await CreateHandler().Handle(
            new UpdateBuildingCommand(building.Id.Value, "Tower B", "TA", null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Building_Belongs_To_A_Different_Tenant()
    {
        Building building = Building.Create(OtherTenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        Func<Task> act = async () => await CreateHandler().Handle(
            new UpdateBuildingCommand(building.Id.Value, "Tower A", "TA", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
