using Mediator;
using MyCondo.Application.Features.Finance.Funds.DTOs;

namespace MyCondo.Application.Features.Finance.Funds.Commands.CreateFund;

public sealed record CreateFundCommand(string Code, string Name, string? Description) : IRequest<FundDto>;
