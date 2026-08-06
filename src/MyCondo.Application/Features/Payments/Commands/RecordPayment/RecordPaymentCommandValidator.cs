using FluentValidation;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Application.Features.Payments.Commands.RecordPayment;

public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.BusinessDate).NotEmpty();
        RuleFor(x => x.ReferenceNumber).MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.PaymentMethod).Must(BeAValidPaymentMethod)
            .WithMessage($"PaymentMethod must be one of: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.");
    }

    private static bool BeAValidPaymentMethod(string value) => Enum.TryParse<PaymentMethod>(value, out _);
}
