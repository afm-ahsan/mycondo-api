using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.GenerateInvoiceBatch;

public sealed record GenerateInvoiceBatchCommand(
    Guid BuildingId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd
) : IRequest<GenerateInvoiceBatchResultDto>, ILifecycleWriteOperation;
