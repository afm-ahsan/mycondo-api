using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;

/// <summary>Nets each account's raw debit/credit activity down to a single signed balance shown in
/// the Debit or Credit column — the classic Trial Balance presentation. Because
/// <c>LedgerPosting.Create</c> rejects any unbalanced posting, sum(Debit column) == sum(Credit
/// column) here is a structural guarantee, not something this handler has to enforce.</summary>
public sealed class GetTrialBalanceQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetTrialBalanceQuery, TrialBalanceReportDto>
{
    public async ValueTask<TrialBalanceReportDto> Handle(GetTrialBalanceQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        IReadOnlyList<TrialBalanceAccountLine> lines = await reports.GetTrialBalanceAsync(tenantId, asOfDate, cancellationToken);

        List<TrialBalanceLineDto> result = [];
        decimal totalDebit = 0m;
        decimal totalCredit = 0m;

        foreach (TrialBalanceAccountLine line in lines)
        {
            decimal net = line.TotalDebit - line.TotalCredit;
            if (net == 0m)
            {
                continue;
            }

            decimal debit = net > 0m ? net : 0m;
            decimal credit = net < 0m ? -net : 0m;
            totalDebit += debit;
            totalCredit += credit;

            result.Add(new TrialBalanceLineDto(
                line.ChartOfAccountId.Value, line.Code, line.Name, line.Category.ToString(),
                line.NormalBalance.ToString(), debit, credit));
        }

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, "Tenant (all accounts)", clock.UtcNow, currentUser.UserId);

        return new TrialBalanceReportDto(metadata, result, totalDebit, totalCredit);
    }
}
