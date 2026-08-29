using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetActiveExpenseTypes;

public sealed record GetActiveExpenseTypesQuery : IRequest<List<ExpenseTypeDto>>, ILifecycleReadOperation;
