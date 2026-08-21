using Mediator;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.ExcludeStatementLine;

public sealed record ExcludeStatementLineCommand(Guid BankStatementLineId, string Reason) : IRequest<BankStatementLineDto>;
