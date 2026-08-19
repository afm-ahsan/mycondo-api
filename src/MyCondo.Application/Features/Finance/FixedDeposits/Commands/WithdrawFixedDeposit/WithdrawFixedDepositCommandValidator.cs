using FluentValidation;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.WithdrawFixedDeposit;

public sealed class WithdrawFixedDepositCommandValidator : AbstractValidator<WithdrawFixedDepositCommand>
{
    public WithdrawFixedDepositCommandValidator()
    {
        RuleFor(x => x.FixedDepositId).NotEmpty();
        RuleFor(x => x.ReceivingFinancialAccountId).NotEmpty();
    }
}
