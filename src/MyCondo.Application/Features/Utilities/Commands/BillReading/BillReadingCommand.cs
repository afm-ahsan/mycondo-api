using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.BillReading;

public sealed record BillReadingCommand(Guid ReadingId) : IRequest<InvoiceDto>;
