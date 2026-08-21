using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.FixedDeposits.Exceptions;

/// <summary>Thrown when a <see cref="FixedDeposit"/> state-transition method (<see cref="FixedDeposit.MarkRenewed"/>,
/// <see cref="FixedDeposit.MarkWithdrawn"/>, <see cref="FixedDeposit.Void"/>) is called from a status it
/// cannot legally transition from — e.g. renewing an already-withdrawn instrument.</summary>
public sealed class FixedDepositInvalidStateTransitionException(FixedDepositId fixedDepositId, FixedDepositStatus status, string attemptedTransition)
    : DomainException($"Fixed Deposit {fixedDepositId} is {status} and cannot {attemptedTransition}.")
{
    public FixedDepositId FixedDepositId { get; } = fixedDepositId;
    public FixedDepositStatus Status { get; } = status;
}
