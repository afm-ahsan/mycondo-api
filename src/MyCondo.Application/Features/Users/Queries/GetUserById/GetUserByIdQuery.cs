using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto>, ILifecycleReadOperation;
