using FluentValidation;

namespace MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;

public sealed class GetLedgerEntriesForAccountQueryValidator : AbstractValidator<GetLedgerEntriesForAccountQuery>
{
    public GetLedgerEntriesForAccountQueryValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
