namespace MyCondo.Application.Features.Finance.AccountMappings.DTOs;

public sealed record AccountMappingDto(Guid AccountMappingId, string PostingRole, Guid ChartOfAccountId);
