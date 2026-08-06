using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDetailDto>;
