using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Operations.Queries.GetGenerators;

public sealed class GetGeneratorsQueryHandler(
    IGeneratorRepository generators,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorsQuery, PagedResult<GeneratorDto>>
{
    public async ValueTask<PagedResult<GeneratorDto>> Handle(GetGeneratorsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;

        PagedResult<Generator> result = await generators.SearchAsync(
            tenantId, buildingId, query.Page, query.PageSize, cancellationToken);

        List<GeneratorDto> items = result.Items.Select(g => g.ToDto()).ToList();

        return new PagedResult<GeneratorDto>(items, result.Page, result.PageSize, result.Total);
    }
}
