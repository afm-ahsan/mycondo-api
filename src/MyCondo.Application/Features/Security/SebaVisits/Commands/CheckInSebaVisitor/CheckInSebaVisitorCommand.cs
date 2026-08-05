using Mediator;
using MyCondo.Application.Features.Security.SebaVisits.DTOs;

namespace MyCondo.Application.Features.Security.SebaVisits.Commands.CheckInSebaVisitor;

public sealed record CheckInSebaVisitorCommand(
    string VisitorFullName,
    string? VisitorPhone,
    string? Organization,
    string? DepartmentOrEmployeeToMeet,
    string? TokenNumber,
    string? RelatedReferenceType,
    Guid? RelatedReferenceId,
    Guid EntryGateId
) : IRequest<SebaVisitDto>;
