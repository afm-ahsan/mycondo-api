using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Queries.GetBankReconciliations;

public sealed record GetBankReconciliationsQuery(Guid FinancialAccountId) : IRequest<List<BankReconciliationDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "finance.bank_reconciliation";
}
