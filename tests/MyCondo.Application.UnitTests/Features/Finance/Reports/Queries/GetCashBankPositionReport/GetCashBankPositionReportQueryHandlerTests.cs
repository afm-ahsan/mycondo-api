using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetCashBankPositionReport;

public class GetCashBankPositionReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetCashBankPositionReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetCashBankPositionReportQueryHandler CreateHandler() => new(_reports, _financialAccounts, _currentUser, _clock);

    private static FinancialAccount CreateFinancialAccount(FinancialAccountType type, string name) =>
        FinancialAccount.Create(TenantId, name, type, null, null, null, new ChartOfAccountId(Guid.NewGuid()), fundId: (FundId?)null, notes: null);

    [Fact]
    public async Task Each_Financial_Account_Reports_Its_Own_Ledger_Balance_Grouped_By_Type()
    {
        FinancialAccount cash = CreateFinancialAccount(FinancialAccountType.Cash, "Petty Cash");
        FinancialAccount bank = CreateFinancialAccount(FinancialAccountType.Bank, "Main Bank");
        _financialAccounts.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([cash, bank]);

        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(
        [
            new TrialBalanceAccountLine(cash.ChartOfAccountId, "1001", "Petty Cash", AccountCategory.Asset, LedgerDirection.Debit, 5_000m, 1_000m),
            new TrialBalanceAccountLine(bank.ChartOfAccountId, "1002", "Main Bank", AccountCategory.Asset, LedgerDirection.Debit, 50_000m, 20_000m),
        ]);

        CashBankPositionReportDto result = await CreateHandler().Handle(new GetCashBankPositionReportQuery(null), CancellationToken.None);

        result.Accounts.Should().HaveCount(2);
        result.Accounts.Single(a => a.FinancialAccountId == cash.Id.Value).Balance.Should().Be(4_000m);
        result.Accounts.Single(a => a.FinancialAccountId == bank.Id.Value).Balance.Should().Be(30_000m);
        result.TotalBalance.Should().Be(34_000m);
    }

    [Fact]
    public async Task Financial_Account_With_No_Ledger_Activity_Reports_Zero_Balance()
    {
        FinancialAccount account = CreateFinancialAccount(FinancialAccountType.Bank, "New Bank Account");
        _financialAccounts.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([account]);
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns([]);

        CashBankPositionReportDto result = await CreateHandler().Handle(new GetCashBankPositionReportQuery(null), CancellationToken.None);

        result.Accounts.Should().ContainSingle();
        result.Accounts[0].Balance.Should().Be(0m);
        result.TotalBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetCashBankPositionReportQuery(null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
