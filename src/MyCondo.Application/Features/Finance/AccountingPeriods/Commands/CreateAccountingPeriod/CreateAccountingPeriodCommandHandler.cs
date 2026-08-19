using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.CreateAccountingPeriod;

public sealed class CreateAccountingPeriodCommandHandler(
    IAccountingPeriodRepository accountingPeriods,
    IFinancialYearRepository financialYears,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<CreateAccountingPeriodCommandHandler> logger
) : IRequestHandler<CreateAccountingPeriodCommand, AccountingPeriodDto>
{
    public async ValueTask<AccountingPeriodDto> Handle(CreateAccountingPeriodCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinancialYearId financialYearId = new(command.FinancialYearId);
        FinancialYear year = await financialYears.GetByIdAsync(financialYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialYear), command.FinancialYearId);
        if (year.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialYear), command.FinancialYearId);
        }

        if (await accountingPeriods.OverlapsAsync(tenantId, command.StartDate, command.EndDate, cancellationToken))
        {
            throw new ConflictException($"An accounting period already covers part of {command.StartDate}..{command.EndDate}.");
        }

        AccountingPeriod period = AccountingPeriod.Create(tenantId, financialYearId, command.Name, command.StartDate, command.EndDate);
        accountingPeriods.Add(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Accounting period {AccountingPeriodId} '{Name}' created for tenant {TenantId}", period.Id, command.Name, tenantId);

        return new AccountingPeriodDto(
            period.Id.Value, period.FinancialYearId.Value, period.Name, period.StartDate, period.EndDate, period.Status.ToString());
    }
}
