using FluentValidation;

namespace MyCondo.Application.Features.Payments.Queries.GetAccountBalance;

public sealed class GetAccountBalanceQueryValidator : AbstractValidator<GetAccountBalanceQuery>
{
    public GetAccountBalanceQueryValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
    }
}
