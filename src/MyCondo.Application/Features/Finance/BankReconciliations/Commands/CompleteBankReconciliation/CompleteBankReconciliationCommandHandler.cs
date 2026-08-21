using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.CompleteBankReconciliation;

public sealed class CompleteBankReconciliationCommandHandler(
    IBankReconciliationRepository reconciliations,
    IBankStatementLineRepository statementLines,
    IFinancialAccountRepository financialAccounts,
    IFinanceReportRepository financeReports,
    IFinanceAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CompleteBankReconciliationCommandHandler> logger
) : IRequestHandler<CompleteBankReconciliationCommand, BankReconciliationDto>
{
    public async ValueTask<BankReconciliationDto> Handle(CompleteBankReconciliationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BankReconciliationId reconciliationId = new(command.BankReconciliationId);
        BankReconciliation reconciliation = await reconciliations.GetByIdAsync(reconciliationId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankReconciliation), command.BankReconciliationId);
        if (reconciliation.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(BankReconciliation), command.BankReconciliationId);
        }

        bool hasUnresolvedLines = await statementLines.HasUnresolvedLinesAsync(reconciliationId, cancellationToken);
        if (hasUnresolvedLines)
        {
            throw new ConflictException(
                $"Bank reconciliation {reconciliationId} has unresolved (Unmatched) statement lines — match, exclude, or adjust every line before completing.");
        }

        FinancialAccount financialAccount = await financialAccounts.GetByIdAsync(reconciliation.FinancialAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), reconciliation.FinancialAccountId.Value);

        (decimal totalDebit, decimal totalCredit) = await financeReports.GetAccountBalanceBeforeAsync(
            tenantId, financialAccount.ChartOfAccountId.Value, reconciliation.StatementDate.AddDays(1), cancellationToken);
        decimal computedLedgerBalance = totalDebit - totalCredit;

        reconciliation.Complete(computedLedgerBalance, currentUser.UserId, clock.UtcNow);
        auditLog.Add(FinanceAuditLogEntry.Record(
            tenantId, clock.UtcNow, currentUser.UserId, "BankReconciliation.Complete", nameof(BankReconciliation), reconciliationId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bank reconciliation {BankReconciliationId} completed for tenant {TenantId}", reconciliationId, tenantId);

        return new BankReconciliationDto(
            reconciliation.Id.Value, reconciliation.FinancialAccountId.Value, reconciliation.StatementDate,
            reconciliation.StatementBalance, reconciliation.OpeningLedgerBalance, reconciliation.Status.ToString(),
            reconciliation.ReconciledAtUtc);
    }
}
