using Mediator;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositById;

public sealed record GetFixedDepositByIdQuery(Guid FixedDepositId) : IRequest<FixedDepositDetailDto>;
