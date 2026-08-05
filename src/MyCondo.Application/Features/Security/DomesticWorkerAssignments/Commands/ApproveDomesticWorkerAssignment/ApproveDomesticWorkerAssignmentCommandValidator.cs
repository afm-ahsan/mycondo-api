using FluentValidation;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.ApproveDomesticWorkerAssignment;

public sealed class ApproveDomesticWorkerAssignmentCommandValidator : AbstractValidator<ApproveDomesticWorkerAssignmentCommand>
{
    public ApproveDomesticWorkerAssignmentCommandValidator()
    {
        RuleFor(x => x.DomesticWorkerAssignmentId).NotEmpty();
    }
}
