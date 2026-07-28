namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Surfaces the current request's client IP for audit / refresh-token tracking.
/// Implemented in the Api layer over <c>IHttpContextAccessor</c>.
/// </summary>
public interface IRequestIpAccessor
{
    string IpAddress { get; }
}
