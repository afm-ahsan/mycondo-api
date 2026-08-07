using FluentValidation;
using MyCondo.Domain.Features.Amenities.PoolIncidents;

namespace MyCondo.Application.Features.Amenities.Commands.ReportPoolIncident;

public sealed class ReportPoolIncidentCommandValidator : AbstractValidator<ReportPoolIncidentCommand>
{
    public ReportPoolIncidentCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.OccurredAtUtc).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Severity).Must(BeAValidSeverity)
            .WithMessage($"Severity must be one of: {string.Join(", ", Enum.GetNames<PoolIncidentSeverity>())}.");
        RuleFor(x => x.ActionTaken).MaximumLength(1000);
    }

    private static bool BeAValidSeverity(string value) => Enum.TryParse<PoolIncidentSeverity>(value, out _);
}
