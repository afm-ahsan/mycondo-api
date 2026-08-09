using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Seeding.Extensions;

/// <summary>Idempotent user provisioning for seeding. Passwords are hashed via the same
/// <see cref="IPasswordHasher"/> normal registration uses (<c>RegisterUserCommandHandler</c>) — never
/// a bespoke/weaker hasher, and the plaintext input is never persisted or logged.</summary>
internal static class UserSeedExtensions
{
    public static async Task<(User User, bool Created)> EnsureUserAsync(
        this IUserRepository users,
        Guid tenantId,
        string email,
        string fullName,
        string password,
        IPasswordHasher passwordHasher,
        IClock clock,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        User? existing = await users.GetByEmailAsync(tenantId, normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return (existing, false);
        }

        string passwordHash = passwordHasher.Hash(password);
        User user = User.Register(tenantId, normalizedEmail, passwordHash, fullName, phoneNumber: null, clock.UtcNow);
        users.Add(user);

        return (user, true);
    }
}
