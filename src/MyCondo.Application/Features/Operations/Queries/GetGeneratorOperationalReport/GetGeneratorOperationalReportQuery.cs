using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorOperationalReport;

public sealed record GetGeneratorOperationalReportQuery(
    Guid? GeneratorId,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<GeneratorOperationalReportLineDto>>;
