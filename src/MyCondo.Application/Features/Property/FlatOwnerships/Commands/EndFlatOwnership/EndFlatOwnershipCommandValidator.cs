using FluentValidation;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.EndFlatOwnership;

public sealed class EndFlatOwnershipCommandValidator : AbstractValidator<EndFlatOwnershipCommand>
{
    public EndFlatOwnershipCommandValidator()
    {
        RuleFor(x => x.FlatOwnershipId).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
    }
}
