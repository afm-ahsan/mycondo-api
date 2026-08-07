using Mediator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorBreakdownRecords;

public sealed record GetGeneratorBreakdownRecordsQuery(
    Guid? GeneratorId,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorBreakdownRecordDto>>;
