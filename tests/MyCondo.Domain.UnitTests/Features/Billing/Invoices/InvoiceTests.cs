using AwesomeAssertions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.Invoices.Exceptions;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Billing.Invoices;

public class InvoiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly LedgerPostingId LedgerPostingId = LedgerPostingId.New();
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    private static InvoiceLineInput OneLine(decimal amount = 1500m) => new(
        ServiceChargeRuleId.New(), "Standard Charge", "ServiceCharge", "FixedAmount", amount, null, 1m,
        amount, "Standard Charge (ServiceCharge)");

    private static (Invoice Invoice, IReadOnlyList<InvoiceLine> Lines) IssueInvoice(params InvoiceLineInput[] lines) =>
        Invoice.Issue(
            TenantId, BuildingId, FlatId, "INV-TEST-2026-000001", InvoiceSource.ServiceCharge, PeriodStart, PeriodEnd,
            PeriodStart, PeriodEnd, lines, LedgerPostingId, LedgerAccountType.AssociationRevenue, null, Now);

    [Fact]
    public void Issue_Computes_Subtotal_And_Total_From_Lines()
    {
        (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = IssueInvoice(OneLine(1500m), OneLine(300m));

        invoice.SubtotalAmount.Should().Be(1800m);
        invoice.TotalAmount.Should().Be(1800m);
        invoice.AmountPaid.Should().Be(0m);
        invoice.Balance.Should().Be(1800m);
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        lines.Should().HaveCount(2);
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
    public void ApplyPayment_Partial_Sets_PartiallyPaid_Status()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));

        invoice.ApplyPayment(400m);

        invoice.AmountPaid.Should().Be(400m);
        invoice.Balance.Should().Be(600m);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void ApplyPayment_Full_Sets_Paid_Status()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));

        invoice.ApplyPayment(1000m);

        invoice.Balance.Should().Be(0m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void ApplyPayment_Throws_When_Amount_Exceeds_Balance()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));

        Action act = () => invoice.ApplyPayment(1000.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ApplyPayment_Throws_When_Amount_Not_Positive()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));

        Action act = () => invoice.ApplyPayment(0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Void_Sets_Status_And_Metadata_When_Unpaid()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        Guid voidedBy = Guid.NewGuid();
        LedgerPostingId reversalPostingId = LedgerPostingId.New();

        invoice.Void("Issued in error", voidedBy, reversalPostingId, Now.AddDays(1));

        invoice.Status.Should().Be(InvoiceStatus.Void);
        invoice.VoidedBy.Should().Be(voidedBy);
        invoice.VoidReason.Should().Be("Issued in error");
        invoice.VoidedAtUtc.Should().Be(Now.AddDays(1));
        invoice.VoidLedgerPostingId.Should().Be(reversalPostingId);
    }

    [Fact]
    public void Void_Throws_When_Already_Void()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        invoice.Void("First void", Guid.NewGuid(), LedgerPostingId.New(), Now);

        Action act = () => invoice.Void("Second attempt", Guid.NewGuid(), LedgerPostingId.New(), Now.AddDays(1));

        act.Should().Throw<InvoiceAlreadyVoidException>();
    }

    [Fact]
    public void Void_Throws_When_Invoice_Has_A_Payment_Allocated()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        invoice.ApplyPayment(200m);

        Action act = () => invoice.Void("Trying anyway", Guid.NewGuid(), LedgerPostingId.New(), Now);

        act.Should().Throw<InvoiceCannotBeVoidedException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Void_Throws_When_Reason_Is_Blank(string reason)
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));

        Action act = () => invoice.Void(reason, Guid.NewGuid(), LedgerPostingId.New(), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReverseAppliedPayment_Full_Restores_Issued_Status()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        invoice.ApplyPayment(1000m);

        invoice.ReverseAppliedPayment(1000m);

        invoice.AmountPaid.Should().Be(0m);
        invoice.Balance.Should().Be(1000m);
        invoice.Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public void ReverseAppliedPayment_Partial_Leaves_PartiallyPaid_Status()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        invoice.ApplyPayment(400m);
        invoice.ApplyPayment(600m); // now Paid

        invoice.ReverseAppliedPayment(600m); // undo only the second payment's allocation

        invoice.AmountPaid.Should().Be(400m);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void ReverseAppliedPayment_Throws_When_Amount_Exceeds_AmountPaid()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        invoice.ApplyPayment(400m);

        Action act = () => invoice.ReverseAppliedPayment(400.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReverseAppliedPayment_Throws_When_Amount_Not_Positive()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(1000m));
        invoice.ApplyPayment(400m);

        Action act = () => invoice.ReverseAppliedPayment(0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Waive_Partial_Sets_PartiallyWaived_Status_And_Reduces_Balance()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));
        Guid waivedBy = Guid.NewGuid();
        LedgerPostingId waivePostingId = LedgerPostingId.New();

        invoice.Waive(200m, "Goodwill", waivedBy, waivePostingId, Now.AddDays(1));

        invoice.WaivedAmount.Should().Be(200m);
        invoice.Balance.Should().Be(300m);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyWaived);
        invoice.WaivedBy.Should().Be(waivedBy);
        invoice.WaiveReason.Should().Be("Goodwill");
        invoice.WaiveLedgerPostingId.Should().Be(waivePostingId);
    }

    [Fact]
    public void Waive_Full_Sets_Waived_Status()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));

        invoice.Waive(500m, "Fully waived", Guid.NewGuid(), LedgerPostingId.New(), Now);

        invoice.Balance.Should().Be(0m);
        invoice.Status.Should().Be(InvoiceStatus.Waived);
    }

    [Fact]
    public void Waive_Then_ApplyPayment_For_Remainder_Sets_Paid_Status()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));
        invoice.Waive(200m, "Goodwill", Guid.NewGuid(), LedgerPostingId.New(), Now);

        invoice.ApplyPayment(300m);

        invoice.Balance.Should().Be(0m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void Waive_Throws_On_Second_Attempt()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));
        invoice.Waive(100m, "First", Guid.NewGuid(), LedgerPostingId.New(), Now);

        Action act = () => invoice.Waive(100m, "Second", Guid.NewGuid(), LedgerPostingId.New(), Now);

        act.Should().Throw<InvoiceAlreadyWaivedException>();
    }

    [Fact]
    public void Waive_Throws_When_Amount_Exceeds_Balance()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));

        Action act = () => invoice.Waive(500.01m, "Too much", Guid.NewGuid(), LedgerPostingId.New(), Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Waive_Throws_When_Invoice_Already_Void()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));
        invoice.Void("Cancelled", Guid.NewGuid(), LedgerPostingId.New(), Now);

        Action act = () => invoice.Waive(100m, "Too late", Guid.NewGuid(), LedgerPostingId.New(), Now);

        act.Should().Throw<InvoiceAlreadyVoidException>();
    }

    [Fact]
    public void Void_Throws_When_Invoice_Has_A_Waiver_Applied()
    {
        (Invoice invoice, _) = IssueInvoice(OneLine(500m));
        invoice.Waive(100m, "Goodwill", Guid.NewGuid(), LedgerPostingId.New(), Now);

        Action act = () => invoice.Void("Trying anyway", Guid.NewGuid(), LedgerPostingId.New(), Now);

        act.Should().Throw<InvoiceCannotBeVoidedException>();
    }
}
