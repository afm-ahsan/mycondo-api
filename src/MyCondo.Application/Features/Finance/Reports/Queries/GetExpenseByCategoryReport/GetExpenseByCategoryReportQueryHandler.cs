using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

public sealed class GetExpenseByCategoryReportQueryHandler(
    IExpenseRepository expenses,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetExpenseByCategoryReportQuery, ExpenseByCategoryReportDto>
{
    public async ValueTask<ExpenseByCategoryReportDto> Handle(
        GetExpenseByCategoryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<ExpenseCategoryActivityLine> composition = await expenses.GetExpenseCompositionByCategoryAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);

        List<ExpenseByCategoryLineDto> lines = composition
            .Select(l => new ExpenseByCategoryLineDto(l.ExpenseCategoryId?.Value, l.CategoryName, l.Total))
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all expenses)", clock.UtcNow, currentUser.UserId);

        return new ExpenseByCategoryReportDto(metadata, lines, lines.Sum(l => l.TotalAmount));
    }
}
