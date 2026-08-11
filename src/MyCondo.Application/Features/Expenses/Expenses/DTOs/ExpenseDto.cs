namespace MyCondo.Application.Features.Expenses.Expenses.DTOs;

public sealed record ExpenseDto(
    Guid ExpenseId,
    Guid BuildingId,
    string BuildingName,
    Guid ExpenseTypeId,
    string ExpenseTypeName,
    DateOnly ExpenseDate,
    string Description,
    string? Payee,
    string? ReferenceNumber,
    decimal Amount,
    string PaymentMethod,
    string? Notes,
    string Status,
    string? VoidReason,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);
