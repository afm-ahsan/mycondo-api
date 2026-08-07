using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorServiceRecords;

public sealed class GetGeneratorServiceRecordsQueryHandler(
    IGeneratorServiceRecordRepository serviceRecords,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorServiceRecordsQuery, PagedResult<GeneratorServiceRecordDto>>
{
    public async ValueTask<PagedResult<GeneratorServiceRecordDto>> Handle(GetGeneratorServiceRecordsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId? generatorId = query.GeneratorId is Guid raw ? new GeneratorId(raw) : null;

        PagedResult<GeneratorServiceRecord> result = await serviceRecords.SearchAsync(
            tenantId, generatorId, query.Page, query.PageSize, cancellationToken);

        List<GeneratorServiceRecordDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<GeneratorServiceRecordDto>(items, result.Page, result.PageSize, result.Total);
    }
}
