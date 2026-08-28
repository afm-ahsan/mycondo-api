using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Commands.RecordPayment;

/// <summary>Finance-write pilot for the ADR-032 Task 10 lifecycle read/write classification — proves an
/// ordinary tenant Finance mutation is blocked under a Restricted/Expired subscription. Never a CondoBD
/// platform-billing-resolution operation (ADR-032 Task 10 §39/§40) — deliberately not
/// <see cref="IBillingResolutionOperation"/>.</summary>
public sealed record RecordPaymentCommand(
    Guid FlatId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    DateOnly BusinessDate,
    string? Description
) : IRequest<PaymentDto>, ILifecycleWriteOperation;
