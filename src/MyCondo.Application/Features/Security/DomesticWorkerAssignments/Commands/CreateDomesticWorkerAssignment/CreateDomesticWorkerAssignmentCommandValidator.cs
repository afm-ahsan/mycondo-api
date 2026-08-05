using FluentValidation;
using MyCondo.Domain.Features.Security.Common;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.CreateDomesticWorkerAssignment;

public sealed class CreateDomesticWorkerAssignmentCommandValidator : AbstractValidator<CreateDomesticWorkerAssignmentCommand>
{
    public CreateDomesticWorkerAssignmentCommandValidator()
    {
        RuleFor(x => x.DomesticWorkerProfileId).NotEmpty();
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.ValidToUtc).GreaterThanOrEqualTo(x => x.ValidFromUtc).When(x => x.ValidToUtc is not null);
        RuleFor(x => x.AllowedDays).Must(BeValidDaysFlags!).When(x => !string.IsNullOrWhiteSpace(x.AllowedDays))
            .WithMessage($"AllowedDays must be a comma-separated combination of: {string.Join(", ", Enum.GetNames<DaysOfWeekFlags>().Where(n => n is not ("None" or "All")))}.");
    }

    private static bool BeValidDaysFlags(string value) => Enum.TryParse<DaysOfWeekFlags>(value, out _);
}
