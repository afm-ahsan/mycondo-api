using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorFuelReceipts;

public sealed class GetGeneratorFuelReceiptsQueryValidator : AbstractValidator<GetGeneratorFuelReceiptsQuery>
{
    public GetGeneratorFuelReceiptsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
