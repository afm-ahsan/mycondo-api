using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Platform.Queries.ListSubscriptionInvoices;

/// <summary>
/// Platform-scope, paginated invoice search across every organization (ADR-034 Task 14D) — optionally
/// filtered by organization, invoice status, overdue state, and/or due-date range. Not RLS-filtered:
/// <c>platform.subscription_invoices</c> is Platform Control Plane data, no tenant RLS policy applies.
/// </summary>
public sealed record ListSubscriptionInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? OrganizationId = null,
    string? Status = null,
    bool? OverdueOnly = null,
    DateOnly? DueDateFrom = null,
    DateOnly? DueDateTo = null
) : IRequest<PagedResult<PlatformSubscriptionInvoiceListItemDto>>;
