using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;

public sealed record CashBankAccountLineDto(
    Guid FinancialAccountId,
    string Name,
    string AccountType,
    string? BankName,
    string? BranchName,
    string? AccountNumber,
    decimal Balance);

public sealed record CashBankPositionReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<CashBankAccountLineDto> Accounts,
    decimal TotalBalance);

public sealed record GetCashBankPositionReportQuery(DateOnly? AsOfDate) : IRequest<CashBankPositionReportDto>;
