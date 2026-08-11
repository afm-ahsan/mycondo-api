using Mediator;

namespace MyCondo.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Creates a user within the caller's own tenant. <see cref="InitialPassword"/> is optional — when
/// omitted, a random temporary password is generated and returned once in <see cref="CreateUserResult"/>
/// for the administrator to relay to the new user out-of-band. TenantId is deliberately not a field
/// here — it is always resolved from <c>ICurrentUserProvider</c>, never accepted from the client.
/// </summary>
public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string? PhoneNumber,
    string? InitialPassword
) : IRequest<CreateUserResult>;
