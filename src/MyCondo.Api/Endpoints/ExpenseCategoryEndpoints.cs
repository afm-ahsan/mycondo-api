using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.ActivateExpenseCategory;
using MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.CreateExpenseCategory;
using MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.DeactivateExpenseCategory;
using MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.UpdateExpenseCategory;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;
using MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetActiveExpenseCategories;
using MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetExpenseCategoriesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ExpenseCategoryEndpoints
{
    public static IEndpointRouteBuilder MapExpenseCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder expenseCategories = app.MapGroup("/api/v1/expense-categories").WithTags("Expenses");

        expenseCategories.MapGet("/", async (string? search, bool? isActive, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ExpenseCategoryDto> result = await sender.Send(
                    new GetExpenseCategoriesForTenantQuery(
                        search, isActive, page is null or < 1 ? 1 : page.Value, pageSize is null or < 1 ? 20 : pageSize.Value),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensecategory.view")
            .Produces<PagedResult<ExpenseCategoryDto>>(StatusCodes.Status200OK);

        expenseCategories.MapGet("/active", async (ISender sender, CancellationToken ct) =>
            {
                List<ExpenseCategoryDto> result = await sender.Send(new GetActiveExpenseCategoriesQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensecategory.view")
            .Produces<List<ExpenseCategoryDto>>(StatusCodes.Status200OK);

        expenseCategories.MapPost("/", async (CreateExpenseCategoryCommand command, ISender sender, CancellationToken ct) =>
            {
                ExpenseCategoryDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensecategory.manage")
            .Produces<ExpenseCategoryDto>(StatusCodes.Status200OK);

        expenseCategories.MapPut("/{id:guid}", async (Guid id, UpdateExpenseCategoryRequest body, ISender sender, CancellationToken ct) =>
            {
                ExpenseCategoryDto result = await sender.Send(
                    new UpdateExpenseCategoryCommand(id, body.Name, body.Code, body.Description, body.DisplayOrder), ct);
                return Results.Ok(result);
            })
            .RequirePermission("expensecategory.manage")
            .Produces<ExpenseCategoryDto>(StatusCodes.Status200OK);

        expenseCategories.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateExpenseCategoryCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("expensecategory.manage")
            .Produces(StatusCodes.Status204NoContent);

        expenseCategories.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ActivateExpenseCategoryCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("expensecategory.manage")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record UpdateExpenseCategoryRequest(string Name, string Code, string? Description, int DisplayOrder);
