namespace MyCondo.Api.Authorization;

public static class EndpointRequireIdempotencyKeyExtensions
{
    /// <summary>
    /// Requires callers to supply <c>X-Idempotency-Key</c> on this financial mutation. Apply to every
    /// POST that posts ledger entries (payments, opening balances, reversals) — see
    /// mycondo-api CLAUDE.md "Financial integrity" and <c>financial-engine.md</c>.
    /// </summary>
    public static RouteHandlerBuilder RequireIdempotencyKey(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<IdempotencyEndpointFilter>();
}
