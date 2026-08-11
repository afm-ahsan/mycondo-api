using Mediator;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpenseById;

public sealed record GetExpenseByIdQuery(Guid ExpenseId) : IRequest<ExpenseDto>;
