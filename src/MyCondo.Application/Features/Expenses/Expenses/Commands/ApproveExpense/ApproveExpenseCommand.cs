using Mediator;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.ApproveExpense;

/// <summary>Approval + primary ledger posting, in one action — see <c>ExpenseStatus</c>'s doc comment
/// for why Template 3 doesn't split these into separate states.</summary>
public sealed record ApproveExpenseCommand(Guid ExpenseId) : IRequest<ExpenseDto>;
