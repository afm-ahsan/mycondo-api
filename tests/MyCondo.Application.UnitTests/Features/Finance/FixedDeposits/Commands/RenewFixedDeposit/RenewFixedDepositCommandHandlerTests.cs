using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
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
    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RenewFixedDepositCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
        StubFinancialPosting();
    }

    /// <summary>Every posting request the handler issued, in order — lets a test assert on the source
    /// reference and fund a posting actually carried, not just that some posting happened.</summary>
    private readonly List<FinancialPostingRequest> _postedRequests = [];

    private void StubFinancialPosting() =>
        _financialPosting.PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                FinancialPostingRequest request = callInfo.Arg<FinancialPostingRequest>();
                _postedRequests.Add(request);
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
        _fixedDeposits, _accruals, _receipts, _financialAccounts, _financialPosting, _auditLog, _unitOfWork, _currentUser,
        _clock, Substitute.For<ILogger<RenewFixedDepositCommandHandler>>());

    private FixedDeposit SetUpActiveFixedDeposit(
        out FinancialAccount fundingAccount, decimal principal = 500_000m, FundId? fundId = null)
    {
        fundingAccount = FinancialAccount.Create(
            TenantId, "Main Bank", FinancialAccountType.Bank, null, null, null, ChartOfAccountId.New(), null, null);
        FixedDeposit fd = FixedDeposit.Place(
            FixedDepositId.New(), TenantId, "FD-001", "City Bank", null, fundingAccount.Id, fundId, principal, 7.5m,
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

    /// <summary>The successor's principal and the ledger's FixedDeposit-asset movement must reconcile
    /// exactly — the capitalization posting can never diverge from (NewPrincipal - OldPrincipal), which
    /// is what "cannot exceed outstanding receivable" protects but doesn't by itself prove is exact.</summary>
    [Fact]
    public async Task Increased_Principal_Capitalization_Amount_Exactly_Equals_The_Principal_Delta()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);
        _accruals.GetTotalAccruedAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(100_000m);
        _receipts.GetTotalReceivedGrossAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(0m);
        const decimal newPrincipal = 517_250m;
        decimal expectedDelta = newPrincipal - predecessor.Principal;

        FixedDepositDto successor = await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, newPrincipal), CancellationToken.None);

        successor.Principal.Should().Be(newPrincipal);
        (successor.Principal - predecessor.Principal).Should().Be(expectedDelta);
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Single(l => l.Role == LedgerAccountType.FixedDeposit).Amount == expectedDelta &&
                r.Lines.Single(l => l.Role == LedgerAccountType.InterestReceivable).Amount == expectedDelta),
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

    // ---------------------------------------------------------------------------------------------
    // Renewal posting traceability — each renewal posting must name the Fixed Deposit whose principal it
    // moves as its SourceId, the same convention placement/maturity/void already use. Without it the
    // tenant-wide FixedDeposit ledger account cannot be decomposed per instrument and the Phase 2A
    // Task 5 Fixed Deposits schedule reports the movement as an unexplained difference.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Capitalization_Posting_Is_Sourced_To_The_Successor_Fixed_Deposit()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);
        _accruals.GetTotalAccruedAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(40_000m);
        _receipts.GetTotalReceivedGrossAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(0m);

        FixedDepositDto successor = await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 530_000m), CancellationToken.None);

        FinancialPostingRequest capitalization = _postedRequests.Should().ContainSingle().Subject;
        capitalization.PostingPurpose.Should().Be("FixedDepositRenewalCapitalization");
        capitalization.SourceId.Should().Be(successor.FixedDepositId,
            "the capitalization establishes the successor's higher principal — it is the successor's own " +
            "placement-equivalent posting");
        capitalization.SourceId.Should().NotBeNull();
    }

    [Fact]
    public async Task Partial_Withdrawal_Posting_Is_Sourced_To_The_Predecessor_Fixed_Deposit()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);

        FixedDepositDto successor = await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 400_000m), CancellationToken.None);

        FinancialPostingRequest withdrawal = _postedRequests.Should().ContainSingle().Subject;
        withdrawal.PostingPurpose.Should().Be("FixedDepositRenewalPartialWithdrawal");
        withdrawal.SourceId.Should().Be(predecessor.Id.Value,
            "a partial withdrawal returns part of the predecessor's principal, exactly as a full " +
            "FixedDepositMaturity does for the deposit being withdrawn from");
        withdrawal.SourceId.Should().NotBe(successor.FixedDepositId,
            "attributing it to the successor would give the live instrument a negative carrying balance");
    }

    [Fact]
    public async Task An_Unchanged_Principal_Renewal_Still_Posts_Nothing_To_Be_Sourced()
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);

        await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, 500_000m), CancellationToken.None);

        _postedRequests.Should().BeEmpty("a pure lineage-continuation renewal has no principal movement to trace");
    }

    [Theory]
    [InlineData(530_000)]
    [InlineData(400_000)]
    public async Task Renewal_Postings_Preserve_The_Funds_Attribution_And_Accounting_Date(decimal newPrincipal)
    {
        FundId fundId = FundId.New();
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, 500_000m, fundId);
        _accruals.GetTotalAccruedAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(100_000m);
        _receipts.GetTotalReceivedGrossAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(0m);

        await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, newPrincipal), CancellationToken.None);

        FinancialPostingRequest request = _postedRequests.Should().ContainSingle().Subject;
        request.TenantId.Should().Be(TenantId);
        request.FundId.Should().Be(fundId, "adding a source reference must not disturb fund attribution");
        request.BusinessDate.Should().Be(new DateOnly(2027, 1, 1));
    }

    [Theory]
    [InlineData(530_000)]
    [InlineData(400_000)]
    public async Task Renewal_Postings_Remain_Balanced(decimal newPrincipal)
    {
        FixedDeposit predecessor = SetUpActiveFixedDeposit(out FinancialAccount account, principal: 500_000m);
        _accruals.GetTotalAccruedAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(100_000m);
        _receipts.GetTotalReceivedGrossAsync(predecessor.Id, Arg.Any<CancellationToken>()).Returns(0m);

        await CreateHandler().Handle(
            RenewalCommand(predecessor.Id.Value, account.Id.Value, newPrincipal), CancellationToken.None);

        // The stub routes through the real LedgerPosting.Create, which throws on an unbalanced posting —
        // reaching here at all proves it balanced; this asserts the debit/credit symmetry explicitly.
        FinancialPostingRequest request = _postedRequests.Should().ContainSingle().Subject;
        request.Lines.Where(l => l.Direction == LedgerDirection.Debit).Sum(l => l.Amount)
            .Should().Be(request.Lines.Where(l => l.Direction == LedgerDirection.Credit).Sum(l => l.Amount));
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
