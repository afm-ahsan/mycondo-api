namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorBreakdownRecordDto(
    Guid GeneratorBreakdownRecordId,
    Guid GeneratorId,
    DateTimeOffset ReportedAtUtc,
    string Description,
    DateTimeOffset DowntimeStartUtc,
    DateTimeOffset? DowntimeEndUtc,
    string? Resolution,
    decimal? Cost);
