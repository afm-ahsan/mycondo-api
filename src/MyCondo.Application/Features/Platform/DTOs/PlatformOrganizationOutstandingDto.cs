namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>
/// One organization's collectible balance in one currency (ADR-034 Task 14D) — grouped by
/// (TenantId, Currency) rather than a single per-organization total so that an organization billed in
/// more than one currency is never silently summed across currencies (see
/// <c>GetOutstandingDuesQueryHandler</c>).
/// </summary>
public sealed record PlatformOrganizationOutstandingDto(
    Guid TenantId,
    string OrganizationName,
    string Currency,
    decimal OutstandingAmount,
    int OutstandingInvoiceCount,
    DateOnly OldestDueDate,
    int? MaxDaysOverdue);
