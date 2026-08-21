using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Expenses.Expenses.Commands.ApproveExpense;
using MyCondo.Application.Features.Expenses.Expenses.Commands.CreateExpense;
using MyCondo.Application.Features.Expenses.Expenses.Commands.PayExpense;
using MyCondo.Application.Features.Expenses.Expenses.Commands.UpdateExpense;
using MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpenseById;
using MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpensesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder expenses = app.MapGroup("/api/v1/expenses").WithTags("Expenses");

        expenses.MapGet("/", async (
                Guid? buildingId, Guid? expenseTypeId, Guid? fundId, string? status, DateOnly? fromDate,
                DateOnly? toDate, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ExpenseDto> result = await sender.Send(
                    new GetExpensesForTenantQuery(
                        buildingId, expenseTypeId, fundId, status, fromDate, toDate,
                        page is null or < 1 ? 1 : page.Value, pageSize is null or < 1 ? 20 : pageSize.Value),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("expense.view")
            .Produces<PagedResult<ExpenseDto>>(StatusCodes.Status200OK);

        expenses.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ExpenseDto result = await sender.Send(new GetExpenseByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("expense.view")
            .Produces<ExpenseDto>(StatusCodes.Status200OK);

        expenses.MapPost("/", async (CreateExpenseRequest body, ISender sender, CancellationToken ct) =>
            {
                ExpenseDto result = await sender.Send(
                    new CreateExpenseCommand(
                        body.BuildingId, body.ExpenseTypeId, body.FundId, body.ExpenseDate, body.AccountingDate,
                        body.Description, body.Payee, body.ReferenceNumber, body.Amount, body.IsPaid,
                        body.PaymentMethod, body.Notes),
                    ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<ExpenseDto>(StatusCodes.Status200OK);

        expenses.MapPut("/{id:guid}", async (Guid id, UpdateExpenseRequest body, ISender sender, CancellationToken ct) =>
            {
                ExpenseDto result = await sender.Send(
                    new UpdateExpenseCommand(
                        id, body.BuildingId, body.ExpenseTypeId, body.FundId, body.ExpenseDate, body.AccountingDate,
                        body.Description, body.Payee, body.ReferenceNumber, body.Amount, body.IsPaid,
                        body.PaymentMethod, body.Notes),
                    ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<ExpenseDto>(StatusCodes.Status200OK);

        expenses.MapPost("/{id:guid}/void", async (Guid id, VoidExpenseRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new VoidExpenseCommand(id, body.Reason), ct);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        expenses.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ExpenseDto result = await sender.Send(new ApproveExpenseCommand(id), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<ExpenseDto>(StatusCodes.Status200OK);

        expenses.MapPost("/{id:guid}/pay", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ExpenseDto result = await sender.Send(new PayExpenseCommand(id), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<ExpenseDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CreateExpenseRequest(
    Guid? BuildingId,
    Guid ExpenseTypeId,
    Guid? FundId,
    DateOnly ExpenseDate,
    DateOnly? AccountingDate,
    string Description,
    string? Payee,
    string? ReferenceNumber,
    decimal Amount,
    bool IsPaid,
    string PaymentMethod,
    string? Notes);

public sealed record UpdateExpenseRequest(
    Guid? BuildingId,
    Guid ExpenseTypeId,
    Guid? FundId,
    DateOnly ExpenseDate,
    DateOnly? AccountingDate,
    string Description,
    string? Payee,
    string? ReferenceNumber,
    decimal Amount,
    bool IsPaid,
    string PaymentMethod,
    string? Notes);

public sealed record VoidExpenseRequest(string Reason);
