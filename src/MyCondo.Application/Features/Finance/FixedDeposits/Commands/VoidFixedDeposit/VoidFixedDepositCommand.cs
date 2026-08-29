using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;

public sealed record VoidFixedDepositCommand(Guid FixedDepositId, string Reason) : IRequest<FixedDepositDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "finance.fixed_deposits";
}
