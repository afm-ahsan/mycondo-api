using FluentValidation;
using MyCondo.Domain.Features.Operations.GeneratorSessions;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorSessions;

public sealed class GetGeneratorSessionsQueryValidator : AbstractValidator<GetGeneratorSessionsQuery>
{
    public GetGeneratorSessionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<GeneratorSessionStatus>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<GeneratorSessionStatus>(value, out _);
}
