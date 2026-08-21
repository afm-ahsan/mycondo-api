using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Integrity.DTOs;
using MyCondo.Domain.Features.Finance.Integrity;

namespace MyCondo.Application.Features.Finance.Integrity.Queries.GetFinancialIntegrityDashboard;

public sealed class GetFinancialIntegrityDashboardQueryHandler(
    IFinanceIntegrityRepository integrity,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFinancialIntegrityDashboardQuery, FinancialIntegrityDashboardDto>
{
    private const int StaleAfterDays = 45;

    public async ValueTask<FinancialIntegrityDashboardDto> Handle(
        GetFinancialIntegrityDashboardQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        long unbalancedPostings = await integrity.CountUnbalancedPostingsAsync(tenantId, cancellationToken);
        long duplicateLogicalPostings = await integrity.CountDuplicateLogicalPostingsAsync(tenantId, cancellationToken);
        long closedPeriodViolations = await integrity.CountClosedPeriodViolationsAsync(tenantId, cancellationToken);
        long staleUnreconciledBankItems = await integrity.CountStaleUnreconciledBankItemsAsync(tenantId, StaleAfterDays, cancellationToken);
        long staleUnreceivedInterestAccruals = await integrity.CountStaleUnreceivedInterestAccrualsAsync(tenantId, StaleAfterDays, cancellationToken);

        bool isHealthy = unbalancedPostings == 0 && duplicateLogicalPostings == 0 && closedPeriodViolations == 0;

        return new FinancialIntegrityDashboardDto(
            unbalancedPostings, duplicateLogicalPostings, closedPeriodViolations, staleUnreconciledBankItems,
            staleUnreceivedInterestAccruals, isHealthy);
    }
}
