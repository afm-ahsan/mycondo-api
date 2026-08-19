using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestReceipt;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestReceipt;

/// <summary>
/// Proves Template 4's "Interest Earned != Interest Received" accrual-first rule: a receipt can never
/// exceed the FD's outstanding accrued-but-unreceived balance.
/// </summary>
public class RecordFixedDepositInterestReceiptCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFixedDepositRepository _fixedDeposits = Substitute.For<IFixedDepositRepository>();
    private readonly IFixedDepositInterestAccrualRepository _accruals = Substitute.For<IFixedDepositInterestAccrualRepository>();
    private readonly IFixedDepositInterestReceiptRepository _receipts = Substitute.For<IFixedDepositInterestReceiptRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RecordFixedDepositInterestReceiptCommandHandlerTests()
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

    private RecordFixedDepositInterestReceiptCommandHandler CreateHandler() => new(
        _fixedDeposits, _accruals, _receipts, _financialAccounts, _financialPosting, _unitOfWork, _currentUser,
        _clock, Substitute.For<ILogger<RecordFixedDepositInterestReceiptCommandHandler>>());

    private FixedDeposit SetUpActiveFixedDeposit()
    {
        FixedDeposit fd = FixedDeposit.Place(
            FixedDepositId.New(), TenantId, "FD-001", "City Bank", null, FinancialAccountId.New(), null, 500_000m,
            7.5m, InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), null, null, null, LedgerPostingId.New(), NowUtc);
        _fixedDeposits.GetByIdAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(fd);
        return fd;
    }

    private FinancialAccount SetUpReceivingAccount()
    {
        FinancialAccount account = FinancialAccount.Create(
            TenantId, "Main Bank", FinancialAccountType.Bank, null, null, null, ChartOfAccountId.New(), null, null);
        _financialAccounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        return account;
    }

    [Fact]
    public async Task Rejects_A_Receipt_That_Exceeds_The_Outstanding_Accrued_Interest()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit();
        FinancialAccount account = SetUpReceivingAccount();
        _accruals.GetTotalAccruedAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(3_000m);
        _receipts.GetTotalReceivedGrossAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(0m);

        Func<Task> act = async () => await CreateHandler().Handle(
            new RecordFixedDepositInterestReceiptCommand(fd.Id.Value, null, 5_000m, 0m, account.Id.Value, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_A_Receipt_Exactly_Equal_To_The_Outstanding_Accrued_Interest()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit();
        FinancialAccount account = SetUpReceivingAccount();
        _accruals.GetTotalAccruedAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(60_000m);
        _receipts.GetTotalReceivedGrossAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(0m);

        FixedDepositInterestReceiptDto result = await CreateHandler().Handle(
            new RecordFixedDepositInterestReceiptCommand(fd.Id.Value, null, 60_000m, 6_000m, account.Id.Value, "CHQ-01", null),
            CancellationToken.None);

        result.GrossAmount.Should().Be(60_000m);
        result.DeductionAmount.Should().Be(6_000m);
        result.NetAmount.Should().Be(54_000m);

        // Gross/deduction/net posting matrix: Dr CashOrBank (net) + Dr InterestDeductionExpense
        // (deduction) / Cr InterestReceivable (gross).
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FixedDepositInterestReceipt" &&
                r.Lines.Count == 3 &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Debit &&
                    l.Amount == 54_000m && l.ExplicitAccountId == account.ChartOfAccountId) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.InterestDeductionExpense && l.Direction == LedgerDirection.Debit && l.Amount == 6_000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.InterestReceivable && l.Direction == LedgerDirection.Credit && l.Amount == 60_000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Omits_The_CashOrBank_Line_When_Fully_Deducted_At_Source()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit();
        FinancialAccount account = SetUpReceivingAccount();
        _accruals.GetTotalAccruedAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(10_000m);
        _receipts.GetTotalReceivedGrossAsync(fd.Id, Arg.Any<CancellationToken>()).Returns(0m);

        await CreateHandler().Handle(
            new RecordFixedDepositInterestReceiptCommand(fd.Id.Value, null, 10_000m, 10_000m, account.Id.Value, null, null),
            CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Count == 2 &&
                r.Lines.All(l => l.Role != LedgerAccountType.CashOrBank) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.InterestDeductionExpense && l.Amount == 10_000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.InterestReceivable && l.Amount == 10_000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Fixed_Deposit_Is_Not_Active()
    {
        FixedDeposit fd = SetUpActiveFixedDeposit();
        fd.Void("cancelled", LedgerPostingId.New(), NowUtc);
        FinancialAccount account = SetUpReceivingAccount();

        Func<Task> act = async () => await CreateHandler().Handle(
            new RecordFixedDepositInterestReceiptCommand(fd.Id.Value, null, 100m, 0m, account.Id.Value, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
