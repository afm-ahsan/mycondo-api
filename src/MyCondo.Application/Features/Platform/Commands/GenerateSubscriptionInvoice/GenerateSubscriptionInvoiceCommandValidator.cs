using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;

public sealed class GenerateSubscriptionInvoiceCommandValidator : AbstractValidator<GenerateSubscriptionInvoiceCommand>
{
    public GenerateSubscriptionInvoiceCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.BillingPeriodStart).NotEqual(default(DateOnly));
    }
}
