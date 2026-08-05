using FluentValidation;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CollectParcel;

public sealed class CollectParcelCommandValidator : AbstractValidator<CollectParcelCommand>
{
    public CollectParcelCommandValidator()
    {
        RuleFor(x => x.ParcelId).NotEmpty();
        RuleFor(x => x.CollectorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Acknowledgement).MaximumLength(200);
    }
}
