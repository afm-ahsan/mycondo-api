using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.Commands.VoidInvoice;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Finance.Services;
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
/// never mutates or deletes the invoice's original issue posting — it only asks
/// <see cref="IFinancialPostingService"/> for a brand-new reversing posting (see docs/conventions
/// "append-only double-entry ledger... No deletes — voids create reversing entries"). Since ADR-027,
/// the handler no longer touches <c>ILedgerPostingRepository</c>/<c>ILedgerEntryRepository</c> directly —
/// that append-only guarantee now lives inside <see cref="FinancialPostingService"/> itself (covered by
/// its own tests); this test proves the handler asks for the right reversal and never persists anything
/// when the domain guard rejects the void. The invoice-status guards themselves (already-void,
/// has-payment) are covered at the domain level in InvoiceTests.
/// </summary>
public class VoidInvoiceCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly LedgerPostingId OriginalPostingId = LedgerPostingId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public VoidInvoiceCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — it
    /// builds an actually-balanced posting via the same <see cref="LedgerPosting.Create"/> factory the
    /// real service uses, from whatever request the handler passes in — without needing a database.</summary>
    private void StubFinancialPosting() =>
        _financialPosting.PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                FinancialPostingRequest request = callInfo.Arg<FinancialPostingRequest>();
                List<LedgerLine> lines = request.Lines
                    .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
                    .ToList();
                (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
                    request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
                    request.SourceId, lines, Now);
                return new FinancialPostingResult(posting, entries);
            });

    private VoidInvoiceCommandHandler CreateHandler() => new(
        _invoices, _financialPosting, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<VoidInvoiceCommandHandler>>());

    private static Invoice UnpaidInvoice(decimal amount = 1800m)
    {
        InvoiceLineInput line = new(
            ServiceChargeRuleId.New(), "Standard Charge", "ServiceCharge", "FixedAmount", amount, null, 1m,
            amount, "Standard Charge (ServiceCharge)");
        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, "INV-TEST-2026-000001", InvoiceSource.ServiceCharge,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            [line], OriginalPostingId, LedgerAccountType.AssociationRevenue, null, Now);
        return invoice;
    }

    [Fact]
    public async Task Requests_A_Reversing_Posting_Referencing_The_Original()
    {
        Invoice invoice = UnpaidInvoice(1800m);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        InvoiceDto result = await CreateHandler().Handle(
            new VoidInvoiceCommand(invoice.Id.Value, "Issued in error"), CancellationToken.None);

        result.Status.Should().Be(InvoiceStatus.Void.ToString());

        // The reversal is the mirror image of the original issue posting (Debit AssociationRevenue /
        // Credit ResidentReceivable) for the full invoice amount, referencing the original posting as
        // its source — proving it reverses rather than silently zeroing or partially adjusting.
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "InvoiceVoid" &&
                r.SourceId == OriginalPostingId.Value &&
                r.Lines.Count == 2 &&
                r.Lines.Any(l => l.Role == LedgerAccountType.AssociationRevenue && l.Direction == LedgerDirection.Debit && l.Amount == 1800m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.ResidentReceivable && l.Direction == LedgerDirection.Credit && l.Amount == 1800m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_Not_Commit_When_The_Invoice_Cannot_Be_Voided()
    {
        Invoice invoice = UnpaidInvoice(1800m);
        invoice.Void("First void", Guid.NewGuid(), LedgerPostingId.New(), Now);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => CreateHandler().Handle(
            new VoidInvoiceCommand(invoice.Id.Value, "Second attempt"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<Exception>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
