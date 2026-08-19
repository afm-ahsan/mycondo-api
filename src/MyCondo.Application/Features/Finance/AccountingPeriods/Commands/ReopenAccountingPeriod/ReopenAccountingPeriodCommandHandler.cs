using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.ReopenAccountingPeriod;

public sealed class ReopenAccountingPeriodCommandHandler(
    IAccountingPeriodRepository accountingPeriods,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ReopenAccountingPeriodCommandHandler> logger
) : IRequestHandler<ReopenAccountingPeriodCommand, AccountingPeriodDto>
{
    public async ValueTask<AccountingPeriodDto> Handle(ReopenAccountingPeriodCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AccountingPeriodId id = new(command.AccountingPeriodId);
        AccountingPeriod period = await accountingPeriods.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccountingPeriod), command.AccountingPeriodId);
        if (period.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(AccountingPeriod), command.AccountingPeriodId);
        }

        period.Reopen();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Accounting period {AccountingPeriodId} reopened for tenant {TenantId}", id, tenantId);

        return new AccountingPeriodDto(
            period.Id.Value, period.FinancialYearId.Value, period.Name, period.StartDate, period.EndDate, period.Status.ToString());
    }
}
