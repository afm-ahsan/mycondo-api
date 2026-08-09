using FluentValidation;

namespace MyCondo.Application.Features.Residents.Commands.LinkResidentToUser;

public sealed class LinkResidentToUserCommandValidator : AbstractValidator<LinkResidentToUserCommand>
{
    public LinkResidentToUserCommandValidator()
    {
        RuleFor(x => x.ResidentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
