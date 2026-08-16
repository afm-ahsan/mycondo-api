using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Leasing.Commands.ActivateOccupancyRegistration;
using MyCondo.Application.Features.Leasing.Commands.AddHouseholdMember;
using MyCondo.Application.Features.Leasing.Commands.ApproveOccupancyRegistrationByOwner;
using MyCondo.Application.Features.Leasing.Commands.AssignVehicleToOccupancyRegistration;
using MyCondo.Application.Features.Leasing.Commands.AssignWorkerToOccupancyRegistration;
using MyCondo.Application.Features.Leasing.Commands.CreateOccupancyRegistration;
using MyCondo.Application.Features.Leasing.Commands.DeactivateHouseholdMember;
using MyCondo.Application.Features.Leasing.Commands.UpdateHouseholdMember;
using MyCondo.Application.Features.Leasing.Commands.EndVehicleAssignment;
using MyCondo.Application.Features.Leasing.Commands.EndWorkerAssignment;
using MyCondo.Application.Features.Leasing.Commands.MoveOutOccupancyRegistration;
using MyCondo.Application.Features.Leasing.Commands.RejectOccupancyRegistrationByManagement;
using MyCondo.Application.Features.Leasing.Commands.RejectOccupancyRegistrationByOwner;
using MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByManagement;
using MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByOwner;
using MyCondo.Application.Features.Leasing.Commands.SetHouseholdMemberPrimaryPhoto;
using MyCondo.Application.Features.Leasing.Commands.SetOccupancyRegistrationPrimaryPhoto;
using MyCondo.Application.Features.Leasing.Commands.SubmitOccupancyRegistration;
using MyCondo.Application.Features.Leasing.Commands.UpdateOccupancyRegistrationDraft;
using MyCondo.Application.Features.Leasing.Commands.VerifyOccupancyRegistrationByManagement;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Queries.GetHouseholdMembers;
using MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationById;
using MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;
using MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationSecurityView;
using MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationStatusHistory;
using MyCondo.Application.Features.Leasing.Queries.GetOccupancySecurityViews;
using MyCondo.Application.Features.Leasing.Queries.GetVehicleAssignmentsForRegistration;
using MyCondo.Application.Features.Leasing.Queries.GetWorkerAssignmentsForRegistration;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// "Tenant Registration" routes — the domain/permission names use <c>OccupancyRegistration</c> /
/// <c>occupancy-registration.*</c> to avoid colliding with this API's multi-tenancy vocabulary; see
/// <c>OccupancyRegistration</c>'s doc comment. The route segment itself stays human-facing.
/// </summary>
public static class OccupancyRegistrationEndpoints
{
    public static IEndpointRouteBuilder MapOccupancyRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder registrations = app.MapGroup("/api/v1/occupancy-registrations").WithTags("Tenant Registrations");

        registrations.MapPost("/", async (CreateOccupancyRegistrationCommand command, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapGet("/", async (Guid? flatId, string? status, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<OccupancyRegistrationDto> result = await sender.Send(
                    new GetOccupancyRegistrationsQuery(flatId, status, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.view")
            .Produces<PagedResult<OccupancyRegistrationDto>>(StatusCodes.Status200OK);

        registrations.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new GetOccupancyRegistrationByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.view")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPut("/{id:guid}", async (Guid id, UpdateOccupancyRegistrationDraftRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(
                    new UpdateOccupancyRegistrationDraftCommand(
                        id, body.PrimaryFullName, body.PrimaryPhone, body.PrimaryEmail, body.PrimaryNationalIdNumber,
                        body.PrimaryDateOfBirth, body.PrimaryGender, body.PrimaryBloodGroup, body.PrimaryReligion,
                        body.PrimaryNationality, body.PrimaryProfession, body.PrimaryPermanentAddress,
                        body.EmergencyContactName, body.EmergencyContactPhone, body.MoveInExpectedDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPut("/{id:guid}/primary-photo", async (Guid id, SetPrimaryPhotoRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(
                    new SetOccupancyRegistrationPrimaryPhotoCommand(id, body.AttachmentId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/submit", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new SubmitOccupancyRegistrationCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/owner-approve", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new ApproveOccupancyRegistrationByOwnerCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.owner-review")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/owner-request-corrections", async (Guid id, ReasonRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(
                    new RequestOccupancyRegistrationCorrectionsByOwnerCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.owner-review")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/owner-reject", async (Guid id, ReasonRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new RejectOccupancyRegistrationByOwnerCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.owner-review")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/management-verify", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new VerifyOccupancyRegistrationByManagementCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.verify")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/management-request-corrections", async (Guid id, ReasonRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(
                    new RequestOccupancyRegistrationCorrectionsByManagementCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.verify")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/management-reject", async (Guid id, ReasonRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new RejectOccupancyRegistrationByManagementCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.verify")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new ActivateOccupancyRegistrationCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.verify")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/move-out", async (Guid id, MoveOutRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationDto result = await sender.Send(new MoveOutOccupancyRegistrationCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.move-out")
            .Produces<OccupancyRegistrationDto>(StatusCodes.Status200OK);

        registrations.MapGet("/{id:guid}/status-history", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<OccupancyRegistrationStatusHistoryDto> result =
                    await sender.Send(new GetOccupancyRegistrationStatusHistoryQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.view")
            .Produces<IReadOnlyList<OccupancyRegistrationStatusHistoryDto>>(StatusCodes.Status200OK);

        registrations.MapGet("/{id:guid}/household-members", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<HouseholdMemberDto> result = await sender.Send(new GetHouseholdMembersQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.view")
            .Produces<IReadOnlyList<HouseholdMemberDto>>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/household-members", async (Guid id, AddHouseholdMemberRequest body, ISender sender, CancellationToken ct) =>
            {
                HouseholdMemberDto result = await sender.Send(
                    new AddHouseholdMemberCommand(
                        id, body.FullName, body.RelationshipToPrimary, body.DateOfBirth, body.Phone,
                        body.NationalIdNumber, body.Gender, body.BirthCertificateNumber, body.BloodGroup,
                        body.Religion, body.Nationality, body.Occupation), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<HouseholdMemberDto>(StatusCodes.Status200OK);

        app.MapPut("/api/v1/household-members/{id:guid}", async (Guid id, UpdateHouseholdMemberRequest body, ISender sender, CancellationToken ct) =>
            {
                HouseholdMemberDto result = await sender.Send(
                    new UpdateHouseholdMemberCommand(
                        id, body.FullName, body.RelationshipToPrimary, body.DateOfBirth, body.Phone,
                        body.NationalIdNumber, body.Gender, body.BirthCertificateNumber, body.BloodGroup,
                        body.Religion, body.Nationality, body.Occupation), ct);
                return Results.Ok(result);
            })
            .WithTags("Tenant Registrations")
            .RequirePermission("occupancy-registration.create")
            .Produces<HouseholdMemberDto>(StatusCodes.Status200OK);

        app.MapPost("/api/v1/household-members/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                HouseholdMemberDto result = await sender.Send(new DeactivateHouseholdMemberCommand(id), ct);
                return Results.Ok(result);
            })
            .WithTags("Tenant Registrations")
            .RequirePermission("occupancy-registration.create")
            .Produces<HouseholdMemberDto>(StatusCodes.Status200OK);

        app.MapPut("/api/v1/household-members/{id:guid}/primary-photo", async (Guid id, SetPrimaryPhotoRequest body, ISender sender, CancellationToken ct) =>
            {
                HouseholdMemberDto result = await sender.Send(
                    new SetHouseholdMemberPrimaryPhotoCommand(id, body.AttachmentId), ct);
                return Results.Ok(result);
            })
            .WithTags("Tenant Registrations")
            .RequirePermission("occupancy-registration.create")
            .Produces<HouseholdMemberDto>(StatusCodes.Status200OK);

        registrations.MapGet("/{id:guid}/worker-assignments", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<OccupancyRegistrationWorkerAssignmentDto> result =
                    await sender.Send(new GetWorkerAssignmentsForRegistrationQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.view")
            .Produces<IReadOnlyList<OccupancyRegistrationWorkerAssignmentDto>>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/worker-assignments", async (Guid id, AssignWorkerRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationWorkerAssignmentDto result =
                    await sender.Send(new AssignWorkerToOccupancyRegistrationCommand(id, body.DomesticWorkerProfileId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationWorkerAssignmentDto>(StatusCodes.Status200OK);

        app.MapPost("/api/v1/worker-assignments/{id:guid}/end", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationWorkerAssignmentDto result = await sender.Send(new EndWorkerAssignmentCommand(id), ct);
                return Results.Ok(result);
            })
            .WithTags("Tenant Registrations")
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationWorkerAssignmentDto>(StatusCodes.Status200OK);

        registrations.MapGet("/{id:guid}/vehicle-assignments", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<OccupancyRegistrationVehicleAssignmentDto> result =
                    await sender.Send(new GetVehicleAssignmentsForRegistrationQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.view")
            .Produces<IReadOnlyList<OccupancyRegistrationVehicleAssignmentDto>>(StatusCodes.Status200OK);

        registrations.MapPost("/{id:guid}/vehicle-assignments", async (Guid id, AssignVehicleRequest body, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationVehicleAssignmentDto result =
                    await sender.Send(new AssignVehicleToOccupancyRegistrationCommand(id, body.VehicleId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationVehicleAssignmentDto>(StatusCodes.Status200OK);

        app.MapPost("/api/v1/vehicle-assignments/{id:guid}/end", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationVehicleAssignmentDto result = await sender.Send(new EndVehicleAssignmentCommand(id), ct);
                return Results.Ok(result);
            })
            .WithTags("Tenant Registrations")
            .RequirePermission("occupancy-registration.create")
            .Produces<OccupancyRegistrationVehicleAssignmentDto>(StatusCodes.Status200OK);

        RouteGroupBuilder security = app.MapGroup("/api/v1/occupancy-registrations/security").WithTags("Tenant Registrations");

        security.MapGet("/", async (Guid? flatId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<OccupancyRegistrationSecuritySummaryDto> result = await sender.Send(
                    new GetOccupancySecurityViewsQuery(flatId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.security-view")
            .Produces<PagedResult<OccupancyRegistrationSecuritySummaryDto>>(StatusCodes.Status200OK);

        security.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OccupancyRegistrationSecurityViewDto result =
                    await sender.Send(new GetOccupancyRegistrationSecurityViewQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("occupancy-registration.security-view")
            .Produces<OccupancyRegistrationSecurityViewDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record UpdateOccupancyRegistrationDraftRequest(
    string PrimaryFullName,
    string? PrimaryPhone,
    string? PrimaryEmail,
    string? PrimaryNationalIdNumber,
    DateOnly? PrimaryDateOfBirth,
    string? PrimaryGender,
    string? PrimaryBloodGroup,
    string? PrimaryReligion,
    string? PrimaryNationality,
    string? PrimaryProfession,
    string? PrimaryPermanentAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    DateOnly? MoveInExpectedDate);

public sealed record ReasonRequest(string Reason);

public sealed record SetPrimaryPhotoRequest(Guid? AttachmentId);

public sealed record MoveOutRequest(string? Reason);

public sealed record AddHouseholdMemberRequest(
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
    string? Occupation);

public sealed record UpdateHouseholdMemberRequest(
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
    string? Occupation);

public sealed record AssignWorkerRequest(Guid DomesticWorkerProfileId);

public sealed record AssignVehicleRequest(Guid VehicleId);
