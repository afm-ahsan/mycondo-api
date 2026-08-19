using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.WaiveFine;

public sealed record WaiveFineCommand(Guid FineId, decimal Amount, string Reason) : IRequest<InvoiceDto>;
