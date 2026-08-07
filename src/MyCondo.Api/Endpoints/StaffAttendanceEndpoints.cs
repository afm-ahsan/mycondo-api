using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ApproveAttendanceCorrection;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockIn;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockOut;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForStaffMember;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;
using MyCondo.Application.Features.Payroll.StaffMembers.Commands.RegisterStaffMember;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;
using MyCondo.Application.Features.Payroll.StaffMembers.Queries.GetStaffMembersForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class StaffAttendanceEndpoints
{
    public static IEndpointRouteBuilder MapStaffAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder staff = app.MapGroup("/api/v1/staff-members").WithTags("StaffAttendance");

        staff.MapPost("/", async (RegisterStaffMemberCommand command, ISender sender, CancellationToken ct) =>
            {
                StaffMemberDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("staffattendance.manage")
            .Produces<StaffMemberDto>(StatusCodes.Status200OK);

        staff.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<StaffMemberDto> result = await sender.Send(
                    new GetStaffMembersForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("staffattendance.view")
            .Produces<PagedResult<StaffMemberDto>>(StatusCodes.Status200OK);

        staff.MapPost("/{id:guid}/clock-in", async (Guid id, ClockInRequest body, ISender sender, CancellationToken ct) =>
            {
                AttendanceRecordDto result = await sender.Send(
                    new ClockInCommand(id, body.WorkDate, body.ScheduledStartUtc, body.ScheduledEndUtc,
                        body.WorkLocation, body.Source), ct);
                return Results.Ok(result);
            })
            .RequirePermission("staffattendance.manage")
            .Produces<AttendanceRecordDto>(StatusCodes.Status200OK);

        staff.MapGet("/{id:guid}/attendance", async (Guid id, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<AttendanceRecordDto> result = await sender.Send(
                    new GetAttendanceRecordsForStaffMemberQuery(id, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("staffattendance.view")
            .Produces<PagedResult<AttendanceRecordDto>>(StatusCodes.Status200OK);

        RouteGroupBuilder attendance = app.MapGroup("/api/v1/attendance-records").WithTags("StaffAttendance");

        attendance.MapGet("/", async (DateOnly? workDate, Guid? staffMemberId, bool? onlyOpen, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<AttendanceRegisterEntryDto> result = await sender.Send(
                    new GetAttendanceRecordsForTenantQuery(workDate, staffMemberId, onlyOpen, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("staffattendance.view")
            .Produces<PagedResult<AttendanceRegisterEntryDto>>(StatusCodes.Status200OK);

        attendance.MapPost("/{id:guid}/clock-out", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                AttendanceRecordDto result = await sender.Send(new ClockOutCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("staffattendance.manage")
            .Produces<AttendanceRecordDto>(StatusCodes.Status200OK);

        attendance.MapPost("/{id:guid}/request-correction", async (Guid id, RequestCorrectionRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RequestAttendanceCorrectionCommand(id, body.Reason), ct);
                return Results.NoContent();
            })
            .RequirePermission("staffattendance.correct")
            .Produces(StatusCodes.Status204NoContent);

        attendance.MapPost("/{id:guid}/approve-correction", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ApproveAttendanceCorrectionCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("staffattendance.approve")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record ClockInRequest(
    DateOnly WorkDate,
    DateTimeOffset? ScheduledStartUtc,
    DateTimeOffset? ScheduledEndUtc,
    string? WorkLocation,
    string Source);

public sealed record RequestCorrectionRequest(string Reason);
