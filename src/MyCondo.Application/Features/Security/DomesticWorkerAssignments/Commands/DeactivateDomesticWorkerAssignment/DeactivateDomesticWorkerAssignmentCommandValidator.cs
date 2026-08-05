using FluentValidation;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.DeactivateDomesticWorkerAssignment;

public sealed class DeactivateDomesticWorkerAssignmentCommandValidator : AbstractValidator<DeactivateDomesticWorkerAssignmentCommand>
{
    public DeactivateDomesticWorkerAssignmentCommandValidator()
    {
        RuleFor(x => x.DomesticWorkerAssignmentId).NotEmpty();
    }
}
