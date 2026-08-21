using Mediator;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.StartBankReconciliation;

public sealed record StartBankReconciliationCommand(
    Guid FinancialAccountId, DateOnly StatementDate, decimal StatementBalance) : IRequest<BankReconciliationDto>;
