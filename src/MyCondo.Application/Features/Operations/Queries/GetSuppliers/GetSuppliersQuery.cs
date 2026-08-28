using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetSuppliers;

public sealed record GetSuppliersQuery(int Page, int PageSize) : IRequest<PagedResult<GasCylinderSupplierDto>>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}
