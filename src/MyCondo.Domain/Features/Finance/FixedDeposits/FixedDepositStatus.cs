namespace MyCondo.Domain.Features.Finance.FixedDeposits;

/// <summary>
/// Lifecycle for a Fixed Deposit instrument (Template 4). <see cref="Active"/> covers both
/// "not yet matured" and "matured but not yet actioned" — maturity is a date comparison against
/// <see cref="FixedDeposit.MaturityDate"/>, not a separately stored state, since nothing about the
/// accounting changes until the Association actually decides to renew or withdraw.
/// <see cref="Renewed"/> and <see cref="Withdrawn"/> are the two terminal "actioned" outcomes;
/// <see cref="Voided"/> is a data-entry correction, only reachable while still <see cref="Active"/> with
/// no interest activity posted yet (see <see cref="FixedDeposit.Void"/>) — a placement with interest
/// history is corrected by withdrawing it, not voiding it, so the interest history is never orphaned.
/// </summary>
public enum FixedDepositStatus
{
    Active = 0,
    Renewed = 1,
    Withdrawn = 2,
    Voided = 3,
}
