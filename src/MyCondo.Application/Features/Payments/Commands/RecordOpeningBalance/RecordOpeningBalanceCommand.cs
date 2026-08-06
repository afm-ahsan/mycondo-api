using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Commands.RecordOpeningBalance;

public sealed record RecordOpeningBalanceCommand(
    Guid FlatId,
    decimal Amount,
    DateOnly BusinessDate,
    string? Description
) : IRequest<IReadOnlyList<LedgerEntryDto>>;
