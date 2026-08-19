using FluentValidation;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.PlaceFixedDeposit;

public sealed class PlaceFixedDepositCommandValidator : AbstractValidator<PlaceFixedDepositCommand>
{
    public PlaceFixedDepositCommandValidator()
    {
        RuleFor(x => x.CertificateNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BranchName).MaximumLength(200);
        RuleFor(x => x.FundingFinancialAccountId).NotEmpty();
        RuleFor(x => x.Principal).GreaterThan(0);
        RuleFor(x => x.InterestRatePercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CalculationMethod).NotEmpty().Must(m => Enum.TryParse<InterestCalculationMethod>(m, out _))
            .WithMessage("CalculationMethod must be one of: Simple, Compound.");
        RuleFor(x => x.PaymentFrequency).NotEmpty().Must(f => Enum.TryParse<InterestPaymentFrequency>(f, out _))
            .WithMessage("PaymentFrequency must be one of: Monthly, Quarterly, SemiAnnually, Annually, AtMaturity.");
        RuleFor(x => x.MaturityDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.ExpectedGrossInterest).GreaterThanOrEqualTo(0).When(x => x.ExpectedGrossInterest is not null);
        RuleFor(x => x.ExpectedDeductionRatePercent).InclusiveBetween(0, 100).When(x => x.ExpectedDeductionRatePercent is not null);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
