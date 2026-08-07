using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RejectCylinderPurchase;

public sealed record RejectCylinderPurchaseCommand(Guid CylinderPurchaseId, string Reason) : IRequest<CylinderPurchaseDto>;
