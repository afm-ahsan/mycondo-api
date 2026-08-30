using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoiceById;

/// <summary>Platform-scope invoice detail (ADR-034 Task 14D), including charge lines and payment
/// history — read-only, no mutation semantics.</summary>
public sealed record GetSubscriptionInvoiceByIdQuery(Guid InvoiceId) : IRequest<PlatformSubscriptionInvoiceDetailDto>;
