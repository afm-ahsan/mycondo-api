using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.RecordStockAdjustment;

public sealed class RecordStockAdjustmentCommandValidator : AbstractValidator<RecordStockAdjustmentCommand>
{
    public RecordStockAdjustmentCommandValidator()
    {
        RuleFor(x => x.CylinderType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SignedQuantity).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
