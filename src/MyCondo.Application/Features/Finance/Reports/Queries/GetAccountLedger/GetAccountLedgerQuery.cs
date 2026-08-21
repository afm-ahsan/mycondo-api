using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;

public sealed record AccountLedgerLineDto(
    Guid EntryId,
    Guid PostingId,
    DateOnly BusinessDate,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    Guid? FlatId,
    string Direction,
    decimal Amount,
    decimal RunningBalance);

public sealed record AccountLedgerReportDto(
    FinanceReportMetadataDto Metadata,
    Guid ChartOfAccountId,
    string ChartOfAccountCode,
    string ChartOfAccountName,
    string NormalBalance,
    decimal OpeningBalance,
    IReadOnlyList<AccountLedgerLineDto> Lines,
    decimal ClosingBalance,
    int Page,
    int PageSize,
    long Total);

public sealed record GetAccountLedgerQuery(
    Guid ChartOfAccountId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize
) : IRequest<AccountLedgerReportDto>;
