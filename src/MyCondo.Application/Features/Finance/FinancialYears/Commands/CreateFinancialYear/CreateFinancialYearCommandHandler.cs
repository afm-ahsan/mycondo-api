using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.CreateFinancialYear;

public sealed class CreateFinancialYearCommandHandler(
    IFinancialYearRepository financialYears,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<CreateFinancialYearCommandHandler> logger
) : IRequestHandler<CreateFinancialYearCommand, FinancialYearDto>
{
    public async ValueTask<FinancialYearDto> Handle(CreateFinancialYearCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        if (await financialYears.OverlapsAsync(tenantId, command.StartDate, command.EndDate, cancellationToken))
        {
            throw new ConflictException(
                $"A financial year already covers part of {command.StartDate}..{command.EndDate}.");
        }

        FinancialYear year = FinancialYear.Create(tenantId, command.Name, command.StartDate, command.EndDate);
        financialYears.Add(year);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Financial year {FinancialYearId} '{Name}' created for tenant {TenantId}", year.Id, command.Name, tenantId);

        return new FinancialYearDto(year.Id.Value, year.Name, year.StartDate, year.EndDate, year.Status.ToString());
    }
}
