using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddReconciliationAdjustment;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.BankReconciliations.Commands.AddReconciliationAdjustment;

/// <summary>Proves the sign-to-direction inference described in the handler's own comment: a positive
/// statement line (money the bank added, e.g. interest) debits the bank account and credits the
/// other-side role; a negative line (a bank charge) is the mirror image — without needing a database,
/// same <see cref="LedgerPosting.Create"/>-backed <see cref="IFinancialPostingService"/> stub every other
/// handler test in this solution uses.</summary>
public class AddReconciliationAdjustmentCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly ChartOfAccountId BankChartOfAccountId = ChartOfAccountId.New();

    private readonly IBankStatementLineRepository _statementLines = Substitute.For<IBankStatementLineRepository>();
    private readonly IBankReconciliationRepository _reconciliations = Substitute.For<IBankReconciliationRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AddReconciliationAdjustmentCommandHandlerTests()
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
                return new FinancialPostingResult(posting, entries);
            });

    private (BankStatementLine Line, FinancialAccount Account) SetUpUnmatchedLine(decimal amount)
    {
        BankReconciliation reconciliation = BankReconciliation.Start(
            TenantId, FinancialAccountId.New(), new DateOnly(2026, 8, 31), 10_000m, 9_500m);
        BankStatementLine line = BankStatementLine.Add(TenantId, reconciliation.Id, new DateOnly(2026, 8, 15), "Bank interest", amount);
        _statementLines.GetByIdAsync(line.Id, Arg.Any<CancellationToken>()).Returns(line);
        _reconciliations.GetByIdAsync(reconciliation.Id, Arg.Any<CancellationToken>()).Returns(reconciliation);

        FinancialAccount account = FinancialAccount.Create(
            TenantId, "Operating Account", FinancialAccountType.Bank, "Test Bank", null, null, BankChartOfAccountId, null, null);
        _financialAccounts.GetByIdAsync(reconciliation.FinancialAccountId, Arg.Any<CancellationToken>()).Returns(account);

        return (line, account);
    }

    private AddReconciliationAdjustmentCommandHandler CreateHandler() => new(
        _statementLines, _reconciliations, _financialAccounts, _financialPosting, _auditLog, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<AddReconciliationAdjustmentCommandHandler>>());

    [Fact]
    public async Task Positive_Line_Debits_The_Bank_Account_And_Credits_The_Other_Side_Role()
    {
        (BankStatementLine line, FinancialAccount account) = SetUpUnmatchedLine(150m);

        await CreateHandler().Handle(
            new AddReconciliationAdjustmentCommand(line.Id.Value, "FDInterestIncome", "Bank interest credited"),
            CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Debit &&
                                  l.Amount == 150m && l.ExplicitAccountId == account.ChartOfAccountId) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.FDInterestIncome && l.Direction == LedgerDirection.Credit && l.Amount == 150m)),
            Arg.Any<CancellationToken>());
        line.Status.Should().Be(BankStatementLineStatus.Adjusted);
    }

    [Fact]
    public async Task Negative_Line_Credits_The_Bank_Account_And_Debits_The_Other_Side_Role()
    {
        (BankStatementLine line, FinancialAccount account) = SetUpUnmatchedLine(-75m);

        await CreateHandler().Handle(
            new AddReconciliationAdjustmentCommand(line.Id.Value, "OperatingExpense", "Bank service charge"),
            CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Credit &&
                                  l.Amount == 75m && l.ExplicitAccountId == account.ChartOfAccountId) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.OperatingExpense && l.Direction == LedgerDirection.Debit && l.Amount == 75m)),
            Arg.Any<CancellationToken>());
        line.Status.Should().Be(BankStatementLineStatus.Adjusted);
    }

    [Fact]
    public async Task Uses_The_Statement_Lines_Own_Id_As_The_Posting_SourceId_For_Idempotency()
    {
        (BankStatementLine line, _) = SetUpUnmatchedLine(150m);

        await CreateHandler().Handle(
            new AddReconciliationAdjustmentCommand(line.Id.Value, "FDInterestIncome", "Bank interest credited"),
            CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.SourceId == line.Id.Value && r.PostingPurpose == "BankReconciliationAdjustment"),
            Arg.Any<CancellationToken>());
    }
}
