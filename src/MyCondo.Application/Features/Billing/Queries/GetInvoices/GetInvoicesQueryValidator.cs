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
        RuleFor(x => x.Source).Must(BeAValidSource!).When(x => x.Source is not null)
            .WithMessage($"Source must be one of: {string.Join(", ", Enum.GetNames<InvoiceSource>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<InvoiceStatus>(value, out _);

    private static bool BeAValidSource(string value) => Enum.TryParse<InvoiceSource>(value, out _);
}
