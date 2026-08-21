using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetTrialBalance;

public class GetTrialBalanceQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetTrialBalanceQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetTrialBalanceQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Total_Debit_Column_Always_Equals_Total_Credit_Column()
    {
        // A genuinely balanced set of postings: Assets(7000+3000) + Expense(2000) == Liability(4000) + Income(8000).
        List<TrialBalanceAccountLine> lines =
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit, 10_000m, 3_000m), // net 7,000 debit
            new(new ChartOfAccountId(Guid.NewGuid()), "1100", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit, 5_000m, 2_000m), // net 3,000 debit
            new(new ChartOfAccountId(Guid.NewGuid()), "2000", "Accounts Payable", AccountCategory.Liability, LedgerDirection.Credit, 500m, 4_500m), // net 4,000 credit
            new(new ChartOfAccountId(Guid.NewGuid()), "4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, 0m, 8_000m), // net 8,000 credit
            new(new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit, 2_000m, 0m), // net 2,000 debit
        ];
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(lines);

        TrialBalanceReportDto result = await CreateHandler().Handle(new GetTrialBalanceQuery(null), CancellationToken.None);

        result.TotalDebit.Should().Be(result.TotalCredit);
        result.TotalDebit.Should().Be(12_000m); // 7,000 + 3,000 + 2,000
    }

    [Fact]
    public async Task Account_With_Net_Debit_Activity_Shows_In_Debit_Column_Only()
    {
        ChartOfAccountId accountId = new(Guid.NewGuid());
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(
        [
            new TrialBalanceAccountLine(accountId, "1000", "Cash", AccountCategory.Asset, LedgerDirection.Debit, 7_000m, 2_000m),
        ]);

        TrialBalanceReportDto result = await CreateHandler().Handle(new GetTrialBalanceQuery(null), CancellationToken.None);

        result.Lines.Should().ContainSingle();
        result.Lines[0].Debit.Should().Be(5_000m);
        result.Lines[0].Credit.Should().Be(0m);
    }

    [Fact]
    public async Task Account_With_Exactly_Offsetting_Activity_Is_Omitted()
    {
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(
        [
            new TrialBalanceAccountLine(new ChartOfAccountId(Guid.NewGuid()), "9999", "Suspense", AccountCategory.Asset, LedgerDirection.Debit, 500m, 500m),
        ]);

        TrialBalanceReportDto result = await CreateHandler().Handle(new GetTrialBalanceQuery(null), CancellationToken.None);

        result.Lines.Should().BeEmpty();
        result.TotalDebit.Should().Be(0m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetTrialBalanceQuery(null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
