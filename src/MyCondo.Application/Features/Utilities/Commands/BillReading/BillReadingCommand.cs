using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Commands.BillReading;

public sealed record BillReadingCommand(Guid ReadingId)
    : IRequest<InvoiceDto>, IHasReadingId, IRequiresResolvedFeature, ILifecycleWriteOperation;
