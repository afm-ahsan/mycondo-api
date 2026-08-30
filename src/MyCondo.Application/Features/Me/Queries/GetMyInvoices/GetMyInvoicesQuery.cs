using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Me.Queries.GetMyInvoices;

public sealed record GetMyInvoicesQuery : IRequest<List<InvoiceDto>>, ILifecycleReadOperation;
