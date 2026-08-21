using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;

namespace MyCondo.Api.Endpoints;

/// <summary>Template 5 Finance reports — the Accounting group (Trial Balance, General Ledger,
/// Account Ledger, Fund Position, Financial Position, Cash Flow) plus the Core/Management group
/// (Financial Overview, Income &amp; Expense, Cash &amp; Bank Position, Service Charge/Gas/Fine
/// Collection, Outstanding Dues, Resident/Flat Financial Statement, Expense Summary/Category/Type/
/// Trend, Fixed Deposit Portfolio/Interest). Routed under <c>/api/v1/reports/finance</c>, distinct
/// from the pre-existing Invoice/Payment-based <c>/api/v1/reports/financial</c> group
/// (<see cref="FinancialReportEndpoints"/>) — these read from the ledger, not from Invoice/Payment
/// directly. General Ledger / Account Ledger reuse the <c>finance.journal.view</c> permission
/// reserved for them since Template 1; most others use the new <c>finance.report.view</c> key.
/// Resident Financial Statement is gated by authentication only (like <c>MeEndpoints</c>'
/// self-service pattern) because its handler enforces a two-tier check itself — <c>finance.report.view</c>
/// (any flat) or the self-service <c>finance.report.statement.own.view</c> (own flat only) — and a
/// route-level single-permission gate would lock out legitimate self-service callers entirely.</summary>
public static class FinanceReportEndpoints
{
    public static IEndpointRouteBuilder MapFinanceReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/finance").WithTags("Finance Reports");

        reports.MapGet("/trial-balance", async (DateOnly? asOfDate, ISender sender, CancellationToken ct) =>
            {
                TrialBalanceReportDto result = await sender.Send(new GetTrialBalanceQuery(asOfDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<TrialBalanceReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/general-ledger", async (
                DateOnly? fromDate, DateOnly? toDate, Guid? chartOfAccountId, Guid? fundId, string? referenceType,
                int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                GeneralLedgerReportDto result = await sender.Send(
                    new GetGeneralLedgerQuery(fromDate, toDate, chartOfAccountId, fundId, referenceType,
                        page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.journal.view")
            .Produces<GeneralLedgerReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/account-ledger/{chartOfAccountId:guid}", async (
                Guid chartOfAccountId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                AccountLedgerReportDto result = await sender.Send(
                    new GetAccountLedgerQuery(chartOfAccountId, fromDate, toDate, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.journal.view")
            .Produces<AccountLedgerReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/fund-position", async (DateOnly? asOfDate, ISender sender, CancellationToken ct) =>
            {
                FundPositionReportDto result = await sender.Send(new GetFundPositionQuery(asOfDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FundPositionReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/financial-position", async (DateOnly? asOfDate, ISender sender, CancellationToken ct) =>
            {
                FinancialPositionReportDto result = await sender.Send(new GetFinancialPositionQuery(asOfDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FinancialPositionReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/cash-flow", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                CashFlowReportDto result = await sender.Send(new GetCashFlowQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<CashFlowReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/overview", async (
                DateOnly? asOfDate, DateOnly? fromDate, DateOnly? toDate, ISender sender, CancellationToken ct) =>
            {
                FinancialOverviewReportDto result = await sender.Send(new GetFinancialOverviewQuery(asOfDate, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FinancialOverviewReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/income-expense", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IncomeExpenseReportDto result = await sender.Send(new GetIncomeExpenseReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<IncomeExpenseReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/cash-bank-position", async (DateOnly? asOfDate, ISender sender, CancellationToken ct) =>
            {
                CashBankPositionReportDto result = await sender.Send(new GetCashBankPositionReportQuery(asOfDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<CashBankPositionReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/service-charge-collection", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                ServiceChargeCollectionReportDto result = await sender.Send(new GetServiceChargeCollectionReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<ServiceChargeCollectionReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/gas-collection", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                GasCollectionReportDto result = await sender.Send(new GetGasCollectionReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<GasCollectionReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/fines", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                FineReportDto result = await sender.Send(new GetFineReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FineReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/outstanding-dues", async (Guid? buildingId, ISender sender, CancellationToken ct) =>
            {
                OutstandingDuesReportDto result = await sender.Send(new GetOutstandingDuesReportQuery(buildingId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<OutstandingDuesReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/resident-statement/{flatId:guid}", async (
                Guid flatId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                ResidentFinancialStatementReportDto result = await sender.Send(
                    new GetResidentFinancialStatementReportQuery(flatId, fromDate, toDate, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<ResidentFinancialStatementReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/flat-statement/{flatId:guid}", async (
                Guid flatId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                FlatFinancialStatementReportDto result = await sender.Send(
                    new GetFlatFinancialStatementQuery(flatId, fromDate, toDate, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FlatFinancialStatementReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/expense-summary", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                ExpenseSummaryReportDto result = await sender.Send(new GetExpenseSummaryReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<ExpenseSummaryReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/expense-by-category", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                ExpenseByCategoryReportDto result = await sender.Send(new GetExpenseByCategoryReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<ExpenseByCategoryReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/expense-by-type", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                ExpenseByTypeReportDto result = await sender.Send(new GetExpenseByTypeReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<ExpenseByTypeReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/expense-trend", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                ExpenseTrendReportDto result = await sender.Send(new GetExpenseTrendReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<ExpenseTrendReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/fixed-deposit-portfolio", async (DateOnly? asOfDate, ISender sender, CancellationToken ct) =>
            {
                FixedDepositPortfolioReportDto result = await sender.Send(new GetFixedDepositPortfolioReportQuery(asOfDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FixedDepositPortfolioReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/fixed-deposit-interest", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                FixedDepositInterestReportDto result = await sender.Send(new GetFixedDepositInterestReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.report.view")
            .Produces<FixedDepositInterestReportDto>(StatusCodes.Status200OK);

        return app;
    }
}
