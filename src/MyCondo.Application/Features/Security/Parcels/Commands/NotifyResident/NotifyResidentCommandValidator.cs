using FluentValidation;

namespace MyCondo.Application.Features.Security.Parcels.Commands.NotifyResident;

public sealed class NotifyResidentCommandValidator : AbstractValidator<NotifyResidentCommand>
{
    public NotifyResidentCommandValidator()
    {
        RuleFor(x => x.ParcelId).NotEmpty();
    }
}
