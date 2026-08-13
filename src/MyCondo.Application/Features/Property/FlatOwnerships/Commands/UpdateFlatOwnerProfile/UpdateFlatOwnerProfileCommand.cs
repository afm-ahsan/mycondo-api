using Mediator;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;

/// <summary>
/// Updates the extended owner-profile fields on an existing owner's Resident record — distinct from
/// the base <c>UpdateResidentCommand</c> (name/phone/email) and from any ownership-relationship
/// operation (<c>CreateFlatOwnershipCommand</c>/<c>EndFlatOwnershipCommand</c>), per the "ownership
/// changes use ownership-specific operations, profile edits use profile-specific operations" boundary.
/// </summary>
public sealed record UpdateFlatOwnerProfileCommand(
    Guid ResidentId,
    string FullName,
    string? Phone,
    string? Email,
    string? AlternatePhone,
    string? NationalIdNumber,
    string? PassportNumber,
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
    string? EmergencyContactPhone
) : IRequest<ResidentDto>;
