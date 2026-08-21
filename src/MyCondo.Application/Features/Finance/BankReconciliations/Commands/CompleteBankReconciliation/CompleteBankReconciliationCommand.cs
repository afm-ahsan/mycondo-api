using Mediator;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.CompleteBankReconciliation;

public sealed record CompleteBankReconciliationCommand(Guid BankReconciliationId) : IRequest<BankReconciliationDto>;
