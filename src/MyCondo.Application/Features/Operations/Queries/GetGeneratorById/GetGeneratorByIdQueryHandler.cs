using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorById;

public sealed class GetGeneratorByIdQueryHandler(
    IGeneratorRepository generators,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorByIdQuery, GeneratorDto>
{
    public async ValueTask<GeneratorDto> Handle(GetGeneratorByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        Generator generator = await generators.GetByIdAsync(new GeneratorId(query.GeneratorId), cancellationToken)
            ?? throw new NotFoundException(nameof(Generator), query.GeneratorId);
        if (generator.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Generator), query.GeneratorId);
        }

        return generator.ToDto();
    }
}
