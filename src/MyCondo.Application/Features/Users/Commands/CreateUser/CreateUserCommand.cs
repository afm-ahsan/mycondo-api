using Mediator;

namespace MyCondo.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Creates a user within the caller's own tenant. The administrator sets <see cref="Password"/>
/// directly — there is no generated-temporary-password flow. TenantId is deliberately not a field
/// here — it is always resolved from <c>ICurrentUserProvider</c>, never accepted from the client.
/// </summary>
public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string? PhoneNumber,
    string Password,
    bool IsActive
) : IRequest<CreateUserResult>;
