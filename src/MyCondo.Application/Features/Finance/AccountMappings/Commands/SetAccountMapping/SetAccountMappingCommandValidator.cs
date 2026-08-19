using FluentValidation;

namespace MyCondo.Application.Features.Finance.AccountMappings.Commands.SetAccountMapping;

public sealed class SetAccountMappingCommandValidator : AbstractValidator<SetAccountMappingCommand>
{
    public SetAccountMappingCommandValidator()
    {
        RuleFor(x => x.PostingRole).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ChartOfAccountId).NotEmpty();
    }
}
