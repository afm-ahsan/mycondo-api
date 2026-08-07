using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Operations.CylinderPurchases.Exceptions;

public sealed class CylinderPurchaseInvalidTransitionException(
    CylinderPurchaseId id, CylinderPurchaseApprovalStatus currentStatus, string attemptedAction)
    : DomainException($"Cylinder purchase {id} cannot {attemptedAction} while ApprovalStatus is {currentStatus}.")
{
    public CylinderPurchaseId CylinderPurchaseId { get; } = id;
}
