using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorBreakdownRecords;

public sealed class GetGeneratorBreakdownRecordsQueryHandler(
    IGeneratorBreakdownRecordRepository breakdowns,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorBreakdownRecordsQuery, PagedResult<GeneratorBreakdownRecordDto>>
{
    public async ValueTask<PagedResult<GeneratorBreakdownRecordDto>> Handle(GetGeneratorBreakdownRecordsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId? generatorId = query.GeneratorId is Guid raw ? new GeneratorId(raw) : null;

        PagedResult<GeneratorBreakdownRecord> result = await breakdowns.SearchAsync(
            tenantId, generatorId, query.Page, query.PageSize, cancellationToken);

        List<GeneratorBreakdownRecordDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<GeneratorBreakdownRecordDto>(items, result.Page, result.PageSize, result.Total);
    }
}
