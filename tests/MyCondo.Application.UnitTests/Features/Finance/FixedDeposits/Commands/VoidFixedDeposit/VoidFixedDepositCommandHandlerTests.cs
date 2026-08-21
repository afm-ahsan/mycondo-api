using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;

/// <summary>
/// Proves the "an FD with interest history is corrected by withdrawing, never voiding" guard —
/// <see cref="VoidFixedDepositCommandHandler"/>'s doc comment.
/// </summary>
public class VoidFixedDepositCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
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

    public VoidFixedDepositCommandHandlerTests()
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

    private VoidFixedDepositCommandHandler CreateHandler() => new(
        _fixedDeposits, _accruals, _receipts, _financialAccounts, _financialPosting, _auditLog, _unitOfWork, _currentUser,
        _clock, Substitute.For<ILogger<VoidFixedDepositCommandHandler>>());

    private FixedDeposit SetUpActiveFixedDeposit(out FinancialAccount fundingAccount)
    {
        fundingAccount = FinancialAccount.Create(
            TenantId, "Main Bank", FinancialAccountType.Bank, null, null, null, ChartOfAccountId.New(), null, null);
        FixedDeposit fd = FixedDeposit.Place(
            FixedDepositId.New(), TenantId, "FD-001", "City Bank", null, fundingAccount.Id, null, 500_000m, 7.5m,
            InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), null, null, null, LedgerPostingId.New(), NowUtc);
        _fixedDeposits.GetByIdAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(fd);
        _financialAccounts.GetByIdAsync(fundingAccount.Id, Arg.Any<CancellationToken>()).Returns(fundingAccount);
        _accruals.GetForFixedDepositAsync(fd.Id, Arg.Any<CancellationToken>()).Returns([]);
        _receipts.GetForFixedDepositAsync(fd.Id, Arg.Any<CancellationToken>()).Returns([]);
        return fd;
    }

    [Fact]
    public async Task Voids_A_Placement_With_No_Interest_History_By_Reversing_The_Placement_Posting()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit(out FinancialAccount fundingAccount);

        await CreateHandler().Handle(new VoidFixedDepositCommand(fd.Id.Value, "Data entry error"), CancellationToken.None);

        fd.Status.Should().Be(FixedDepositStatus.Voided);
        fd.VoidReversalPostingId.Should().NotBeNull();

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FixedDepositVoid" &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Debit &&
                    l.Amount == 500_000m && l.ExplicitAccountId == fundingAccount.ChartOfAccountId) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.FixedDeposit && l.Direction == LedgerDirection.Credit && l.Amount == 500_000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_Voiding_A_Fixed_Deposit_That_Has_Interest_Accruals()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit(out _);
        FixedDepositInterestAccrual accrual = FixedDepositInterestAccrual.Record(
            FixedDepositInterestAccrualId.New(), TenantId, fd.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            new DateOnly(2026, 1, 31), 3_000m, null, LedgerPostingId.New(), NowUtc);
        _accruals.GetForFixedDepositAsync(fd.Id, Arg.Any<CancellationToken>()).Returns([accrual]);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new VoidFixedDepositCommand(fd.Id.Value, "Data entry error"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        fd.Status.Should().Be(FixedDepositStatus.Active);
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Fixed_Deposit_Is_Already_Voided()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit(out _);
        fd.Void("First void", LedgerPostingId.New(), NowUtc);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new VoidFixedDepositCommand(fd.Id.Value, "Second void"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
