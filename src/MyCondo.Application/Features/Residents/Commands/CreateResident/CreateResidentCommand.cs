using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Residents.Commands.CreateResident;

/// <summary>Core write pilot for the ADR-032 Task 10 lifecycle read/write classification — proves an
/// ordinary core write is blocked under a Restricted/Expired subscription.</summary>
public sealed record CreateResidentCommand(
    Guid FlatId,
    string FullName,
    string? Phone,
    string? Email,
    string ResidentType
) : IRequest<ResidentDto>, ILifecycleWriteOperation;
