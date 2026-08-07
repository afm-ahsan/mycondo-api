using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Commands.ConfirmBookingPayment;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Amenities.Facilities;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Commands.ConfirmBookingPayment;

/// <summary>
/// Handler-level tests, same NSubstitute pattern as BillReadingCommandHandlerTests (Slice F) —
/// proving the handler bills the booking charge as a FacilityBooking invoice and posts the deposit as
/// a separate, balancing CashOrBank/RefundableDepositsHeld ledger posting, without needing a real
/// database.
/// </summary>
public class ConfirmBookingPaymentCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IInvoiceSequenceRepository _sequences = Substitute.For<IInvoiceSequenceRepository>();
    private readonly ILedgerPostingRepository _ledgerPostings = Substitute.For<ILedgerPostingRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ConfirmBookingPaymentCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
        _sequences.GetNextValueAsync(TenantId, BuildingId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(1);
        _buildings.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>())
            .Returns(Building.Create(TenantId, "Aisha Tower", "AISHA", null, Now));
    }

    private ConfirmBookingPaymentCommandHandler CreateHandler() => new(
        _bookings, _buildings, _invoices, _sequences, _ledgerPostings, _ledgerEntries, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<ConfirmBookingPaymentCommandHandler>>());

    private static Booking AwaitingPaymentBooking(decimal bookingCharge, decimal deposit)
    {
        Booking booking = Booking.Request(
            TenantId, FacilityId.New(), BuildingId, FlatId, "Wedding reception",
            Now.AddDays(20), Now.AddDays(20).AddHours(5), 30, 30, 80, false, bookingCharge, deposit, 24, 50m, Now, Now);
        booking.Submit(Now);
        return booking;
    }

    [Fact]
    public async Task Bills_Charge_And_Posts_Deposit_When_Both_Are_Positive()
    {
        Booking booking = AwaitingPaymentBooking(1200m, 3000m);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        BookingDto result = await CreateHandler().Handle(new ConfirmBookingPaymentCommand(booking.Id.Value), CancellationToken.None);

        result.Status.Should().Be("Confirmed");
        result.InvoiceId.Should().NotBeNull();
        result.DepositCollectionPostingId.Should().NotBeNull();

        _invoices.Received(1).Add(Arg.Is<Invoice>(i => i.Source == InvoiceSource.FacilityBooking && i.TotalAmount == 1200m));
        _ledgerPostings.Received(2).Add(Arg.Any<LedgerPosting>());
    }

    [Fact]
    public async Task Skips_Invoice_When_BookingCharge_Is_Zero()
    {
        Booking booking = AwaitingPaymentBooking(0m, 3000m);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        BookingDto result = await CreateHandler().Handle(new ConfirmBookingPaymentCommand(booking.Id.Value), CancellationToken.None);

        result.InvoiceId.Should().BeNull();
        result.DepositCollectionPostingId.Should().NotBeNull();
        _invoices.DidNotReceive().Add(Arg.Any<Invoice>());
        _ledgerPostings.Received(1).Add(Arg.Any<LedgerPosting>());
    }

    [Fact]
    public async Task Skips_Deposit_Posting_When_Deposit_Is_Zero()
    {
        Booking booking = AwaitingPaymentBooking(1200m, 0m);
        _bookings.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        BookingDto result = await CreateHandler().Handle(new ConfirmBookingPaymentCommand(booking.Id.Value), CancellationToken.None);

        result.DepositCollectionPostingId.Should().BeNull();
        result.InvoiceId.Should().NotBeNull();
        _ledgerPostings.Received(1).Add(Arg.Any<LedgerPosting>());
    }
}
