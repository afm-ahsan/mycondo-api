using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.ActivateExpenseType;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.CreateExpenseType;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.DeactivateExpenseType;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.UpdateExpenseType;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetActiveExpenseTypes;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetExpenseTypesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ExpenseTypeEndpoints
{
    public static IEndpointRouteBuilder MapExpenseTypeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder expenseTypes = app.MapGroup("/api/v1/expense-types").WithTags("Expenses");

        expenseTypes.MapGet("/", async (string? search, bool? isActive, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ExpenseTypeDto> result = await sender.Send(
                    new GetExpenseTypesForTenantQuery(
                        search, isActive, page is null or < 1 ? 1 : page.Value, pageSize is null or < 1 ? 20 : pageSize.Value),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensetype.view")
            .Produces<PagedResult<ExpenseTypeDto>>(StatusCodes.Status200OK);

        expenseTypes.MapGet("/active", async (ISender sender, CancellationToken ct) =>
            {
                List<ExpenseTypeDto> result = await sender.Send(new GetActiveExpenseTypesQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensetype.view")
            .Produces<List<ExpenseTypeDto>>(StatusCodes.Status200OK);

        expenseTypes.MapPost("/", async (CreateExpenseTypeRequest body, ISender sender, CancellationToken ct) =>
            {
                ExpenseTypeDto result = await sender.Send(
                    new CreateExpenseTypeCommand(body.ExpenseCategoryId, body.Name, body.Code, body.Description, body.DisplayOrder), ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensetype.manage")
            .Produces<ExpenseTypeDto>(StatusCodes.Status200OK);

        expenseTypes.MapPut("/{id:guid}", async (Guid id, UpdateExpenseTypeRequest body, ISender sender, CancellationToken ct) =>
            {
                ExpenseTypeDto result = await sender.Send(
                    new UpdateExpenseTypeCommand(id, body.ExpenseCategoryId, body.Name, body.Code, body.Description, body.DisplayOrder), ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensetype.manage")
            .Produces<ExpenseTypeDto>(StatusCodes.Status200OK);

        expenseTypes.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateExpenseTypeCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("expensetype.manage")
            .Produces(StatusCodes.Status204NoContent);

        expenseTypes.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ActivateExpenseTypeCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("expensetype.manage")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record CreateExpenseTypeRequest(Guid ExpenseCategoryId, string Name, string Code, string? Description, int DisplayOrder);

public sealed record UpdateExpenseTypeRequest(Guid ExpenseCategoryId, string Name, string Code, string? Description, int DisplayOrder);
