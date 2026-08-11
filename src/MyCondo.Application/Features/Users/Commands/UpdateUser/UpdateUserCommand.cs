using Mediator;

namespace MyCondo.Application.Features.Users.Commands.UpdateUser;

/// <summary>Email is deliberately not editable here — see <c>User.UpdateProfile</c>.</summary>
public sealed record UpdateUserCommand(Guid UserId, string FullName, string? PhoneNumber) : IRequest;
