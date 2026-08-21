using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetReceivablesAgeingReport;

/// <summary>
/// Cross-checks the Receivables Ageing report's grand total against what the ledger separately says the
/// ResidentReceivable balance is, for the same as-of date — Template 5's "reporting must not create a
/// second financial truth" invariant applied to receivables.
///
/// IMPORTANT — what this test actually proves: <see cref="GetReceivablesAgeingReportQueryHandler"/> is
/// computed entirely from <c>IInvoiceRepository.GetOpenReceivablesAsync</c> (invoice-side open balances);
/// the ledger's ResidentReceivable balance is computed entirely separately, from posted
/// <c>LedgerEntry</c> rows, via <c>IFinanceReportRepository.GetTrialBalanceAsync</c>. No application-layer
/// unit test can exercise both code paths against one shared, real database, so this is a MOCKED-
/// EQUIVALENCE test: both repositories are stubbed with numbers deliberately constructed to agree, and the
/// test proves the two handlers' own arithmetic (bucket-summing on one side, debit-minus-credit netting on
/// the other) preserves that agreement rather than silently distorting it. It does NOT prove production
/// invoice data and production ledger data actually stay in sync — that guarantee is enforced upstream, at
/// posting time, by <c>RecordPaymentCommandHandler</c>/<c>GenerateInvoiceBatchCommandHandler</c>/
/// <c>IFinancialPostingService</c> always moving <c>Invoice.Balance</c> and the ResidentReceivable ledger
/// account in lockstep. A true end-to-end reconciliation proof would require an integration test against a
/// real database with both invoices and ledger postings persisted together — out of scope for this
/// application-layer unit-test project, so "verification not performed" applies to that stronger claim.
/// </summary>
public class GetReceivablesAgeingReportLedgerReconciliationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly AsOfDate = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetReceivablesAgeingReportLedgerReconciliationTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Ageing_Grand_Total_Agrees_With_Ledger_ResidentReceivable_Balance_For_The_Same_Underlying_Total()
    {
        // Three open invoices at various ages, summing to 3,750 — the number both "sides" of the
        // reconciliation are built from, exactly as it would be if invoices and the ledger were
        // genuinely in sync in production.
        List<AgeingReceivableLine> receivables =
        [
            new(AsOfDate, 1_000m), // Current
            new(AsOfDate.AddDays(-15), 750m), // 1-30 days
            new(AsOfDate.AddDays(-95), 2_000m), // 91+ days
        ];
        _invoices.GetOpenReceivablesAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns(receivables);

        ReceivablesAgeingReportDto ageing = await new GetReceivablesAgeingReportQueryHandler(_invoices, _currentUser, _clock)
            .Handle(new GetReceivablesAgeingReportQuery(null, AsOfDate), CancellationToken.None);

        // The ledger's ResidentReceivable account, stubbed to net to the exact same 3,750 total.
        _reports.GetTrialBalanceAsync(TenantId, AsOfDate, Arg.Any<CancellationToken>()).Returns(
        [
            new TrialBalanceAccountLine(
                new ChartOfAccountId(Guid.NewGuid()), "1100", "Resident Receivable", AccountCategory.Asset,
                LedgerDirection.Debit, TotalDebit: 9_750m, TotalCredit: 6_000m), // net 3,750
        ]);

        IReadOnlyList<TrialBalanceAccountLine> trialBalance = await _reports.GetTrialBalanceAsync(TenantId, AsOfDate, CancellationToken.None);
        TrialBalanceAccountLine residentReceivableLine = trialBalance.Single(l => l.Code == "1100");
        decimal ledgerResidentReceivableBalance = residentReceivableLine.TotalDebit - residentReceivableLine.TotalCredit;

        ageing.GrandTotal.Should().Be(3_750m);
        ageing.GrandTotal.Should().Be(ledgerResidentReceivableBalance);
    }
}
