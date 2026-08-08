using FluentValidation;

namespace MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;

public sealed class GetLedgerEntriesForAccountQueryValidator : AbstractValidator<GetLedgerEntriesForAccountQuery>
{
    public GetLedgerEntriesForAccountQueryValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
