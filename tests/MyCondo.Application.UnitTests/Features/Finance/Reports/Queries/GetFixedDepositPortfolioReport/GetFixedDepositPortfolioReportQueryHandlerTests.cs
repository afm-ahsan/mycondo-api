using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;

public class GetFixedDepositPortfolioReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IFixedDepositRepository _fixedDeposits = Substitute.For<IFixedDepositRepository>();
    private readonly IFixedDepositInterestAccrualRepository _accruals = Substitute.For<IFixedDepositInterestAccrualRepository>();
    private readonly IFixedDepositInterestReceiptRepository _receipts = Substitute.For<IFixedDepositInterestReceiptRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFixedDepositPortfolioReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetFixedDepositPortfolioReportQueryHandler CreateHandler() => new(_fixedDeposits, _accruals, _receipts, _currentUser, _clock);

    private static FixedDeposit CreateActiveFixedDeposit(Guid tenantId, decimal principal) =>
        FixedDeposit.Place(
            FixedDepositId.New(), tenantId, $"CERT-{Guid.NewGuid():N}", "Test Bank", null,
            new FinancialAccountId(Guid.NewGuid()), null, principal, 6.5m, InterestCalculationMethod.Simple,
            InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            null, null, null, new LedgerPostingId(Guid.NewGuid()), Now);

    [Fact]
    public async Task Outstanding_Accrued_Interest_Is_Accrued_Minus_Received_Per_Instrument()
    {
        FixedDeposit fd1 = CreateActiveFixedDeposit(TenantId, 100_000m);
        FixedDeposit fd2 = CreateActiveFixedDeposit(TenantId, 50_000m);
        _fixedDeposits.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([fd1, fd2]);
        _fixedDeposits.GetActivePrincipalTotalAsync(TenantId, Arg.Any<CancellationToken>()).Returns(150_000m);

        _accruals.GetTotalsByFixedDepositAsync(TenantId, null, Today, Arg.Any<CancellationToken>())
            .Returns(
            [
                new FixedDepositAccrualTotal(fd1.Id, 3, 3_000m),
                new FixedDepositAccrualTotal(fd2.Id, 2, 1_000m),
            ]);
        _receipts.GetTotalsByFixedDepositAsync(TenantId, null, Today, Arg.Any<CancellationToken>())
            .Returns([new FixedDepositReceiptTotal(fd1.Id, 1, 1_200m, 120m)]); // fd2 has no receipts yet

        FixedDepositPortfolioReportDto result = await CreateHandler().Handle(
            new GetFixedDepositPortfolioReportQuery(null), CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        result.Lines.Single(l => l.FixedDepositId == fd1.Id.Value).OutstandingAccruedInterest.Should().Be(1_800m); // 3,000 - 1,200
        result.Lines.Single(l => l.FixedDepositId == fd2.Id.Value).OutstandingAccruedInterest.Should().Be(1_000m); // 1,000 - 0
        result.TotalOutstandingAccruedInterest.Should().Be(2_800m);
        result.TotalPrincipal.Should().Be(150_000m); // reuses IFixedDepositRepository.GetActivePrincipalTotalAsync
        result.ActiveCount.Should().Be(2);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetFixedDepositPortfolioReportQuery(null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
