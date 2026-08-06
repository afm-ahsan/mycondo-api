using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Application.Features.Payments.Commands.ReversePayment;

public sealed class ReversePaymentCommandHandler(
    IPaymentRepository payments,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReversePaymentCommandHandler> logger
) : IRequestHandler<ReversePaymentCommand, PaymentDto>
{
    public async ValueTask<PaymentDto> Handle(ReversePaymentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PaymentId id = new(command.PaymentId);
        Payment payment = await payments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), command.PaymentId);
        if (payment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Payment), command.PaymentId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        payment.Reverse(command.Reason, currentUser.UserId, nowUtc);

        string description = $"Reversal of payment {payment.Id}: {command.Reason}";
        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.ResidentReceivable, payment.FlatId, LedgerDirection.Debit, payment.Amount, description),
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, payment.Amount, description),
        ];

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "PaymentReversal",
            payment.LedgerPostingId.Value, lines, nowUtc);

        ledgerPostings.Add(posting);
        ledgerEntries.AddRange(entries);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} reversed for tenant {TenantId}, reversal posting {PostingId}",
            payment.Id, tenantId, posting.Id);

        return payment.ToDto();
    }
}
