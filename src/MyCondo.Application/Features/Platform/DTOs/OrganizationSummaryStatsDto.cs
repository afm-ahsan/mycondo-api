namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record OrganizationSummaryStatsDto(
    int Total,
    int Active,
    int Suspended,
    int PendingActivation,
    int RecentlyCreated);
