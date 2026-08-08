using Mediator;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payments.Queries.GetPayments;

public sealed record GetPaymentsQuery(
    Guid? FlatId,
    string? Status,
    string? PaymentMethod,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize
) : IRequest<PagedResult<PaymentDto>>;
