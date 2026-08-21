using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;

public sealed class GetExpenseByTypeReportQueryHandler(
    IExpenseRepository expenses,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetExpenseByTypeReportQuery, ExpenseByTypeReportDto>
{
    public async ValueTask<ExpenseByTypeReportDto> Handle(GetExpenseByTypeReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<ExpenseTypeActivityLine> composition = await expenses.GetExpenseCompositionByTypeAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);

        List<ExpenseByTypeLineDto> lines = composition
            .Select(l => new ExpenseByTypeLineDto(
                l.ExpenseTypeId.Value, l.TypeName, l.ExpenseCategoryId?.Value, l.CategoryName, l.Count, l.Total))
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all expenses)", clock.UtcNow, currentUser.UserId);

        return new ExpenseByTypeReportDto(metadata, lines, lines.Sum(l => l.TotalAmount));
    }
}
