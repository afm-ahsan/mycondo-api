namespace MyCondo.Application.Features.Residents.DTOs;

public sealed record ResidentDto(
    Guid ResidentId,
    Guid FlatId,
    string FullName,
    string? Phone,
    string? Email,
    string ResidentType,
    string? AlternatePhone,
    string? NationalIdNumberMasked,
    string? PassportNumberMasked,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PresentAddress,
    string? PermanentAddress,
    string? FatherName,
    string? MotherName,
    string? MaritalStatus,
    string? Profession,
    string? Employer,
    string? OfficeAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone);
