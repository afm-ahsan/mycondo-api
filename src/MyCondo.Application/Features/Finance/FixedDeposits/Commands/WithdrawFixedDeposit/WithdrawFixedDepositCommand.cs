using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.WithdrawFixedDeposit;

public sealed record WithdrawFixedDepositCommand(
    Guid FixedDepositId,
    DateOnly? AccountingDate,
    Guid ReceivingFinancialAccountId) : IRequest<FixedDepositDto>, IRequiresFeature
{
    public string FeatureKey => "finance.fixed_deposits";
}
