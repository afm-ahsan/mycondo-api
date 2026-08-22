using FluentValidation;

namespace MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectory;

public sealed class GetSecurityDirectoryQueryValidator : AbstractValidator<GetSecurityDirectoryQuery>
{
    public GetSecurityDirectoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.AccessStatus).Must(s => s is null or "Authorized" or "Revoked")
            .WithMessage("AccessStatus must be 'Authorized' or 'Revoked'.");
    }
}
