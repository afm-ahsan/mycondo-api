using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Commands.CreateGate;

/// <summary>Also the non-core/feature-gated write pilot for the ADR-032 Task 10 lifecycle read/write
/// classification — proves a feature-entitled write is still blocked under a Restricted/Expired
/// subscription (lifecycle denial, never feature_not_entitled).</summary>
public sealed record CreateGateCommand(
    Guid BuildingId,
    string Name,
    string Code,
    string? Description,
    bool IsEntryAllowed,
    bool IsExitAllowed,
    int DisplayOrder
) : IRequest<GateDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.gates";
}
