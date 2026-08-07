using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ApproveCylinderPurchase;

public sealed record ApproveCylinderPurchaseCommand(Guid CylinderPurchaseId) : IRequest<CylinderPurchaseDto>;
