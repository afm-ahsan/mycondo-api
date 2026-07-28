namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real cross-tenant isolation tests belong here once RLS policies exist (see
/// mycondo-docs/02-architecture/Architecture_Decision_Register.md ADR-009 and
/// mycondo-docs/07-delivery/MASTER_BACKLOG.md MT-2/MT-4). As of Wave 0.5, no migration creates any
/// RLS policy — only the `tenant_id` column convention and the `app.current_tenant_id` session
/// variable setter exist. A test asserting cross-tenant access "fails" would currently pass for the
/// wrong reason (no policy to test) or require Testcontainers.PostgreSql, which isn't wired up yet.
///
/// This placeholder is deliberately marked Skip, with a reason, rather than either an empty pass or
/// an invented assertion — so CI output honestly shows "not yet implemented" instead of looking like
/// real coverage.
/// </summary>
public class CrossTenantIsolationTests
{
    [Fact(Skip = "Blocked on RLS policies (ADR-009) and Testcontainers.PostgreSql wiring (MT-2/MT-4) — Wave 1 scope.")]
    public void TenantA_Cannot_Read_TenantB_Rows()
    {
    }
}
