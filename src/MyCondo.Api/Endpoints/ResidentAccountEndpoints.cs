using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Payments.Commands.OpenResidentAccount;
using MyCondo.Application.Features.Payments.Commands.RecordOpeningBalance;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetAccountBalance;
using MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ResidentAccountEndpoints
{
    public static IEndpointRouteBuilder MapResidentAccountEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder accounts = app.MapGroup("/api/v1/resident-accounts").WithTags("ResidentAccounts");

        accounts.MapPost("/", async (OpenResidentAccountCommand command, ISender sender, CancellationToken ct) =>
            {
                ResidentAccountDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("residentaccount.manage")
            .Produces<ResidentAccountDto>(StatusCodes.Status200OK);

        accounts.MapPost("/opening-balance", async (RecordOpeningBalanceCommand command, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<LedgerEntryDto> result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("residentaccount.manage")
            .RequireIdempotencyKey()
            .Produces<IReadOnlyList<LedgerEntryDto>>(StatusCodes.Status200OK);

        accounts.MapGet("/{flatId:guid}/balance", async (Guid flatId, ISender sender, CancellationToken ct) =>
            {
                AccountBalanceDto result = await sender.Send(new GetAccountBalanceQuery(flatId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("residentaccount.view")
            .Produces<AccountBalanceDto>(StatusCodes.Status200OK);

        accounts.MapGet("/{flatId:guid}/ledger-entries", async (
                Guid flatId, DateOnly? fromDate, DateOnly? toDate, string? referenceType, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<LedgerEntryDto> result = await sender.Send(
                    new GetLedgerEntriesForAccountQuery(
                        flatId, fromDate, toDate, referenceType, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("payment.view")
            .Produces<PagedResult<LedgerEntryDto>>(StatusCodes.Status200OK);

        return app;
    }
}
