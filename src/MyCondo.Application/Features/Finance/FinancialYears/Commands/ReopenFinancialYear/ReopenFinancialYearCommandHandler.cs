using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.ReopenFinancialYear;

public sealed class ReopenFinancialYearCommandHandler(
    IFinancialYearRepository financialYears,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ReopenFinancialYearCommandHandler> logger
) : IRequestHandler<ReopenFinancialYearCommand, FinancialYearDto>
{
    public async ValueTask<FinancialYearDto> Handle(ReopenFinancialYearCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinancialYearId id = new(command.FinancialYearId);
        FinancialYear year = await financialYears.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialYear), command.FinancialYearId);
        if (year.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialYear), command.FinancialYearId);
        }

        year.Reopen();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Financial year {FinancialYearId} reopened for tenant {TenantId}", id, tenantId);

        return new FinancialYearDto(year.Id.Value, year.Name, year.StartDate, year.EndDate, year.Status.ToString());
    }
}
