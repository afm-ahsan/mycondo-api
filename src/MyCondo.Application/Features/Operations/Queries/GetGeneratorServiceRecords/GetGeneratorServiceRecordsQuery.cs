using Mediator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorServiceRecords;

public sealed record GetGeneratorServiceRecordsQuery(
    Guid? GeneratorId,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorServiceRecordDto>>;
