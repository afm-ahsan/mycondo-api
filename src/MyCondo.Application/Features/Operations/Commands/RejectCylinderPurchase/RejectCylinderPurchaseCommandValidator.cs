using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.RejectCylinderPurchase;

public sealed class RejectCylinderPurchaseCommandValidator : AbstractValidator<RejectCylinderPurchaseCommand>
{
    public RejectCylinderPurchaseCommandValidator()
    {
        RuleFor(x => x.CylinderPurchaseId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
