using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Proves the Financial Integrity Dashboard's EF LINQ queries actually translate and execute against
/// real PostgreSQL — a unit test against mocked repositories cannot catch a translation failure the way
/// a real query execution can (this is exactly how the interest-accrual check's original grouped-join
/// implementation was caught and rewritten — see <c>FinanceIntegrityRepository</c>'s own comment). The
/// unbalanced-posting "detects a real problem" case seeds a corrupt row via raw SQL, deliberately
/// bypassing <see cref="LedgerPosting.Create"/>'s in-memory balance invariant — exactly the kind of
/// defect (a bad migration, a manual DB edit) this dashboard exists to catch. Requires a Docker daemon —
/// see <see cref="MultiTenancyPostgresFixture"/>'s doc comment.
/// </summary>
public class FinanceIntegrityRepositoryTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public FinanceIntegrityRepositoryTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Empty_Tenant_Reports_Every_Count_Zero()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinanceIntegrityRepository repository = new(db);

        (await repository.CountUnbalancedPostingsAsync(tenantId, CancellationToken.None)).Should().Be(0);
        (await repository.CountDuplicateLogicalPostingsAsync(tenantId, CancellationToken.None)).Should().Be(0);
        (await repository.CountClosedPeriodViolationsAsync(tenantId, CancellationToken.None)).Should().Be(0);
        (await repository.CountStaleUnreconciledBankItemsAsync(tenantId, 45, CancellationToken.None)).Should().Be(0);
        (await repository.CountStaleUnreceivedInterestAccrualsAsync(tenantId, 45, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task A_Balanced_Posting_Is_Not_Flagged_As_Unbalanced()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 500m, "line"),
            new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 500m, "line"),
        ];
        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, DateOnly.FromDateTime(DateTime.UtcNow), "Balanced", "Payment", Guid.NewGuid(), lines, DateTimeOffset.UtcNow);
        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);
        await db.SaveChangesAsync();

        FinanceIntegrityRepository repository = new(db);
        (await repository.CountUnbalancedPostingsAsync(tenantId, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task A_Posting_Whose_Entries_Were_Corrupted_Directly_In_The_Database_Is_Flagged_As_Unbalanced()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 500m, "line"),
            new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 500m, "line"),
        ];
        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, DateOnly.FromDateTime(DateTime.UtcNow), "Corrupted", "Payment", Guid.NewGuid(), lines, DateTimeOffset.UtcNow);
        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);
        await db.SaveChangesAsync();

        // Bypass every domain invariant on purpose — simulates a bad migration/manual edit corrupting an
        // already-posted entry's amount, which LedgerPosting.Create can never allow through the normal API.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE payments.ledger_entries SET amount = 999 WHERE posting_id = {posting.Id.Value} AND direction = 'Debit'");

        FinanceIntegrityRepository repository = new(db);
        (await repository.CountUnbalancedPostingsAsync(tenantId, CancellationToken.None)).Should().Be(1);
    }

    // No "two postings sharing a reference" test: ux_ledger_postings_tenant_id_reference_type_reference_id
    // (ADR-027) actively rejects that at the database level for any row created against this schema —
    // confirmed by attempting exactly this scenario, which raises Postgres error 23505 rather than
    // silently succeeding. That is the desired outcome (the constraint is doing its job) and also means
    // this specific duplicate cannot be simulated against a fully-migrated schema without dropping the
    // constraint, which is disproportionate for a test. CountDuplicateLogicalPostingsAsync's practical
    // value is the narrow historical-data-predating-the-index scenario the Deferred Verification
    // Register (item 2) already documents as a distinct, separately-tracked risk — the query shape
    // itself (a GroupBy + count filter, no join) carries materially lower EF-translation risk than
    // CountStaleUnreceivedInterestAccrualsAsync's left join, which the tests above do exercise for real.
}
