using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.GenerateInvoiceBatch;

public sealed class GenerateInvoiceBatchCommandValidator : AbstractValidator<GenerateInvoiceBatchCommand>
{
    public GenerateInvoiceBatchCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.PeriodStart).NotEmpty();
        RuleFor(x => x.PeriodEnd).NotEmpty();
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("PeriodEnd must not precede PeriodStart.");
    }
}
