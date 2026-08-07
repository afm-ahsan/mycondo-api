namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorServiceRecordDto(
    Guid GeneratorServiceRecordId,
    Guid GeneratorId,
    DateTimeOffset PerformedAtUtc,
    string Description,
    decimal? Cost,
    Guid? PerformedBy);
