using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.BankReconciliations;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.ExcludeStatementLine;

public sealed class ExcludeStatementLineCommandHandler(
    IBankStatementLineRepository statementLines,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ExcludeStatementLineCommandHandler> logger
) : IRequestHandler<ExcludeStatementLineCommand, BankStatementLineDto>
{
    public async ValueTask<BankStatementLineDto> Handle(ExcludeStatementLineCommand command, CancellationToken cancellationToken)
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

        line.Exclude(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bank statement line {BankStatementLineId} excluded, tenant {TenantId}", lineId, tenantId);

        return new BankStatementLineDto(
            line.Id.Value, line.BankReconciliationId.Value, line.TransactionDate, line.Description, line.Amount,
            line.Status.ToString(), line.MatchedLedgerEntryId?.Value, line.AdjustmentPostingId?.Value, line.ResolutionNotes);
    }
}
