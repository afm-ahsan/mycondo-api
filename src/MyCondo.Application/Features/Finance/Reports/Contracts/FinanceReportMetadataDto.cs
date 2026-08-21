namespace MyCondo.Application.Features.Finance.Reports.Contracts;

/// <summary>Every Finance report response carries this so a rendered report is self-describing —
/// the Template 5 "API Metadata" requirement (period/scope/currency/basis/posted-only/generated
/// timestamp). <see cref="PostedOnly"/> is always true: every figure here is derived from posted
/// <c>LedgerEntry</c> rows — there is no draft/unposted ledger state in this system.</summary>
public sealed record FinanceReportMetadataDto(
    DateOnly? FromDate,
    DateOnly? ToDate,
    DateOnly? AsOfDate,
    string ScopeSummary,
    string Currency,
    string AccountingBasis,
    bool PostedOnly,
    DateTimeOffset GeneratedAtUtc,
    Guid? GeneratedByUserId)
{
    public static FinanceReportMetadataDto ForAsOf(
        DateOnly asOfDate, string scopeSummary, DateTimeOffset generatedAtUtc, Guid? generatedByUserId) =>
        new(null, null, asOfDate, scopeSummary, "BDT", "Accrual", true, generatedAtUtc, generatedByUserId);

    public static FinanceReportMetadataDto ForPeriod(
        DateOnly fromDate, DateOnly toDate, string scopeSummary, DateTimeOffset generatedAtUtc, Guid? generatedByUserId) =>
        new(fromDate, toDate, null, scopeSummary, "BDT", "Accrual", true, generatedAtUtc, generatedByUserId);
}
