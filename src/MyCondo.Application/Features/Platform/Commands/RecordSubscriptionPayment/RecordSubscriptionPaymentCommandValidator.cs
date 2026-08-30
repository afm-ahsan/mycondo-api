using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.RecordSubscriptionPayment;

public sealed class RecordSubscriptionPaymentCommandValidator : AbstractValidator<RecordSubscriptionPaymentCommand>
{
    public RecordSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SubscriptionInvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PaymentDate).NotEqual(default(DateOnly));
        RuleFor(x => x.ReferenceNumber).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
