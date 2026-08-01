using Mediator;

namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed record GetUsersForTenantQuery : IRequest<List<UserSummaryDto>>;
