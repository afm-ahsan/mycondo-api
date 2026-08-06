namespace MyCondo.Application.Features.Billing.DTOs;

public sealed record InvoiceDetailDto(InvoiceDto Invoice, IReadOnlyList<InvoiceLineDto> Lines);
