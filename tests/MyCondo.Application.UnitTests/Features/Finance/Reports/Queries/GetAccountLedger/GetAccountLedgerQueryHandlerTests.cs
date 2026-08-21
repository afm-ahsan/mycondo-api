using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetAccountLedger;

public class GetAccountLedgerQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly IChartOfAccountRepository _chartOfAccounts = Substitute.For<IChartOfAccountRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetAccountLedgerQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetAccountLedgerQueryHandler CreateHandler() => new(_reports, _chartOfAccounts, _currentUser, _clock);

    [Fact]
    public async Task Running_Balance_Accumulates_Debit_Normal_Account_Correctly()
    {
        ChartOfAccountId accountId = new(Guid.NewGuid());
        ChartOfAccount account = ChartOfAccount.Create(TenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit);
        _chartOfAccounts.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(account);

        DateOnly from = new(2026, 8, 1);
        _reports.GetAccountBalanceBeforeAsync(TenantId, accountId.Value, from, Arg.Any<CancellationToken>())
            .Returns((TotalDebit: 1_000m, TotalCredit: 200m)); // opening balance = 800

        List<LedgerActivityLine> lines =
        [
            new(new LedgerEntryId(Guid.NewGuid()), new LedgerPostingId(Guid.NewGuid()), from, "Charge", "Invoice", null,
                accountId, "1000", "Cash & Bank", null, null, LedgerDirection.Debit, 500m),
            new(new LedgerEntryId(Guid.NewGuid()), new LedgerPostingId(Guid.NewGuid()), from.AddDays(1), "Payment", "Payment", null,
                accountId, "1000", "Cash & Bank", null, null, LedgerDirection.Credit, 300m),
        ];
        _reports.GetGeneralLedgerAsync(TenantId, from, null, accountId.Value, null, null, 1, 50, true, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LedgerActivityLine>(lines, 1, 50, 2));

        AccountLedgerReportDto result = await CreateHandler().Handle(
            new GetAccountLedgerQuery(accountId.Value, from, null, 1, 50), CancellationToken.None);

        result.OpeningBalance.Should().Be(800m);
        result.Lines[0].RunningBalance.Should().Be(1_300m); // 800 + 500 debit
        result.Lines[1].RunningBalance.Should().Be(1_000m); // 1300 - 300 credit
        result.ClosingBalance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Unknown_Or_Cross_Tenant_Account_Throws_NotFound()
    {
        ChartOfAccountId accountId = new(Guid.NewGuid());
        _chartOfAccounts.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((ChartOfAccount?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetAccountLedgerQuery(accountId.Value, null, null, 1, 50), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
