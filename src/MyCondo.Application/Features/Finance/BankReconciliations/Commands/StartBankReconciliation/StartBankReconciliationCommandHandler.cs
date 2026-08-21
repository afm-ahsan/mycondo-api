using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.StartBankReconciliation;

public sealed class StartBankReconciliationCommandHandler(
    IBankReconciliationRepository reconciliations,
    IFinancialAccountRepository financialAccounts,
    IFinanceReportRepository financeReports,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<StartBankReconciliationCommandHandler> logger
) : IRequestHandler<StartBankReconciliationCommand, BankReconciliationDto>
{
    public async ValueTask<BankReconciliationDto> Handle(StartBankReconciliationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinancialAccountId financialAccountId = new(command.FinancialAccountId);
        FinancialAccount account = await financialAccounts.GetByIdAsync(financialAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), command.FinancialAccountId);
        if (account.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialAccount), command.FinancialAccountId);
        }

        // "Balance strictly before StatementDate + 1 day" == "balance as of and including StatementDate" —
        // reuses GetAccountBalanceBeforeAsync's existing, already-tested exclusive-upper-bound semantics
        // rather than adding a second, near-duplicate ledger query method.
        (decimal totalDebit, decimal totalCredit) = await financeReports.GetAccountBalanceBeforeAsync(
            tenantId, account.ChartOfAccountId.Value, command.StatementDate.AddDays(1), cancellationToken);
        decimal openingLedgerBalance = totalDebit - totalCredit;

        BankReconciliation reconciliation = BankReconciliation.Start(
            tenantId, financialAccountId, command.StatementDate, command.StatementBalance, openingLedgerBalance);
        reconciliations.Add(reconciliation);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bank reconciliation {BankReconciliationId} started for Financial Account {FinancialAccountId}, tenant {TenantId}",
            reconciliation.Id, financialAccountId, tenantId);

        return new BankReconciliationDto(
            reconciliation.Id.Value, reconciliation.FinancialAccountId.Value, reconciliation.StatementDate,
            reconciliation.StatementBalance, reconciliation.OpeningLedgerBalance, reconciliation.Status.ToString(),
            reconciliation.ReconciledAtUtc);
    }
}
