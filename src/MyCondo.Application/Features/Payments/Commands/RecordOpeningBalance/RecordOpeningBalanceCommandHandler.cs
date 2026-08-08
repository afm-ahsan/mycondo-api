using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Commands.RecordOpeningBalance;

public sealed class RecordOpeningBalanceCommandHandler(
    IResidentAccountRepository accounts,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordOpeningBalanceCommandHandler> logger
) : IRequestHandler<RecordOpeningBalanceCommand, IReadOnlyList<LedgerEntryDto>>
{
    public async ValueTask<IReadOnlyList<LedgerEntryDto>> Handle(
        RecordOpeningBalanceCommand command, CancellationToken cancellationToken)
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

        string description = string.IsNullOrWhiteSpace(command.Description) ? "Opening balance" : command.Description;
        DateTimeOffset nowUtc = clock.UtcNow;

        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Debit, command.Amount, description),
            new LedgerLine(LedgerAccountType.OpeningBalanceEquity, null, LedgerDirection.Credit, command.Amount, description),
        ];

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, command.BusinessDate, description, "OpeningBalance", null, lines, nowUtc);

        ledgerPostings.Add(posting);
        ledgerEntries.AddRange(entries);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Opening balance {Amount} posted for flat {FlatId}, tenant {TenantId}, posting {PostingId}",
            command.Amount, flatId, tenantId, posting.Id);

        return entries.Select(e => e.ToDto(posting.ReferenceType, posting.ReferenceId)).ToList();
    }
}
