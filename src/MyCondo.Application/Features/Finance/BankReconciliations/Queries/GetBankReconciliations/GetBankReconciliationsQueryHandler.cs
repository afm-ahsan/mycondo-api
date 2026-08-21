using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Queries.GetBankReconciliations;

public sealed class GetBankReconciliationsQueryHandler(
    IBankReconciliationRepository reconciliations,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBankReconciliationsQuery, List<BankReconciliationDto>>
{
    public async ValueTask<List<BankReconciliationDto>> Handle(GetBankReconciliationsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<BankReconciliation> results = await reconciliations.GetForFinancialAccountAsync(
            new FinancialAccountId(query.FinancialAccountId), cancellationToken);

        return results
            .Select(r => new BankReconciliationDto(
                r.Id.Value, r.FinancialAccountId.Value, r.StatementDate, r.StatementBalance, r.OpeningLedgerBalance,
                r.Status.ToString(), r.ReconciledAtUtc))
            .ToList();
    }
}
