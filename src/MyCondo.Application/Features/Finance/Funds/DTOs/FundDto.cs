namespace MyCondo.Application.Features.Finance.Funds.DTOs;

public sealed record FundDto(Guid FundId, string Code, string Name, string? Description, bool IsActive);
