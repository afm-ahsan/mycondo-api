using Mediator;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Commands.UpdateGate;

public sealed record UpdateGateCommand(
    Guid GateId,
    string Name,
    string Code,
    string? Description,
    bool IsEntryAllowed,
    bool IsExitAllowed,
    int DisplayOrder
) : IRequest<GateDto>;
