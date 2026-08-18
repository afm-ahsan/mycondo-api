using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetPoolSessions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Queries.GetPoolSessions;

/// <summary>
/// Usage History lists many sessions per page — this locks in that flat/actor names are resolved via
/// the batched resolvers (one extra round-trip each, not one per row) and that each row still gets
/// its own correct name out of the shared lookup.
/// </summary>
public class GetPoolSessionsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPoolSessionRepository _poolSessions = Substitute.For<IPoolSessionRepository>();
    private readonly IFlatDisplayNameResolver _flatDisplayNames = Substitute.For<IFlatDisplayNameResolver>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetPoolSessionsQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetPoolSessionsQueryHandler CreateHandler() => new(_poolSessions, _flatDisplayNames, _users, _currentUser);

    [Fact]
    public async Task Handle_Resolves_Each_Rows_Flat_And_Actor_Names_Via_A_Single_Batched_Lookup()
    {
        User guard = User.Register(TenantId, "guard@mycondo.test", "hash", "Guard One", null, Now);
        FlatId flatA = FlatId.New();
        FlatId flatB = FlatId.New();
        PoolSession openSession = PoolSession.CheckIn(
            TenantId, FacilityId.New(), flatA, PoolPersonType.Resident, PoolAgeCategory.Adult, null, null, null,
            guard.Id.Value, null, Now);
        PoolSession closedSession = PoolSession.CheckIn(
            TenantId, FacilityId.New(), flatB, PoolPersonType.Guest, PoolAgeCategory.Adult, null, 50m, null,
            guard.Id.Value, null, Now);
        closedSession.CheckOut(guard.Id.Value, Now);

        _poolSessions.SearchAsync(TenantId, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PoolSession>([openSession, closedSession], 1, 20, 2));
        _flatDisplayNames.ResolveManyAsync(Arg.Any<IReadOnlyCollection<FlatId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<FlatId, string> { [flatA] = "AISHA A1", [flatB] = "AISHA B2" });
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([guard]);

        PagedResult<PoolSessionDto> result = await CreateHandler().Handle(
            new GetPoolSessionsQuery(null, null, null, 1, 20), CancellationToken.None);

        result.Items.Should().SatisfyRespectively(
            first =>
            {
                first.FlatDisplayName.Should().Be("AISHA A1");
                first.CheckedInByDisplayName.Should().Be("Guard One");
                first.CheckedOutByDisplayName.Should().BeNull();
            },
            second =>
            {
                second.FlatDisplayName.Should().Be("AISHA B2");
                second.CheckedInByDisplayName.Should().Be("Guard One");
                second.CheckedOutByDisplayName.Should().Be("Guard One");
            });
        await _flatDisplayNames.Received(1).ResolveManyAsync(Arg.Any<IReadOnlyCollection<FlatId>>(), Arg.Any<CancellationToken>());
        await _users.Received(1).GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>());
    }
}
