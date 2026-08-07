using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetSuppliers;

public sealed class GetSuppliersQueryValidator : AbstractValidator<GetSuppliersQuery>
{
    public GetSuppliersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
