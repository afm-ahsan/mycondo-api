using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.AssessFine;

public sealed record AssessFineCommand(
    Guid FlatId,
    decimal Amount,
    string Reason,
    DateOnly BusinessDate
) : IRequest<InvoiceDto>, ILifecycleWriteOperation;
