using FluentValidation;
using MyCondo.Domain.Features.Expenses.Expenses;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpensesForTenant;

public sealed class GetExpensesForTenantQueryValidator : AbstractValidator<GetExpensesForTenantQuery>
{
    public GetExpensesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<ExpenseStatus>())}.");
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("ToDate must not be before FromDate.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<ExpenseStatus>(value, out _);
}
