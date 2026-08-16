namespace MyCondo.Application.Features.Leasing.DTOs;

public sealed record HouseholdMemberDto(
    Guid HouseholdMemberId,
    Guid OccupancyRegistrationId,
    string FullName,
    string RelationshipToPrimary,
    DateOnly? DateOfBirth,
    string? Phone,
    string? NationalIdNumberMasked,
    string? Gender,
    string? BirthCertificateNumberMasked,
    string? BloodGroup,
    string? Religion,
    string? Nationality,
    string? Occupation,
    bool IsActive);
