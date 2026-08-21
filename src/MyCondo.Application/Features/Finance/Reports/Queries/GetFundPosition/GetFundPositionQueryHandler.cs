using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;

/// <summary>Fund is an optional <c>LedgerEntry</c> dimension — see <c>Fund.cs</c> — that no posting
/// role attributes to yet, so every configured fund legitimately reports a zero balance until a
/// posting flow starts tagging one. That is correct current-state behavior, not a bug in this
/// report.</summary>
public sealed class GetFundPositionQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFundPositionQuery, FundPositionReportDto>
{
    public async ValueTask<FundPositionReportDto> Handle(GetFundPositionQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        IReadOnlyList<FundActivityLine> activity = await reports.GetFundActivityAsync(tenantId, asOfDate, cancellationToken);

        List<FundPositionLineDto> lines = activity
            .Select(f => new FundPositionLineDto(f.FundId.Value, f.Code, f.Name, f.TotalDebit - f.TotalCredit))
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, "Tenant (all funds)", clock.UtcNow, currentUser.UserId);

        return new FundPositionReportDto(metadata, lines, lines.Sum(l => l.Balance));
    }
}
