using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Primitives;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payments.Idempotency;

namespace MyCondo.Api.Authorization;

/// <summary>
/// Enforces <c>X-Idempotency-Key</c> on financial mutations (see CLAUDE.md "Financial integrity"
/// and <c>financial-engine.md</c>). A replay with the same key and an identical request body returns
/// the stored response without re-executing the handler; a replay with a different body is rejected
/// via <see cref="MyCondo.Domain.Features.Payments.Idempotency.Exceptions.IdempotencyKeyConflictException"/>.
///
/// This is a transport concern, not a domain rule, so it lives here as an <see cref="IEndpointFilter"/>
/// rather than a Mediator pipeline behavior. The key is persisted in a separate
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call after the handler's own commit — it closes the
/// common case (a client retrying after a timeout/disconnect gets the stored response back) but does
/// not fully protect against two genuinely concurrent requests racing past the existence check at the
/// same instant, nor against a crash in the narrow window between the handler's commit and this
/// filter's own save. Closing that gap fully would mean moving `SaveChangesAsync` out of individual
/// command handlers into a shared pipeline behavior so both writes commit atomically — out of scope
/// for this slice; flagged as a follow-up hardening item.
/// </summary>
public sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    private const string HeaderName = "X-Idempotency-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        ICurrentUserProvider currentUser = http.RequestServices.GetRequiredService<ICurrentUserProvider>();

        if (currentUser.TenantId is not Guid tenantId)
        {
            return Results.Forbid();
        }

        if (!http.Request.Headers.TryGetValue(HeaderName, out StringValues rawKey) || string.IsNullOrWhiteSpace(rawKey))
        {
            return Results.BadRequest(new { error = $"The {HeaderName} header is required for this request." });
        }

        string key = rawKey.ToString();
        string requestPath = http.Request.Path;
        string requestHash = ComputeRequestHash(context.Arguments);

        IIdempotencyKeyRepository idempotencyKeys =
            http.RequestServices.GetRequiredService<IIdempotencyKeyRepository>();

        IdempotencyKey? existing = await idempotencyKeys.FindAsync(tenantId, key, requestPath, http.RequestAborted);
        if (existing is not null)
        {
            existing.EnsureMatches(requestHash);
            return Results.Content(existing.ResponseBody, "application/json", statusCode: existing.ResponseStatusCode);
        }

        object? result = await next(context);

        int statusCode = result is IStatusCodeHttpResult statusResult
            ? statusResult.StatusCode ?? StatusCodes.Status200OK
            : StatusCodes.Status200OK;
        object? responseValue = result is IValueHttpResult valueResult ? valueResult.Value : result;

        if (statusCode is >= 200 and < 300)
        {
            string responseBody = JsonSerializer.Serialize(responseValue);
            IUnitOfWork unitOfWork = http.RequestServices.GetRequiredService<IUnitOfWork>();
            IClock clock = http.RequestServices.GetRequiredService<IClock>();

            idempotencyKeys.Add(IdempotencyKey.Record(
                tenantId, key, requestPath, requestHash, statusCode, responseBody, clock.UtcNow));
            await unitOfWork.SaveChangesAsync(http.RequestAborted);
        }

        return result;
    }

    private static string ComputeRequestHash(IList<object?> arguments)
    {
        IEnumerable<object?> hashableArguments = arguments.Where(a => a is not CancellationToken and not ISender);
        string json = JsonSerializer.Serialize(hashableArguments);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
