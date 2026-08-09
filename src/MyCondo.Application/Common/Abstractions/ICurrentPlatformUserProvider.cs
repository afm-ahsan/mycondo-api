namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Platform-scope analogue of <see cref="ICurrentUserProvider"/> — a deliberately separate interface,
/// not a nullable extension of the tenant one, so no code path can ever be ambiguous about which
/// identity type it's looking at. See mycondo-docs ADR-019.
/// </summary>
public interface ICurrentPlatformUserProvider
{
    Guid? PlatformUserId { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
