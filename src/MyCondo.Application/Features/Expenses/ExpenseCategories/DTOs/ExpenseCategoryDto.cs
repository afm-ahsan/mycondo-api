namespace MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;

public sealed record ExpenseCategoryDto(
    Guid ExpenseCategoryId,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int DisplayOrder
);
