using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;

public class GetIncomeExpenditureStatementQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly StartDate = new(2026, 6, 1);
    private static readonly DateOnly EndDate = new(2026, 6, 30);

    private readonly IFinancialStatementReportingService _reportingService = Substitute.For<IFinancialStatementReportingService>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetIncomeExpenditureStatementQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(UserId);
        _clock.UtcNow.Returns(Now);
    }

    private GetIncomeExpenditureStatementQueryHandler CreateHandler() =>
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
        _reportingService.GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, fundId, Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementSnapshot(groups, warnings));

    private Task<IncomeExpenditureStatementDto> HandleAsync(Guid? fundId = null) =>
        CreateHandler().Handle(new GetIncomeExpenditureStatementQuery(StartDate, EndDate, fundId), CancellationToken.None).AsTask();

    [Fact]
    public async Task Service_Charge_Income_Rolls_Up_Under_Income()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 414_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Income.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.ServiceChargeIncome && g.Amount == 414_000m);
        dto.TotalIncome.Should().Be(414_000m);
    }

    [Fact]
    public async Task Interest_Income_Rolls_Up_Under_Income()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.InterestIncome, Account("4200", "Interest Income", 28_500m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Income.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.InterestIncome && g.Amount == 28_500m);
        dto.TotalIncome.Should().Be(28_500m);
    }

    [Fact]
    public async Task Other_Income_Rolls_Up_Under_Income()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.OtherIncome, Account("4300", "Other Income", 9_200m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Income.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.OtherIncome && g.Amount == 9_200m);
    }

    [Fact]
    public async Task Operating_Expense_Rolls_Up_Under_Expenditure()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 250_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Expenditure.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.OperatingExpenses && g.Amount == 250_000m);
        dto.TotalExpenditure.Should().Be(250_000m);
    }

    [Fact]
    public async Task Multiple_Income_Groups_Sum_Into_TotalIncome()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 414_000m)),
            Group(FinancialStatementGroup.InterestIncome, Account("4200", "Interest Income", 28_500m)),
            Group(FinancialStatementGroup.OtherIncome, Account("4300", "Other Income", 9_200m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Income.Groups.Should().HaveCount(3);
        dto.TotalIncome.Should().Be(451_700m);
    }

    [Fact]
    public async Task Multiple_Expenditure_Groups_Sum_Into_TotalExpenditure()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 250_000m)),
            Group(FinancialStatementGroup.InterestExpense, Account("5100", "Interest Expense", 1_200m)),
            Group(FinancialStatementGroup.OtherExpenses, Account("5900", "Other Expenses", 800m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Expenditure.Groups.Should().HaveCount(3);
        dto.TotalExpenditure.Should().Be(252_000m);
    }

    [Fact]
    public async Task Income_Amount_Is_Already_NormalBalance_Signed_By_The_Reporting_Service()
    {
        // The service (Task 2) already nets Credit - Debit for an Income (credit-normal) account; the
        // handler must pass that signed amount through unchanged, not re-derive a sign.
        SetSnapshot(null, Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 100_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Income.Groups.Single().Amount.Should().Be(100_000m);
    }

    [Fact]
    public async Task Expense_Amount_Is_Already_NormalBalance_Signed_By_The_Reporting_Service()
    {
        // Symmetric case: the service already nets Debit - Credit for an Expense (debit-normal) account.
        SetSnapshot(null, Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 60_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.Expenditure.Groups.Single().Amount.Should().Be(60_000m);
    }

    [Fact]
    public async Task Income_Exceeding_Expenditure_Produces_A_Positive_Surplus()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 100_000m)),
            Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 40_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.SurplusDeficit.Should().Be(60_000m);
    }

    [Fact]
    public async Task Expenditure_Exceeding_Income_Produces_A_Negative_Deficit()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 40_000m)),
            Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 100_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.SurplusDeficit.Should().Be(-60_000m);
    }

    [Fact]
    public async Task Equal_Income_And_Expenditure_Produces_An_Exact_BreakEven()
    {
        SetSnapshot(
            null,
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 50_000m)),
            Group(FinancialStatementGroup.OperatingExpenses, Account("5000", "Operating Expense", 50_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.SurplusDeficit.Should().Be(0m);
    }

    [Fact]
    public async Task StartDate_And_EndDate_Are_Forwarded_To_The_Reporting_Service_Unchanged()
    {
        SetSnapshot(null);

        await HandleAsync();

        await _reportingService.Received(1)
            .GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FundId_Is_Forwarded_To_The_Reporting_Service_And_Echoed_On_The_Dto()
    {
        Guid fundId = Guid.NewGuid();
        SetSnapshot(fundId, Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 1_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync(fundId);

        dto.FundId.Should().Be(fundId);
        await _reportingService.Received(1).GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, fundId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unmapped_Income_Account_Warning_Is_Propagated_To_The_Dto()
    {
        UnmappedAccountWarning warning = new(
            Guid.NewGuid(), "4999", "Custom Sponsorship Income", AccountCategory.Income, FinancialStatementGroup.OtherIncome, 2_000m);

        SetSnapshot(
            null,
            warnings: [warning],
            Group(FinancialStatementGroup.OtherIncome, Account("4999", "Custom Sponsorship Income", 2_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.UnmappedAccountWarnings.Should().ContainSingle();
        dto.UnmappedAccountWarnings[0].Code.Should().Be("4999");
        dto.UnmappedAccountWarnings[0].Category.Should().Be(AccountCategory.Income);
    }

    [Fact]
    public async Task Unmapped_Expense_Account_Warning_Is_Propagated_To_The_Dto()
    {
        UnmappedAccountWarning warning = new(
            Guid.NewGuid(), "5999", "Custom Consultancy Fee", AccountCategory.Expense, FinancialStatementGroup.OtherExpenses, 3_500m);

        SetSnapshot(
            null,
            warnings: [warning],
            Group(FinancialStatementGroup.OtherExpenses, Account("5999", "Custom Consultancy Fee", 3_500m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.UnmappedAccountWarnings.Should().ContainSingle();
        dto.UnmappedAccountWarnings[0].Code.Should().Be("5999");
        dto.UnmappedAccountWarnings[0].Category.Should().Be(AccountCategory.Expense);
    }

    [Fact]
    public async Task Explicitly_Classified_Custom_Tenant_Account_Produces_No_Warning()
    {
        SetSnapshot(null, Group(FinancialStatementGroup.UtilityGasIncome, Account("4100", "Gas Income", 18_400m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.UnmappedAccountWarnings.Should().BeEmpty();
        dto.Income.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.UtilityGasIncome);
    }

    [Fact]
    public async Task Asset_Or_Liability_Unmapped_Warnings_Are_Not_Surfaced_On_The_Income_Expenditure_Statement()
    {
        // A period snapshot naturally includes non-Income/Expense activity too (e.g. the Cash side of a
        // receipt); this statement only concerns Income/Expense, so any such warning must be filtered out.
        UnmappedAccountWarning assetWarning = new(
            Guid.NewGuid(), "1999", "Custom Petty Cash", AccountCategory.Asset, FinancialStatementGroup.OtherAssets, 1_500m);

        SetSnapshot(
            null,
            warnings: [assetWarning],
            Group(FinancialStatementGroup.ServiceChargeIncome, Account("4000", "Service Charge Income", 10_000m)));

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.UnmappedAccountWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task Zero_Activity_Snapshot_Produces_A_BreakEven_Statement_With_No_Groups()
    {
        SetSnapshot(null);

        IncomeExpenditureStatementDto dto = await HandleAsync();

        dto.TotalIncome.Should().Be(0m);
        dto.TotalExpenditure.Should().Be(0m);
        dto.SurplusDeficit.Should().Be(0m);
        dto.Income.Groups.Should().BeEmpty();
        dto.Expenditure.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Uses_The_Callers_TenantId_And_Never_Another_Tenants()
    {
        Guid otherTenantId = Guid.NewGuid();
        _reportingService.GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementSnapshot([], []));

        await HandleAsync();

        await _reportingService.Received(1).GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>());
        await _reportingService.DidNotReceive().GetPeriodSnapshotAsync(otherTenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_Tenant_Context_Throws_Forbidden()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => HandleAsync();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
