using AwesomeAssertions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Bookings.Exceptions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Amenities.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FacilityId FacilityId = FacilityId.New();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();

    private static Booking RequestBooking(
        bool approvalRequired = true, decimal bookingCharge = 500m, decimal deposit = 2000m,
        int cancellationDeadlineHours = 24, decimal cancellationDeductionPercentage = 50m) =>
        Booking.Request(
            TenantId, FacilityId, BuildingId, FlatId, "Birthday party", Now.AddDays(10), Now.AddDays(10).AddHours(4),
            60, 60, 50, approvalRequired, bookingCharge, deposit, cancellationDeadlineHours,
            cancellationDeductionPercentage, Now, Now);

    [Fact]
    public void Request_Starts_Draft_And_Computes_PaymentRequired()
    {
        Booking booking = RequestBooking();

        booking.Status.Should().Be(BookingStatus.Draft);
        booking.PaymentRequired.Should().BeTrue();
        booking.Version.Should().Be(1);
    }

    [Fact]
    public void Request_PaymentRequired_False_When_Charge_And_Deposit_Are_Zero()
    {
        Booking booking = Booking.Request(
            TenantId, FacilityId, BuildingId, FlatId, "Free meeting", Now.AddDays(5), Now.AddDays(5).AddHours(1), 0, 0,
            10, false, 0m, 0m, 24, 0m, null, Now);

        booking.PaymentRequired.Should().BeFalse();
    }

    [Fact]
    public void Request_Throws_When_EndAtUtc_Not_After_StartAtUtc()
    {
        Action act = () => Booking.Request(
            TenantId, FacilityId, BuildingId, FlatId, "Bad window", Now, Now, 0, 0, 10, false, 0m, 0m, 24, 0m, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_Goes_To_PendingApproval_When_ApprovalRequired()
    {
        Booking booking = RequestBooking(approvalRequired: true);

        booking.Submit(Now);

        booking.Status.Should().Be(BookingStatus.PendingApproval);
    }

    [Fact]
    public void Submit_Goes_To_AwaitingPayment_When_No_Approval_But_Payment_Required()
    {
        Booking booking = RequestBooking(approvalRequired: false);

        booking.Submit(Now);

        booking.Status.Should().Be(BookingStatus.AwaitingPayment);
    }

    [Fact]
    public void Submit_Goes_Directly_To_Confirmed_When_Free_And_No_Approval()
    {
        Booking booking = Booking.Request(
            TenantId, FacilityId, BuildingId, FlatId, "Free meeting", Now.AddDays(5), Now.AddDays(5).AddHours(1), 0, 0,
            10, false, 0m, 0m, 24, 0m, null, Now);

        booking.Submit(Now);

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Approve_Transitions_PendingApproval_To_AwaitingPayment()
    {
        Booking booking = RequestBooking(approvalRequired: true);
        booking.Submit(Now);

        booking.Approve(Guid.NewGuid(), Now);

        booking.Status.Should().Be(BookingStatus.AwaitingPayment);
    }

    [Fact]
    public void Reject_Transitions_PendingApproval_To_Rejected()
    {
        Booking booking = RequestBooking(approvalRequired: true);
        booking.Submit(Now);

        booking.Reject("Facility unavailable that date", Guid.NewGuid(), Now);

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectedReason.Should().Be("Facility unavailable that date");
    }

    [Fact]
    public void ConfirmPayment_Transitions_AwaitingPayment_To_Confirmed_And_Stores_References()
    {
        Booking booking = RequestBooking(approvalRequired: false);
        booking.Submit(Now);
        InvoiceId invoiceId = InvoiceId.New();
        LedgerPostingId depositPostingId = LedgerPostingId.New();

        booking.ConfirmPayment(invoiceId, depositPostingId, Now);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.InvoiceId.Should().Be(invoiceId);
        booking.DepositCollectionPostingId.Should().Be(depositPostingId);
    }

    private static Booking ConfirmedBooking()
    {
        Booking booking = RequestBooking(approvalRequired: false);
        booking.Submit(Now);
        booking.ConfirmPayment(InvoiceId.New(), LedgerPostingId.New(), Now);
        return booking;
    }

    [Fact]
    public void CheckIn_Then_Complete_Then_Inspect_ClosesTheBooking()
    {
        Booking booking = ConfirmedBooking();
        booking.CheckIn(Guid.NewGuid(), Now);
        booking.Complete(Now);

        booking.Inspect(Guid.NewGuid(), "All clean", null, 1800m, 200m, LedgerPostingId.New(), Now);

        booking.Status.Should().Be(BookingStatus.ClosedAfterInspection);
        booking.DepositRefundedAmount.Should().Be(1800m);
        booking.DepositDeductedAmount.Should().Be(200m);
    }

    [Fact]
    public void Inspect_Throws_When_Not_Completed()
    {
        Booking booking = ConfirmedBooking();

        Action act = () => booking.Inspect(Guid.NewGuid(), null, null, null, null, null, Now);

        act.Should().Throw<BookingInvalidTransitionException>();
    }

    [Theory]
    [InlineData(BookingStatus.Draft)]
    [InlineData(BookingStatus.PendingApproval)]
    [InlineData(BookingStatus.AwaitingPayment)]
    [InlineData(BookingStatus.Confirmed)]
    public void Cancel_Succeeds_From_PreCheckIn_Statuses(BookingStatus status)
    {
        Booking booking = status switch
        {
            BookingStatus.Draft => RequestBooking(),
            BookingStatus.PendingApproval => Submitted(RequestBooking(approvalRequired: true)),
            BookingStatus.AwaitingPayment => Submitted(RequestBooking(approvalRequired: false)),
            BookingStatus.Confirmed => ConfirmedBooking(),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        booking.Cancel("Change of plans", Guid.NewGuid(), null, null, null, Now);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    private static Booking Submitted(Booking booking)
    {
        booking.Submit(Now);
        return booking;
    }

    [Fact]
    public void Cancel_Throws_When_Already_CheckedIn()
    {
        Booking booking = ConfirmedBooking();
        booking.CheckIn(Guid.NewGuid(), Now);

        Action act = () => booking.Cancel("Too late", Guid.NewGuid(), null, null, null, Now);

        act.Should().Throw<BookingInvalidTransitionException>();
    }

    [Fact]
    public void MarkNoShow_Transitions_Confirmed_To_NoShow()
    {
        Booking booking = ConfirmedBooking();

        booking.MarkNoShow(1000m, 1000m, LedgerPostingId.New(), Now);

        booking.Status.Should().Be(BookingStatus.NoShow);
        booking.DepositDeductedAmount.Should().Be(1000m);
    }

    [Fact]
    public void MarkNoShow_Throws_When_Not_Confirmed()
    {
        Booking booking = RequestBooking();

        Action act = () => booking.MarkNoShow(null, null, null, Now);

        act.Should().Throw<BookingInvalidTransitionException>();
    }
}
