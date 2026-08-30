using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Domain.UnitTests.Features.Platform.SubscriptionPayments;

public class SubscriptionPaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly SubscriptionInvoiceId InvoiceId = SubscriptionInvoiceId.New();
    private static readonly DateOnly PaymentDate = new(2026, 3, 5);

    [Fact]
    public void Record_Populates_Every_Field_And_Trims_Text()
    {
        SubscriptionPayment payment = SubscriptionPayment.Record(
            TenantId, InvoiceId, 5000m, " bdt ", PaymentDate, " BANK-REF-001 ", "  Wire transfer  ", Now);

        payment.TenantId.Should().Be(TenantId);
        payment.SubscriptionInvoiceId.Should().Be(InvoiceId);
        payment.Amount.Should().Be(5000m);
        payment.Currency.Should().Be("bdt");
        payment.PaymentDate.Should().Be(PaymentDate);
        payment.ReferenceNumber.Should().Be("BANK-REF-001");
        payment.Notes.Should().Be("Wire transfer");
        payment.RecordedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Record_Allows_Null_Notes()
    {
        SubscriptionPayment payment = SubscriptionPayment.Record(
            TenantId, InvoiceId, 5000m, "BDT", PaymentDate, "BANK-REF-002", null, Now);

        payment.Notes.Should().BeNull();
    }

    [Fact]
    public void Record_Throws_For_Empty_TenantId()
    {
        Action act = () => SubscriptionPayment.Record(
            Guid.Empty, InvoiceId, 5000m, "BDT", PaymentDate, "BANK-REF-003", null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_Throws_When_Amount_Is_Not_Positive()
    {
        Action act = () => SubscriptionPayment.Record(
            TenantId, InvoiceId, 0m, "BDT", PaymentDate, "BANK-REF-004", null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_Throws_For_Missing_Currency()
    {
        Action act = () => SubscriptionPayment.Record(
            TenantId, InvoiceId, 5000m, " ", PaymentDate, "BANK-REF-005", null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_Throws_For_Missing_ReferenceNumber()
    {
        Action act = () => SubscriptionPayment.Record(
            TenantId, InvoiceId, 5000m, "BDT", PaymentDate, " ", null, Now);

        act.Should().Throw<ArgumentException>();
    }
}
