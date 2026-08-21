using Mediator;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Queries.GetBankReconciliation;

public sealed record GetBankReconciliationQuery(Guid BankReconciliationId) : IRequest<BankReconciliationDetailDto>;
