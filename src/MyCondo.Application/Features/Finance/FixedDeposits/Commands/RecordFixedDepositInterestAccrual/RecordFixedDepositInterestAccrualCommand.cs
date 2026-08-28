using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestAccrual;

public sealed record RecordFixedDepositInterestAccrualCommand(
    Guid FixedDepositId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly? AccountingDate,
    decimal GrossAmount,
    string? Notes) : IRequest<FixedDepositInterestAccrualDto>, IRequiresFeature
{
    public string FeatureKey => "finance.fixed_deposits";
}
