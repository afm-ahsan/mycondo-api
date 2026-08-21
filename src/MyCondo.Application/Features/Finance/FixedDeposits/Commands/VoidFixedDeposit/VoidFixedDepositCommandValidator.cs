using FluentValidation;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;

public sealed class VoidFixedDepositCommandValidator : AbstractValidator<VoidFixedDepositCommand>
{
    public VoidFixedDepositCommandValidator()
    {
        RuleFor(x => x.FixedDepositId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
