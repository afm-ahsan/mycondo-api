using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryValidator : AbstractValidator<GetStockMovementsQuery>
{
    public GetStockMovementsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
