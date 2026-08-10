using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationById;

public sealed record GetOrganizationByIdQuery(Guid OrganizationId) : IRequest<OrganizationDetailDto>;
