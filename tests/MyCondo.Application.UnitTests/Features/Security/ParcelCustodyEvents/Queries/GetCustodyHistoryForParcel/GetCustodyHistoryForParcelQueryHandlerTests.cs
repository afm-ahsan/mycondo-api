using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.ParcelCustodyEvents.Queries.GetCustodyHistoryForParcel;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Security.ParcelCustodyEvents.Queries.GetCustodyHistoryForParcel;

/// <summary>
/// Custody history must never surface a raw actor GUID (the "By 019f..." bug) — these tests lock in
/// the resolved-name/System/Unknown-user fallback chain.
/// </summary>
public class GetCustodyHistoryForParcelQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 18, 5, 56, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly ParcelId ParcelId = ParcelId.New();

    private readonly IParcelCustodyEventRepository _custodyEvents = Substitute.For<IParcelCustodyEventRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    private GetCustodyHistoryForParcelQueryHandler CreateHandler() => new(_custodyEvents, _users, _currentUser);

    [Fact]
    public async Task Handle_Resolves_PerformedBy_To_The_Actor_Full_Name()
    {
        _currentUser.TenantId.Returns(TenantId);
        User actor = User.Register(TenantId, "ahsan@mycondo.test", "hash", "Ahsan Uddin", null, NowUtc);
        ParcelCustodyEvent custodyEvent = ParcelCustodyEvent.Record(
            TenantId, ParcelId, ParcelStatus.Received, actor.Id.Value, "Parcel received", NowUtc);

        _custodyEvents.GetForParcelAsync(TenantId, ParcelId, Arg.Any<CancellationToken>()).Returns([custodyEvent]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([actor]);

        List<Application.Features.Security.ParcelCustodyEvents.DTOs.ParcelCustodyEventDto> result =
            await CreateHandler().Handle(new GetCustodyHistoryForParcelQuery(ParcelId.Value), CancellationToken.None);

        result.Should().ContainSingle(e => e.PerformedByDisplayName == "Ahsan Uddin");
    }

    [Fact]
    public async Task Handle_Uses_System_When_PerformedBy_Is_Null()
    {
        _currentUser.TenantId.Returns(TenantId);
        ParcelCustodyEvent custodyEvent = ParcelCustodyEvent.Record(
            TenantId, ParcelId, ParcelStatus.Collected, null, null, NowUtc);

        _custodyEvents.GetForParcelAsync(TenantId, ParcelId, Arg.Any<CancellationToken>()).Returns([custodyEvent]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        List<Application.Features.Security.ParcelCustodyEvents.DTOs.ParcelCustodyEventDto> result =
            await CreateHandler().Handle(new GetCustodyHistoryForParcelQuery(ParcelId.Value), CancellationToken.None);

        result.Should().ContainSingle(e => e.PerformedByDisplayName == "System");
    }

    [Fact]
    public async Task Handle_Uses_Unknown_User_When_The_Actor_No_Longer_Exists()
    {
        _currentUser.TenantId.Returns(TenantId);
        Guid deletedActorId = Guid.NewGuid();
        ParcelCustodyEvent custodyEvent = ParcelCustodyEvent.Record(
            TenantId, ParcelId, ParcelStatus.Received, deletedActorId, null, NowUtc);

        _custodyEvents.GetForParcelAsync(TenantId, ParcelId, Arg.Any<CancellationToken>()).Returns([custodyEvent]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        List<Application.Features.Security.ParcelCustodyEvents.DTOs.ParcelCustodyEventDto> result =
            await CreateHandler().Handle(new GetCustodyHistoryForParcelQuery(ParcelId.Value), CancellationToken.None);

        result.Should().ContainSingle(e => e.PerformedByDisplayName == "Unknown user");
    }
}
