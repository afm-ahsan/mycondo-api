using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierById;

public sealed record GetSupplierByIdQuery(Guid GasCylinderSupplierId) : IRequest<GasCylinderSupplierDto>;
