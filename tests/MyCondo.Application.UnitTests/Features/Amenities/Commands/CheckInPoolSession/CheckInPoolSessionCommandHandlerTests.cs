using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Amenities.Commands.CheckInPoolSession;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Commands.CheckInPoolSession;

/// <summary>
/// Proves capacity/eligibility enforcement and the pool.override escape hatch. The facility-row lock
/// (<see cref="IFacilityRepository.LockForCapacityCheckAsync"/>) itself is only meaningfully provable
/// against a real Postgres instance under concurrent load (MultiTenancyTests, not executable in this
/// environment) — these tests prove the single-request enforcement logic the lock protects.
/// </summary>
public class CheckInPoolSessionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FacilityId FacilityId = FacilityId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IFacilityRepository _facilities = Substitute.For<IFacilityRepository>();
    private readonly IBlackoutDateRepository _blackoutDates = Substitute.For<IBlackoutDateRepository>();
    private readonly IPoolSessionRepository _poolSessions = Substitute.For<IPoolSessionRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IFlatDisplayNameResolver _flatDisplayNames = Substitute.For<IFlatDisplayNameResolver>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CheckInPoolSessionCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
        _blackoutDates.GetActiveForFacilityAsync(TenantId, FacilityId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<BlackoutDate>)[]);
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>())
            .Returns(Flat.Create(TenantId, BuildingId.New(), "3B", 3, FlatType.Residential, Now));
        _flatDisplayNames.ResolveAsync(FlatId, Arg.Any<CancellationToken>()).Returns("AISHA 3B");
    }

    private CheckInPoolSessionCommandHandler CreateHandler() => new(
        _facilities, _blackoutDates, _poolSessions, _flats, _flatDisplayNames, _users, _invoices, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<CheckInPoolSessionCommandHandler>>());

    private static Facility PoolFacility(int capacity) => Facility.Create(
        TenantId, BuildingId.New(), "Main Pool", FacilityType.SwimmingPool, capacity, null, null, false, null, null,
        24, 0m, 200m, 12, false, false, Now);

    private static CheckInPoolSessionCommand ValidCommand() => new(
        FacilityId.Value, FlatId.Value, "Resident", "Adult", null, true, null);

    [Fact]
    public async Task CheckIn_Succeeds_When_Under_Capacity()
    {
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(PoolFacility(capacity: 10));
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(5);

        PoolSessionDto result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");
        result.FlatDisplayName.Should().Be("AISHA 3B");
        await _facilities.Received(1).LockForCapacityCheckAsync(FacilityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckIn_Resolves_The_Checking_In_Staff_Member_To_A_Display_Name_Not_A_Raw_Id()
    {
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(PoolFacility(capacity: 10));
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(0);
        Guid staffId = Guid.NewGuid();
        _currentUser.UserId.Returns(staffId);
        _users.GetByIdAsync(new UserId(staffId), Arg.Any<CancellationToken>())
            .Returns(User.Register(TenantId, "guard@mycondo.test", "hash", "Ahsan Uddin", null, Now));

        PoolSessionDto result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.CheckedInByDisplayName.Should().Be("Ahsan Uddin");
        result.CheckedOutByDisplayName.Should().BeNull();
    }

    [Fact]
    public async Task CheckIn_Throws_When_At_Capacity_Without_Override()
    {
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(PoolFacility(capacity: 5));
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(5);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*capacity*");
        _poolSessions.DidNotReceive().Add(Arg.Any<PoolSession>());
    }

    [Fact]
    public async Task CheckIn_Throws_When_At_Capacity_With_Reason_But_No_Override_Permission()
    {
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(PoolFacility(capacity: 5));
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(5);
        _currentUser.HasPermission("pool.override").Returns(false);
        CheckInPoolSessionCommand command = ValidCommand() with { OverrideReason = "Manager approved extra capacity" };

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*pool.override*");
    }

    [Fact]
    public async Task CheckIn_Succeeds_At_Capacity_With_Reason_And_Override_Permission()
    {
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(PoolFacility(capacity: 5));
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(5);
        _currentUser.HasPermission("pool.override").Returns(true);
        CheckInPoolSessionCommand command = ValidCommand() with { OverrideReason = "Manager approved extra capacity" };

        PoolSessionDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.OverrideReason.Should().Be("Manager approved extra capacity");
    }

    [Fact]
    public async Task CheckIn_Throws_When_Facility_Is_Not_A_Pool()
    {
        Facility hall = Facility.Create(
            TenantId, BuildingId.New(), "Community Hall", FacilityType.CommunityHall, 100, null, null, true, 500m,
            2000m, 24, 50m, null, null, false, false, Now);
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(hall);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CheckIn_Throws_When_Facility_Closed_By_Blackout_Today()
    {
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(PoolFacility(capacity: 10));
        DateOnly today = DateOnly.FromDateTime(Now.UtcDateTime);
        BlackoutDate closure = BlackoutDate.Create(TenantId, FacilityId, today, today, "Annual maintenance", Now);
        _blackoutDates.GetActiveForFacilityAsync(TenantId, FacilityId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<BlackoutDate>)[closure]);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*closed today*");
    }

    [Fact]
    public async Task CheckIn_Throws_When_Outside_Operating_Hours_Without_Override()
    {
        // Asia/Dhaka is UTC+6 with no DST — 17:00 UTC is 23:00 local, outside a 06:00–22:00 window.
        DateTimeOffset outsideHoursUtc = new(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(outsideHoursUtc);
        Facility facility = Facility.Create(
            TenantId, BuildingId.New(), "Main Pool", FacilityType.SwimmingPool, 10, new TimeOnly(6, 0), new TimeOnly(22, 0),
            false, null, null, 24, 0m, 200m, 12, false, false, outsideHoursUtc);
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(facility);
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(0);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*operating hours*");
    }

    [Fact]
    public async Task CheckIn_Succeeds_Within_Operating_Hours()
    {
        // 08:00 UTC is 14:00 Dhaka local, inside a 06:00–22:00 window.
        DateTimeOffset withinHoursUtc = new(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(withinHoursUtc);
        Facility facility = Facility.Create(
            TenantId, BuildingId.New(), "Main Pool", FacilityType.SwimmingPool, 10, new TimeOnly(6, 0), new TimeOnly(22, 0),
            false, null, null, 24, 0m, 200m, 12, false, false, withinHoursUtc);
        _facilities.GetByIdAsync(FacilityId, Arg.Any<CancellationToken>()).Returns(facility);
        _poolSessions.CountOpenAsync(TenantId, FacilityId, Arg.Any<CancellationToken>()).Returns(0);

        PoolSessionDto result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");
    }
}
