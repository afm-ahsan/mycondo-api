using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;

/// <summary>Supporting schedules for the Income &amp; Expenditure Statement over
/// [<see cref="StartDate"/>, <see cref="EndDate"/>] inclusive — the same explicit period the statement
/// itself requires, so a schedule can never be rendered against a different window than the figure it
/// explains. <see cref="Notes"/> narrows the response to specific schedules; omit or leave empty for all
/// of them.</summary>
public sealed record GetIncomeExpenditureNotesQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? FundId,
    IReadOnlyList<FinancialStatementNoteKey>? Notes) : IRequest<FinancialStatementNotesDto>, ILifecycleReadOperation;
