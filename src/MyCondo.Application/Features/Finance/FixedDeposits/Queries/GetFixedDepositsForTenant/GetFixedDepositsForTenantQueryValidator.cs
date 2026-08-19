using FluentValidation;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositsForTenant;

public sealed class GetFixedDepositsForTenantQueryValidator : AbstractValidator<GetFixedDepositsForTenantQuery>
{
    public GetFixedDepositsForTenantQueryValidator()
    {
        RuleFor(x => x.Status).Must(s => s is null || Enum.TryParse<FixedDepositStatus>(s, out _))
            .WithMessage("Status must be one of: Active, Renewed, Withdrawn, Voided.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
