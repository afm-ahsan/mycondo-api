using FluentValidation;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Application.Features.Payments.Queries.GetPayments;

public sealed class GetPaymentsQueryValidator : AbstractValidator<GetPaymentsQuery>
{
    public GetPaymentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<PaymentStatus>())}.");
        RuleFor(x => x.PaymentMethod).Must(BeAValidPaymentMethod!).When(x => x.PaymentMethod is not null)
            .WithMessage($"PaymentMethod must be one of: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<PaymentStatus>(value, out _);

    private static bool BeAValidPaymentMethod(string value) => Enum.TryParse<PaymentMethod>(value, out _);
}
