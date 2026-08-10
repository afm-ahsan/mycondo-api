namespace MyCondo.Application.Common.Authorization;

/// <summary>
/// The single source of truth for the global permission catalogue — every entry here should exist as a
/// row in <c>identity.permissions</c>. Consolidated from the 14 historical EF Core migrations that used
/// to seed this table directly (<c>Seed_Permission_Catalogue</c>, <c>Seed_Security_Permissions</c>,
/// <c>Seed_DomesticWorker_ServiceProvider_StaffAttendance_SebaVisitor_Permissions</c>,
/// <c>Seed_Parcel_Permissions</c>, <c>Seed_Payments_Permissions</c>, <c>Seed_Billing_Permissions</c>,
/// <c>Seed_Utility_Permissions</c>, <c>Seed_Facility_Pool_Permissions</c>,
/// <c>Seed_Operations_Permissions</c>, <c>Seed_Leasing_Permissions</c>,
/// <c>Seed_Leasing_SecurityViewPermission</c>, <c>Seed_UtilityReport_Permission</c>,
/// <c>Seed_Platform_Permission_Catalogue</c>, <c>Seed_Invoice_View_Own_Permission</c> — preserved as
/// historical record, not re-run for new environments). <see cref="MyCondo.Application.Common.Services.PermissionSeeder"/>
/// reconciles this catalogue against the database by <c>Name</c> going forward; new permissions are
/// added here, never via a new migration.
/// </summary>
public static class PermissionCatalogue
{
    public static readonly (string Name, string Description, string Module, bool IsBuildingScopable)[] Entries =
    [
        // Seed_Permission_Catalogue (original 47-permission MVP catalogue)
        ("tenant.view", "View tenant details", "tenant", false),
        ("tenant.manage", "Create, activate, and suspend tenants", "tenant", false),
        ("user.view", "View users", "user", false),
        ("user.create", "Create users", "user", false),
        ("user.update", "Update user details", "user", false),
        ("user.disable", "Disable user accounts", "user", false),
        ("property.view", "View property hierarchy", "property", true),
        ("property.create", "Create properties/buildings/units", "property", true),
        ("property.update", "Update property hierarchy", "property", true),
        ("property.delete", "Delete property hierarchy entries", "property", true),
        ("resident.view", "View residents", "resident", true),
        ("resident.create", "Create resident records", "resident", true),
        ("resident.update", "Update resident records", "resident", true),
        ("resident.disable", "Disable resident records", "resident", true),
        ("ownership.view", "View ownership records", "ownership", true),
        ("ownership.manage", "Create and update ownership records", "ownership", true),
        ("lease.view", "View leases", "lease", true),
        ("lease.manage", "Create and update leases", "lease", true),
        ("billing.rule.view", "View service-charge rules", "billing", true),
        ("billing.rule.manage", "Create and update service-charge rules", "billing", true),
        ("billing.generate", "Run billing generation batches", "billing", true),
        ("invoice.view", "View invoices", "invoice", true),
        ("invoice.void", "Void invoices", "invoice", true),
        ("payment.view", "View payments", "payment", true),
        ("payment.record", "Record payments", "payment", true),
        ("payment.reverse", "Reverse payments", "payment", true),
        ("expense.view", "View expenses", "expense", true),
        ("expense.manage", "Create and update expenses", "expense", true),
        ("complaint.view", "View complaints", "complaint", true),
        ("complaint.create", "Create complaints", "complaint", true),
        ("complaint.assign", "Assign complaints to staff", "complaint", true),
        ("complaint.manage", "Manage complaint lifecycle", "complaint", true),
        ("workorder.view", "View work orders", "workorder", true),
        ("workorder.create", "Create work orders", "workorder", true),
        ("workorder.assign", "Assign work orders to staff", "workorder", true),
        ("workorder.complete", "Mark work orders complete", "workorder", true),
        ("notification.view", "View notifications", "notification", false),
        ("notification.manage", "Manage notification templates and dispatch", "notification", false),
        ("document.view", "View documents", "document", false),
        ("document.upload", "Upload documents", "document", false),
        ("document.delete", "Delete documents", "document", false),
        ("report.financial.view", "View financial reports", "report", false),
        ("report.operational.view", "View operational reports", "report", false),
        ("role.view", "View roles", "role", false),
        ("role.manage", "Create roles and manage grants/assignments", "role", false),
        ("permission.view", "View the permission catalogue", "permission", false),
        ("audit.view", "View audit records", "audit", false),

        // Seed_Security_Permissions (Slice B — Guest + Vehicle access)
        ("visitor.view", "View guest profiles and visit history", "visitor", true),
        ("visitor.create", "Create guest profiles", "visitor", true),
        ("visitor.checkin", "Check guests in", "visitor", true),
        ("visitor.checkout", "Check guests out", "visitor", true),
        ("visitor.override", "Override a blocked guest's entry", "visitor", true),
        ("visitor.block.manage", "Block and unblock guest profiles", "visitor", true),
        ("vehicle.view", "View vehicles and trip history", "vehicle", true),
        ("vehicle.create", "Register vehicles", "vehicle", true),
        ("vehicle.checkin", "Check vehicles in", "vehicle", true),
        ("vehicle.checkout", "Check vehicles out", "vehicle", true),
        ("vehicle.override", "Override a blocked vehicle's entry", "vehicle", true),
        ("vehicle.block.manage", "Block and unblock vehicles", "vehicle", true),
        ("report.security.view", "View security reports (currently inside, gate activity)", "report", false),

        // Seed_DomesticWorker_ServiceProvider_StaffAttendance_SebaVisitor_Permissions (Slice B2)
        ("domesticworker.view", "View domestic worker profiles and assignments", "domesticworker", true),
        ("domesticworker.manage", "Register and manage domestic worker profiles (status, verification)", "domesticworker", true),
        ("domesticworker.assignment.manage", "Create, approve, and deactivate domestic worker flat assignments", "domesticworker", true),
        ("domesticworker.checkin", "Check domestic workers in", "domesticworker", true),
        ("domesticworker.checkout", "Check domestic workers out", "domesticworker", true),
        ("domesticworker.override", "Override a blocked/suspended/out-of-schedule domestic worker's entry", "domesticworker", true),
        ("serviceprovider.view", "View service provider profiles and assignments", "serviceprovider", true),
        ("serviceprovider.manage", "Register and manage service provider profiles (status, verification)", "serviceprovider", true),
        ("serviceprovider.assignment.manage", "Create, approve, and deactivate service provider flat assignments", "serviceprovider", true),
        ("serviceprovider.checkin", "Check service providers in", "serviceprovider", true),
        ("serviceprovider.checkout", "Check service providers out", "serviceprovider", true),
        ("serviceprovider.override", "Override a blocked/suspended/out-of-schedule service provider's entry", "serviceprovider", true),
        ("staffattendance.view", "View staff members and attendance records", "staffattendance", true),
        ("staffattendance.manage", "Register staff members and record clock-in/clock-out", "staffattendance", true),
        ("staffattendance.correct", "Request a correction to an attendance record", "staffattendance", true),
        ("staffattendance.approve", "Approve a requested attendance correction", "staffattendance", true),
        ("sebavisitor.view", "View Seba office visits", "sebavisitor", true),
        ("sebavisitor.manage", "Check Seba office visitors in", "sebavisitor", true),
        ("sebavisitor.checkout", "Check Seba office visitors out and record outcome", "sebavisitor", true),

        // Seed_Parcel_Permissions (Slice C)
        ("parcel.view", "View parcels and custody history", "parcel", true),
        ("parcel.receive", "Receive parcels at the front desk", "parcel", true),
        ("parcel.update", "Update parcel details (e.g. mark damaged)", "parcel", true),
        ("parcel.notify", "Notify a resident their parcel has arrived", "parcel", true),
        ("parcel.handover", "Hand a parcel over to its collector", "parcel", true),
        ("parcel.return", "Return or reject a parcel", "parcel", true),
        ("parcel.escalate", "Escalate a parcel as lost", "parcel", true),

        // Seed_Payments_Permissions (Slice D)
        ("residentaccount.view", "View resident account details and ledger history", "residentaccount", true),
        ("residentaccount.manage", "Open resident accounts and record opening balances", "residentaccount", true),

        // Seed_Billing_Permissions (Slice E)
        ("billing.invoice.view", "View invoices and invoice lines", "billing", true),
        ("billing.invoice.generate", "Run invoice batch generation", "billing", true),
        ("billing.invoice.void", "Void an unpaid invoice", "billing", true),

        // Seed_Utility_Permissions (Slice F)
        ("utility.meter.view", "View electricity/gas meters", "utility", true),
        ("utility.meter.manage", "Install, assign, and manage meters", "utility", true),
        ("utility.rateplan.view", "View utility rate plans", "utility", true),
        ("utility.rateplan.manage", "Create and end utility rate plans", "utility", true),
        ("utility.reading.view", "View meter readings and consumption history", "utility", true),
        ("utility.reading.record", "Record and review meter readings", "utility", true),
        ("utility.reading.finalize", "Finalize and bill a meter reading", "utility", true),
        ("utility.reading.correct", "Correct a finalized or billed meter reading", "utility", true),

        // Seed_Facility_Pool_Permissions (Slice G)
        ("facility.view", "View facilities", "facility", true),
        ("facility.manage", "Create and configure facilities and blackout dates", "facility", true),
        ("facility.booking.view", "View facility bookings", "facility", true),
        ("facility.booking.create", "Request a facility booking", "facility", true),
        ("facility.booking.approve", "Approve or reject a pending facility booking", "facility", true),
        ("facility.booking.cancel", "Cancel a facility booking or mark it no-show", "facility", true),
        ("facility.booking.refund", "Settle a facility booking's deposit after inspection", "facility", true),
        ("facility.booking.inspect", "Check in, complete, and inspect a facility booking", "facility", true),
        ("pool.view", "View swimming pool sessions and incidents", "pool", true),
        ("pool.manage", "Configure swimming pool facility settings", "pool", true),
        ("pool.checkin", "Check a person into the swimming pool", "pool", true),
        ("pool.checkout", "Check a person out of the swimming pool", "pool", true),
        ("pool.override", "Bypass swimming pool capacity or eligibility rules with an override reason", "pool", true),
        ("pool.incident.manage", "Record a swimming pool incident", "pool", true),
        ("report.facility", "View facility utilization and booking revenue reports", "report", false),

        // Seed_Operations_Permissions (Slice H)
        ("generator.view", "View generators, sessions, fuel, and maintenance records", "generator", true),
        ("generator.manage", "Create, edit, and deactivate generator assets", "generator", true),
        ("generator.operation.manage", "Start and stop a generator runtime session", "generator", true),
        ("generator.fuel.manage", "Record generator fuel receipts and reconciliation", "generator", true),
        ("generator.maintenance.manage", "Manage generator maintenance schedules, service records, and breakdowns", "generator", true),
        ("generator.report", "View generator runtime, fuel, and maintenance reports", "generator", false),
        ("gascylinder.view", "View gas cylinder suppliers, purchases, and stock", "gascylinder", true),
        ("gascylinder.purchase.manage", "Record and manage gas cylinder suppliers and purchases", "gascylinder", true),
        ("gascylinder.stock.manage", "Record gas cylinder stock receipts, issues, empty returns, and reconciliations", "gascylinder", true),
        ("gascylinder.approve", "Approve or reject a gas cylinder purchase, or authorize a controlled stock adjustment", "gascylinder", true),
        ("gascylinder.report", "View gas cylinder purchase, consumption, and supplier comparison reports", "gascylinder", false),

        // Seed_Leasing_Permissions (Tenant/Occupancy Registration)
        ("occupancy-registration.view", "View tenant registrations, household members, and their status history", "occupancy-registration", true),
        ("occupancy-registration.create", "Create and edit a draft tenant registration, manage household members, and submit for review", "occupancy-registration", true),
        ("occupancy-registration.owner-review", "Approve, request corrections on, or reject a submitted tenant registration (owner stage)", "occupancy-registration", true),
        ("occupancy-registration.verify", "Verify, request corrections on, reject, or activate an owner-approved tenant registration (management stage)", "occupancy-registration", true),
        ("occupancy-registration.move-out", "Record the move-out of an active tenant registration", "occupancy-registration", true),

        // Seed_Leasing_SecurityViewPermission
        ("occupancy-registration.security-view", "View the restricted security-facing tenant registration directory (operational info only, no sensitive personal data)", "occupancy-registration", true),

        // Seed_UtilityReport_Permission (UX-5)
        ("utility.report", "View utility consumption, reading-status, and meter-status reports", "report", false),

        // Seed_Platform_Permission_Catalogue (Platform scope, Phase 1)
        ("platform.organization.read", "View organization (tenant) metadata", "platform", false),
        ("platform.organization.create", "Provision new organizations", "platform", false),
        ("platform.organization.update", "Update organization metadata", "platform", false),
        ("platform.organization.suspend", "Suspend organizations", "platform", false),
        ("platform.organization.activate", "Activate a newly provisioned organization", "platform", false),
        ("platform.organization.reactivate", "Reactivate a suspended organization", "platform", false),
        ("platform.organization.features.manage", "Enable/disable product modules for an organization", "platform", false),
        ("platform.subscription.read", "View subscription/plan information", "platform", false),
        ("platform.subscription.manage", "Manage subscriptions/plans", "platform", false),
        ("platform.support.access", "Perform controlled cross-tenant support operations", "platform", false),
        ("platform.audit.read", "View platform-level audit records", "platform", false),
        ("platform.diagnostics.read", "View platform diagnostics", "platform", false),

        // Seed_Invoice_View_Own_Permission (Phase 3, ADR-021)
        ("invoice.view.own", "View own invoices via self-service (owned/occupied flats only)", "billing", true),
    ];
}
