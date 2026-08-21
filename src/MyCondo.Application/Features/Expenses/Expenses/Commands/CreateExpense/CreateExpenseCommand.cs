using Mediator;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.CreateExpense;

public sealed record CreateExpenseCommand(
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
    string? Notes
) : IRequest<ExpenseDto>;
