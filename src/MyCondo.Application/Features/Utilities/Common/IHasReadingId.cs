namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Implemented by reading-scoped requests that carry a <c>ReadingId</c> — their commercial
/// feature (Electricity vs. Gas) is resolved via <see cref="ReadingFeatureResolver{TRequest}"/>
/// (ADR-033 Task 09A).</summary>
public interface IHasReadingId
{
    Guid ReadingId { get; }
}
