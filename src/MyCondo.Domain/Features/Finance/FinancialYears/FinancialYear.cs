using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.FinancialYears.Exceptions;

namespace MyCondo.Domain.Features.Finance.FinancialYears;

/// <summary>A tenant's accounting year — the parent of its <c>AccountingPeriod</c>s. Closing a year
/// requires all of its periods to already be closed (enforced by the application-layer close command,
/// since that's a cross-aggregate check); the year itself only tracks its own open/closed status.</summary>
public sealed class FinancialYear : AggregateRoot<FinancialYearId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FinancialYearStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private FinancialYear()
    {
        Name = null!;
    }

    private FinancialYear(FinancialYearId id, Guid tenantId, string name, DateOnly startDate, DateOnly endDate)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = FinancialYearStatus.Open;
    }

    public static FinancialYear Create(Guid tenantId, string name, DateOnly startDate, DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (endDate <= startDate)
        {
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));
        }

        return new FinancialYear(FinancialYearId.New(), tenantId, name.Trim(), startDate, endDate);
    }

    public void Close()
    {
        if (Status == FinancialYearStatus.Closed)
        {
            throw new FinancialYearAlreadyClosedException(Id);
        }

        Status = FinancialYearStatus.Closed;
    }

    public void Reopen() => Status = FinancialYearStatus.Open;
}
