using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;

public sealed record GeneralLedgerLineDto(
    Guid EntryId,
    Guid PostingId,
    DateOnly BusinessDate,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    Guid? ChartOfAccountId,
    string? ChartOfAccountCode,
    string? ChartOfAccountName,
    Guid? FlatId,
    Guid? FundId,
    string Direction,
    decimal Amount);

public sealed record GeneralLedgerReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<GeneralLedgerLineDto> Lines,
    int Page,
    int PageSize,
    long Total);

public sealed record GetGeneralLedgerQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? ChartOfAccountId,
    Guid? FundId,
    string? ReferenceType,
    int Page,
    int PageSize
) : IRequest<GeneralLedgerReportDto>;
