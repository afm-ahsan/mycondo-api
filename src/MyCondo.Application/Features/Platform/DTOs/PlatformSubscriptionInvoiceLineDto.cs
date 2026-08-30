namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record PlatformSubscriptionInvoiceLineDto(
    Guid LineId,
    string PackageNameSnapshot,
    int PackageVersionNumberSnapshot,
    string BillingCycleSnapshot,
    decimal BasePriceSnapshot,
    decimal DiscountSnapshot,
    decimal LineAmount,
    string Description);
