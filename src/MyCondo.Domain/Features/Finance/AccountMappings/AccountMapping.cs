using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Domain.Features.Finance.AccountMappings;

/// <summary>
/// The posting matrix: resolves a semantic <see cref="PostingRole"/> (today, one of the 6
/// <c>LedgerAccountType</c> names — see ADR-027) to the tenant's actual <see cref="ChartOfAccountId"/>.
/// The Application-layer centralized posting service is the only caller — handlers never hard-code an
/// account id, they name the role they're posting to and let this mapping resolve it. Stored as a plain
/// string key (not the enum type) so future posting roles beyond the original 6 don't require a
/// schema/enum change, only a new seeded row.
/// </summary>
public sealed class AccountMapping : AggregateRoot<AccountMappingId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string PostingRole { get; private set; }
    public ChartOfAccountId ChartOfAccountId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private AccountMapping()
    {
        PostingRole = null!;
    }

    private AccountMapping(AccountMappingId id, Guid tenantId, string postingRole, ChartOfAccountId chartOfAccountId)
        : base(id)
    {
        TenantId = tenantId;
        PostingRole = postingRole;
        ChartOfAccountId = chartOfAccountId;
    }

    public static AccountMapping Create(Guid tenantId, string postingRole, ChartOfAccountId chartOfAccountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postingRole);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AccountMapping(AccountMappingId.New(), tenantId, postingRole.Trim(), chartOfAccountId);
    }

    public void Remap(ChartOfAccountId chartOfAccountId) => ChartOfAccountId = chartOfAccountId;
}
