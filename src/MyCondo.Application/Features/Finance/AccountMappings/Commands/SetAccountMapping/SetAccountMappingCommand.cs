using Mediator;
using MyCondo.Application.Features.Finance.AccountMappings.DTOs;

namespace MyCondo.Application.Features.Finance.AccountMappings.Commands.SetAccountMapping;

/// <summary>Creates the mapping if the posting role has none yet, or remaps it if it does — the
/// account-mapping matrix's write side is idempotent-by-role rather than needing separate Create/Update
/// commands, since a tenant only ever wants "this role points at this account" as one operation.</summary>
public sealed record SetAccountMappingCommand(string PostingRole, Guid ChartOfAccountId) : IRequest<AccountMappingDto>;
