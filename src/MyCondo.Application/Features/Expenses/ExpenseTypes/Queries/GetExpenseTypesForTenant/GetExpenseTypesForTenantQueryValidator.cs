using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetExpenseTypesForTenant;

public sealed class GetExpenseTypesForTenantQueryValidator : AbstractValidator<GetExpenseTypesForTenantQuery>
{
    public GetExpenseTypesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
