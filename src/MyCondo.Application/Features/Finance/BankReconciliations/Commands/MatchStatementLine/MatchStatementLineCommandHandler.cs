using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.MatchStatementLine;

public sealed class MatchStatementLineCommandHandler(
    IBankStatementLineRepository statementLines,
    IBankReconciliationRepository reconciliations,
    IFinancialAccountRepository financialAccounts,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<MatchStatementLineCommandHandler> logger
) : IRequestHandler<MatchStatementLineCommand, BankStatementLineDto>
{
    public async ValueTask<BankStatementLineDto> Handle(MatchStatementLineCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BankStatementLineId lineId = new(command.BankStatementLineId);
        BankStatementLine line = await statementLines.GetByIdAsync(lineId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankStatementLine), command.BankStatementLineId);
        if (line.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(BankStatementLine), command.BankStatementLineId);
        }

        BankReconciliation reconciliation = await reconciliations.GetByIdAsync(line.BankReconciliationId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankReconciliation), line.BankReconciliationId.Value);

        LedgerEntryId ledgerEntryId = new(command.LedgerEntryId);
        LedgerEntry ledgerEntry = await ledgerEntries.GetByIdAsync(ledgerEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerEntry), command.LedgerEntryId);
        if (ledgerEntry.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(LedgerEntry), command.LedgerEntryId);
        }

        FinancialAccount financialAccount = await financialAccounts.GetByIdAsync(reconciliation.FinancialAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), reconciliation.FinancialAccountId.Value);
        if (ledgerEntry.ChartOfAccountId != financialAccount.ChartOfAccountId)
        {
            throw new ConflictException(
                $"Ledger entry {ledgerEntry.Id} does not belong to Financial Account {financialAccount.Id}'s ledger account.");
        }

        line.MatchTo(ledgerEntryId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bank statement line {BankStatementLineId} matched to ledger entry {LedgerEntryId}, tenant {TenantId}",
            lineId, ledgerEntryId, tenantId);

        return new BankStatementLineDto(
            line.Id.Value, line.BankReconciliationId.Value, line.TransactionDate, line.Description, line.Amount,
            line.Status.ToString(), line.MatchedLedgerEntryId?.Value, line.AdjustmentPostingId?.Value, line.ResolutionNotes);
    }
}
