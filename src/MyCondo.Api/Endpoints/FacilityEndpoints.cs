using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Amenities.Commands.CreateBlackoutDate;
using MyCondo.Application.Features.Amenities.Commands.CreateFacility;
using MyCondo.Application.Features.Amenities.Commands.DeactivateBlackoutDate;
using MyCondo.Application.Features.Amenities.Commands.DeactivateFacility;
using MyCondo.Application.Features.Amenities.Commands.ReactivateFacility;
using MyCondo.Application.Features.Amenities.Commands.UpdateFacilityConfiguration;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetBlackoutDatesForFacility;
using MyCondo.Application.Features.Amenities.Queries.GetFacilities;
using MyCondo.Application.Features.Amenities.Queries.GetFacilityById;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class FacilityEndpoints
{
    public static IEndpointRouteBuilder MapFacilityEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder facilities = app.MapGroup("/api/v1/facilities").WithTags("Facilities");

        facilities.MapPost("/", async (CreateFacilityCommand command, ISender sender, CancellationToken ct) =>
            {
                FacilityDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.manage")
            .Produces<FacilityDto>(StatusCodes.Status200OK);

        facilities.MapGet("/", async (Guid? buildingId, string? facilityType, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<FacilityDto> result = await sender.Send(
                    new GetFacilitiesQuery(buildingId, facilityType, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.view")
            .Produces<PagedResult<FacilityDto>>(StatusCodes.Status200OK);

        facilities.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                FacilityDto result = await sender.Send(new GetFacilityByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.view")
            .Produces<FacilityDto>(StatusCodes.Status200OK);

        facilities.MapPut("/{id:guid}", async (Guid id, UpdateFacilityConfigurationRequest body, ISender sender, CancellationToken ct) =>
            {
                FacilityDto result = await sender.Send(
                    new UpdateFacilityConfigurationCommand(
                        id, body.Name, body.Capacity, body.OperatingHoursStart, body.OperatingHoursEnd, body.RequiresApproval,
                        body.BookingChargeAmount, body.DepositAmount, body.CancellationDeadlineHours,
                        body.CancellationDeductionPercentage, body.GuestFeeAmount, body.MinimumAgeUnaccompanied,
                        body.RequiresSafetyAcknowledgement, body.BlocksEntryIfAccountOverdue),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.manage")
            .Produces<FacilityDto>(StatusCodes.Status200OK);

        facilities.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                FacilityDto result = await sender.Send(new DeactivateFacilityCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.manage")
            .Produces<FacilityDto>(StatusCodes.Status200OK);

        facilities.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                FacilityDto result = await sender.Send(new ReactivateFacilityCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.manage")
            .Produces<FacilityDto>(StatusCodes.Status200OK);

        facilities.MapPost("/{id:guid}/blackout-dates", async (Guid id, CreateBlackoutDateRequest body, ISender sender, CancellationToken ct) =>
            {
                BlackoutDateDto result = await sender.Send(
                    new CreateBlackoutDateCommand(id, body.DateFrom, body.DateTo, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.manage")
            .Produces<BlackoutDateDto>(StatusCodes.Status200OK);

        facilities.MapGet("/{id:guid}/blackout-dates", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<BlackoutDateDto> result = await sender.Send(new GetBlackoutDatesForFacilityQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.view")
            .Produces<IReadOnlyList<BlackoutDateDto>>(StatusCodes.Status200OK);

        facilities.MapPost("/blackout-dates/{blackoutDateId:guid}/deactivate", async (Guid blackoutDateId, ISender sender, CancellationToken ct) =>
            {
                BlackoutDateDto result = await sender.Send(new DeactivateBlackoutDateCommand(blackoutDateId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.manage")
            .Produces<BlackoutDateDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record UpdateFacilityConfigurationRequest(
    string Name,
    int Capacity,
    TimeOnly? OperatingHoursStart,
    TimeOnly? OperatingHoursEnd,
    bool RequiresApproval,
    decimal? BookingChargeAmount,
    decimal? DepositAmount,
    int CancellationDeadlineHours,
    decimal CancellationDeductionPercentage,
    decimal? GuestFeeAmount,
    int? MinimumAgeUnaccompanied,
    bool RequiresSafetyAcknowledgement,
    bool BlocksEntryIfAccountOverdue);

public sealed record CreateBlackoutDateRequest(DateOnly DateFrom, DateOnly DateTo, string Reason);
