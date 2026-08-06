using Mediator;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Billing.Queries.GetInvoices;

public sealed record GetInvoicesQuery(
    Guid? BuildingId,
    Guid? FlatId,
    string? Status,
    string? Source,
    int Page,
    int PageSize
) : IRequest<PagedResult<InvoiceDto>>;
