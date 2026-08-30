using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;

/// <summary>
/// One organization's Platform Control Plane billing timeline (ADR-034 Task 14L) — every invoice-issued
/// and payment-recorded event, merged and paginated newest-first. <paramref name="DateFrom"/>/
/// <paramref name="DateTo"/> filter each event by its own authoritative date (invoice <c>IssueDate</c>
/// for an InvoiceIssued event, payment <c>PaymentDate</c> for a PaymentRecorded event).
/// </summary>
public sealed record GetOrganizationBillingHistoryQuery(
    Guid OrganizationId,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<OrganizationBillingHistoryEventDto>>;
