using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.RecordFuelReceipt;

public sealed class RecordFuelReceiptCommandValidator : AbstractValidator<RecordFuelReceiptCommand>
{
    public RecordFuelReceiptCommandValidator()
    {
        RuleFor(x => x.GeneratorId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost is not null);
        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
