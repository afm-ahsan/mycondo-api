using FluentValidation;

namespace MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

public sealed class GetTenantBySlugQueryValidator : AbstractValidator<GetTenantBySlugQuery>
{
    public GetTenantBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(63);
    }
}
