using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.CompleteBankReconciliation;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.BankReconciliations.Commands.CompleteBankReconciliation;

public class CompleteBankReconciliationCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly ChartOfAccountId BankChartOfAccountId = ChartOfAccountId.New();

    private readonly IBankReconciliationRepository _reconciliations = Substitute.For<IBankReconciliationRepository>();
    private readonly IBankStatementLineRepository _statementLines = Substitute.For<IBankStatementLineRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly IFinanceReportRepository _financeReports = Substitute.For<IFinanceReportRepository>();
    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CompleteBankReconciliationCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(NowUtc);
    }

    private BankReconciliation SetUp(decimal statementBalance, decimal ledgerDebit, decimal ledgerCredit, bool hasUnresolvedLines)
    {
        BankReconciliation reconciliation = BankReconciliation.Start(
            TenantId, FinancialAccountId.New(), new DateOnly(2026, 8, 31), statementBalance, 0m);
        _reconciliations.GetByIdAsync(reconciliation.Id, Arg.Any<CancellationToken>()).Returns(reconciliation);
        _statementLines.HasUnresolvedLinesAsync(reconciliation.Id, Arg.Any<CancellationToken>()).Returns(hasUnresolvedLines);

        FinancialAccount account = FinancialAccount.Create(
            TenantId, "Operating Account", FinancialAccountType.Bank, "Test Bank", null, null, BankChartOfAccountId, null, null);
        _financialAccounts.GetByIdAsync(reconciliation.FinancialAccountId, Arg.Any<CancellationToken>()).Returns(account);

        _financeReports.GetAccountBalanceBeforeAsync(
            TenantId, BankChartOfAccountId.Value, reconciliation.StatementDate.AddDays(1), Arg.Any<CancellationToken>())
            .Returns((ledgerDebit, ledgerCredit));

        return reconciliation;
    }

    private CompleteBankReconciliationCommandHandler CreateHandler() => new(
        _reconciliations, _statementLines, _financialAccounts, _financeReports, _auditLog, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CompleteBankReconciliationCommandHandler>>());

    [Fact]
    public async Task Throws_When_Unresolved_Lines_Remain_And_Never_Reaches_The_Domain_Balance_Check()
    {
        BankReconciliation reconciliation = SetUp(10_000m, 10_000m, 0m, hasUnresolvedLines: true);

        Func<Task> act = () => CreateHandler().Handle(
            new CompleteBankReconciliationCommand(reconciliation.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        reconciliation.Status.Should().Be(BankReconciliationStatus.InProgress);
    }

    [Fact]
    public async Task Completes_When_No_Unresolved_Lines_Remain_And_The_Ledger_Balance_Ties_Out()
    {
        BankReconciliation reconciliation = SetUp(10_000m, 10_000m, 0m, hasUnresolvedLines: false);

        await CreateHandler().Handle(new CompleteBankReconciliationCommand(reconciliation.Id.Value), CancellationToken.None);

        reconciliation.Status.Should().Be(BankReconciliationStatus.Reconciled);
        _auditLog.Received(1).Add(Arg.Is<FinanceAuditLogEntry>(e => e.Action == "BankReconciliation.Complete"));
    }

    [Fact]
    public async Task Throws_When_The_Ledger_Balance_Does_Not_Tie_Out_To_The_Statement_Balance()
    {
        BankReconciliation reconciliation = SetUp(10_000m, 9_800m, 0m, hasUnresolvedLines: false);

        Func<Task> act = () => CreateHandler().Handle(
            new CompleteBankReconciliationCommand(reconciliation.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<BankReconciliationBalanceMismatchException>();
        reconciliation.Status.Should().Be(BankReconciliationStatus.InProgress);
        _auditLog.DidNotReceive().Add(Arg.Any<FinanceAuditLogEntry>());
    }
}
