using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetExpenseCategoriesForTenant;

public sealed class GetExpenseCategoriesForTenantQueryValidator : AbstractValidator<GetExpenseCategoriesForTenantQuery>
{
    public GetExpenseCategoriesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
