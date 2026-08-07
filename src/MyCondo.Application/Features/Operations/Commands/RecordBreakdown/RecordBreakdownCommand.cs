using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RecordBreakdown;

public sealed record RecordBreakdownCommand(
    Guid GeneratorId,
    DateTimeOffset ReportedAtUtc,
    string Description,
    DateTimeOffset DowntimeStartUtc
) : IRequest<GeneratorBreakdownRecordDto>;
