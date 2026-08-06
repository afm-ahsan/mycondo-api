using AwesomeAssertions;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.Payments.Exceptions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Payments.Payments;

public class PaymentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly LedgerPostingId LedgerPostingId = LedgerPostingId.New();
    private static readonly DateOnly BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow);

    private static Payment RecordPayment() =>
        Payment.Record(TenantId, FlatId, 1000m, PaymentMethod.Cash, "REF-1", BusinessDate, Guid.NewGuid(), LedgerPostingId, Now);

    [Fact]
    public void Record_Starts_Posted_With_Version_One()
    {
        Payment payment = RecordPayment();

        payment.Status.Should().Be(PaymentStatus.Posted);
        payment.LedgerPostingId.Should().Be(LedgerPostingId);
        payment.Version.Should().Be(1);
    }

    [Fact]
    public void Record_Throws_When_Amount_Is_Not_Positive()
    {
        Action act = () => Payment.Record(TenantId, FlatId, 0m, PaymentMethod.Cash, null, BusinessDate, null, LedgerPostingId, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Reverse_Sets_Reversed_Status_And_Fields()
    {
        Payment payment = RecordPayment();
        Guid reversedBy = Guid.NewGuid();

        payment.Reverse("Duplicate entry", reversedBy, Now.AddHours(1));

        payment.Status.Should().Be(PaymentStatus.Reversed);
        payment.ReversedBy.Should().Be(reversedBy);
        payment.ReversalReason.Should().Be("Duplicate entry");
        payment.ReversedAtUtc.Should().Be(Now.AddHours(1));
        payment.Version.Should().Be(2);
    }

    [Fact]
    public void Reverse_Throws_When_Already_Reversed()
    {
        Payment payment = RecordPayment();
        payment.Reverse("First reversal", Guid.NewGuid(), Now);

        Action act = () => payment.Reverse("Second attempt", Guid.NewGuid(), Now.AddHours(1));

        act.Should().Throw<PaymentAlreadyReversedException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reverse_Throws_When_Reason_Is_Blank(string reason)
    {
        Payment payment = RecordPayment();

        Action act = () => payment.Reverse(reason, Guid.NewGuid(), Now);

        act.Should().Throw<ArgumentException>();
    }
}
