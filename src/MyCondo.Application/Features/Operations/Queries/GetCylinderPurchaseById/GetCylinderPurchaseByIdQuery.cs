using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderPurchaseById;

public sealed record GetCylinderPurchaseByIdQuery(Guid CylinderPurchaseId) : IRequest<CylinderPurchaseDto>;
