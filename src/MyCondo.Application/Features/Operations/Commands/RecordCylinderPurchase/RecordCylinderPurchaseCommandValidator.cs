using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.RecordCylinderPurchase;

public sealed class RecordCylinderPurchaseCommandValidator : AbstractValidator<RecordCylinderPurchaseCommand>
{
    public RecordCylinderPurchaseCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CylinderType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.CylinderWeightKg).GreaterThan(0);
        RuleFor(x => x.RatePerCylinder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DeliveryOrOtherCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
