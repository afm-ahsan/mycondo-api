using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RecordFuelReceipt;

public sealed record RecordFuelReceiptCommand(
    Guid GeneratorId,
    DateTimeOffset ReceivedAtUtc,
    decimal Quantity,
    decimal? Cost,
    string? Supplier,
    string? Remarks
) : IRequest<GeneratorFuelReceiptDto>;
