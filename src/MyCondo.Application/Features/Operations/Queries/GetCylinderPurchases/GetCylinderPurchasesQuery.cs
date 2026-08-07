using Mediator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderPurchases;

public sealed record GetCylinderPurchasesQuery(
    Guid? SupplierId,
    string? ApprovalStatus,
    int Page,
    int PageSize
) : IRequest<PagedResult<CylinderPurchaseDto>>;
