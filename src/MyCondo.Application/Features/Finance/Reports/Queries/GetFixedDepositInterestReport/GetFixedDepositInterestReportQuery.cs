using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

public sealed record FixedDepositInterestLineDto(
    Guid FixedDepositId,
    string CertificateNumber,
    decimal AccruedGrossForPeriod,
    decimal ReceivedGrossForPeriod,
    decimal ReceivedDeductionForPeriod,
    decimal ReceivedNetForPeriod);

/// <summary>Template 4's accrual-first FD interest model, verified against the actual posting handlers
/// (not assumed): accrual posts "Dr InterestReceivable / Cr FDInterestIncome" (interest income is
/// recognized at accrual, gross), receipt posts "Dr CashOrBank (net) [+ Dr InterestDeductionExpense
/// (deduction)] / Cr InterestReceivable (gross)" — a receipt never touches FDInterestIncome. So the
/// true reconciliation identity is <see cref="AccruedGrossForPeriod"/> == <see cref="LedgerInterestIncomeForPeriod"/>,
/// not anything receipt-shaped — <see cref="IsReconciled"/> checks exactly that.
/// <see cref="OutstandingAccruedNotReceivedAsOfToDate"/> is cumulative (all-time accrued minus all-time
/// received, as of <c>ToDate</c>) rather than period-bounded, since "outstanding balance" is inherently a
/// point-in-time concept — it mirrors the ledger's InterestReceivable account balance.</summary>
public sealed record FixedDepositInterestReportDto(
    FinanceReportMetadataDto Metadata,
    decimal AccruedGrossForPeriod,
    decimal LedgerInterestIncomeForPeriod,
    bool IsReconciled,
    decimal ReceivedGrossForPeriod,
    decimal ReceivedDeductionForPeriod,
    decimal ReceivedNetForPeriod,
    decimal OutstandingAccruedNotReceivedAsOfToDate,
    IReadOnlyList<FixedDepositInterestLineDto> ByFixedDeposit);

public sealed record GetFixedDepositInterestReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<FixedDepositInterestReportDto>;
