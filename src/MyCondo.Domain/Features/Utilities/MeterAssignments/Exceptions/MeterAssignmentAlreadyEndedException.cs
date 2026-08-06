using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Utilities.MeterAssignments.Exceptions;

public sealed class MeterAssignmentAlreadyEndedException(MeterAssignmentId assignmentId)
    : DomainException($"Meter assignment {assignmentId} already has an AssignedToUtc date set.")
{
    public MeterAssignmentId AssignmentId { get; } = assignmentId;
}
