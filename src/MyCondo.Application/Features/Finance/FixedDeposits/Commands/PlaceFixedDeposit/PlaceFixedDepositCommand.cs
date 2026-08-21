using Mediator;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.PlaceFixedDeposit;

public sealed record PlaceFixedDepositCommand(
    string CertificateNumber,
    string BankName,
    string? BranchName,
    Guid FundingFinancialAccountId,
    Guid? FundId,
    decimal Principal,
    decimal InterestRatePercent,
    string CalculationMethod,
    string PaymentFrequency,
    DateOnly StartDate,
    DateOnly MaturityDate,
    decimal? ExpectedGrossInterest,
    decimal? ExpectedDeductionRatePercent,
    string? Notes) : IRequest<FixedDepositDto>;
