using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.MarkCylinderPurchasePaid;

public sealed record MarkCylinderPurchasePaidCommand(Guid CylinderPurchaseId) : IRequest<CylinderPurchaseDto>;
