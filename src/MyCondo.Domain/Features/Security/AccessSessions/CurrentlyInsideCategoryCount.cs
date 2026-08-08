namespace MyCondo.Domain.Features.Security.AccessSessions;

/// <summary>Current-snapshot count of open (CheckedIn) access sessions by category.</summary>
public sealed record CurrentlyInsideCategoryCount(AccessCategory Category, int Count);
