using FluentValidation;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.CreateFinancialAccount;

public sealed class CreateFinancialAccountCommandValidator : AbstractValidator<CreateFinancialAccountCommand>
{
    public CreateFinancialAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccountType).NotEmpty().Must(t => Enum.TryParse<FinancialAccountType>(t, out _))
            .WithMessage("AccountType must be one of: " + nameof(FinancialAccountType.Cash) + ", " +
                nameof(FinancialAccountType.Bank) + ", " + nameof(FinancialAccountType.MobileFinancialService) + ".");
        RuleFor(x => x.BankName).MaximumLength(200);
        RuleFor(x => x.BranchName).MaximumLength(200);
        RuleFor(x => x.AccountNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
