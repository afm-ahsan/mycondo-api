using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Funds.DTOs;

namespace MyCondo.Application.Features.Finance.Funds.Queries.GetFunds;

public sealed record GetFundsQuery : IRequest<List<FundDto>>, ILifecycleReadOperation;
