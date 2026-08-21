using Mediator;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;

/// <summary>Renews a Fixed Deposit, creating its successor. <paramref name="NewPrincipal"/> may differ
/// from the predecessor's own principal — greater means accrued interest is being capitalized into the
/// new instrument, lesser means a partial withdrawal was taken at renewal; either difference is posted
/// (see the handler). <paramref name="FundingFinancialAccountId"/> is where any such capitalization
/// draws from or partial-withdrawal proceeds return to — typically the predecessor's own funding
/// account, but not required to be.</summary>
public sealed record RenewFixedDepositCommand(
    Guid FixedDepositId,
    string NewCertificateNumber,
    string? NewBranchName,
    Guid FundingFinancialAccountId,
    decimal NewPrincipal,
    decimal NewInterestRatePercent,
    string NewCalculationMethod,
    string NewPaymentFrequency,
    DateOnly NewStartDate,
    DateOnly NewMaturityDate,
    decimal? NewExpectedGrossInterest,
    decimal? NewExpectedDeductionRatePercent,
    string? Notes) : IRequest<FixedDepositDto>;
