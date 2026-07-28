using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Identity.Users.Exceptions;

public sealed class UserAlreadyDeactivatedException(UserId userId)
    : DomainException($"User {userId} is already deactivated.")
{
    public UserId UserId { get; } = userId;
}
