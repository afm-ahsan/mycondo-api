using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Commands.CancelBooking;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Commands.CancelBooking;

/// <summary>
/// Proves the deposit-settlement math in <see cref="CancelBookingCommandHandler"/>: cancelling at or
/// before the booking's snapshotted cancellation deadline refunds the deposit in full; cancelling
/// within the deadline forfeits the snapshotted deduction percentage.
/// </summary>
public class CancelBookingCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly FacilityId FacilityId = FacilityId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly ILedgerPostingRepository _ledgerPostings = Substitute.For<ILedgerPostingRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CancelBookingCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private CancelBookingCommandHandler CreateHandler() => new(
        _bookings, _ledgerPostings, _ledgerEntries, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CancelBookingCommandHandler>>());

    private static Booking ConfirmedBookingStartingIn(TimeSpan untilStart, decimal deposit, int deadlineHours, decimal deductionPercentage)
    {
        Booking booking = Booking.Request(
            TenantId, FacilityId, BuildingId, FlatId, "Anniversary dinner", Now.Add(untilStart), Now.Add(untilStart).AddHours(3),
            30, 30, 40, false, 0m, deposit, deadlineHours, deductionPercentage, Now, Now);
        booking.Submit(Now);
        booking.ConfirmPayment(null, LedgerPostingId.New(), Now);
        return booking;
    }

    [Fact]
    public async Task Refunds_Deposit_In_Full_When_Cancelled_Before_Deadline()
    {
        Booking booking = ConfirmedBookingStartingIn(TimeSpan.FromDays(10), 2000m, 24, 50m);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        BookingDto result = await CreateHandler().Handle(new CancelBookingCommand(booking.Id.Value, "Change of plans"), CancellationToken.None);

        result.DepositRefundedAmount.Should().Be(2000m);
        result.DepositDeductedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Forfeits_DeductionPercentage_When_Cancelled_Within_Deadline()
    {
        Booking booking = ConfirmedBookingStartingIn(TimeSpan.FromHours(2), 2000m, 24, 50m);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        BookingDto result = await CreateHandler().Handle(new CancelBookingCommand(booking.Id.Value, "Emergency"), CancellationToken.None);

        result.DepositDeductedAmount.Should().Be(1000m);
        result.DepositRefundedAmount.Should().Be(1000m);
        _ledgerPostings.Received(1).Add(Arg.Is<LedgerPosting>(p => p.ReferenceType == "FacilityBookingCancellationSettlement"));
    }

    [Fact]
    public async Task Skips_Settlement_When_No_Deposit_Was_Collected()
    {
        Booking booking = Booking.Request(
            TenantId, FacilityId, BuildingId, FlatId, "Free meeting", Now.AddDays(5), Now.AddDays(5).AddHours(1), 0, 0,
            10, false, 0m, 0m, 24, 0m, null, Now);
        booking.Submit(Now);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        BookingDto result = await CreateHandler().Handle(new CancelBookingCommand(booking.Id.Value, "No longer needed"), CancellationToken.None);

        result.DepositRefundedAmount.Should().BeNull();
        result.DepositDeductedAmount.Should().BeNull();
        _ledgerPostings.DidNotReceive().Add(Arg.Any<LedgerPosting>());
    }
}
