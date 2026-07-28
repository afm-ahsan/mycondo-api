using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Abstractions;

public interface ISuperAdminBootstrapper
{
    /// <summary>
    /// Provisions a tenant-wide <c>SuperAdmin</c> system role (every catalogue permission), grants it,
    /// and assigns it to <paramref name="user"/>. Used by both <c>RegisterUserCommandHandler</c>
    /// (first user of a tenant registering themselves) and the <c>MyCondo.DbMigrator</c> tool's
    /// production bootstrap command (ADR-015) — the same sequence, two different entry points, so it
    /// lives here instead of being duplicated. Adds to the caller's current unit of work; the caller
    /// owns calling <c>IUnitOfWork.SaveChangesAsync</c> afterward.
    /// </summary>
    Task BootstrapAsync(Guid tenantId, User user, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
