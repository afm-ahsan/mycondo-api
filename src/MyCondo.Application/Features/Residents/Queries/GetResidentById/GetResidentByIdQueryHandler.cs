using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Residents.Mappings;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Queries.GetResidentById;

public sealed class GetResidentByIdQueryHandler(
    IResidentRepository residents,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetResidentByIdQuery, ResidentDto>
{
    public async ValueTask<ResidentDto> Handle(GetResidentByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId id = new(query.ResidentId);
        Resident resident = await residents.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), query.ResidentId);

        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), query.ResidentId);
        }

        return resident.ToDto();
    }
}
