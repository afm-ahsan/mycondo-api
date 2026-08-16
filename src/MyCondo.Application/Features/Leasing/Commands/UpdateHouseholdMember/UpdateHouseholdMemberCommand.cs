using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.UpdateHouseholdMember;

public sealed record UpdateHouseholdMemberCommand(
    Guid HouseholdMemberId,
    string FullName,
    string RelationshipToPrimary,
    DateOnly? DateOfBirth,
    string? Phone,
    string? NationalIdNumber,
    string? Gender,
    string? BirthCertificateNumber,
    string? BloodGroup,
    string? Religion,
    string? Nationality,
    string? Occupation
) : IRequest<HouseholdMemberDto>;
