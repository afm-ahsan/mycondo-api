using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Commands.CreateGate;

public sealed record CreateGateCommand(
    Guid BuildingId,
    string Name,
    string Code,
    string? Description,
    bool IsEntryAllowed,
    bool IsExitAllowed,
    int DisplayOrder
) : IRequest<GateDto>, IRequiresFeature
{
    public string FeatureKey => "security.gates";
}
