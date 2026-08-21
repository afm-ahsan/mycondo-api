using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;

public sealed record FixedDepositPortfolioLineDto(
    Guid FixedDepositId,
    string CertificateNumber,
    string BankName,
    string? BranchName,
    decimal Principal,
    decimal InterestRatePercent,
    DateOnly StartDate,
    DateOnly MaturityDate,
    string Status,
    decimal TotalAccruedInterest,
    decimal TotalReceivedInterest,
    decimal OutstandingAccruedInterest);

/// <summary><see cref="TotalPrincipal"/> is <c>FixedDepositStatus.Active</c> principal only —
/// a Renewed instrument's principal now lives on its successor and a Withdrawn instrument's principal
/// is no longer Fixed Deposit money at all, so including either would double-count or misstate current
/// holdings (Template 4's "Fixed Deposit != Spendable Cash" distinction, kept internally consistent:
/// this figure never conflates historical instruments with current ones either).</summary>
public sealed record FixedDepositPortfolioReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<FixedDepositPortfolioLineDto> Lines,
    decimal TotalPrincipal,
    decimal TotalOutstandingAccruedInterest,
    int ActiveCount);

/// <summary>Every Fixed Deposit instrument (Active/Renewed/Withdrawn — Voided ones are data-entry
/// corrections, never real principal, and are excluded) as of <see cref="AsOfDate"/>, each with its
/// current accrued-but-unreceived interest (accrued-to-date minus received-to-date). Board/admin-facing,
/// gated by <c>finance.report.view</c>.</summary>
public sealed record GetFixedDepositPortfolioReportQuery(DateOnly? AsOfDate) : IRequest<FixedDepositPortfolioReportDto>;
