using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Commands.WaiveFine;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.WaiveFine;

/// <summary>
/// Handler-level tests for WaiveFineCommandHandler — proves it's scoped to
/// <see cref="InvoiceSource.Fine"/> only, and posts Dr AdjustmentsAndWaivers / Cr ResidentReceivable.
/// </summary>
public class WaiveFineCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public WaiveFineCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
        StubFinancialPosting();
    }

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

    private WaiveFineCommandHandler CreateHandler() => new(
        _invoices, _financialPosting, _auditLog, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<WaiveFineCommandHandler>>());

    private static Invoice AssessedFine(decimal amount = 500m)
    {
        InvoiceLineInput line = new(null, "Fine", "Fine", "Fixed", amount, null, 1m, amount, "Test fine");
        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, "FIN-TEST-2026-000001", InvoiceSource.Fine, Today, Today, Today, Today,
            [line], LedgerPostingId.New(), LedgerAccountType.FineIncome, null, Now);
        return invoice;
    }

    private static Invoice ServiceChargeInvoice(decimal amount = 500m)
    {
        InvoiceLineInput line = new(null, "Charge", "ServiceCharge", "FixedAmount", amount, null, 1m, amount, "Test charge");
        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, "INV-TEST-2026-000001", InvoiceSource.ServiceCharge, Today, Today, Today, Today,
            [line], LedgerPostingId.New(), LedgerAccountType.AssociationRevenue, null, Now);
        return invoice;
    }

    [Fact]
    public async Task Waives_A_Fine_And_Posts_AdjustmentsAndWaivers_Against_ResidentReceivable()
    {
        Invoice fine = AssessedFine(500m);
        _invoices.GetByIdAsync(fine.Id, Arg.Any<CancellationToken>()).Returns(fine);

        InvoiceDto result = await CreateHandler().Handle(
            new WaiveFineCommand(fine.Id.Value, 200m, "Goodwill"), CancellationToken.None);

        result.WaivedAmount.Should().Be(200m);
        result.Balance.Should().Be(300m);
        result.Status.Should().Be(InvoiceStatus.PartiallyWaived.ToString());

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FineWaiver" &&
                r.SourceId == fine.Id.Value &&
                r.Lines.Any(l => l.Role == LedgerAccountType.AdjustmentsAndWaivers && l.Direction == LedgerDirection.Debit && l.Amount == 200m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.ResidentReceivable && l.FlatId == FlatId && l.Direction == LedgerDirection.Credit && l.Amount == 200m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Invoice_Is_Not_A_Fine()
    {
        Invoice serviceCharge = ServiceChargeInvoice(500m);
        _invoices.GetByIdAsync(serviceCharge.Id, Arg.Any<CancellationToken>()).Returns(serviceCharge);

        Func<Task> act = () => CreateHandler().Handle(
            new WaiveFineCommand(serviceCharge.Id.Value, 100m, "Trying anyway"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<Exception>();
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }
}
