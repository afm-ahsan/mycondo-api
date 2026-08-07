using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.CheckOutPoolSession;

public sealed class CheckOutPoolSessionCommandValidator : AbstractValidator<CheckOutPoolSessionCommand>
{
    public CheckOutPoolSessionCommandValidator()
    {
        RuleFor(x => x.PoolSessionId).NotEmpty();
    }
}
