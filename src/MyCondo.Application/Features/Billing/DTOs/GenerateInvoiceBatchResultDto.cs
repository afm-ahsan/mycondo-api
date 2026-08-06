namespace MyCondo.Application.Features.Billing.DTOs;

public sealed record BatchItemOutcomeDto(Guid FlatId, string Reason);

public sealed record GenerateInvoiceBatchResultDto(
    int RequestedCount,
    int GeneratedCount,
    int AlreadyExistingCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<Guid> GeneratedInvoiceIds,
    IReadOnlyList<BatchItemOutcomeDto> SkippedItems,
    IReadOnlyList<BatchItemOutcomeDto> FailedItems);
