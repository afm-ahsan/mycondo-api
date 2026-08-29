using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositById;

public sealed record GetFixedDepositByIdQuery(Guid FixedDepositId) : IRequest<FixedDepositDetailDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "finance.fixed_deposits";
}
