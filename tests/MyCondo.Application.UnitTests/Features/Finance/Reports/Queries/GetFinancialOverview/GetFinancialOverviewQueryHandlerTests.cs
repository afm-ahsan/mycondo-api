using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetFinancialOverview;

public class GetFinancialOverviewQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly IAccountMappingRepository _accountMappings = Substitute.For<IAccountMappingRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly IFixedDepositRepository _fixedDeposits = Substitute.For<IFixedDepositRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFinancialOverviewQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _financialAccounts.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
        _reports.GetCashAndBankChartOfAccountIdsAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
        _accountMappings.ResolveAccountIdAsync(TenantId, nameof(LedgerAccountType.ResidentReceivable), Arg.Any<CancellationToken>())
            .Returns((ChartOfAccountId?)null);
        _fixedDeposits.GetActivePrincipalTotalAsync(TenantId, Arg.Any<CancellationToken>()).Returns(0m);
        _invoices.GetFinancialAggregateAsync(TenantId, null, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new InvoiceFinancialAggregate(0m, 0m, 0, 0, 0));
        _payments.GetTotalCollectedAsync(TenantId, null, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(0m);
        _expenses.GetExpenseCompositionByCategoryAsync(TenantId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _reports.GetTrialBalanceAsync(TenantId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetFinancialOverviewQueryHandler CreateHandler() => new(
        _reports, _accountMappings, _financialAccounts, _fixedDeposits, _invoices, _payments, _expenses, _currentUser, _clock);

    [Fact]
    public async Task Surplus_Deficit_Is_Income_Minus_Expense_Not_Cash_Minus_Expense()
    {
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, 0m, 100_000m),
            new(new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit, 70_000m, 0m),
        ]);

        FinancialOverviewReportDto result = await CreateHandler().Handle(
            new GetFinancialOverviewQuery(null, null, null), CancellationToken.None);

        result.IncomeTotal.Should().Be(100_000m);
        result.ExpenseTotal.Should().Be(70_000m);
        result.SurplusDeficit.Should().Be(30_000m); // Income - Expense, never cash-derived
    }

    [Fact]
    public async Task Cash_Bank_And_Mfs_Balances_Are_Reported_Separately_And_Sum_To_Available_Liquid_Funds()
    {
        ChartOfAccountId cashAccountId = new(Guid.NewGuid());
        ChartOfAccountId bankAccountId = new(Guid.NewGuid());
        FinancialAccount cash = FinancialAccount.Create(
            TenantId, "Petty Cash", FinancialAccountType.Cash, null, null, null, cashAccountId, (FundId?)null, null);
        FinancialAccount bank = FinancialAccount.Create(
            TenantId, "Main Bank", FinancialAccountType.Bank, null, null, null, bankAccountId, (FundId?)null, null);

        _financialAccounts.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([cash, bank]);
        _reports.GetCashAndBankChartOfAccountIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns([cashAccountId.Value, bankAccountId.Value]);
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(
        [
            new(cashAccountId, "1001", "Petty Cash", AccountCategory.Asset, LedgerDirection.Debit, 5_000m, 0m),
            new(bankAccountId, "1002", "Main Bank", AccountCategory.Asset, LedgerDirection.Debit, 40_000m, 10_000m),
        ]);

        FinancialOverviewReportDto result = await CreateHandler().Handle(
            new GetFinancialOverviewQuery(null, null, null), CancellationToken.None);

        result.CashPosition.CashInHand.Should().Be(5_000m);
        result.CashPosition.BankBalance.Should().Be(30_000m);
        result.CashPosition.MobileFinancialServiceBalance.Should().Be(0m);
        result.CashPosition.AvailableLiquidFunds.Should().Be(35_000m);
    }

    [Fact]
    public async Task Billed_And_Collected_Stay_Separate_Fields_In_Collection_Performance()
    {
        _invoices.GetFinancialAggregateAsync(TenantId, null, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new InvoiceFinancialAggregate(50_000m, 20_000m, 3, 1, 0));
        _payments.GetTotalCollectedAsync(TenantId, null, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(38_000m);

        FinancialOverviewReportDto result = await CreateHandler().Handle(
            new GetFinancialOverviewQuery(null, null, null), CancellationToken.None);

        result.CollectionPerformance.Billed.Should().Be(50_000m);
        result.CollectionPerformance.Collected.Should().Be(38_000m);
    }

    [Fact]
    public async Task Expense_Composition_Percentages_Sum_To_Total_Category_Amounts()
    {
        _expenses.GetExpenseCompositionByCategoryAsync(TenantId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new ExpenseCategoryActivityLine(new(Guid.NewGuid()), "Maintenance", 6_000m),
                new ExpenseCategoryActivityLine(new(Guid.NewGuid()), "Security", 4_000m),
            ]);

        FinancialOverviewReportDto result = await CreateHandler().Handle(
            new GetFinancialOverviewQuery(null, null, null), CancellationToken.None);

        result.ExpenseComposition.Should().HaveCount(2);
        result.ExpenseComposition.Single(l => l.CategoryName == "Maintenance").PercentageOfTotal.Should().Be(60m);
        result.ExpenseComposition.Single(l => l.CategoryName == "Security").PercentageOfTotal.Should().Be(40m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetFinancialOverviewQuery(null, null, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
