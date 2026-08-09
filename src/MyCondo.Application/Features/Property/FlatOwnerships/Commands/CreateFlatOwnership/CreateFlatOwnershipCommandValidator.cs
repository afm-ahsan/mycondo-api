using FluentValidation;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public sealed class CreateFlatOwnershipCommandValidator : AbstractValidator<CreateFlatOwnershipCommand>
{
    public CreateFlatOwnershipCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
    }
}
