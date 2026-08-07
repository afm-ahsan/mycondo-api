using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceDueReport;

public sealed record GetGeneratorMaintenanceDueReportQuery : IRequest<IReadOnlyList<GeneratorMaintenanceDueReportLineDto>>;
