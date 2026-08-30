using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;

/// <summary>
/// Platform Control Plane billing/collections summary (ADR-034 Task 14L), grouped by currency.
/// <paramref name="DateFrom"/>/<paramref name="DateTo"/> scope only the period-flow figures
/// (<see cref="PlatformBillingSummaryCurrencyDto.InvoicedAmount"/> against invoice <c>IssueDate</c>,
/// <see cref="PlatformBillingSummaryCurrencyDto.CollectedAmount"/> against payment <c>PaymentDate</c>);
/// outstanding/overdue figures are always the current persisted snapshot, independent of this range.
/// <paramref name="OrganizationId"/> narrows every figure to one organization; omitted, the summary
/// covers the whole platform.
/// </summary>
public sealed record GetPlatformBillingSummaryQuery(
    Guid? OrganizationId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null
) : IRequest<IReadOnlyList<PlatformBillingSummaryCurrencyDto>>;
