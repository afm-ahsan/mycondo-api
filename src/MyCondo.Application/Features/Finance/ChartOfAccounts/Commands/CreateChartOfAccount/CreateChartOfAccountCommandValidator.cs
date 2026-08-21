using FluentValidation;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.ChartOfAccounts.Commands.CreateChartOfAccount;

public sealed class CreateChartOfAccountCommandValidator : AbstractValidator<CreateChartOfAccountCommand>
{
    public CreateChartOfAccountCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().Must(c => Enum.TryParse<AccountCategory>(c, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<AccountCategory>())}.");
        RuleFor(x => x.NormalBalance).NotEmpty().Must(d => Enum.TryParse<LedgerDirection>(d, out _))
            .WithMessage($"NormalBalance must be one of: {string.Join(", ", Enum.GetNames<LedgerDirection>())}.");
    }
}
