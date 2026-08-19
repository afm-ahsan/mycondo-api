using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;

/// <summary>
/// Proves the three renewal-principal-difference outcomes described in
/// <see cref="RenewFixedDepositCommandHandler"/>'s doc comment: unchanged (no posting), increased
/// (capitalization, capped at outstanding receivable), decreased (partial withdrawal).
/// </summary>
public class RenewFixedDepositCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFixedDepositRepository _fixedDeposits = Substitute.For<IFixedDepositRepository>();
    private readonly IFixedDepositInterestAccrualRepository _accruals = Substitute.For<IFixedDepositInterestAccrualRepository>();
    private readonly IFixedDepositInterestReceiptRepository _receipts = Substitute.For<IFixedDepositInterestReceiptRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RenewFixedDepositCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
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
                    request.SourceId, lines, NowUtc);
                foreach (LedgerEntry entry in entries)
                {
                    entry.SetFinanceDimensions(ChartOfAccountId.New(), request.FundId, null);
                }
                return new FinancialPostingResult(posting, entries);
            });

    private RenewFixedDepositCommandHandler CreateHandler() => new(
        _fixedDeposits, _accruals, _receipts, _financialAccounts, _financialPosting, _unitOfWork, _currentUser,
        _clock, Substitute.For<ILogger<RenewFixedDepositCommandHandler>>());

    private FixedDeposit SetUpActiveFixedDeposit(out FinancialAccount fundingAccount, decimal principal = 500_000m)
    {
        fundingAccount = FinancialAccount.Create(
            TenantId, "Main Bank", FinancialAccountType.Bank, null, null, null, ChartOfAccountId.New(), null, null);
        FixedDeposit fd = FixedDeposit.Place(
            FixedDepositId.New(), TenantId, "FD-001", "City Bank", null, fundingAccount.Id, null, principal, 7.5m,
            InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), null, null, null, LedgerPostingId.New(), NowUtc);
        _fixedDeposits.GetByIdAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(fd);
        _financialAccounts.GetByIdAsync(fundingAccount.Id, Arg.Any<CancellationToken>()).Returns(fundingAccount);
        return fd;
    }

    private static RenewFixedDepositCommand RenewalCommand(Guid fixedDepositId, Guid fundingAccountId, decimal newPrincipal) => new(
        fixedDepositId, "FD-001-R1", null, fundingAccountId, newPrincipal, 8m, "Simple", "Monthly",
        new DateOnly(2027, 1, 1), new DateOnly(2028, 1, 1), null, null, null);

    [Fact]
    public async Task Unchanged_Principal_Posts_Nothing_And_Marks_Predecessor_Renewed()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);

        FixedDepositDto successor = await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 500_000m), CancellationToken.None);

        predecessor.Status.Should().Be(FixedDepositStatus.Renewed);
        predecessor.RenewalAdjustmentPostingId.Should().BeNull();
        successor.CertificateNumber.Should().Be("FD-001-R1");
        successor.Principal.Should().Be(500_000m);
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Increased_Principal_Capitalizes_Interest_Up_To_The_Outstanding_Receivable()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);
        _accruals.GetTotalAccruedAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(40_000m);
        _receipts.GetTotalReceivedGrossAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(10_000m);

        await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 530_000m), CancellationToken.None);

        predecessor.RenewalAdjustmentPostingId.Should().NotBeNull();
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FixedDepositRenewalCapitalization" &&
                r.Lines.Any(l => l.Role == LedgerAccountType.FixedDeposit && l.Direction == LedgerDirection.Debit && l.Amount == 30_000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.InterestReceivable && l.Direction == LedgerDirection.Credit && l.Amount == 30_000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Increased_Principal_Beyond_Outstanding_Receivable_Is_Rejected()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);
        _accruals.GetTotalAccruedAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(10_000m);
        _receipts.GetTotalReceivedGrossAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(0m);

        Func<Task> act = async () => await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 530_000m), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        predecessor.Status.Should().Be(FixedDepositStatus.Active);
    }

    [Fact]
    public async Task Decreased_Principal_Posts_A_Partial_Withdrawal()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);

        await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 400_000m), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FixedDepositRenewalPartialWithdrawal" &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Debit &&
                    l.Amount == 100_000m && l.ExplicitAccountId == account.ChartOfAccountId) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.FixedDeposit && l.Direction == LedgerDirection.Credit && l.Amount == 100_000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Certificate_Number_Already_Exists()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account);
        _fixedDeposits.ExistsForCertificateNumberAsync(TenantId, "FD-001-R1", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 500_000m), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
