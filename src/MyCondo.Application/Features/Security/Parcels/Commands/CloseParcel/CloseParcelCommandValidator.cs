using FluentValidation;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CloseParcel;

public sealed class CloseParcelCommandValidator : AbstractValidator<CloseParcelCommand>
{
    private static readonly string[] ValidOutcomes =
        [nameof(ParcelStatus.Returned), nameof(ParcelStatus.Rejected), nameof(ParcelStatus.LostOrEscalated)];

    public CloseParcelCommandValidator()
    {
        RuleFor(x => x.ParcelId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Outcome).Must(o => ValidOutcomes.Contains(o))
            .WithMessage($"Outcome must be one of: {string.Join(", ", ValidOutcomes)}.");
    }
}
