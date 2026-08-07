using FluentValidation;
using MyCondo.Domain.Features.Operations.CylinderPurchases;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderPurchases;

public sealed class GetCylinderPurchasesQueryValidator : AbstractValidator<GetCylinderPurchasesQuery>
{
    public GetCylinderPurchasesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.ApprovalStatus).Must(BeAValidStatus!).When(x => x.ApprovalStatus is not null)
            .WithMessage($"ApprovalStatus must be one of: {string.Join(", ", Enum.GetNames<CylinderPurchaseApprovalStatus>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<CylinderPurchaseApprovalStatus>(value, out _);
}
