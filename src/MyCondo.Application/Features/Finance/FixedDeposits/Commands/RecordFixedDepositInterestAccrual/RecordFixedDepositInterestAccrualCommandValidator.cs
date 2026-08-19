using FluentValidation;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestAccrual;

public sealed class RecordFixedDepositInterestAccrualCommandValidator : AbstractValidator<RecordFixedDepositInterestAccrualCommand>
{
    public RecordFixedDepositInterestAccrualCommandValidator()
    {
        RuleFor(x => x.FixedDepositId).NotEmpty();
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart);
        RuleFor(x => x.GrossAmount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
