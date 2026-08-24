using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;
using MyCondo.Application.Features.Finance.FinancialStatements.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;

public class GetStatementOfFinancialPositionQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 6, 30, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly AsOfDate = new(2026, 6, 30);

    private readonly IFinancialStatementReportingService _reportingService = Substitute.For<IFinancialStatementReportingService>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetStatementOfFinancialPositionQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(UserId);
        _clock.UtcNow.Returns(Now);
    }

    private GetStatementOfFinancialPositionQueryHandler CreateHandler() =>
        new(_reportingService, _currentUser, _clock);

    private static FinancialStatementAccountAmount Account(string code, string name, decimal amount) =>
        new(Guid.NewGuid(), code, name, amount);

    private static FinancialStatementGroupAmount Group(
        FinancialStatementGroup group, params FinancialStatementAccountAmount[] accounts) =>
        new(group, FinancialStatementGroupCategories.CategoryOf(group), accounts.Sum(a => a.Amount), accounts);

    private void SetSnapshot(Guid? fundId, params FinancialStatementGroupAmount[] groups) =>
        SetSnapshot(fundId, warnings: [], groups);

    private void SetSnapshot(
        Guid? fundId, IReadOnlyList<UnmappedAccountWarning> warnings, params FinancialStatementGroupAmount[] groups) =>
        _reportingService.GetAsOfSnapshotAsync(TenantId, AsOfDate, fundId, Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementSnapshot(groups, warnings));

    [Fact]
    public async Task Balanced_Position_Reports_Zero_Difference_And_IsBalanced_True()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.CashAndBank, Account("1000", "Cash & Bank", 100_000m)),
            Group(FinancialStatementGroup.AccountsPayable, Account("2000", "Accounts Payable", 20_000m)),
            Group(FinancialStatementGroup.FundBalance, Account("3000", "Fund Balance", 80_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.TotalAssets.Should().Be(100_000m);
        dto.TotalLiabilities.Should().Be(20_000m);
        dto.TotalFunds.Should().Be(80_000m);
        dto.TotalLiabilitiesAndFunds.Should().Be(100_000m);
        dto.Difference.Should().Be(0m);
        dto.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task Cash_And_Bank_Group_Rolls_Up_Under_Assets()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.CashAndBank, Account("1000", "Cash & Bank", 55_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.Assets.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.CashAndBank && g.Amount == 55_000m);
        dto.Assets.Total.Should().Be(55_000m);
    }

    [Fact]
    public async Task Investments_And_Fixed_Deposits_Group_Rolls_Up_Under_Assets()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.InvestmentsAndFixedDeposits, Account("1100", "Fixed Deposit", 300_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.Assets.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.InvestmentsAndFixedDeposits);
        dto.Assets.Total.Should().Be(300_000m);
    }

    [Fact]
    public async Task Receivables_Group_Rolls_Up_Under_Assets()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.Receivables, Account("1200", "Service Charge Receivable", 42_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.Assets.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.Receivables && g.Amount == 42_000m);
    }

    [Fact]
    public async Task Accounts_Payable_Group_Rolls_Up_Under_Liabilities()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.AccountsPayable, Account("2000", "Accounts Payable", 15_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.Liabilities.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.AccountsPayable);
        dto.Liabilities.Total.Should().Be(15_000m);
    }

    [Fact]
    public async Task Resident_Advances_Group_Rolls_Up_Under_Liabilities()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.ResidentAdvances, Account("2100", "Resident Advances", 9_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.Liabilities.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.ResidentAdvances && g.Amount == 9_000m);
    }

    [Fact]
    public async Task Fund_Balance_Group_Rolls_Up_Under_Funds()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.FundBalance, Account("3000", "General Fund", 60_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.Funds.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.FundBalance);
        dto.Funds.Total.Should().Be(60_000m);
        dto.TotalFunds.Should().Be(60_000m, "no Income/Expense activity means CumulativeSurplusDeficit is zero");
    }

    [Fact]
    public async Task Prior_Period_Balances_Are_Carried_Through_As_Cumulative_Group_Amounts()
    {
        // The snapshot itself is a cumulative-to-date aggregation (Task 2); the handler must not
        // re-restrict it to "this period only" — whatever the service returns for AsOfDate flows
        // straight into the section total unchanged.
        SetSnapshot(null, Group(FinancialStatementGroup.CashAndBank, Account("1000", "Cash & Bank", 250_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.TotalAssets.Should().Be(250_000m);
    }

    [Fact]
    public async Task Income_Increases_The_Cumulative_Surplus()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 30_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.CumulativeSurplusDeficit.Should().Be(30_000m);
        dto.TotalFunds.Should().Be(30_000m);
    }

    [Fact]
    public async Task Expense_Reduces_The_Cumulative_Surplus()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 30_000m)),
            Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 12_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.CumulativeSurplusDeficit.Should().Be(18_000m);
    }

    [Fact]
    public async Task Expense_Exceeding_Income_Produces_A_Negative_Surplus_Deficit()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 10_000m)),
            Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 25_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.CumulativeSurplusDeficit.Should().Be(-15_000m);
        dto.TotalFunds.Should().Be(-15_000m);
    }

    [Fact]
    public async Task A_Fully_Reversed_Income_Group_Contributes_No_Surplus()
    {
        // Task 2's own service already omits groups whose accounts net to exactly zero (a reversed
        // posting); the handler must not reintroduce a phantom surplus when no Income/Expense group
        // is present in the snapshot at all.
        SetSnapshot(null, Group(FinancialStatementGroup.CashAndBank, Account("1000", "Cash & Bank", 5_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.CumulativeSurplusDeficit.Should().Be(0m);
    }

    [Fact]
    public async Task FundId_Is_Forwarded_To_The_Reporting_Service_And_Echoed_On_The_Dto()
    {
        Guid fundId = Guid.NewGuid();
        SetSnapshot(fundId, Group(FinancialStatementGroup.CashAndBank, Account("1000", "Cash & Bank", 1_000m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, fundId), CancellationToken.None);

        dto.FundId.Should().Be(fundId);
        await _reportingService.Received(1).GetAsOfSnapshotAsync(TenantId, AsOfDate, fundId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unmapped_Account_Warnings_Are_Propagated_To_The_Dto()
    {
        UnmappedAccountWarning warning = new(
            Guid.NewGuid(), "1999", "Custom Petty Cash", AccountCategory.Asset, FinancialStatementGroup.OtherAssets, 1_500m);

        SetSnapshot(
            null,
            warnings: [warning],
            Group(FinancialStatementGroup.OtherAssets, Account("1999", "Custom Petty Cash", 1_500m)));

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.UnmappedAccountWarnings.Should().ContainSingle();
        dto.UnmappedAccountWarnings[0].Code.Should().Be("1999");
        dto.UnmappedAccountWarnings[0].EffectiveStatementGroup.Should().Be(FinancialStatementGroup.OtherAssets);
    }

    [Fact]
    public async Task Zero_Activity_Snapshot_Produces_A_Balanced_Zero_Statement()
    {
        SetSnapshot(null);

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.TotalAssets.Should().Be(0m);
        dto.TotalLiabilitiesAndFunds.Should().Be(0m);
        dto.Difference.Should().Be(0m);
        dto.IsBalanced.Should().BeTrue();
        dto.Assets.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Assets_Not_Matching_Liabilities_And_Funds_Is_Detected_And_Not_Hidden()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.CashAndBank, Account("1000", "Cash & Bank", 100_000m)));
        // No offsetting Liability/Equity group supplied — an intentionally unbalanced fixture.

        StatementOfFinancialPositionDto dto =
            await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        dto.IsBalanced.Should().BeFalse();
        dto.Difference.Should().Be(100_000m);
    }

    [Fact]
    public async Task Uses_The_Callers_TenantId_And_Never_Another_Tenants()
    {
        Guid otherTenantId = Guid.NewGuid();
        _reportingService.GetAsOfSnapshotAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementSnapshot([], []));

        await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None);

        await _reportingService.Received(1).GetAsOfSnapshotAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>());
        await _reportingService.DidNotReceive().GetAsOfSnapshotAsync(otherTenantId, AsOfDate, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_Tenant_Context_Throws_Forbidden()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler()
            .Handle(new GetStatementOfFinancialPositionQuery(AsOfDate, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Missing_AsOfDate_Defaults_To_The_Tenant_Clocks_Current_Date()
    {
        _reportingService.GetAsOfSnapshotAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementSnapshot([], []));

        await CreateHandler().Handle(new GetStatementOfFinancialPositionQuery(null, null), CancellationToken.None);

        await _reportingService.Received(1).GetAsOfSnapshotAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>());
    }
}
