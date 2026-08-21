using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Mappings;

/// <summary>Names/ids resolved by the caller (Financial Account/Fund lookups) and interest totals
/// aggregated from the separate <see cref="FixedDepositInterestAccrual"/>/<see cref="FixedDepositInterestReceipt"/>
/// repositories — same "handler resolves, mapping just shapes" split <c>ExpenseMappings</c> uses.
/// <see cref="ToDto(FixedDeposit, string?, string?, string?, decimal, decimal, DateOnly)"/>'s
/// <c>today</c> parameter drives <see cref="FixedDepositDto.IsMatured"/> — see
/// <see cref="FixedDepositStatus"/>'s doc comment for why maturity is a date comparison, not a stored
/// status.</summary>
public static class FixedDepositMappings
{
    public static FixedDepositDto ToDto(
        this FixedDeposit fixedDeposit, string? fundingAccountName, string? receivingAccountName,
        string? fundName, decimal totalInterestAccrued, decimal totalInterestReceivedGross, DateOnly today) => new(
        fixedDeposit.Id.Value,
        fixedDeposit.CertificateNumber,
        fixedDeposit.BankName,
        fixedDeposit.BranchName,
        fixedDeposit.FundingFinancialAccountId.Value,
        fundingAccountName,
        fixedDeposit.ReceivingFinancialAccountId?.Value,
        receivingAccountName,
        fixedDeposit.FundId?.Value,
        fundName,
        fixedDeposit.Principal,
        fixedDeposit.InterestRatePercent,
        fixedDeposit.CalculationMethod.ToString(),
        fixedDeposit.PaymentFrequency.ToString(),
        fixedDeposit.StartDate,
        fixedDeposit.MaturityDate,
        fixedDeposit.Status == FixedDepositStatus.Active && fixedDeposit.MaturityDate <= today,
        fixedDeposit.ExpectedGrossInterest,
        fixedDeposit.ExpectedDeductionRatePercent,
        fixedDeposit.Notes,
        fixedDeposit.Status.ToString(),
        fixedDeposit.PredecessorFixedDepositId?.Value,
        fixedDeposit.SuccessorFixedDepositId?.Value,
        totalInterestAccrued,
        totalInterestReceivedGross,
        totalInterestAccrued - totalInterestReceivedGross,
        fixedDeposit.VoidReason);

    public static FixedDepositInterestAccrualDto ToDto(this FixedDepositInterestAccrual accrual) => new(
        accrual.Id.Value, accrual.FixedDepositId.Value, accrual.PeriodStart, accrual.PeriodEnd,
        accrual.AccountingDate, accrual.GrossAmount, accrual.Notes, accrual.IsReversed, accrual.CreatedAtUtc);

    public static FixedDepositInterestReceiptDto ToDto(this FixedDepositInterestReceipt receipt) => new(
        receipt.Id.Value, receipt.FixedDepositId.Value, receipt.AccountingDate, receipt.GrossAmount,
        receipt.DeductionAmount, receipt.NetAmount, receipt.ReceivingFinancialAccountId.Value,
        receipt.ReferenceNumber, receipt.Notes, receipt.IsReversed, receipt.CreatedAtUtc);
}
