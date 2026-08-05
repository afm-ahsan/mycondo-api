using FluentValidation;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.DeactivateServiceProviderAssignment;

public sealed class DeactivateServiceProviderAssignmentCommandValidator : AbstractValidator<DeactivateServiceProviderAssignmentCommand>
{
    public DeactivateServiceProviderAssignmentCommandValidator()
    {
        RuleFor(x => x.ServiceProviderAssignmentId).NotEmpty();
    }
}
