using Mediator;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.WithdrawFixedDeposit;

public sealed record WithdrawFixedDepositCommand(
    Guid FixedDepositId,
    DateOnly? AccountingDate,
    Guid ReceivingFinancialAccountId) : IRequest<FixedDepositDto>;
