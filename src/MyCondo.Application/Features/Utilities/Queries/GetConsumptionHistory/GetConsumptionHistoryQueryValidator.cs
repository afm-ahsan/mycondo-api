using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Queries.GetConsumptionHistory;

public sealed class GetConsumptionHistoryQueryValidator : AbstractValidator<GetConsumptionHistoryQuery>
{
    public GetConsumptionHistoryQueryValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not precede FromDate.");
    }
}
