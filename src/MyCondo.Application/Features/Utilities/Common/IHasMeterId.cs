namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Implemented by meter-scoped requests that carry a <c>MeterId</c> — their commercial feature
/// (Electricity vs. Gas) is resolved via <see cref="MeterFeatureResolver{TRequest}"/> (ADR-033 Task 09A).</summary>
public interface IHasMeterId
{
    Guid MeterId { get; }
}
