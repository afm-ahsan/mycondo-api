namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Marks a Mediator request as a genuine read — no domain mutation, no audit write, no lazily-created
/// data (ADR-032 Task 10 §36) — that must keep working while <see cref="ISubscriptionLifecycleAccessService"/>
/// reports <see cref="TenantAccessMode.ReadOnly"/> (Restricted/Expired/Canceled subscription, ADR-032 §5).
///
/// <para>Declared directly on the request type, mirroring <c>IRequiresFeature</c> — not a reflection-read
/// attribute. Unmarked requests default to being treated as writes by <c>TenantLifecycleBehavior</c> (fail
/// closed): this is a deliberate MVP-1.1 pilot rollout (ADR-032 Task 10 §59/§60), not a full sweep of every
/// handler — see the small representative set that carries this marker today (one core query, one
/// non-core/feature-gated query, one Finance read). Extending coverage to more requests is safe, additive,
/// and does not require touching this behavior or its tests.</para>
/// </summary>
public interface ILifecycleReadOperation;
