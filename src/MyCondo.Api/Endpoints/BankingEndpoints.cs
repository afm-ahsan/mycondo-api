using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Finance.FinancialAccounts.Commands.ActivateFinancialAccount;
using MyCondo.Application.Features.Finance.FinancialAccounts.Commands.CreateFinancialAccount;
using MyCondo.Application.Features.Finance.FinancialAccounts.Commands.DeactivateFinancialAccount;
using MyCondo.Application.Features.Finance.FinancialAccounts.Commands.UpdateFinancialAccount;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;
using MyCondo.Application.Features.Finance.FinancialAccounts.Queries.GetFinancialAccountsForTenant;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.PlaceFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestAccrual;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestReceipt;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.WithdrawFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositById;
using MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositsForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

/// <summary>Template 4 (Banking, Fixed Deposits &amp; Interest) endpoints — Financial Accounts (physical
/// money locations) and the Fixed Deposit register/interest lifecycle. Tenant-wide Association treasury
/// data, same permission-at-endpoint pattern as <see cref="FinanceEndpoints"/> (no building scoping).
/// </summary>
public static class BankingEndpoints
{
    public static IEndpointRouteBuilder MapBankingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder financialAccounts = app.MapGroup("/api/v1/finance/financial-accounts").WithTags("Banking");

        financialAccounts.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetFinancialAccountsForTenantQuery(), ct)))
            .RequirePermission("finance.bankaccount.view")
            .Produces<List<FinancialAccountDto>>(StatusCodes.Status200OK);

        financialAccounts.MapPost("/", async (CreateFinancialAccountRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new CreateFinancialAccountCommand(
                        body.Name, body.AccountType, body.BankName, body.BranchName, body.AccountNumber,
                        body.FundId, body.Notes),
                    ct)))
            .RequirePermission("finance.bankaccount.manage")
            .Produces<FinancialAccountDto>(StatusCodes.Status200OK);

        financialAccounts.MapPut("/{id:guid}", async (Guid id, UpdateFinancialAccountRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new UpdateFinancialAccountCommand(
                        id, body.Name, body.BankName, body.BranchName, body.AccountNumber, body.FundId, body.Notes),
                    ct)))
            .RequirePermission("finance.bankaccount.manage")
            .Produces<FinancialAccountDto>(StatusCodes.Status200OK);

        financialAccounts.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ActivateFinancialAccountCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("finance.bankaccount.manage")
            .Produces(StatusCodes.Status204NoContent);

        financialAccounts.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateFinancialAccountCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("finance.bankaccount.manage")
            .Produces(StatusCodes.Status204NoContent);

        RouteGroupBuilder fixedDeposits = app.MapGroup("/api/v1/finance/fixed-deposits").WithTags("Banking");

        fixedDeposits.MapGet("/", async (
                string? status, Guid? fundId, Guid? fundingFinancialAccountId, int? page, int? pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<FixedDepositDto> result = await sender.Send(
                    new GetFixedDepositsForTenantQuery(
                        status, fundId, fundingFinancialAccountId, page is null or < 1 ? 1 : page.Value,
                        pageSize is null or < 1 ? 20 : pageSize.Value),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("finance.fixeddeposit.view")
            .Produces<PagedResult<FixedDepositDto>>(StatusCodes.Status200OK);

        fixedDeposits.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetFixedDepositByIdQuery(id), ct)))
            .RequirePermission("finance.fixeddeposit.view")
            .Produces<FixedDepositDetailDto>(StatusCodes.Status200OK);

        fixedDeposits.MapPost("/", async (PlaceFixedDepositRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new PlaceFixedDepositCommand(
                        body.CertificateNumber, body.BankName, body.BranchName, body.FundingFinancialAccountId,
                        body.FundId, body.Principal, body.InterestRatePercent, body.CalculationMethod,
                        body.PaymentFrequency, body.StartDate, body.MaturityDate, body.ExpectedGrossInterest,
                        body.ExpectedDeductionRatePercent, body.Notes),
                    ct)))
            .RequirePermission("finance.fixeddeposit.place")
            .Produces<FixedDepositDto>(StatusCodes.Status200OK);

        fixedDeposits.MapPost("/{id:guid}/renew", async (Guid id, RenewFixedDepositRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new RenewFixedDepositCommand(
                        id, body.NewCertificateNumber, body.NewBranchName, body.FundingFinancialAccountId,
                        body.NewPrincipal, body.NewInterestRatePercent, body.NewCalculationMethod,
                        body.NewPaymentFrequency, body.NewStartDate, body.NewMaturityDate,
                        body.NewExpectedGrossInterest, body.NewExpectedDeductionRatePercent, body.Notes),
                    ct)))
            .RequirePermission("finance.fixeddeposit.manage")
            .Produces<FixedDepositDto>(StatusCodes.Status200OK);

        fixedDeposits.MapPost("/{id:guid}/withdraw", async (Guid id, WithdrawFixedDepositRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new WithdrawFixedDepositCommand(id, body.AccountingDate, body.ReceivingFinancialAccountId), ct)))
            .RequirePermission("finance.fixeddeposit.manage")
            .Produces<FixedDepositDto>(StatusCodes.Status200OK);

        fixedDeposits.MapPost("/{id:guid}/void", async (Guid id, VoidFixedDepositRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new VoidFixedDepositCommand(id, body.Reason), ct)))
            .RequirePermission("finance.fixeddeposit.manage")
            .Produces<FixedDepositDto>(StatusCodes.Status200OK);

        fixedDeposits.MapPost("/{id:guid}/interest/accruals", async (
                Guid id, RecordFixedDepositInterestAccrualRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new RecordFixedDepositInterestAccrualCommand(
                        id, body.PeriodStart, body.PeriodEnd, body.AccountingDate, body.GrossAmount, body.Notes),
                    ct)))
            .RequirePermission("finance.fixeddeposit.interest.record")
            .Produces<FixedDepositInterestAccrualDto>(StatusCodes.Status200OK);

        fixedDeposits.MapPost("/{id:guid}/interest/receipts", async (
                Guid id, RecordFixedDepositInterestReceiptRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new RecordFixedDepositInterestReceiptCommand(
                        id, body.AccountingDate, body.GrossAmount, body.DeductionAmount,
                        body.ReceivingFinancialAccountId, body.ReferenceNumber, body.Notes),
                    ct)))
            .RequirePermission("finance.fixeddeposit.interest.record")
            .Produces<FixedDepositInterestReceiptDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CreateFinancialAccountRequest(
    string Name, string AccountType, string? BankName, string? BranchName, string? AccountNumber,
    Guid? FundId, string? Notes);

public sealed record UpdateFinancialAccountRequest(
    string Name, string? BankName, string? BranchName, string? AccountNumber, Guid? FundId, string? Notes);

public sealed record PlaceFixedDepositRequest(
    string CertificateNumber, string BankName, string? BranchName, Guid FundingFinancialAccountId, Guid? FundId,
    decimal Principal, decimal InterestRatePercent, string CalculationMethod, string PaymentFrequency,
    DateOnly StartDate, DateOnly MaturityDate, decimal? ExpectedGrossInterest,
    decimal? ExpectedDeductionRatePercent, string? Notes);

public sealed record RenewFixedDepositRequest(
    string NewCertificateNumber, string? NewBranchName, Guid FundingFinancialAccountId, decimal NewPrincipal,
    decimal NewInterestRatePercent, string NewCalculationMethod, string NewPaymentFrequency,
    DateOnly NewStartDate, DateOnly NewMaturityDate, decimal? NewExpectedGrossInterest,
    decimal? NewExpectedDeductionRatePercent, string? Notes);

public sealed record WithdrawFixedDepositRequest(DateOnly? AccountingDate, Guid ReceivingFinancialAccountId);

public sealed record VoidFixedDepositRequest(string Reason);

public sealed record RecordFixedDepositInterestAccrualRequest(
    DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly? AccountingDate, decimal GrossAmount, string? Notes);

public sealed record RecordFixedDepositInterestReceiptRequest(
    DateOnly? AccountingDate, decimal GrossAmount, decimal DeductionAmount, Guid ReceivingFinancialAccountId,
    string? ReferenceNumber, string? Notes);
