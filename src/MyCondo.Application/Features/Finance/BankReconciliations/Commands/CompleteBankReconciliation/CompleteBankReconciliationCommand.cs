using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.CompleteBankReconciliation;

public sealed record CompleteBankReconciliationCommand(Guid BankReconciliationId) : IRequest<BankReconciliationDto>, IRequiresFeature
{
    public string FeatureKey => "finance.bank_reconciliation";
}
