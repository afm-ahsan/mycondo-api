using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.AddHouseholdMember;

public sealed record AddHouseholdMemberCommand(
    Guid OccupancyRegistrationId,
    string FullName,
    string RelationshipToPrimary,
    DateOnly? DateOfBirth,
    string? Phone,
    string? NationalIdNumber
) : IRequest<HouseholdMemberDto>;
