using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.MatchStatementLine;

public sealed record MatchStatementLineCommand(Guid BankStatementLineId, Guid LedgerEntryId) : IRequest<BankStatementLineDto>, IRequiresFeature
{
    public string FeatureKey => "finance.bank_reconciliation";
}
