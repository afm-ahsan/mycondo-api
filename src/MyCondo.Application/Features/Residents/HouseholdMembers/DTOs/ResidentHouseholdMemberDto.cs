namespace MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;

public sealed record ResidentHouseholdMemberDto(
    Guid ResidentHouseholdMemberId,
    Guid ResidentId,
    string FullName,
    string RelationshipType,
    string Gender,
    DateOnly DateOfBirth,
    string? NationalIdNumberMasked,
    string? BirthCertificateNumberMasked,
    string? BloodGroup,
    string? Religion,
    string? Nationality,
    string? Occupation,
    bool IsActive);
