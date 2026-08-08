using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutGuest;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Guests;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Security.AccessSessions.Commands.CheckOutGuest;

/// <summary>
/// The "already checked out" state-transition guard is already covered at the domain level
/// (AccessSessionTests.CheckOut_Throws_When_Already_CheckedOut). This covers the handler's own
/// authorization guard: a caller must not be able to check out a session belonging to a different
/// tenant, or a non-Guest session, by ID — both must surface as NotFound rather than leaking whether
/// the ID exists at all.
/// </summary>
public class CheckOutGuestCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IAccessSessionRepository _accessSessions = Substitute.For<IAccessSessionRepository>();
    private readonly IGateRepository _gates = Substitute.For<IGateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CheckOutGuestCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
    }

    private CheckOutGuestCommandHandler CreateHandler() => new(
        _accessSessions, _gates, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CheckOutGuestCommandHandler>>());

    private static AccessSession OpenGuestSession(Guid tenantId) => AccessSession.CheckInGuest(
        tenantId, GuestProfileId.New(), FlatId, "Visiting", GateId.New(), Guid.NewGuid(), null, null, null, Now);

    [Fact]
    public async Task Throws_NotFound_When_The_Session_Belongs_To_Another_Tenant()
    {
        AccessSession session = OpenGuestSession(Guid.NewGuid()); // a different tenant than the caller
        _accessSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", Now);
        _gates.GetByIdAsync(gate.Id, Arg.Any<CancellationToken>()).Returns(gate);

        Func<Task> act = () => CreateHandler().Handle(
            new CheckOutGuestCommand(session.Id.Value, gate.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_The_Session_Is_Not_A_Guest_Category()
    {
        AccessSession vehicleSession = AccessSession.CheckInVehicle(
            TenantId, MyCondo.Domain.Features.Security.Vehicles.VehicleId.New(), FlatId, GateId.New(),
            Guid.NewGuid(), null, null, Now);
        _accessSessions.GetByIdAsync(vehicleSession.Id, Arg.Any<CancellationToken>()).Returns(vehicleSession);
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", Now);
        _gates.GetByIdAsync(gate.Id, Arg.Any<CancellationToken>()).Returns(gate);

        Func<Task> act = () => CreateHandler().Handle(
            new CheckOutGuestCommand(vehicleSession.Id.Value, gate.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Succeeds_And_Checks_Out_A_Same_Tenant_Guest_Session()
    {
        AccessSession session = OpenGuestSession(TenantId);
        _accessSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", Now);
        _gates.GetByIdAsync(gate.Id, Arg.Any<CancellationToken>()).Returns(gate);

        AccessSessionDto result = await CreateHandler().Handle(
            new CheckOutGuestCommand(session.Id.Value, gate.Id.Value), CancellationToken.None);

        result.Status.Should().Be(AccessSessionStatus.CheckedOut.ToString());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
