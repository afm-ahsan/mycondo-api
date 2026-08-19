using Mediator;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;

public sealed record VoidFixedDepositCommand(Guid FixedDepositId, string Reason) : IRequest<FixedDepositDto>;
