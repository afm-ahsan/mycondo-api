using Mediator;
using MyCondo.Application.Features.Finance.AccountMappings.DTOs;

namespace MyCondo.Application.Features.Finance.AccountMappings.Queries.GetAccountMappings;

public sealed record GetAccountMappingsQuery : IRequest<List<AccountMappingDto>>;
