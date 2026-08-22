using FluentValidation;

namespace MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectoryDetail;

public sealed class GetSecurityDirectoryDetailQueryValidator : AbstractValidator<GetSecurityDirectoryDetailQuery>
{
    public GetSecurityDirectoryDetailQueryValidator()
    {
        RuleFor(x => x.ResidentType).Must(t => t is "Owner" or "Tenant")
            .WithMessage("ResidentType must be 'Owner' or 'Tenant'.");
    }
}
