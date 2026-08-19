using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.AccountMappings.Exceptions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Services;

/// <summary>
/// <see cref="FinancialPostingService"/> is the single centralized posting path every ledger call
/// site (ADR-027) now goes through — these tests cover what it adds on top of the unmodified, already
/// separately-tested <see cref="LedgerPosting.Create"/>: account-mapping resolution (and its explicit
/// failure), idempotency, and closed-period prevention.
/// </summary>
public class FinancialPostingServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly BusinessDate = DateOnly.FromDateTime(Now.UtcDateTime);
    private static readonly ChartOfAccountId CashAccountId = ChartOfAccountId.New();
    private static readonly ChartOfAccountId ReceivableAccountId = ChartOfAccountId.New();

    private readonly IAccountMappingRepository _accountMappings = Substitute.For<IAccountMappingRepository>();
    private readonly IAccountingPeriodRepository _accountingPeriods = Substitute.For<IAccountingPeriodRepository>();
    private readonly ILedgerPostingRepository _ledgerPostings = Substitute.For<ILedgerPostingRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public FinancialPostingServiceTests()
    {
        _clock.UtcNow.Returns(Now);
        _accountMappings.ResolveAccountIdAsync(TenantId, "CashOrBank", Arg.Any<CancellationToken>()).Returns(CashAccountId);
        _accountMappings.ResolveAccountIdAsync(TenantId, "ResidentReceivable", Arg.Any<CancellationToken>()).Returns(ReceivableAccountId);
        _accountingPeriods.FindCoveringAsync(TenantId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((AccountingPeriod?)null);
    }

    private FinancialPostingService CreateService() => new(
        _accountMappings, _accountingPeriods, _ledgerPostings, _ledgerEntries, _clock,
        Substitute.For<ILogger<FinancialPostingService>>());

    private static FinancialPostingLine[] BalancedPaymentLines(decimal amount) =>
    [
        new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount),
        new FinancialPostingLine(LedgerAccountType.ResidentReceivable, FlatId, LedgerDirection.Credit, amount),
    ];

    [Fact]
    public async Task PostAsync_Resolves_Each_Line_To_Its_Mapped_Account_And_Stages_The_Posting()
    {
        FinancialPostingResult result = await CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Payment", "Payment", null, BalancedPaymentLines(500m)),
            CancellationToken.None);

        result.Posting.ReferenceType.Should().Be("Payment");
        result.Entries.Should().HaveCount(2);
        result.Entries.Should().ContainSingle(e => e.AccountType == LedgerAccountType.CashOrBank && e.ChartOfAccountId == CashAccountId);
        result.Entries.Should().ContainSingle(e => e.AccountType == LedgerAccountType.ResidentReceivable && e.ChartOfAccountId == ReceivableAccountId);

        _ledgerPostings.Received(1).Add(result.Posting);
        _ledgerEntries.Received(1).AddRange(Arg.Is<IEnumerable<LedgerEntry>>(e => e.Count() == 2));
    }

    [Fact]
    public async Task PostAsync_Throws_MissingAccountMappingException_When_A_Role_Has_No_Mapping()
    {
        _accountMappings.ResolveAccountIdAsync(TenantId, "CashOrBank", Arg.Any<CancellationToken>())
            .Returns((ChartOfAccountId?)null);

        Func<Task> act = () => CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Payment", "Payment", null, BalancedPaymentLines(500m)),
            CancellationToken.None);

        await act.Should().ThrowAsync<MissingAccountMappingException>();
        _ledgerPostings.DidNotReceive().Add(Arg.Any<LedgerPosting>());
    }

    [Fact]
    public async Task PostAsync_Throws_Conflict_When_A_Posting_Already_Exists_For_The_Same_Source()
    {
        Guid sourceId = Guid.NewGuid();
        _ledgerPostings.ExistsAsync(TenantId, "InvoiceVoid", sourceId, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Void", "InvoiceVoid", sourceId, BalancedPaymentLines(500m)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _ledgerPostings.DidNotReceive().Add(Arg.Any<LedgerPosting>());
    }

    [Fact]
    public async Task PostAsync_Allows_Repeated_Postings_With_No_SourceId()
    {
        // Several existing call sites (e.g. batch invoice generation) intentionally post with no
        // natural source id and rely on an upstream domain-level dedup check instead — the service
        // must not reject these just because SourceId is null.
        FinancialPostingResult first = await CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Invoice", "Invoice", null, BalancedPaymentLines(500m)),
            CancellationToken.None);
        FinancialPostingResult second = await CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Invoice", "Invoice", null, BalancedPaymentLines(300m)),
            CancellationToken.None);

        first.Posting.Id.Should().NotBe(second.Posting.Id);
    }

    [Fact]
    public async Task PostAsync_Throws_When_The_Business_Date_Falls_In_A_Closed_Period()
    {
        AccountingPeriod period = AccountingPeriod.Create(
            TenantId, Domain.Features.Finance.FinancialYears.FinancialYearId.New(), "2026-03",
            BusinessDate.AddDays(-10), BusinessDate.AddDays(10));
        period.Close();
        _accountingPeriods.FindCoveringAsync(TenantId, BusinessDate, Arg.Any<CancellationToken>()).Returns(period);

        Func<Task> act = () => CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Payment", "Payment", null, BalancedPaymentLines(500m)),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountingPeriodClosedException>();
        _ledgerPostings.DidNotReceive().Add(Arg.Any<LedgerPosting>());
    }

    [Fact]
    public async Task PostAsync_Stamps_The_Covering_Open_Period_Onto_Every_Entry()
    {
        AccountingPeriod period = AccountingPeriod.Create(
            TenantId, Domain.Features.Finance.FinancialYears.FinancialYearId.New(), "2026-03",
            BusinessDate.AddDays(-10), BusinessDate.AddDays(10));
        _accountingPeriods.FindCoveringAsync(TenantId, BusinessDate, Arg.Any<CancellationToken>()).Returns(period);

        FinancialPostingResult result = await CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Payment", "Payment", null, BalancedPaymentLines(500m)),
            CancellationToken.None);

        result.Entries.Should().OnlyContain(e => e.AccountingPeriodId == period.Id);
    }

    /// <summary>Template 4's additive <see cref="FinancialPostingLine.ExplicitAccountId"/> escape hatch
    /// (Financial Accounts, each with their own ledger account) — resolves directly to the supplied
    /// account instead of consulting <see cref="IAccountMappingRepository"/> for that line, while a
    /// same-request line with no override still resolves via its role as before.</summary>
    [Fact]
    public async Task PostAsync_Resolves_A_Line_With_ExplicitAccountId_Without_Consulting_The_Role_Mapping()
    {
        ChartOfAccountId specificBankAccountId = ChartOfAccountId.New();
        FinancialPostingLine[] lines =
        [
            new FinancialPostingLine(LedgerAccountType.FixedDeposit, null, LedgerDirection.Debit, 100_000m),
            new FinancialPostingLine(
                LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, 100_000m,
                ExplicitAccountId: specificBankAccountId),
        ];
        _accountMappings.ResolveAccountIdAsync(TenantId, "FixedDeposit", Arg.Any<CancellationToken>())
            .Returns(ChartOfAccountId.New());

        FinancialPostingResult result = await CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "FD placement", "FixedDepositPlacement", null, lines),
            CancellationToken.None);

        result.Entries.Should().ContainSingle(e => e.AccountType == LedgerAccountType.CashOrBank && e.ChartOfAccountId == specificBankAccountId);
        await _accountMappings.DidNotReceive().ResolveAccountIdAsync(TenantId, "CashOrBank", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostAsync_Resolves_Two_Same_Role_Lines_To_Different_Explicit_Accounts()
    {
        ChartOfAccountId accountA = ChartOfAccountId.New();
        ChartOfAccountId accountB = ChartOfAccountId.New();
        FinancialPostingLine[] lines =
        [
            new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 40_000m, ExplicitAccountId: accountA),
            new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, 40_000m, ExplicitAccountId: accountB),
        ];

        FinancialPostingResult result = await CreateService().PostAsync(
            new FinancialPostingRequest(TenantId, BusinessDate, "Inter-account transfer", "FixedDepositRenewalPartialWithdrawal", null, lines),
            CancellationToken.None);

        result.Entries.Should().ContainSingle(e => e.ChartOfAccountId == accountA);
        result.Entries.Should().ContainSingle(e => e.ChartOfAccountId == accountB);
    }
}
