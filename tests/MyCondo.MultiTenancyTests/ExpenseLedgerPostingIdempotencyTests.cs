using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Template 3 closure item 1/2: proves — against a real, migrated PostgreSQL database (not mocks) —
/// exactly what makes a <see cref="LedgerPosting"/> idempotent. <see cref="LedgerPostingConfiguration"/>
/// (see its own doc comment) declares <c>ux_ledger_postings_tenant_id_reference_type_reference_id</c> as
/// a unique index on (TenantId, ReferenceType, ReferenceId) where <see cref="FinancialPostingRequest.PostingPurpose"/>
/// IS <see cref="LedgerPosting.ReferenceType"/> and <see cref="FinancialPostingRequest.SourceId"/> IS
/// <see cref="LedgerPosting.ReferenceId"/> (same columns, not a separate concept) — so the four distinct
/// Expense-lifecycle <c>PostingPurpose</c> values (ExpenseRecording/ExpensePayment/ExpenseVoid/
/// ExpensePaymentVoid) already carry distinct uniqueness identities without needing a schema change.
/// Also proves paid-expense void atomicity: <see cref="VoidExpenseCommandHandler"/> issues exactly one
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call after both reversal postings are staged, so a failure
/// on the second reversal leaves nothing committed. Requires a Docker daemon — see
/// <see cref="MultiTenancyPostgresFixture"/>'s doc comment.
/// </summary>
public class ExpenseLedgerPostingIdempotencyTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(NowUtc.UtcDateTime);

    private readonly MultiTenancyPostgresFixture _fixture;

    public ExpenseLedgerPostingIdempotencyTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static LedgerLine[] BalancedLines(LedgerAccountType debitRole, LedgerAccountType creditRole, decimal amount) =>
    [
        new LedgerLine(debitRole, null, LedgerDirection.Debit, amount, "line"),
        new LedgerLine(creditRole, null, LedgerDirection.Credit, amount, "line"),
    ];

    [Fact]
    public async Task All_Four_Expense_Lifecycle_Postings_Coexist_For_The_Same_Expense()
    {
        Guid tenantId = Guid.NewGuid();
        Guid expenseId = Guid.NewGuid();
        Guid recordingPostingId = Guid.NewGuid();
        Guid paymentPostingId = Guid.NewGuid();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        // Mirrors exactly what ApproveExpenseCommandHandler/PayExpenseCommandHandler/
        // VoidExpenseCommandHandler pass as PostingPurpose/SourceId: the two "forward" postings key off
        // Expense.Id, the two reversal postings key off the LedgerPostingId of the posting they reverse.
        (LedgerPosting recording, _) = LedgerPosting.Create(
            tenantId, Today, "Expense recording", "ExpenseRecording", expenseId,
            BalancedLines(LedgerAccountType.OperatingExpense, LedgerAccountType.AccountsPayable, 1000m), NowUtc);
        (LedgerPosting payment, _) = LedgerPosting.Create(
            tenantId, Today, "Expense payment", "ExpensePayment", expenseId,
            BalancedLines(LedgerAccountType.AccountsPayable, LedgerAccountType.CashOrBank, 1000m), NowUtc);
        (LedgerPosting voidReversal, _) = LedgerPosting.Create(
            tenantId, Today, "Expense void", "ExpenseVoid", recordingPostingId,
            BalancedLines(LedgerAccountType.AccountsPayable, LedgerAccountType.OperatingExpense, 1000m), NowUtc);
        (LedgerPosting paymentVoidReversal, _) = LedgerPosting.Create(
            tenantId, Today, "Payment void", "ExpensePaymentVoid", paymentPostingId,
            BalancedLines(LedgerAccountType.CashOrBank, LedgerAccountType.AccountsPayable, 1000m), NowUtc);

        db.Set<LedgerPosting>().AddRange(recording, payment, voidReversal, paymentVoidReversal);
        await db.SaveChangesAsync();

        List<LedgerPosting> persisted = await db.Set<LedgerPosting>()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync();

        persisted.Should().HaveCount(4);
        persisted.Select(p => p.ReferenceType).Should().BeEquivalentTo(
            ["ExpenseRecording", "ExpensePayment", "ExpenseVoid", "ExpensePaymentVoid"]);
    }

    [Fact]
    public async Task Retrying_The_Same_PostingPurpose_And_SourceId_Is_Rejected_By_The_Database_Unique_Index()
    {
        Guid tenantId = Guid.NewGuid();
        Guid expenseId = Guid.NewGuid();

        await using (MyCondoDbContext firstDb = _fixture.CreateDbContext(tenantId))
        {
            (LedgerPosting first, _) = LedgerPosting.Create(
                tenantId, Today, "Expense recording", "ExpenseRecording", expenseId,
                BalancedLines(LedgerAccountType.OperatingExpense, LedgerAccountType.AccountsPayable, 1000m), NowUtc);
            firstDb.Set<LedgerPosting>().Add(first);
            await firstDb.SaveChangesAsync();
        }

        // Simulates a duplicate/retried ApproveExpenseCommand landing after the first one already
        // committed: same TenantId + PostingPurpose ("ExpenseRecording") + SourceId (Expense.Id) — the
        // exact tuple the unique index is on. Deliberately bypasses FinancialPostingService's own
        // ExistsAsync pre-check (a separate, race-prone read) to prove the real backstop is the
        // database constraint itself, not just the application-level check.
        await using MyCondoDbContext retryDb = _fixture.CreateDbContext(tenantId);
        (LedgerPosting duplicate, _) = LedgerPosting.Create(
            tenantId, Today, "Expense recording (retry)", "ExpenseRecording", expenseId,
            BalancedLines(LedgerAccountType.OperatingExpense, LedgerAccountType.AccountsPayable, 1000m), NowUtc);
        retryDb.Set<LedgerPosting>().Add(duplicate);

        Func<Task> act = () => retryDb.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task FinancialPostingService_Rejects_A_Retried_Posting_For_The_Same_Purpose_And_Source_Against_The_Real_Database()
    {
        Guid tenantId = Guid.NewGuid();
        Guid expenseId = Guid.NewGuid();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        db.Set<AccountMapping>().AddRange(
            AccountMapping.Create(tenantId, "OperatingExpense", ChartOfAccountId.New()),
            AccountMapping.Create(tenantId, "AccountsPayable", ChartOfAccountId.New()));
        await db.SaveChangesAsync();

        FinancialPostingService service = new(
            new AccountMappingRepository(db), new ChartOfAccountRepository(db), new AccountingPeriodRepository(db),
            new LedgerPostingRepository(db), new LedgerEntryRepository(db), new FixedClock(NowUtc),
            NullLogger<FinancialPostingService>.Instance);

        FinancialPostingRequest request = new(
            tenantId, Today, "Expense: Elevator maintenance", "ExpenseRecording", expenseId,
            [
                new FinancialPostingLine(LedgerAccountType.OperatingExpense, null, LedgerDirection.Debit, 1000m),
                new FinancialPostingLine(LedgerAccountType.AccountsPayable, null, LedgerDirection.Credit, 1000m),
            ]);

        await service.PostAsync(request, CancellationToken.None);
        await db.SaveChangesAsync();

        // Same tenant, same PostingPurpose, same SourceId — the exact shape of a duplicate/retried
        // ApproveExpenseCommand call reaching the posting service a second time.
        Func<Task> act = () => service.PostAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        long count = await db.Set<LedgerPosting>()
            .CountAsync(x => x.TenantId == tenantId && x.ReferenceType == "ExpenseRecording" && x.ReferenceId == expenseId);
        count.Should().Be(1, "the retried call must not create a second posting");
    }

    [Fact]
    public async Task VoidExpense_On_A_Paid_Expense_Persists_Both_Reversals_And_The_Voided_Status_Together()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        ExpenseTypeId expenseTypeId = ExpenseTypeId.New();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        SeedAccountMappings(db, tenantId);

        Expense expense = Expense.Record(
            tenantId, buildingId, expenseTypeId, fundId: null, Today, accountingDate: null, "Cleaning",
            null, null, 1000m, isPaid: false, PaymentMethod.Cash, null, NowUtc);
        expense.MarkPosted(LedgerPostingId.New(), null, NowUtc);
        expense.MarkPaid(LedgerPostingId.New(), ChartOfAccountId.New(), NowUtc);
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync();

        VoidExpenseCommandHandler handler = BuildHandler(db, tenantId);
        await handler.Handle(new VoidExpenseCommand(expense.Id.Value, "Duplicate entry"), CancellationToken.None);

        await using MyCondoDbContext verifyDb = _fixture.CreateDbContext(tenantId);
        Expense reloaded = await verifyDb.Set<Expense>().SingleAsync(x => x.Id == expense.Id);
        reloaded.Status.Should().Be(ExpenseStatus.Voided);
        reloaded.ReversalPostingId.Should().NotBeNull();
        reloaded.PaymentReversalPostingId.Should().NotBeNull();

        List<LedgerPosting> reversals = await verifyDb.Set<LedgerPosting>()
            .Where(x => x.TenantId == tenantId && (x.ReferenceType == "ExpenseVoid" || x.ReferenceType == "ExpensePaymentVoid"))
            .ToListAsync();
        reversals.Should().HaveCount(2, "both the primary reversal and the payment reversal must persist");
    }

    [Fact]
    public async Task VoidExpense_On_A_Paid_Expense_Rolls_Back_Entirely_When_The_Second_Reversal_Fails()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        ExpenseTypeId expenseTypeId = ExpenseTypeId.New();
        LedgerPostingId recordingPostingId = LedgerPostingId.New();
        LedgerPostingId paymentPostingId = LedgerPostingId.New();

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);
        SeedAccountMappings(seedDb, tenantId);

        Expense expense = Expense.Record(
            tenantId, buildingId, expenseTypeId, fundId: null, Today, accountingDate: null, "Cleaning",
            null, null, 1000m, isPaid: false, PaymentMethod.Cash, null, NowUtc);
        expense.MarkPosted(recordingPostingId, null, NowUtc);
        expense.MarkPaid(paymentPostingId, ChartOfAccountId.New(), NowUtc);
        seedDb.Set<Expense>().Add(expense);

        // Force the SECOND posting attempt inside VoidExpenseCommandHandler (purpose "ExpenseVoid",
        // SourceId = recordingPostingId) to fail, while leaving the FIRST attempt (purpose
        // "ExpensePaymentVoid", SourceId = paymentPostingId) free to succeed at the application-level
        // check — this simulates a genuine mid-operation failure landing strictly between the two
        // postings, not on the first one.
        (LedgerPosting conflicting, _) = LedgerPosting.Create(
            tenantId, Today, "Pre-existing conflicting reversal", "ExpenseVoid", recordingPostingId.Value,
            BalancedLines(LedgerAccountType.AccountsPayable, LedgerAccountType.OperatingExpense, 1000m), NowUtc);
        seedDb.Set<LedgerPosting>().Add(conflicting);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        VoidExpenseCommandHandler handler = BuildHandler(db, tenantId);

        Func<Task> act = () => handler.Handle(new VoidExpenseCommand(expense.Id.Value, "Correction"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();

        await using MyCondoDbContext verifyDb = _fixture.CreateDbContext(tenantId);
        Expense reloaded = await verifyDb.Set<Expense>().SingleAsync(x => x.Id == expense.Id);
        reloaded.Status.Should().Be(ExpenseStatus.Paid, "the status flip must not commit when the second reversal fails");
        reloaded.PaymentReversalPostingId.Should().BeNull();

        long paymentVoidCount = await verifyDb.Set<LedgerPosting>()
            .CountAsync(x => x.TenantId == tenantId && x.ReferenceType == "ExpensePaymentVoid" && x.ReferenceId == paymentPostingId.Value);
        paymentVoidCount.Should().Be(0, "the first reversal must not have been left partially committed");

        long expenseVoidCount = await verifyDb.Set<LedgerPosting>()
            .CountAsync(x => x.TenantId == tenantId && x.ReferenceType == "ExpenseVoid" && x.ReferenceId == recordingPostingId.Value);
        expenseVoidCount.Should().Be(1, "only the pre-existing seeded row — the handler's own attempt must not have duplicated it");
    }

    private static void SeedAccountMappings(MyCondoDbContext db, Guid tenantId) =>
        db.Set<AccountMapping>().AddRange(
            AccountMapping.Create(tenantId, "OperatingExpense", ChartOfAccountId.New()),
            AccountMapping.Create(tenantId, "AccountsPayable", ChartOfAccountId.New()),
            AccountMapping.Create(tenantId, "CashOrBank", ChartOfAccountId.New()));

    private static VoidExpenseCommandHandler BuildHandler(MyCondoDbContext db, Guid tenantId)
    {
        IFinancialPostingService financialPosting = new FinancialPostingService(
            new AccountMappingRepository(db), new ChartOfAccountRepository(db), new AccountingPeriodRepository(db),
            new LedgerPostingRepository(db), new LedgerEntryRepository(db), new FixedClock(NowUtc),
            NullLogger<FinancialPostingService>.Instance);

        return new VoidExpenseCommandHandler(
            new ExpenseRepository(db), financialPosting, new FinanceAuditLogRepository(db), db,
            new FixedCurrentUser(tenantId), new FixedClock(NowUtc),
            NullLogger<VoidExpenseCommandHandler>.Instance);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FixedCurrentUser(Guid tenantId) : ICurrentUserProvider
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? TenantId => tenantId;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
        public bool HasPermission(string permission) => true;
        public bool HasPermissionForBuilding(string permission, Guid? buildingId) => true;
        public IReadOnlyList<Guid> BuildingIds => [];
    }
}
