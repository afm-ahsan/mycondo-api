using FluentValidation;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;

public sealed class RenewFixedDepositCommandValidator : AbstractValidator<RenewFixedDepositCommand>
{
    public RenewFixedDepositCommandValidator()
    {
        RuleFor(x => x.FixedDepositId).NotEmpty();
        RuleFor(x => x.NewCertificateNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NewBranchName).MaximumLength(200);
        RuleFor(x => x.FundingFinancialAccountId).NotEmpty();
        RuleFor(x => x.NewPrincipal).GreaterThan(0);
        RuleFor(x => x.NewInterestRatePercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewCalculationMethod).NotEmpty().Must(m => Enum.TryParse<InterestCalculationMethod>(m, out _))
            .WithMessage("NewCalculationMethod must be one of: Simple, Compound.");
        RuleFor(x => x.NewPaymentFrequency).NotEmpty().Must(f => Enum.TryParse<InterestPaymentFrequency>(f, out _))
            .WithMessage("NewPaymentFrequency must be one of: Monthly, Quarterly, SemiAnnually, Annually, AtMaturity.");
        RuleFor(x => x.NewMaturityDate).GreaterThan(x => x.NewStartDate);
        RuleFor(x => x.NewExpectedGrossInterest).GreaterThanOrEqualTo(0).When(x => x.NewExpectedGrossInterest is not null);
        RuleFor(x => x.NewExpectedDeductionRatePercent).InclusiveBetween(0, 100).When(x => x.NewExpectedDeductionRatePercent is not null);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
