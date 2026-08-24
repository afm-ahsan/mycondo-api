using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetFinancialPositionNotes;

/// <summary>Thin composition over <see cref="IFinancialStatementNoteService"/> — the schedules and their
/// reconciliation arithmetic live in the service, exactly as Tasks 3/4 delegate statement aggregation to
/// <c>IFinancialStatementReportingService</c>. Tenant resolution is default-deny: without a tenant
/// context no schedule is produced at all, rather than an organization-wide one.</summary>
public sealed class GetFinancialPositionNotesQueryHandler(
    IFinancialStatementNoteService noteService,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFinancialPositionNotesQuery, FinancialStatementNotesDto>
{
    public async ValueTask<FinancialStatementNotesDto> Handle(
        GetFinancialPositionNotesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        IReadOnlyList<FinancialStatementNote> notes = await noteService.GetAsOfNotesAsync(
            tenantId, asOfDate, query.FundId, query.Notes ?? [], cancellationToken);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, query.FundId is null ? "Tenant (all funds)" : "Single fund", clock.UtcNow, currentUser.UserId);

        return new FinancialStatementNotesDto(metadata, query.FundId, notes);
    }
}
