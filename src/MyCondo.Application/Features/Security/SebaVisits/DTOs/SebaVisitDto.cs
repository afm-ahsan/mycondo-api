namespace MyCondo.Application.Features.Security.SebaVisits.DTOs;

public sealed record SebaVisitDto(
    Guid AccessSessionId,
    string VisitorFullName,
    string? VisitorPhone,
    string? Organization,
    string? DepartmentOrEmployeeToMeet,
    string? TokenNumber,
    string? RelatedReferenceType,
    Guid? RelatedReferenceId,
    string? ServiceOutcome,
    bool Acknowledged,
    Guid EntryGateId,
    DateTimeOffset EntryAtUtc,
    Guid? ExitGateId,
    DateTimeOffset? ExitAtUtc,
    string Status);
