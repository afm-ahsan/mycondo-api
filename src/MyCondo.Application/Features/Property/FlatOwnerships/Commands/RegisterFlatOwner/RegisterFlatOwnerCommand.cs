using Mediator;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;

/// <summary>
/// Atomically registers a new Flat Owner: creates the shared <c>Resident</c> party record (Owner type,
/// with the full identity/contact/family-professional profile captured by the registration wizard) and
/// grants the first <c>FlatOwnership</c> in one operation. Deliberately does not go through a
/// Draft/Submitted/Verified approval lifecycle like Tenant Registration — ownership registration has no
/// concrete approval requirement, so this both creates and finalizes the record; additional owned Flats
/// are added afterward via the existing <c>CreateFlatOwnershipCommand</c> (ownership-specific operation,
/// not folded into this one).
/// </summary>
public sealed record RegisterFlatOwnerCommand(
    Guid FlatId,
    DateOnly StartDate,
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
    string? EmergencyContactPhone,
    string? BloodGroup,
    string? Religion,
    string? Nationality
) : IRequest<RegisterFlatOwnerResult>;

public sealed record RegisterFlatOwnerResult(Guid ResidentId, Guid FlatOwnershipId, ResidentDto Resident);
