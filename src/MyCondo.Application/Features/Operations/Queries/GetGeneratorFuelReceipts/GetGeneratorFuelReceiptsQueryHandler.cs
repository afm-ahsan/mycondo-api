using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorFuelReceipts;

public sealed class GetGeneratorFuelReceiptsQueryHandler(
    IGeneratorFuelReceiptRepository fuelReceipts,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorFuelReceiptsQuery, PagedResult<GeneratorFuelReceiptDto>>
{
    public async ValueTask<PagedResult<GeneratorFuelReceiptDto>> Handle(GetGeneratorFuelReceiptsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId? generatorId = query.GeneratorId is Guid raw ? new GeneratorId(raw) : null;

        PagedResult<GeneratorFuelReceipt> result = await fuelReceipts.SearchAsync(
            tenantId, generatorId, query.Page, query.PageSize, cancellationToken);

        List<GeneratorFuelReceiptDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<GeneratorFuelReceiptDto>(items, result.Page, result.PageSize, result.Total);
    }
}
