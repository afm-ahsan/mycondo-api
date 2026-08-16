using Mediator;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.SaveOwnerResidentProfile;

/// <summary>
/// Finds-or-creates the shared <c>Resident</c> party record for a prospective Flat Owner and sets their
/// extended owner-profile fields — deliberately does NOT grant a <c>FlatOwnership</c> (see
/// <c>CreateFlatOwnershipCommand</c> for that). Split out from <c>RegisterFlatOwnerCommand</c> so the
/// Flat Owner Registration wizard can persist identity/contact/household/document data incrementally
/// (Household and Documents steps need a real ResidentId before the final Review/Submit step) while the actual
/// ownership grant — the business-meaningful commitment — still happens atomically at the end, exactly
/// like today. A Resident created here with no FlatOwnership ever granted is an accepted, inert
/// abandoned-draft outcome, the same class of limitation Tenant Registration's Draft
/// OccupancyRegistration already has.
/// </summary>
public sealed record SaveOwnerResidentProfileCommand(
    Guid FlatId,
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
) : IRequest<ResidentDto>;
