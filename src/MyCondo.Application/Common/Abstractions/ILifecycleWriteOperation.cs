namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Explicitly documents a Mediator request as an ordinary mutation blocked while
/// <see cref="ISubscriptionLifecycleAccessService"/> reports <see cref="TenantAccessMode.ReadOnly"/>
/// (ADR-032 §5). Not required for correctness — <c>TenantLifecycleBehavior</c> already treats any request
/// that is neither <see cref="ILifecycleReadOperation"/> nor <see cref="IBillingResolutionOperation"/> as
/// blocked by default (fail closed) — but self-documents intent on the small pilot set of representative
/// write requests this foundation task exercises (ADR-032 Task 10 §59/§60).
/// </summary>
public interface ILifecycleWriteOperation;
