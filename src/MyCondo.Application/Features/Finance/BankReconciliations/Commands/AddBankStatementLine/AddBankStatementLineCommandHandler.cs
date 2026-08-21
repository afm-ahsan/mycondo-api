using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.BankReconciliations;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddBankStatementLine;

public sealed class AddBankStatementLineCommandHandler(
    IBankReconciliationRepository reconciliations,
    IBankStatementLineRepository statementLines,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<AddBankStatementLineCommandHandler> logger
) : IRequestHandler<AddBankStatementLineCommand, BankStatementLineDto>
{
    public async ValueTask<BankStatementLineDto> Handle(AddBankStatementLineCommand command, CancellationToken cancellationToken)
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

        if (reconciliation.Status == BankReconciliationStatus.Reconciled)
        {
            throw new ConflictException($"Bank reconciliation {reconciliation.Id} is already reconciled.");
        }

        BankStatementLine line = BankStatementLine.Add(
            tenantId, reconciliationId, command.TransactionDate, command.Description, command.Amount);
        statementLines.Add(line);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bank statement line {BankStatementLineId} added to reconciliation {BankReconciliationId}, tenant {TenantId}",
            line.Id, reconciliationId, tenantId);

        return new BankStatementLineDto(
            line.Id.Value, line.BankReconciliationId.Value, line.TransactionDate, line.Description, line.Amount,
            line.Status.ToString(), line.MatchedLedgerEntryId?.Value, line.AdjustmentPostingId?.Value, line.ResolutionNotes);
    }
}
