using Mediator;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.CreateExpense;

public sealed record CreateExpenseCommand(
    Guid BuildingId,
    Guid ExpenseTypeId,
    DateOnly ExpenseDate,
    string Description,
    string? Payee,
    string? ReferenceNumber,
    decimal Amount,
    string PaymentMethod,
    string? Notes
) : IRequest<ExpenseDto>;
