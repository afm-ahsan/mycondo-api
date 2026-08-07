using MyCondo.Application.Common;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Leasing.Mappings;

/// <summary>National ID/passport numbers are masked in every DTO — same discipline as
/// <c>GuestProfileDto</c>/<c>DomesticWorkerProfileDto</c>/<c>ServiceProviderProfileDto</c> via
/// <see cref="IdentityMasking"/>. No endpoint in this feature exposes the unmasked value.</summary>
internal static class LeasingMappings
{
    public static OccupancyRegistrationDto ToDto(this OccupancyRegistration registration) => new(
        registration.Id.Value, registration.FlatId.Value, registration.PrimaryResidentId.Value,
        registration.OccupancyType.ToString(), registration.PrimaryFullName, registration.PrimaryPhone,
        registration.PrimaryEmail, IdentityMasking.Mask(registration.PrimaryNationalIdNumber), registration.PrimaryDateOfBirth,
        registration.PrimaryPermanentAddress, registration.EmergencyContactName, registration.EmergencyContactPhone,
        registration.PrimaryPhotoAttachmentId, registration.MoveInExpectedDate,
        registration.Status.ToString(), registration.SubmittedAtUtc, registration.OwnerReviewedAtUtc,
        registration.ManagementVerifiedAtUtc, registration.ActivatedAtUtc, registration.MovedOutAtUtc,
        registration.MoveOutReason, registration.CorrectionsRequestedReason, registration.RejectedReason);

    public static HouseholdMemberDto ToDto(this HouseholdMember member) => new(
        member.Id.Value, member.OccupancyRegistrationId.Value, member.FullName, member.RelationshipToPrimary,
        member.DateOfBirth, member.Phone, IdentityMasking.Mask(member.NationalIdNumber), member.IsActive);

    public static OccupancyRegistrationStatusHistoryDto ToDto(this OccupancyRegistrationStatusHistory entry) => new(
        entry.Id.Value, entry.OccupancyRegistrationId.Value, entry.FromStatus?.ToString(), entry.ToStatus.ToString(),
        entry.ChangedBy, entry.ChangedAtUtc, entry.Reason);

    public static OccupancyRegistrationWorkerAssignmentDto ToDto(
        this OccupancyRegistrationWorkerAssignment assignment, DomesticWorkerProfile worker) => new(
        assignment.Id.Value, assignment.OccupancyRegistrationId.Value, assignment.DomesticWorkerProfileId.Value,
        worker.FullName, worker.Phone, worker.WorkerType.ToString(), worker.VerificationStatus.ToString(),
        assignment.AssignedAtUtc, assignment.EndedAtUtc, assignment.IsActive);

    public static OccupancyRegistrationVehicleAssignmentDto ToDto(
        this OccupancyRegistrationVehicleAssignment assignment, Vehicle vehicle) => new(
        assignment.Id.Value, assignment.OccupancyRegistrationId.Value, assignment.VehicleId.Value,
        vehicle.RegistrationNumber, vehicle.VehicleType.ToString(), vehicle.IsBlocked, assignment.AssignedAtUtc,
        assignment.EndedAtUtc, assignment.IsActive);
}
