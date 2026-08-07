namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record PoolSessionDto(
    Guid PoolSessionId,
    Guid FacilityId,
    Guid FlatId,
    string PersonType,
    string AgeCategory,
    Guid? AccompaniedBySessionId,
    DateTimeOffset EntryAtUtc,
    DateTimeOffset? ExitAtUtc,
    decimal? GuestFeeAmount,
    DateTimeOffset? SafetyAcknowledgedAtUtc,
    Guid? CheckedInBy,
    Guid? CheckedOutBy,
    string? OverrideReason,
    string Status);
