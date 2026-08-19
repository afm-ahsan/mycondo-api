using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Finance.AccountingPeriods.Commands.CloseAccountingPeriod;
using MyCondo.Application.Features.Finance.AccountingPeriods.Commands.CreateAccountingPeriod;
using MyCondo.Application.Features.Finance.AccountingPeriods.Commands.ReopenAccountingPeriod;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;
using MyCondo.Application.Features.Finance.AccountingPeriods.Queries.GetAccountingPeriods;
using MyCondo.Application.Features.Finance.AccountMappings.Commands.SetAccountMapping;
using MyCondo.Application.Features.Finance.AccountMappings.DTOs;
using MyCondo.Application.Features.Finance.AccountMappings.Queries.GetAccountMappings;
using MyCondo.Application.Features.Finance.ChartOfAccounts.Commands.CreateChartOfAccount;
using MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;
using MyCondo.Application.Features.Finance.ChartOfAccounts.Queries.GetChartOfAccounts;
using MyCondo.Application.Features.Finance.FinancialYears.Commands.CloseFinancialYear;
using MyCondo.Application.Features.Finance.FinancialYears.Commands.CreateFinancialYear;
using MyCondo.Application.Features.Finance.FinancialYears.Commands.ReopenFinancialYear;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;
using MyCondo.Application.Features.Finance.FinancialYears.Queries.GetFinancialYears;
using MyCondo.Application.Features.Finance.Funds.Commands.CreateFund;
using MyCondo.Application.Features.Finance.Funds.DTOs;
using MyCondo.Application.Features.Finance.Funds.Queries.GetFunds;

namespace MyCondo.Api.Endpoints;

/// <summary>Finance Foundation &amp; Posting Engine administration endpoints (ADR-027) — Chart of Accounts,
/// account mappings, funds, financial years, and accounting periods. Journal entries themselves are not
/// browsed here; they're posted internally by <c>IFinancialPostingService</c> from the Billing/Payments/
/// Amenities/Utilities call sites and remain visible via each of those features' own existing endpoints
/// (e.g. <c>GetLedgerEntriesForAccount</c>) — a general cross-account journal browser is Reporting-phase
/// scope, not this foundation template's.</summary>
public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder accounts = app.MapGroup("/api/v1/finance/accounts").WithTags("Finance");

        accounts.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetChartOfAccountsQuery(), ct)))
            .RequirePermission("finance.account.view")
            .Produces<List<ChartOfAccountDto>>(StatusCodes.Status200OK);

        accounts.MapPost("/", async (CreateChartOfAccountCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.account.manage")
            .Produces<ChartOfAccountDto>(StatusCodes.Status200OK);

        RouteGroupBuilder mappings = app.MapGroup("/api/v1/finance/account-mappings").WithTags("Finance");

        mappings.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetAccountMappingsQuery(), ct)))
            .RequirePermission("finance.account.view")
            .Produces<List<AccountMappingDto>>(StatusCodes.Status200OK);

        mappings.MapPost("/", async (SetAccountMappingCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.mapping.manage")
            .Produces<AccountMappingDto>(StatusCodes.Status200OK);

        RouteGroupBuilder funds = app.MapGroup("/api/v1/finance/funds").WithTags("Finance");

        funds.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetFundsQuery(), ct)))
            .RequirePermission("finance.fund.view")
            .Produces<List<FundDto>>(StatusCodes.Status200OK);

        funds.MapPost("/", async (CreateFundCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.fund.manage")
            .Produces<FundDto>(StatusCodes.Status200OK);

        RouteGroupBuilder financialYears = app.MapGroup("/api/v1/finance/financial-years").WithTags("Finance");

        financialYears.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetFinancialYearsQuery(), ct)))
            .RequirePermission("finance.period.manage")
            .Produces<List<FinancialYearDto>>(StatusCodes.Status200OK);

        financialYears.MapPost("/", async (CreateFinancialYearCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.period.manage")
            .Produces<FinancialYearDto>(StatusCodes.Status200OK);

        financialYears.MapPost("/{id:guid}/close", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new CloseFinancialYearCommand(id), ct)))
            .RequirePermission("finance.period.close")
            .Produces<FinancialYearDto>(StatusCodes.Status200OK);

        financialYears.MapPost("/{id:guid}/reopen", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ReopenFinancialYearCommand(id), ct)))
            .RequirePermission("finance.period.reopen")
            .Produces<FinancialYearDto>(StatusCodes.Status200OK);

        RouteGroupBuilder periods = app.MapGroup("/api/v1/finance/accounting-periods").WithTags("Finance");

        periods.MapGet("/", async (Guid financialYearId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetAccountingPeriodsQuery(financialYearId), ct)))
            .RequirePermission("finance.period.manage")
            .Produces<List<AccountingPeriodDto>>(StatusCodes.Status200OK);

        periods.MapPost("/", async (CreateAccountingPeriodCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.period.manage")
            .Produces<AccountingPeriodDto>(StatusCodes.Status200OK);

        periods.MapPost("/{id:guid}/close", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new CloseAccountingPeriodCommand(id), ct)))
            .RequirePermission("finance.period.close")
            .Produces<AccountingPeriodDto>(StatusCodes.Status200OK);

        periods.MapPost("/{id:guid}/reopen", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ReopenAccountingPeriodCommand(id), ct)))
            .RequirePermission("finance.period.reopen")
            .Produces<AccountingPeriodDto>(StatusCodes.Status200OK);

        return app;
    }
}
