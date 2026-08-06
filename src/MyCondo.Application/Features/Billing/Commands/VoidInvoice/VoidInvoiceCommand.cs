using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.VoidInvoice;

public sealed record VoidInvoiceCommand(Guid InvoiceId, string Reason) : IRequest<InvoiceDto>;
