namespace MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;

public sealed record ExpenseTypeDto(
    Guid ExpenseTypeId,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int DisplayOrder
);
