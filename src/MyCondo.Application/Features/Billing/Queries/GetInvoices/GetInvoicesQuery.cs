using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Billing.Queries.GetInvoices;

/// <summary>Finance-read pilot for the ADR-032 Task 10 lifecycle read/write classification — proves a
/// tenant Finance read keeps working under a Restricted/Expired subscription (never conflated with
/// CondoBD platform-billing resolution, ADR-032 Task 10 §39/§40).</summary>
public sealed record GetInvoicesQuery(
    Guid? BuildingId,
    Guid? FlatId,
    string? Status,
    string? Source,
    int Page,
    int PageSize
) : IRequest<PagedResult<InvoiceDto>>, ILifecycleReadOperation;
