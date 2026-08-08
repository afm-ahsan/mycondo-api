using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.Commands.VoidInvoice;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.VoidInvoice;

/// <summary>
/// Proves the append-only ledger invariant at the handler level: <see cref="VoidInvoiceCommandHandler"/>
/// never mutates or deletes the invoice's original issue posting — it only adds a new reversing
/// posting/entries (see docs/conventions "append-only double-entry ledger... No deletes — voids create
/// reversing entries"). The invoice-status guards themselves (already-void, has-payment) are covered at
/// the domain level in InvoiceTests; this covers what the handler does in addition to that guard.
/// </summary>
public class VoidInvoiceCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly LedgerPostingId OriginalPostingId = LedgerPostingId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly ILedgerPostingRepository _ledgerPostings = Substitute.For<ILedgerPostingRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public VoidInvoiceCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
    }

    private VoidInvoiceCommandHandler CreateHandler() => new(
        _invoices, _ledgerPostings, _ledgerEntries, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<VoidInvoiceCommandHandler>>());

    private static Invoice UnpaidInvoice(decimal amount = 1800m)
    {
        InvoiceLineInput line = new(
            ServiceChargeRuleId.New(), "Standard Charge", "ServiceCharge", "FixedAmount", amount, null, 1m,
            amount, "Standard Charge (ServiceCharge)");
        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, "INV-TEST-2026-000001", InvoiceSource.ServiceCharge,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            [line], OriginalPostingId, Now);
        return invoice;
    }

    [Fact]
    public async Task Adds_A_Reversing_Posting_Without_Touching_The_Original()
    {
        Invoice invoice = UnpaidInvoice(1800m);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        InvoiceDto result = await CreateHandler().Handle(
            new VoidInvoiceCommand(invoice.Id.Value, "Issued in error"), CancellationToken.None);

        result.Status.Should().Be(InvoiceStatus.Void.ToString());

        // A brand new posting is added — the original issue posting (OriginalPostingId) is never
        // looked up or mutated by this handler; ILedgerPostingRepository exposes only Add (no
        // Remove/Update), so the append-only invariant is structural, not just behavioral here.
        _ledgerPostings.Received(1).Add(Arg.Is<LedgerPosting>(p =>
            p.Id != OriginalPostingId && p.ReferenceType == "InvoiceVoid" && p.ReferenceId == OriginalPostingId.Value));

        // The reversal is the mirror image of the original issue posting (Debit AssociationRevenue /
        // Credit ResidentReceivable) for the full invoice amount — proving it reverses rather than
        // silently zeroing or partially adjusting the balance.
        _ledgerEntries.Received(1).AddRange(Arg.Is<IEnumerable<LedgerEntry>>(entries =>
            entries.Count() == 2 &&
            entries.Any(e => e.AccountType == LedgerAccountType.AssociationRevenue && e.Direction == LedgerDirection.Debit && e.Amount == 1800m) &&
            entries.Any(e => e.AccountType == LedgerAccountType.ResidentReceivable && e.Direction == LedgerDirection.Credit && e.Amount == 1800m)));
    }

    [Fact]
    public async Task Does_Not_Touch_The_Ledger_When_The_Invoice_Cannot_Be_Voided()
    {
        Invoice invoice = UnpaidInvoice(1800m);
        invoice.Void("First void", Guid.NewGuid(), LedgerPostingId.New(), Now);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => CreateHandler().Handle(
            new VoidInvoiceCommand(invoice.Id.Value, "Second attempt"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<Exception>();
        _ledgerPostings.DidNotReceive().Add(Arg.Any<LedgerPosting>());
        _ledgerEntries.DidNotReceive().AddRange(Arg.Any<IEnumerable<LedgerEntry>>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
