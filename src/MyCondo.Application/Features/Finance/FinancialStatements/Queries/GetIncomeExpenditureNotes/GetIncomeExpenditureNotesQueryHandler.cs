using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;

/// <summary>Period counterpart of <c>GetFinancialPositionNotesQueryHandler</c> — same delegation, same
/// default-deny tenant resolution.</summary>
public sealed class GetIncomeExpenditureNotesQueryHandler(
    IFinancialStatementNoteService noteService,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetIncomeExpenditureNotesQuery, FinancialStatementNotesDto>
{
    public async ValueTask<FinancialStatementNotesDto> Handle(
        GetIncomeExpenditureNotesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<FinancialStatementNote> notes = await noteService.GetPeriodNotesAsync(
            tenantId, query.StartDate, query.EndDate, query.FundId, query.Notes ?? [], cancellationToken);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.StartDate, query.EndDate, query.FundId is null ? "Tenant (all funds)" : "Single fund",
            clock.UtcNow, currentUser.UserId);

        return new FinancialStatementNotesDto(metadata, query.FundId, notes);
    }
}
