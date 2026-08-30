using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.UnitTests.Features.Platform.SubscriptionInvoices;

public class SubscriptionInvoiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly OrganizationSubscriptionId OrganizationSubscriptionId = OrganizationSubscriptionId.New();
    private static readonly SubscriptionPackageVersionId PackageVersionId = SubscriptionPackageVersionId.New();
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    private static SubscriptionInvoiceLineInput OneLine(decimal basePrice = 8000m, decimal discount = 0m) => new(
        PackageVersionId, "Professional", 1, BillingCycle.Monthly, basePrice, discount, basePrice - discount,
        "Professional (Monthly)");

    private static (SubscriptionInvoice Invoice, IReadOnlyList<SubscriptionInvoiceLine> Lines) IssueInvoice(
        params SubscriptionInvoiceLineInput[] lines) =>
        SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId, "SUBINV-2026-000001", PeriodStart, PeriodEnd,
            PeriodStart, PeriodEnd, "BDT", lines, Now);

    [Fact]
    public void Issue_Computes_TotalAmount_From_Lines_And_Starts_Issued()
    {
        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) =
            IssueInvoice(OneLine(8000m), OneLine(500m));

        invoice.TenantId.Should().Be(TenantId);
        invoice.OrganizationSubscriptionId.Should().Be(OrganizationSubscriptionId);
        invoice.InvoiceNumber.Should().Be("SUBINV-2026-000001");
        invoice.Currency.Should().Be("BDT");
        invoice.TotalAmount.Should().Be(8500m);
        invoice.OutstandingAmount.Should().Be(8500m);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
        invoice.IssuedAtUtc.Should().Be(Now);
        lines.Should().HaveCount(2);
    }

    [Fact]
    public void Issue_Throws_For_Empty_TenantId()
    {
        Action act = () => SubscriptionInvoice.Issue(
            Guid.Empty, OrganizationSubscriptionId, "SUBINV-2026-000001", PeriodStart, PeriodEnd,
            PeriodStart, PeriodEnd, "BDT", [OneLine()], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_Throws_When_No_Lines()
    {
        Action act = () => IssueInvoice();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_Throws_When_A_Line_Amount_Is_Not_Positive()
    {
        Action act = () => IssueInvoice(OneLine(0m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_Throws_When_BillingPeriodEnd_Precedes_BillingPeriodStart()
    {
        Action act = () => SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId, "SUBINV-2026-000001", PeriodEnd, PeriodStart,
            PeriodStart, PeriodEnd, "BDT", [OneLine()], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_Throws_When_DueDate_Precedes_IssueDate()
    {
        Action act = () => SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId, "SUBINV-2026-000001", PeriodStart, PeriodEnd,
            PeriodEnd, PeriodStart, "BDT", [OneLine()], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkPaid_From_Issued_Zeroes_Outstanding_And_Sets_PaidAt()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());
        DateTimeOffset paidAt = Now.AddDays(5);

        invoice.MarkPaid(paidAt);

        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Paid);
        invoice.OutstandingAmount.Should().Be(0m);
        invoice.PaidAtUtc.Should().Be(paidAt);
    }

    [Fact]
    public void MarkPaid_From_Paid_Throws_Invalid_Transition()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());
        invoice.MarkPaid(Now.AddDays(5));

        Action act = () => invoice.MarkPaid(Now.AddDays(6));

        act.Should().Throw<SubscriptionInvoiceInvalidTransitionException>();
    }

    [Fact]
    public void Void_From_Issued_Zeroes_Outstanding_And_Records_Reason()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());
        DateTimeOffset voidedAt = Now.AddDays(1);

        invoice.Void("Billed against the wrong subscription", voidedAt);

        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Void);
        invoice.OutstandingAmount.Should().Be(0m);
        invoice.VoidedAtUtc.Should().Be(voidedAt);
        invoice.VoidReason.Should().Be("Billed against the wrong subscription");
    }

    [Fact]
    public void Void_After_Paid_Throws_Invalid_Transition()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());
        invoice.MarkPaid(Now.AddDays(5));

        Action act = () => invoice.Void("too late", Now.AddDays(6));

        act.Should().Throw<SubscriptionInvoiceInvalidTransitionException>();
    }

    [Fact]
    public void Void_Throws_For_Missing_Reason()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());

        Action act = () => invoice.Void(" ", Now.AddDays(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cancel_From_Issued_Zeroes_Outstanding_And_Records_Reason()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());
        DateTimeOffset canceledAt = Now.AddDays(2);

        invoice.Cancel("Commercial waiver", canceledAt);

        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Canceled);
        invoice.OutstandingAmount.Should().Be(0m);
        invoice.CanceledAtUtc.Should().Be(canceledAt);
        invoice.CancelReason.Should().Be("Commercial waiver");
    }

    [Fact]
    public void Cancel_After_Void_Throws_Invalid_Transition()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine());
        invoice.Void("wrong subscription", Now.AddDays(1));

        Action act = () => invoice.Cancel("too late", Now.AddDays(2));

        act.Should().Throw<SubscriptionInvoiceInvalidTransitionException>();
    }

    [Fact]
    public void ApplyPayment_Partial_Reduces_Outstanding_And_Leaves_Issued()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));

        invoice.ApplyPayment(3000m, "BDT", Now.AddDays(5));

        invoice.OutstandingAmount.Should().Be(5000m);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
        invoice.PaidAtUtc.Should().BeNull();
    }

    [Fact]
    public void ApplyPayment_Full_Zeroes_Outstanding_And_Transitions_To_Paid()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));
        DateTimeOffset paidAt = Now.AddDays(5);

        invoice.ApplyPayment(8000m, "BDT", paidAt);

        invoice.OutstandingAmount.Should().Be(0m);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().Be(paidAt);
    }

    [Fact]
    public void ApplyPayment_Two_Partial_Payments_Settle_The_Invoice()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));

        invoice.ApplyPayment(5000m, "BDT", Now.AddDays(3));
        invoice.ApplyPayment(3000m, "BDT", Now.AddDays(6));

        invoice.OutstandingAmount.Should().Be(0m);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Paid);
    }

    [Fact]
    public void ApplyPayment_Throws_When_Amount_Is_Not_Positive()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));

        Action act = () => invoice.ApplyPayment(0m, "BDT", Now.AddDays(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ApplyPayment_Throws_When_Amount_Exceeds_Outstanding()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));

        Action act = () => invoice.ApplyPayment(8000.01m, "BDT", Now.AddDays(1));

        act.Should().Throw<SubscriptionInvoicePaymentExceedsOutstandingException>();
    }

    [Fact]
    public void ApplyPayment_Throws_When_Currency_Does_Not_Match_The_Invoice()
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));

        Action act = () => invoice.ApplyPayment(1000m, "USD", Now.AddDays(1));

        act.Should().Throw<SubscriptionInvoiceCurrencyMismatchException>();
    }

    [Theory]
    [InlineData(SubscriptionInvoiceStatus.Paid)]
    [InlineData(SubscriptionInvoiceStatus.Void)]
    [InlineData(SubscriptionInvoiceStatus.Canceled)]
    public void ApplyPayment_Throws_When_Invoice_Is_Not_Issued(SubscriptionInvoiceStatus status)
    {
        (SubscriptionInvoice invoice, _) = IssueInvoice(OneLine(8000m));
        switch (status)
        {
            case SubscriptionInvoiceStatus.Paid:
                invoice.MarkPaid(Now.AddDays(1));
                break;
            case SubscriptionInvoiceStatus.Void:
                invoice.Void("wrong subscription", Now.AddDays(1));
                break;
            case SubscriptionInvoiceStatus.Canceled:
                invoice.Cancel("commercial waiver", Now.AddDays(1));
                break;
        }

        Action act = () => invoice.ApplyPayment(1000m, "BDT", Now.AddDays(2));

        act.Should().Throw<SubscriptionInvoiceNotPayableException>();
    }
}
