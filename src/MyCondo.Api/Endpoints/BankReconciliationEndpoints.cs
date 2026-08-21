using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddBankStatementLine;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddReconciliationAdjustment;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.CompleteBankReconciliation;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.ExcludeStatementLine;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.MatchStatementLine;
using MyCondo.Application.Features.Finance.BankReconciliations.Commands.StartBankReconciliation;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Application.Features.Finance.BankReconciliations.Queries.GetBankReconciliation;
using MyCondo.Application.Features.Finance.BankReconciliations.Queries.GetBankReconciliations;

namespace MyCondo.Api.Endpoints;

/// <summary>Template 6 (Governance, Reconciliation &amp; Production Readiness) — monthly Bank
/// Reconciliation of a Financial Account against its statement. Tenant-wide Association treasury data,
/// same permission-at-endpoint pattern as <see cref="BankingEndpoints"/>.</summary>
public static class BankReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapBankReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reconciliations = app.MapGroup("/api/v1/finance/bank-reconciliations").WithTags("Banking");

        reconciliations.MapGet("/", async (Guid financialAccountId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetBankReconciliationsQuery(financialAccountId), ct)))
            .RequirePermission("finance.reconciliation.view")
            .Produces<List<BankReconciliationDto>>(StatusCodes.Status200OK);

        reconciliations.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetBankReconciliationQuery(id), ct)))
            .RequirePermission("finance.reconciliation.view")
            .Produces<BankReconciliationDetailDto>(StatusCodes.Status200OK);

        reconciliations.MapPost("/", async (StartBankReconciliationCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.reconciliation.manage")
            .Produces<BankReconciliationDto>(StatusCodes.Status200OK);

        reconciliations.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new CompleteBankReconciliationCommand(id), ct)))
            .RequirePermission("finance.reconciliation.reconcile")
            .Produces<BankReconciliationDto>(StatusCodes.Status200OK);

        RouteGroupBuilder lines = app.MapGroup("/api/v1/finance/bank-statement-lines").WithTags("Banking");

        lines.MapPost("/", async (AddBankStatementLineCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .RequirePermission("finance.reconciliation.manage")
            .Produces<BankStatementLineDto>(StatusCodes.Status200OK);

        lines.MapPost("/{id:guid}/match", async (Guid id, MatchStatementLineLedgerEntryRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new MatchStatementLineCommand(id, body.LedgerEntryId), ct)))
            .RequirePermission("finance.reconciliation.manage")
            .Produces<BankStatementLineDto>(StatusCodes.Status200OK);

        lines.MapPost("/{id:guid}/exclude", async (Guid id, ExcludeStatementLineReasonRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ExcludeStatementLineCommand(id, body.Reason), ct)))
            .RequirePermission("finance.reconciliation.manage")
            .Produces<BankStatementLineDto>(StatusCodes.Status200OK);

        lines.MapPost("/{id:guid}/adjust", async (Guid id, AddReconciliationAdjustmentRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new AddReconciliationAdjustmentCommand(id, body.OtherSideRole, body.Description), ct)))
            .RequirePermission("finance.reconciliation.manage")
            .Produces<BankStatementLineDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record MatchStatementLineLedgerEntryRequest(Guid LedgerEntryId);

public sealed record ExcludeStatementLineReasonRequest(string Reason);

public sealed record AddReconciliationAdjustmentRequest(string OtherSideRole, string Description);
