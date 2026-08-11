using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.Expenses.Exceptions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Expenses.Expenses;

/// <summary>
/// A recorded operational expense for a Building. Reuses <see cref="Payments.Payments.PaymentMethod"/>
/// rather than introducing a duplicate enum (CLAUDE.md's migration rules: "do not introduce duplicate
/// concepts already represented by existing entities"). See <see cref="ExpenseStatus"/> for the
/// deliberately minimal lifecycle.
/// </summary>
public sealed class Expense : AggregateRoot<ExpenseId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public ExpenseTypeId ExpenseTypeId { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public string Description { get; private set; }
    public string? Payee { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? Notes { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public string? VoidReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Expense()
    {
        Description = null!;
    }

    private Expense(
        ExpenseId id,
        Guid tenantId,
        BuildingId buildingId,
        ExpenseTypeId expenseTypeId,
        DateOnly expenseDate,
        string description,
        string? payee,
        string? referenceNumber,
        decimal amount,
        PaymentMethod paymentMethod,
        string? notes,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        ExpenseTypeId = expenseTypeId;
        ExpenseDate = expenseDate;
        Description = description;
        Payee = payee;
        ReferenceNumber = referenceNumber;
        Amount = amount;
        PaymentMethod = paymentMethod;
        Notes = notes;
        Status = ExpenseStatus.Recorded;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Expense Record(
        Guid tenantId,
        BuildingId buildingId,
        ExpenseTypeId expenseTypeId,
        DateOnly expenseDate,
        string description,
        string? payee,
        string? referenceNumber,
        decimal amount,
        PaymentMethod paymentMethod,
        string? notes,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        return new Expense(
            ExpenseId.New(), tenantId, buildingId, expenseTypeId, expenseDate, description.Trim(),
            payee?.Trim(), referenceNumber?.Trim(), amount, paymentMethod, notes?.Trim(), nowUtc);
    }

    /// <summary>Full edit — only while <see cref="ExpenseStatus.Recorded"/>. Once
    /// <see cref="ExpenseStatus.Voided"/>, the record is frozen; correct a mistake by voiding and
    /// recording a new one, never by editing a voided row back to "fix" it.</summary>
    public void Update(
        BuildingId buildingId,
        ExpenseTypeId expenseTypeId,
        DateOnly expenseDate,
        string description,
        string? payee,
        string? referenceNumber,
        decimal amount,
        PaymentMethod paymentMethod,
        string? notes,
        DateTimeOffset nowUtc)
    {
        if (Status == ExpenseStatus.Voided)
        {
            throw new ExpenseAlreadyVoidedException(Id);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        BuildingId = buildingId;
        ExpenseTypeId = expenseTypeId;
        ExpenseDate = expenseDate;
        Description = description.Trim();
        Payee = payee?.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        Amount = amount;
        PaymentMethod = paymentMethod;
        Notes = notes?.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Void(string reason, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == ExpenseStatus.Voided)
        {
            return;
        }

        Status = ExpenseStatus.Voided;
        VoidReason = reason.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
