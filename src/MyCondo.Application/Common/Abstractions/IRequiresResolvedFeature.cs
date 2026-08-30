namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Marks a MediatR request whose commercial FeatureKey cannot be determined from the request payload
/// alone (ADR-033 Task 09A) — it must be resolved from a persisted resource (e.g. a booking's facility
/// type, a meter/reading/rate plan's utility type) via the matching
/// <see cref="IRequestFeatureResolver{TRequest}"/>. A request whose FeatureKey is already known from its
/// own payload should implement <see cref="IRequiresFeature"/> instead — the two markers are mutually
/// exclusive.
/// </summary>
public interface IRequiresResolvedFeature;
