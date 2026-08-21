using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.CloseFinancialYear;

/// <summary>Requires every accounting period under the year to already be closed — a cross-aggregate
/// check that belongs at the application layer, not on <see cref="FinancialYear"/> itself (ADR-027).</summary>
public sealed class CloseFinancialYearCommandHandler(
    IFinancialYearRepository financialYears,
    IAccountingPeriodRepository accountingPeriods,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<CloseFinancialYearCommandHandler> logger
) : IRequestHandler<CloseFinancialYearCommand, FinancialYearDto>
{
    public async ValueTask<FinancialYearDto> Handle(CloseFinancialYearCommand command, CancellationToken cancellationToken)
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

        IReadOnlyList<AccountingPeriod> periods = await accountingPeriods.GetAllForFinancialYearAsync(id, cancellationToken);
        if (periods.Any(p => p.Status == AccountingPeriodStatus.Open))
        {
            throw new ConflictException("Every accounting period under this financial year must be closed before the year can be closed.");
        }

        year.Close();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Financial year {FinancialYearId} closed for tenant {TenantId}", id, tenantId);

        return new FinancialYearDto(year.Id.Value, year.Name, year.StartDate, year.EndDate, year.Status.ToString());
    }
}
