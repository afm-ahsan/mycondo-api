using FluentValidation;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.UpdateFinancialAccount;

public sealed class UpdateFinancialAccountCommandValidator : AbstractValidator<UpdateFinancialAccountCommand>
{
    public UpdateFinancialAccountCommandValidator()
    {
        RuleFor(x => x.FinancialAccountId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).MaximumLength(200);
        RuleFor(x => x.BranchName).MaximumLength(200);
        RuleFor(x => x.AccountNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
