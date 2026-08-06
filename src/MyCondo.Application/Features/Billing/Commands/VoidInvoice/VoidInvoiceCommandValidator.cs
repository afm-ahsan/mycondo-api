using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.VoidInvoice;

public sealed class VoidInvoiceCommandValidator : AbstractValidator<VoidInvoiceCommand>
{
    public VoidInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
