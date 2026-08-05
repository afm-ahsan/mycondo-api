namespace MyCondo.Application.Features.Security.AccessSessions.DTOs;

public sealed record AccessSessionDto(
    Guid AccessSessionId,
    string AccessCategory,
    Guid? GuestProfileId,
    Guid? VehicleId,
    Guid? HostFlatId,
    string? PurposeOfVisit,
    Guid EntryGateId,
    DateTimeOffset EntryAtUtc,
    Guid? ExitGateId,
    DateTimeOffset? ExitAtUtc,
    Guid? CheckedInBy,
    Guid? CheckedOutBy,
    string ApprovalStatus,
    string? PassOrQrNumber,
    string? Remarks,
    string Status,
    string? OverrideReason);
