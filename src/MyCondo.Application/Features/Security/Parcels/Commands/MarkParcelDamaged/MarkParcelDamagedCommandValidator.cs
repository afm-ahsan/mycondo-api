using FluentValidation;

namespace MyCondo.Application.Features.Security.Parcels.Commands.MarkParcelDamaged;

public sealed class MarkParcelDamagedCommandValidator : AbstractValidator<MarkParcelDamagedCommand>
{
    public MarkParcelDamagedCommandValidator()
    {
        RuleFor(x => x.ParcelId).NotEmpty();
        RuleFor(x => x.DamageNote).NotEmpty().MaximumLength(500);
    }
}
