using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.PlaceFixedDeposit;

/// <summary>
/// Places a new Fixed Deposit and posts its principal movement — "Dr FixedDeposit / Cr CashOrBank" —
/// through the centralized <see cref="IFinancialPostingService"/>, resolved directly to the funding
/// <see cref="FinancialAccount"/>'s own ledger account via <see cref="FinancialPostingLine.ExplicitAccountId"/>
/// (Template 4). Idempotent the same way Expense postings are: the (pre-generated) FixedDeposit id is
/// the posting's <c>SourceId</c> under a dedicated <c>PostingPurpose</c>.
/// </summary>
public sealed class PlaceFixedDepositCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFinancialAccountRepository financialAccounts,
    IFundRepository funds,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<PlaceFixedDepositCommandHandler> logger
) : IRequestHandler<PlaceFixedDepositCommand, FixedDepositDto>
{
    public async ValueTask<FixedDepositDto> Handle(PlaceFixedDepositCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinancialAccountId fundingAccountId = new(command.FundingFinancialAccountId);
        FinancialAccount fundingAccount = await financialAccounts.GetByIdAsync(fundingAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), command.FundingFinancialAccountId);
        if (fundingAccount.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialAccount), command.FundingFinancialAccountId);
        }

        if (!fundingAccount.IsActive)
        {
            throw new ConflictException($"Financial Account {fundingAccount.Id} is inactive and cannot fund a placement.");
        }

        string certificateNumber = command.CertificateNumber.Trim();
        if (await fixedDeposits.ExistsForCertificateNumberAsync(tenantId, certificateNumber, cancellationToken))
        {
            throw new ConflictException($"A Fixed Deposit with certificate number '{certificateNumber}' already exists.");
        }

        FundId? fundId = command.FundId is Guid fundGuid ? new FundId(fundGuid) : null;
        Fund? fund = fundId is FundId f ? await funds.GetByIdAsync(f, cancellationToken) : null;
        if (fundId is not null && (fund is null || fund.TenantId != tenantId))
        {
            throw new NotFoundException(nameof(Fund), command.FundId!.Value);
        }

        FixedDepositId fixedDepositId = FixedDepositId.New();
        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(LedgerAccountType.FixedDeposit, null, LedgerDirection.Debit, command.Principal),
            new FinancialPostingLine(
                LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, command.Principal,
                ExplicitAccountId: fundingAccount.ChartOfAccountId),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, command.StartDate, $"Fixed Deposit placement: {certificateNumber} ({fundingAccount.BankName ?? fundingAccount.Name})",
                "FixedDepositPlacement", fixedDepositId.Value, postingLines, fundId),
            cancellationToken);

        FixedDeposit fixedDeposit = FixedDeposit.Place(
            fixedDepositId, tenantId, certificateNumber, command.BankName, command.BranchName, fundingAccountId,
            fundId, command.Principal, command.InterestRatePercent,
            Enum.Parse<InterestCalculationMethod>(command.CalculationMethod),
            Enum.Parse<InterestPaymentFrequency>(command.PaymentFrequency), command.StartDate, command.MaturityDate,
            command.ExpectedGrossInterest, command.ExpectedDeductionRatePercent, command.Notes,
            posted.Posting.Id, clock.UtcNow);

        fixedDeposits.Add(fixedDeposit);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fixed Deposit {FixedDepositId} '{CertificateNumber}' placed for tenant {TenantId}, posting {PostingId}",
            fixedDeposit.Id, certificateNumber, tenantId, posted.Posting.Id);

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return fixedDeposit.ToDto(fundingAccount.Name, null, fund?.Name, 0m, 0m, today);
    }
}
