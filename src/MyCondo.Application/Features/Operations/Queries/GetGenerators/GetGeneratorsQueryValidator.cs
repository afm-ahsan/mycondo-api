using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetGenerators;

public sealed class GetGeneratorsQueryValidator : AbstractValidator<GetGeneratorsQuery>
{
    public GetGeneratorsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
