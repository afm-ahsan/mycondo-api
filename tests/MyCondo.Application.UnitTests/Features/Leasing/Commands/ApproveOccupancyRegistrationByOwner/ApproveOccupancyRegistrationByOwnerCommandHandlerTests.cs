using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.Commands.ApproveOccupancyRegistrationByOwner;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations.Exceptions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.ApproveOccupancyRegistrationByOwner;

/// <summary>
/// The "must be Submitted" transition guard itself is already covered at the domain level
/// (OccupancyRegistrationTests.ApproveByOwner_Throws_When_Not_Submitted). This covers what the
/// handler does in addition to that guard: recording an <see cref="OccupancyRegistrationStatusHistory"/>
/// audit entry with the correct from/to status — the mechanism the project uses in place of a
/// separate notification framework (see leasing feature docs).
/// </summary>
public class ApproveOccupancyRegistrationByOwnerCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly ResidentId ResidentId = ResidentId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IOccupancyRegistrationStatusHistoryRepository _history = Substitute.For<IOccupancyRegistrationStatusHistoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ApproveOccupancyRegistrationByOwnerCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
    }

    private ApproveOccupancyRegistrationByOwnerCommandHandler CreateHandler() => new(
        _registrations, _history, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<ApproveOccupancyRegistrationByOwnerCommandHandler>>());

    private static OccupancyRegistration SubmittedRegistration()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", "01700000000", "jane@example.com",
            "1234567890", new DateOnly(1990, 1, 1), "Female", null, null, null, null, null, null, null,
            "123 Example Road, Dhaka", "John Doe", "01711111111", null, Now);
        registration.Submit(Guid.NewGuid(), Now);
        return registration;
    }

    [Fact]
    public async Task Records_A_Submitted_To_OwnerApproved_History_Entry_On_Approval()
    {
        OccupancyRegistration registration = SubmittedRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        OccupancyRegistrationDto result = await CreateHandler().Handle(
            new ApproveOccupancyRegistrationByOwnerCommand(registration.Id.Value), CancellationToken.None);

        result.Status.Should().Be(OccupancyRegistrationStatus.OwnerApproved.ToString());
        _history.Received(1).Add(Arg.Is<OccupancyRegistrationStatusHistory>(h =>
            h.FromStatus == OccupancyRegistrationStatus.Submitted &&
            h.ToStatus == OccupancyRegistrationStatus.OwnerApproved &&
            h.OccupancyRegistrationId == registration.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_Not_Record_History_Or_Save_When_The_Registration_Is_Not_Submitted()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", "01700000000", "jane@example.com",
            "1234567890", new DateOnly(1990, 1, 1), "Female", null, null, null, null, null, null, null,
            "123 Example Road, Dhaka", "John Doe", "01711111111", null, Now);
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        Func<Task> act = () => CreateHandler().Handle(
            new ApproveOccupancyRegistrationByOwnerCommand(registration.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<OccupancyRegistrationInvalidTransitionException>();
        _history.DidNotReceive().Add(Arg.Any<OccupancyRegistrationStatusHistory>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
