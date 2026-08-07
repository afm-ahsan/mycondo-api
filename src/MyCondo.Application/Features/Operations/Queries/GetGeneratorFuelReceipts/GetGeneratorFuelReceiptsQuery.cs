using Mediator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorFuelReceipts;

public sealed record GetGeneratorFuelReceiptsQuery(
    Guid? GeneratorId,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorFuelReceiptDto>>;
