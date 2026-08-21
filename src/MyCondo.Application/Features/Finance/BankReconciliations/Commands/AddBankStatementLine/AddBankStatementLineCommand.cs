using Mediator;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddBankStatementLine;

public sealed record AddBankStatementLineCommand(
    Guid BankReconciliationId, DateOnly TransactionDate, string Description, decimal Amount)
    : IRequest<BankStatementLineDto>;
