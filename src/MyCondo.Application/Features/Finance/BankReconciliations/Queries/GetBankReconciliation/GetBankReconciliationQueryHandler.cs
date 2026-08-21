using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Features.Finance.BankReconciliations;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Queries.GetBankReconciliation;

public sealed class GetBankReconciliationQueryHandler(
    IBankReconciliationRepository reconciliations,
    IBankStatementLineRepository statementLines,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBankReconciliationQuery, BankReconciliationDetailDto>
{
    public async ValueTask<BankReconciliationDetailDto> Handle(GetBankReconciliationQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BankReconciliationId id = new(query.BankReconciliationId);
        BankReconciliation reconciliation = await reconciliations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BankReconciliation), query.BankReconciliationId);
        if (reconciliation.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(BankReconciliation), query.BankReconciliationId);
        }

        IReadOnlyList<BankStatementLine> lines = await statementLines.GetForReconciliationAsync(id, cancellationToken);

        BankReconciliationDto dto = new(
            reconciliation.Id.Value, reconciliation.FinancialAccountId.Value, reconciliation.StatementDate,
            reconciliation.StatementBalance, reconciliation.OpeningLedgerBalance, reconciliation.Status.ToString(),
            reconciliation.ReconciledAtUtc);

        List<BankStatementLineDto> lineDtos = lines
            .Select(l => new BankStatementLineDto(
                l.Id.Value, l.BankReconciliationId.Value, l.TransactionDate, l.Description, l.Amount,
                l.Status.ToString(), l.MatchedLedgerEntryId?.Value, l.AdjustmentPostingId?.Value, l.ResolutionNotes))
            .ToList();

        return new BankReconciliationDetailDto(dto, lineDtos);
    }
}
