using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Seed;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// Closure item 4 (Billing↔Finance integration template): proves
/// <see cref="FinanceChartOfAccountBackfillSeeder"/> reconciles every existing tenant, writes each
/// tenant's rows only through that tenant's own <see cref="ITenantScopedUnitOfWork"/> instance (never
/// cross-tenant), and is idempotent on repeat execution. No real database — every dependency is
/// substituted; <see cref="ITenantRepository"/>/<see cref="IClock"/> are resolved from a real, minimal
/// <see cref="IServiceProvider"/> since the seeder resolves them via <see cref="IServiceScopeFactory"/>
/// (matching how it resolves them against the live app's DI container), not injected directly.
/// </summary>
public class FinanceChartOfAccountBackfillSeederTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static (IServiceScopeFactory ScopeFactory, ITenantRepository Tenants) BuildScopeFactory(List<Tenant> tenants)
    {
        ITenantRepository tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(tenants);

        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        ServiceCollection services = new();
        services.AddSingleton(tenantRepository);
        services.AddSingleton(clock);
        IServiceProvider provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IServiceScopeFactory>(), tenantRepository);
    }

    private static (ITenantScopedUnitOfWork Uow, IChartOfAccountRepository ChartOfAccounts, IAccountMappingRepository AccountMappings) BuildTenantUow()
    {
        ITenantScopedUnitOfWork uow = Substitute.For<ITenantScopedUnitOfWork>();
        IChartOfAccountRepository chartOfAccounts = Substitute.For<IChartOfAccountRepository>();
        IAccountMappingRepository accountMappings = Substitute.For<IAccountMappingRepository>();
        uow.ChartOfAccounts.Returns(chartOfAccounts);
        uow.AccountMappings.Returns(accountMappings);
        return (uow, chartOfAccounts, accountMappings);
    }

    [Fact]
    public async Task Reconciles_Every_Tenant_Returned_By_The_Repository()
    {
        Tenant tenantA = Tenant.Provision("Tenant A", "tenant-a", Now);
        Tenant tenantB = Tenant.Provision("Tenant B", "tenant-b", Now);
        (IServiceScopeFactory scopeFactory, _) = BuildScopeFactory([tenantA, tenantB]);

        (ITenantScopedUnitOfWork uowA, _, _) = BuildTenantUow();
        (ITenantScopedUnitOfWork uowB, _, _) = BuildTenantUow();
        ITenantScopedUnitOfWorkFactory tenantUowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
        tenantUowFactory.Create(tenantA.Id.Value).Returns(uowA);
        tenantUowFactory.Create(tenantB.Id.Value).Returns(uowB);

        FinanceChartOfAccountBackfillSeeder seeder = new(scopeFactory, tenantUowFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        tenantUowFactory.Received(1).Create(tenantA.Id.Value);
        tenantUowFactory.Received(1).Create(tenantB.Id.Value);
        await uowA.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await uowB.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Never_Writes_One_Tenants_Accounts_Through_Another_Tenants_Unit_Of_Work()
    {
        Tenant tenantA = Tenant.Provision("Tenant A", "tenant-a", Now);
        Tenant tenantB = Tenant.Provision("Tenant B", "tenant-b", Now);
        (IServiceScopeFactory scopeFactory, _) = BuildScopeFactory([tenantA, tenantB]);

        (ITenantScopedUnitOfWork uowA, IChartOfAccountRepository chartOfAccountsA, IAccountMappingRepository accountMappingsA) = BuildTenantUow();
        (ITenantScopedUnitOfWork uowB, IChartOfAccountRepository chartOfAccountsB, IAccountMappingRepository accountMappingsB) = BuildTenantUow();
        ITenantScopedUnitOfWorkFactory tenantUowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
        tenantUowFactory.Create(tenantA.Id.Value).Returns(uowA);
        tenantUowFactory.Create(tenantB.Id.Value).Returns(uowB);

        List<ChartOfAccount> addedToA = [];
        chartOfAccountsA.Add(Arg.Do<ChartOfAccount>(a => addedToA.Add(a)));
        List<ChartOfAccount> addedToB = [];
        chartOfAccountsB.Add(Arg.Do<ChartOfAccount>(a => addedToB.Add(a)));

        FinanceChartOfAccountBackfillSeeder seeder = new(scopeFactory, tenantUowFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        addedToA.Should().OnlyContain(a => a.TenantId == tenantA.Id.Value);
        addedToB.Should().OnlyContain(a => a.TenantId == tenantB.Id.Value);
        addedToA.Should().NotBeEmpty();
        addedToB.Should().NotBeEmpty();
        // Neither tenant's ChartOfAccounts repository ever received an Add call for the other
        // tenant's rows — proves the per-tenant ITenantScopedUnitOfWork isolation actually holds, not
        // just that the resulting rows happen to carry the right TenantId value.
        chartOfAccountsA.Received(addedToA.Count).Add(Arg.Any<ChartOfAccount>());
        chartOfAccountsB.Received(addedToB.Count).Add(Arg.Any<ChartOfAccount>());
    }

    [Fact]
    public async Task Second_Run_Against_An_Already_Seeded_Tenant_Creates_Nothing_New()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now);
        (IServiceScopeFactory scopeFactory, _) = BuildScopeFactory([tenant]);

        (ITenantScopedUnitOfWork uow, IChartOfAccountRepository chartOfAccounts, IAccountMappingRepository accountMappings) = BuildTenantUow();
        // Simulate "already fully seeded": every system account code already exists, so
        // FinanceChartOfAccountSeeder's ExistsForCodeAsync/GetByRoleAsync checks find everything present.
        chartOfAccounts.ExistsForCodeAsync(tenant.Id.Value, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        string[] codes = ["1000", "1100", "2100", "2200", "3900", "4000", "4010", "4020", "4030", "4900"];
        List<ChartOfAccount> allAccounts = codes
            .Select(code => ChartOfAccount.Create(
                tenant.Id.Value, code, code, AccountCategory.Asset, LedgerDirectionForCode(code), null, isSystemAccount: true))
            .ToList();
        chartOfAccounts.GetAllForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(allAccounts);
        foreach (ChartOfAccount account in allAccounts)
        {
            accountMappings.GetByRoleAsync(tenant.Id.Value, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(AccountMapping.Create(tenant.Id.Value, "AnyRole", account.Id));
        }

        ITenantScopedUnitOfWorkFactory tenantUowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
        tenantUowFactory.Create(tenant.Id.Value).Returns(uow);

        FinanceChartOfAccountBackfillSeeder seeder = new(scopeFactory, tenantUowFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        chartOfAccounts.DidNotReceive().Add(Arg.Any<ChartOfAccount>());
        accountMappings.DidNotReceive().Add(Arg.Any<AccountMapping>());
    }

    private static MyCondo.Domain.Features.Payments.Ledger.LedgerDirection LedgerDirectionForCode(string code) =>
        code is "1000" or "1100" ? MyCondo.Domain.Features.Payments.Ledger.LedgerDirection.Debit
        : MyCondo.Domain.Features.Payments.Ledger.LedgerDirection.Credit;
}
