using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Settings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.Integrity;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FinancialYears;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Payments.Idempotency;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.StaffMembers;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformRefreshTokens;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Guests;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;
using MyCondo.Domain.Features.Security.SebaVisits;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;
using MyCondo.Domain.Features.Security.Vehicles;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.Readings;
using MyCondo.Infrastructure.Identity;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using MyCondo.Infrastructure.Persistence.Repositories;
using MyCondo.Infrastructure.Storage;
using MyCondo.Infrastructure.Time;
using StackExchange.Redis;

namespace MyCondo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain abstractions
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidV7IdGenerator>();

        // Identity (validated settings)
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<Argon2Settings>()
            .BindConfiguration(Argon2Settings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StorageSettings>()
            .BindConfiguration(StorageSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<IFileStorageService, LocalDiskFileStorageService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserContextResolver, UserContextResolver>();
        services.AddScoped<IPlatformTokenService, PlatformJwtTokenService>();
        services.AddScoped<IPlatformUserContextResolver, PlatformUserContextResolver>();

        // EF Core interceptors
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<TenantContextConnectionInterceptor>();
        services.AddScoped<ITenantScopedUnitOfWorkFactory, TenantScopedUnitOfWorkFactory>();

        // DbContext
        services.AddDbContext<MyCondoDbContext>((sp, options) =>
        {
            string? connectionString = configuration.GetConnectionString("Default")
                ?? configuration["MYCONDO_DB_CONNECTION_STRING"];

            options.UseNpgsql(connectionString, npg =>
                {
                    npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public");
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<DispatchDomainEventsInterceptor>(),
                    sp.GetRequiredService<TenantContextConnectionInterceptor>());

            if (string.Equals(
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Development",
                    StringComparison.Ordinal))
            {
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyCondoDbContext>());

        // Development-only seeders — cheap, inert classes; DatabaseSeederExtensions.SeedDatabaseAsync
        // is the only thing that actually calls them, gated on IHostEnvironment.IsDevelopment(). See
        // that class for why they're registered unconditionally here rather than conditionally.
        services.AddScoped<Seed.PlatformBootstrapSeeder>();
        services.AddScoped<Seed.ArpDevelopmentBootstrapSeeder>();
        services.AddScoped<Seed.DevelopmentTenantSeeder>();

        // Every-environment seeders (not Development-only — see each one's own doc comment for why).
        services.AddScoped<Seed.FinanceChartOfAccountBackfillSeeder>();
        services.AddScoped<Seed.TenantRoleCatalogueBackfillSeeder>();
        services.AddScoped<Seed.ExpenseCategoryCatalogueBackfillSeeder>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantModuleRepository, TenantModuleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IPlatformRoleRepository, PlatformRoleRepository>();
        services.AddScoped<IPlatformRolePermissionRepository, PlatformRolePermissionRepository>();
        services.AddScoped<IPlatformUserRoleAssignmentRepository, PlatformUserRoleAssignmentRepository>();
        services.AddScoped<IPlatformRefreshTokenRepository, PlatformRefreshTokenRepository>();
        services.AddScoped<IPlatformAuditLogRepository, PlatformAuditLogRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IFlatRepository, FlatRepository>();
        services.AddScoped<IFlatOwnershipRepository, FlatOwnershipRepository>();
        services.AddScoped<IGateRepository, GateRepository>();
        services.AddScoped<IResidentRepository, ResidentRepository>();
        services.AddScoped<IResidentHouseholdMemberRepository, ResidentHouseholdMemberRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<IExpenseTypeRepository, ExpenseTypeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IGuestProfileRepository, GuestProfileRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IAccessSessionRepository, AccessSessionRepository>();
        services.AddScoped<IDomesticWorkerProfileRepository, DomesticWorkerProfileRepository>();
        services.AddScoped<IDomesticWorkerAssignmentRepository, DomesticWorkerAssignmentRepository>();
        services.AddScoped<IServiceProviderProfileRepository, ServiceProviderProfileRepository>();
        services.AddScoped<IServiceProviderAssignmentRepository, ServiceProviderAssignmentRepository>();
        services.AddScoped<IStaffMemberRepository, StaffMemberRepository>();
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<ISebaVisitDetailRepository, SebaVisitDetailRepository>();
        services.AddScoped<IParcelRepository, ParcelRepository>();
        services.AddScoped<IParcelCustodyEventRepository, ParcelCustodyEventRepository>();
        services.AddScoped<ILedgerPostingRepository, LedgerPostingRepository>();
        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
        services.AddScoped<IChartOfAccountRepository, ChartOfAccountRepository>();
        services.AddScoped<IAccountMappingRepository, AccountMappingRepository>();
        services.AddScoped<IFundRepository, FundRepository>();
        services.AddScoped<IFinancialYearRepository, FinancialYearRepository>();
        services.AddScoped<IAccountingPeriodRepository, AccountingPeriodRepository>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<IFixedDepositRepository, FixedDepositRepository>();
        services.AddScoped<IFixedDepositInterestAccrualRepository, FixedDepositInterestAccrualRepository>();
        services.AddScoped<IFixedDepositInterestReceiptRepository, FixedDepositInterestReceiptRepository>();
        services.AddScoped<IFinanceReportRepository, FinanceReportRepository>();
        services.AddScoped<IFinanceAuditLogRepository, FinanceAuditLogRepository>();
        services.AddScoped<IBankReconciliationRepository, BankReconciliationRepository>();
        services.AddScoped<IBankStatementLineRepository, BankStatementLineRepository>();
        services.AddScoped<IFinanceIntegrityRepository, FinanceIntegrityRepository>();
        services.AddScoped<IResidentAccountRepository, ResidentAccountRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IIdempotencyKeyRepository, IdempotencyKeyRepository>();
        services.AddScoped<IServiceChargeRuleRepository, ServiceChargeRuleRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceSequenceRepository, InvoiceSequenceRepository>();
        services.AddScoped<IPaymentAllocationRepository, PaymentAllocationRepository>();
        services.AddScoped<IMeterRepository, MeterRepository>();
        services.AddScoped<IMeterAssignmentRepository, MeterAssignmentRepository>();
        services.AddScoped<IRatePlanRepository, RatePlanRepository>();
        services.AddScoped<IReadingRepository, ReadingRepository>();
        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<IBlackoutDateRepository, BlackoutDateRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPoolSessionRepository, PoolSessionRepository>();
        services.AddScoped<IPoolIncidentRepository, PoolIncidentRepository>();
        services.AddScoped<IGeneratorRepository, GeneratorRepository>();
        services.AddScoped<IGeneratorSessionRepository, GeneratorSessionRepository>();
        services.AddScoped<IGeneratorFuelReceiptRepository, GeneratorFuelReceiptRepository>();
        services.AddScoped<IGeneratorMaintenanceScheduleRepository, GeneratorMaintenanceScheduleRepository>();
        services.AddScoped<IGeneratorServiceRecordRepository, GeneratorServiceRecordRepository>();
        services.AddScoped<IGeneratorBreakdownRecordRepository, GeneratorBreakdownRecordRepository>();
        services.AddScoped<IGasCylinderSupplierRepository, GasCylinderSupplierRepository>();
        services.AddScoped<ICylinderPurchaseRepository, CylinderPurchaseRepository>();
        services.AddScoped<ICylinderStockMovementRepository, CylinderStockMovementRepository>();
        services.AddScoped<IMonthlyCylinderReconciliationRepository, MonthlyCylinderReconciliationRepository>();
        services.AddScoped<IOccupancyRegistrationRepository, OccupancyRegistrationRepository>();
        services.AddScoped<IHouseholdMemberRepository, HouseholdMemberRepository>();
        services.AddScoped<IOccupancyRegistrationStatusHistoryRepository, OccupancyRegistrationStatusHistoryRepository>();
        services.AddScoped<IOccupancyRegistrationWorkerAssignmentRepository, OccupancyRegistrationWorkerAssignmentRepository>();
        services.AddScoped<IOccupancyRegistrationVehicleAssignmentRepository, OccupancyRegistrationVehicleAssignmentRepository>();

        // Redis (lazy singleton)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string connectionString = configuration.GetConnectionString("Redis")
                ?? configuration["MYCONDO_REDIS_CONNECTION_STRING"]
                ?? "localhost:6379";

            return ConnectionMultiplexer.Connect(connectionString);
        });

        return services;
    }
}
