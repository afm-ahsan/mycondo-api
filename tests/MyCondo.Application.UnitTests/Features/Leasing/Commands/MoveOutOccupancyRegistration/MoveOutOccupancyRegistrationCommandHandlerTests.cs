using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.Commands.MoveOutOccupancyRegistration;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations.Exceptions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.MoveOutOccupancyRegistration;

/// <summary>Proves the move-out cascade deactivates every active household member in the same
/// transaction, matching the requirement that access-relevant info drop out of active security
/// views once occupancy ends.</summary>
public class MoveOutOccupancyRegistrationCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IHouseholdMemberRepository _members = Substitute.For<IHouseholdMemberRepository>();
    private readonly IOccupancyRegistrationStatusHistoryRepository _history = Substitute.For<IOccupancyRegistrationStatusHistoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public MoveOutOccupancyRegistrationCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private MoveOutOccupancyRegistrationCommandHandler CreateHandler() => new(
        _registrations, _members, _history, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<MoveOutOccupancyRegistrationCommandHandler>>());

    private static OccupancyRegistration ActiveRegistration()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId.New(), ResidentId.New(), ResidentType.Occupant, "Jane Doe", null, null, null, null, null, null,
            null, null, Now);
        registration.Submit(Guid.NewGuid(), Now);
        registration.ApproveByOwner(Guid.NewGuid(), Now);
        registration.VerifyByManagement(Guid.NewGuid(), Now);
        registration.Activate(Now);
        return registration;
    }

    [Fact]
    public async Task Deactivates_All_Active_Household_Members()
    {
        OccupancyRegistration registration = ActiveRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        HouseholdMember activeMember = HouseholdMember.Add(
            TenantId, registration.Id, "John Doe", "Spouse", null, null, null, Now);
        HouseholdMember alreadyInactiveMember = HouseholdMember.Add(
            TenantId, registration.Id, "Old Member", "Sibling", null, null, null, Now);
        alreadyInactiveMember.Deactivate();
        _members.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<HouseholdMember> { activeMember, alreadyInactiveMember });

        OccupancyRegistrationDto result = await CreateHandler().Handle(
            new MoveOutOccupancyRegistrationCommand(registration.Id.Value, "Relocated"), CancellationToken.None);

        result.Status.Should().Be(nameof(OccupancyRegistrationStatus.MovedOut));
        activeMember.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_When_Registration_Is_Not_Active()
    {
        OccupancyRegistration draft = OccupancyRegistration.Register(
            TenantId, FlatId.New(), ResidentId.New(), ResidentType.Occupant, "Jane Doe", null, null, null, null, null, null,
            null, null, Now);
        _registrations.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        Func<Task> act = () => CreateHandler()
            .Handle(new MoveOutOccupancyRegistrationCommand(draft.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<OccupancyRegistrationInvalidTransitionException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Registration_Belongs_To_Another_Tenant()
    {
        OccupancyRegistration otherTenantRegistration = OccupancyRegistration.Register(
            Guid.NewGuid(), FlatId.New(), ResidentId.New(), ResidentType.Occupant, "Jane Doe", null, null, null, null, null,
            null, null, null, Now);
        _registrations.GetByIdAsync(otherTenantRegistration.Id, Arg.Any<CancellationToken>()).Returns(otherTenantRegistration);

        Func<Task> act = () => CreateHandler()
            .Handle(new MoveOutOccupancyRegistrationCommand(otherTenantRegistration.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
