using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetFinancialPositionNotes;

/// <summary>Supporting schedules for the Statement of Financial Position as of
/// <see cref="AsOfDate"/> (defaults to today when omitted). <see cref="FundId"/> must match the fund the
/// statement itself was rendered for — a fund-scoped statement with organization-wide notes would be
/// misleading, so the same filter is applied to every schedule. <see cref="Notes"/> narrows the response
/// to specific schedules; omit or leave empty for all of them.</summary>
public sealed record GetFinancialPositionNotesQuery(
    DateOnly? AsOfDate,
    Guid? FundId,
    IReadOnlyList<FinancialStatementNoteKey>? Notes) : IRequest<FinancialStatementNotesDto>, ILifecycleReadOperation;
