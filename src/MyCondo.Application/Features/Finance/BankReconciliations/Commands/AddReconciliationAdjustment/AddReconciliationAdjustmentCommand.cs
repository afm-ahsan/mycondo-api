using Mediator;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddReconciliationAdjustment;

/// <summary>Posts a brand-new ledger entry for a statement line the ledger didn't yet know about (a
/// bank charge, interest credit) — <paramref name="OtherSideRole"/> names the existing
/// <c>LedgerAccountType</c> for the non-bank side (e.g. "OperatingExpense" for a bank charge), the same
/// finite role vocabulary every other posting call site already uses; no new account types are
/// introduced by this feature.</summary>
public sealed record AddReconciliationAdjustmentCommand(
    Guid BankStatementLineId, string OtherSideRole, string Description) : IRequest<BankStatementLineDto>;
