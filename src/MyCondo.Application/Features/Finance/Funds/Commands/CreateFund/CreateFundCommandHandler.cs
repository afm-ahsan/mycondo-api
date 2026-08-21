using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Funds.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Application.Features.Finance.Funds.Commands.CreateFund;

public sealed class CreateFundCommandHandler(
    IFundRepository funds,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<CreateFundCommandHandler> logger
) : IRequestHandler<CreateFundCommand, FundDto>
{
    public async ValueTask<FundDto> Handle(CreateFundCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string code = command.Code.Trim();
        if (await funds.ExistsForCodeAsync(tenantId, code, cancellationToken))
        {
            throw new ConflictException($"A fund with code '{code}' already exists.");
        }

        Fund fund = Fund.Create(tenantId, code, command.Name, command.Description);
        funds.Add(fund);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Fund {FundId} '{Code}' created for tenant {TenantId}", fund.Id, code, tenantId);

        return new FundDto(fund.Id.Value, fund.Code, fund.Name, fund.Description, fund.IsActive);
    }
}
