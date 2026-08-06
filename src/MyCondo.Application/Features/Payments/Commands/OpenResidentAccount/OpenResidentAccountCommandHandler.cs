using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Commands.OpenResidentAccount;

public sealed class OpenResidentAccountCommandHandler(
    IResidentAccountRepository accounts,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<OpenResidentAccountCommandHandler> logger
) : IRequestHandler<OpenResidentAccountCommand, ResidentAccountDto>
{
    public async ValueTask<ResidentAccountDto> Handle(OpenResidentAccountCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        ResidentAccount? existing = await accounts.GetByFlatIdAsync(tenantId, flatId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A resident account already exists for flat {command.FlatId}.");
        }

        ResidentAccount account = ResidentAccount.Open(tenantId, flatId, clock.UtcNow);
        accounts.Add(account);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Resident account {ResidentAccountId} opened for flat {FlatId}, tenant {TenantId}", account.Id, flatId, tenantId);

        return account.ToDto();
    }
}
