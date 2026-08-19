using FluentValidation;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestReceipt;

public sealed class RecordFixedDepositInterestReceiptCommandValidator : AbstractValidator<RecordFixedDepositInterestReceiptCommand>
{
    public RecordFixedDepositInterestReceiptCommandValidator()
    {
        RuleFor(x => x.FixedDepositId).NotEmpty();
        RuleFor(x => x.GrossAmount).GreaterThan(0);
        RuleFor(x => x.DeductionAmount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(x => x.GrossAmount);
        RuleFor(x => x.ReceivingFinancialAccountId).NotEmpty();
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
