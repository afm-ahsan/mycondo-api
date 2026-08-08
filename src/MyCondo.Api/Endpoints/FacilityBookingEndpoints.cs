using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Amenities.Commands.ApproveBooking;
using MyCondo.Application.Features.Amenities.Commands.CancelBooking;
using MyCondo.Application.Features.Amenities.Commands.CheckInBooking;
using MyCondo.Application.Features.Amenities.Commands.CompleteBooking;
using MyCondo.Application.Features.Amenities.Commands.ConfirmBookingPayment;
using MyCondo.Application.Features.Amenities.Commands.InspectBooking;
using MyCondo.Application.Features.Amenities.Commands.MarkBookingNoShow;
using MyCondo.Application.Features.Amenities.Commands.RejectBooking;
using MyCondo.Application.Features.Amenities.Commands.RequestBooking;
using MyCondo.Application.Features.Amenities.Commands.SubmitBooking;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetBookingById;
using MyCondo.Application.Features.Amenities.Queries.GetBookings;
using MyCondo.Application.Features.Amenities.Queries.GetUpcomingBookings;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class FacilityBookingEndpoints
{
    public static IEndpointRouteBuilder MapFacilityBookingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder bookings = app.MapGroup("/api/v1/facility-bookings").WithTags("Facility Bookings");

        bookings.MapPost("/", async (RequestBookingCommand command, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.create")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapGet("/", async (
                Guid? facilityId, Guid? flatId, string? status, Guid? buildingId, string? eventType,
                string? paymentStatus, DateTimeOffset? fromDate, DateTimeOffset? toDate, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<BookingDto> result = await sender.Send(
                    new GetBookingsQuery(
                        facilityId, flatId, status, buildingId, eventType, paymentStatus, fromDate, toDate,
                        page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.view")
            .Produces<PagedResult<BookingDto>>(StatusCodes.Status200OK);

        bookings.MapGet("/upcoming", async (ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<BookingDto> result = await sender.Send(new GetUpcomingBookingsQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.view")
            .Produces<IReadOnlyList<BookingDto>>(StatusCodes.Status200OK);

        bookings.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new GetBookingByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.view")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/submit", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new SubmitBookingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.create")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new ApproveBookingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.approve")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/reject", async (Guid id, RejectBookingRequest body, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new RejectBookingCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.approve")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/confirm-payment", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new ConfirmBookingPaymentCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.approve")
            .RequireIdempotencyKey()
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/check-in", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new CheckInBookingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.inspect")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new CompleteBookingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.inspect")
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/inspect", async (Guid id, InspectBookingRequest body, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(
                    new InspectBookingCommand(id, body.Notes, body.DamageDeductionAmount, body.DamageDeductionReason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.refund")
            .RequireIdempotencyKey()
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/cancel", async (Guid id, CancelBookingRequest body, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new CancelBookingCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.cancel")
            .RequireIdempotencyKey()
            .Produces<BookingDto>(StatusCodes.Status200OK);

        bookings.MapPost("/{id:guid}/mark-no-show", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BookingDto result = await sender.Send(new MarkBookingNoShowCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("facility.booking.cancel")
            .RequireIdempotencyKey()
            .Produces<BookingDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record RejectBookingRequest(string Reason);

public sealed record InspectBookingRequest(string? Notes, decimal? DamageDeductionAmount, string? DamageDeductionReason);

public sealed record CancelBookingRequest(string Reason);
