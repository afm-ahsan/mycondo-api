using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Amenities.Commands.CheckOutPoolSession;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Commands.CheckOutPoolSession;

/// <summary>
/// Usage History previously showed the raw checked-in-by/checked-out-by GUID for pool sessions
/// (same bug class fixed for Parcel custody actors) — these tests lock in the resolved-name/System/
/// Unknown-user fallback chain and the flat display name on checkout.
/// </summary>
public class CheckOutPoolSessionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPoolSessionRepository _poolSessions = Substitute.For<IPoolSessionRepository>();
    private readonly IFlatDisplayNameResolver _flatDisplayNames = Substitute.For<IFlatDisplayNameResolver>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CheckOutPoolSessionCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _flatDisplayNames.ResolveAsync(FlatId, Arg.Any<CancellationToken>()).Returns("AISHA 3B");
    }

    private CheckOutPoolSessionCommandHandler CreateHandler() => new(
        _poolSessions, _flatDisplayNames, _users, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CheckOutPoolSessionCommandHandler>>());

    private static PoolSession OpenSession(Guid checkedInBy) => PoolSession.CheckIn(
        TenantId, FacilityId.New(), FlatId, PoolPersonType.Resident, PoolAgeCategory.Adult, null, null, null,
        checkedInBy, null, Now);

    [Fact]
    public async Task CheckOut_Resolves_Both_The_Checked_In_And_Checked_Out_Actor_Names()
    {
        User guard = User.Register(TenantId, "guard@mycondo.test", "hash", "Guard One", null, Now);
        User supervisor = User.Register(TenantId, "supervisor@mycondo.test", "hash", "Supervisor Two", null, Now);
        PoolSession session = OpenSession(guard.Id.Value);
        _poolSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _currentUser.UserId.Returns(supervisor.Id.Value);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([guard, supervisor]);

        PoolSessionDto result = await CreateHandler().Handle(
            new CheckOutPoolSessionCommand(session.Id.Value), CancellationToken.None);

        result.CheckedInByDisplayName.Should().Be("Guard One");
        result.CheckedOutByDisplayName.Should().Be("Supervisor Two");
        result.FlatDisplayName.Should().Be("AISHA 3B");
    }

    [Fact]
    public async Task CheckOut_Falls_Back_To_Unknown_User_For_The_Original_Checked_In_Actor_And_System_For_An_Anonymous_Checkout()
    {
        PoolSession session = OpenSession(Guid.NewGuid());
        _poolSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _currentUser.UserId.Returns((Guid?)null);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        PoolSessionDto result = await CreateHandler().Handle(
            new CheckOutPoolSessionCommand(session.Id.Value), CancellationToken.None);

        result.CheckedInByDisplayName.Should().Be("Unknown user");
        result.CheckedOutByDisplayName.Should().Be("System");
    }

    [Fact]
    public async Task CheckOut_Throws_When_The_Session_Belongs_To_A_Different_Tenant()
    {
        PoolSession session = PoolSession.CheckIn(
            Guid.NewGuid(), FacilityId.New(), FlatId, PoolPersonType.Resident, PoolAgeCategory.Adult, null, null,
            null, Guid.NewGuid(), null, Now);
        _poolSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        Func<Task> act = () => CreateHandler().Handle(
            new CheckOutPoolSessionCommand(session.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
