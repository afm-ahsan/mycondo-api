using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.ReverseFine;

public sealed record ReverseFineCommand(Guid FineId, string Reason) : IRequest<InvoiceDto>;
