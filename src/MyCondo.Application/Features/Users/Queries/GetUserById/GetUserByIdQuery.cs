using Mediator;

namespace MyCondo.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto>;
