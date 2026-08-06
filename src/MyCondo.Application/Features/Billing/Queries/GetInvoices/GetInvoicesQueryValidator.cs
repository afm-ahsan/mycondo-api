using FluentValidation;
using MyCondo.Domain.Features.Billing.Invoices;

namespace MyCondo.Application.Features.Billing.Queries.GetInvoices;

public sealed class GetInvoicesQueryValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<InvoiceStatus>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<InvoiceStatus>(value, out _);
}
