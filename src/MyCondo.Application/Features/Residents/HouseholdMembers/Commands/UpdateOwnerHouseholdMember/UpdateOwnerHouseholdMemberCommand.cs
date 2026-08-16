using Mediator;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.UpdateOwnerHouseholdMember;

public sealed record UpdateOwnerHouseholdMemberCommand(
    Guid ResidentHouseholdMemberId,
    string FullName,
    string RelationshipType,
    string Gender,
    DateOnly DateOfBirth,
    string? NationalIdNumber,
    string? BirthCertificateNumber,
    string? BloodGroup,
    string? Religion,
    string? Nationality,
    string? Occupation
) : IRequest<ResidentHouseholdMemberDto>;
