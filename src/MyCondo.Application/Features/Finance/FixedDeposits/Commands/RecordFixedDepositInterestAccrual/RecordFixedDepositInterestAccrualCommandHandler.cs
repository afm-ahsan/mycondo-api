using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestAccrual;

/// <summary>
/// Records one period's earned FD interest — "Dr InterestReceivable / Cr FDInterestIncome" — via
/// <see cref="IFinancialPostingService"/>. Uses a freshly generated
/// <see cref="FixedDepositInterestAccrualId"/> (not <c>FixedDepositId</c>) as the posting <c>SourceId</c>
/// so repeated monthly accruals for the same FD, all under the same <c>PostingPurpose</c>, don't collide
/// with the duplicate-posting idempotency guard.
/// </summary>
public sealed class RecordFixedDepositInterestAccrualCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordFixedDepositInterestAccrualCommandHandler> logger
) : IRequestHandler<RecordFixedDepositInterestAccrualCommand, FixedDepositInterestAccrualDto>
{
    public async ValueTask<FixedDepositInterestAccrualDto> Handle(
        RecordFixedDepositInterestAccrualCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FixedDepositId fixedDepositId = new(command.FixedDepositId);
        FixedDeposit fixedDeposit = await fixedDeposits.GetByIdAsync(fixedDepositId, cancellationToken)
            ?? throw new NotFoundException(nameof(FixedDeposit), command.FixedDepositId);
        if (fixedDeposit.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FixedDeposit), command.FixedDepositId);
        }

        if (fixedDeposit.Status != FixedDepositStatus.Active)
        {
            throw new ConflictException($"Fixed Deposit {fixedDeposit.Id} is {fixedDeposit.Status} and cannot accrue interest.");
        }

        DateOnly accountingDate = command.AccountingDate ?? command.PeriodEnd;
        FixedDepositInterestAccrualId accrualId = FixedDepositInterestAccrualId.New();

        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(LedgerAccountType.InterestReceivable, null, LedgerDirection.Debit, command.GrossAmount),
            new FinancialPostingLine(LedgerAccountType.FDInterestIncome, null, LedgerDirection.Credit, command.GrossAmount),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, accountingDate, $"FD interest accrual: {fixedDeposit.CertificateNumber} ({command.PeriodStart:yyyy-MM-dd}–{command.PeriodEnd:yyyy-MM-dd})",
                "FixedDepositInterestAccrual", accrualId.Value, postingLines, fixedDeposit.FundId),
            cancellationToken);

        FixedDepositInterestAccrual accrual = FixedDepositInterestAccrual.Record(
            accrualId, tenantId, fixedDepositId, command.PeriodStart, command.PeriodEnd, accountingDate,
            command.GrossAmount, command.Notes, posted.Posting.Id, clock.UtcNow);

        accruals.Add(accrual);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FD Interest Accrual {AccrualId} recorded for Fixed Deposit {FixedDepositId}, tenant {TenantId}, posting {PostingId}",
            accrual.Id, fixedDepositId, tenantId, posted.Posting.Id);

        return accrual.ToDto();
    }
}
