using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Commands.RecordPayment;

public sealed class RecordPaymentCommandHandler(
    IResidentAccountRepository accounts,
    IPaymentRepository payments,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordPaymentCommandHandler> logger
) : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    public async ValueTask<PaymentDto> Handle(RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        ResidentAccount? account = await accounts.GetByFlatIdAsync(tenantId, flatId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException(nameof(ResidentAccount), command.FlatId);
        }

        PaymentMethod method = Enum.Parse<PaymentMethod>(command.PaymentMethod);
        string description = string.IsNullOrWhiteSpace(command.Description)
            ? $"Payment received from flat {command.FlatId}"
            : command.Description;
        DateTimeOffset nowUtc = clock.UtcNow;

        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, command.Amount, description),
            new LedgerLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Credit, command.Amount, description),
        ];

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, command.BusinessDate, description, "Payment", null, lines, nowUtc);

        ledgerPostings.Add(posting);
        ledgerEntries.AddRange(entries);

        Payment payment = Payment.Record(
            tenantId, flatId, command.Amount, method, command.ReferenceNumber, command.BusinessDate,
            currentUser.UserId, posting.Id, nowUtc);
        payments.Add(payment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} of {Amount} recorded for flat {FlatId}, tenant {TenantId}",
            payment.Id, command.Amount, flatId, tenantId);

        return payment.ToDto();
    }
}
