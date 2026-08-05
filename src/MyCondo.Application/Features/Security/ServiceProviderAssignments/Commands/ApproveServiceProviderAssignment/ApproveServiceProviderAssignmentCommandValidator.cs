using FluentValidation;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.ApproveServiceProviderAssignment;

public sealed class ApproveServiceProviderAssignmentCommandValidator : AbstractValidator<ApproveServiceProviderAssignmentCommand>
{
    public ApproveServiceProviderAssignmentCommandValidator()
    {
        RuleFor(x => x.ServiceProviderAssignmentId).NotEmpty();
    }
}
