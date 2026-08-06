using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.GetAccountBalance;

public sealed record GetAccountBalanceQuery(Guid FlatId) : IRequest<AccountBalanceDto>;
