using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the global permission catalogue from the governing strategy document
/// (MyCondo_Solution_Architecture_Assessment_and_Delivery_Strategy.md §15) — ~47 concrete
/// &lt;module&gt;.&lt;resource&gt;.&lt;action&gt; permissions, not wildcards (the JWT-claims permission
/// check does exact string matching; see mycondo-docs ADR-011). <c>permissions</c> has no RLS (global
/// reference data, same set for every tenant), so a plain <c>InsertData</c> is safe regardless of
/// tenant context. <c>IsBuildingScopable</c> marks operational/financial permissions as building-scopable
/// and administrative/platform ones as not — a documented judgment call, adjustable later.
/// </summary>
public partial class Seed_Permission_Catalogue : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("062d7f0a-5bd5-4bfb-bfd0-426e9dc6eff5"), "View tenant details", "tenant", false, "tenant.view" },
                { new Guid("6f17e05d-89ed-4273-987c-3cba7ae06d4f"), "Create, activate, and suspend tenants", "tenant", false, "tenant.manage" },
                { new Guid("1f0c8d20-7de9-42ec-a454-133ba8494ab0"), "View users", "user", false, "user.view" },
                { new Guid("accc01b6-76c9-4c8c-95e3-ed94f878ef7b"), "Create users", "user", false, "user.create" },
                { new Guid("73eabf2e-7dfa-4278-988c-b8bcc9d54b67"), "Update user details", "user", false, "user.update" },
                { new Guid("16255e69-29a7-4ea5-9cb1-bab52d652fb3"), "Disable user accounts", "user", false, "user.disable" },
                { new Guid("45542cce-d68c-403f-819f-dbdb8aadd901"), "View property hierarchy", "property", true, "property.view" },
                { new Guid("8fa90790-cdeb-4422-a983-932cfa203da9"), "Create properties/buildings/units", "property", true, "property.create" },
                { new Guid("fb5d25f6-18f9-4295-8ce7-49ef9ca67df4"), "Update property hierarchy", "property", true, "property.update" },
                { new Guid("50640c8a-5616-4fbb-ac9c-02a4f9e44e11"), "Delete property hierarchy entries", "property", true, "property.delete" },
                { new Guid("4f70dc2f-d34d-4c38-86ab-935bdf123f75"), "View residents", "resident", true, "resident.view" },
                { new Guid("3e4c9f65-a755-4a81-9d15-489dd87ff92c"), "Create resident records", "resident", true, "resident.create" },
                { new Guid("881ffdf7-ae39-49d9-987a-d05bc075af69"), "Update resident records", "resident", true, "resident.update" },
                { new Guid("f674daec-a339-4151-88df-7f9ded8177dc"), "Disable resident records", "resident", true, "resident.disable" },
                { new Guid("7723eb4e-73c3-44e5-b636-9c8d1522ab7c"), "View ownership records", "ownership", true, "ownership.view" },
                { new Guid("9fc89d3a-67c3-447a-8892-9b8f86712954"), "Create and update ownership records", "ownership", true, "ownership.manage" },
                { new Guid("97b3a21a-e07d-4e0e-888d-67de8e68316d"), "View leases", "lease", true, "lease.view" },
                { new Guid("fa5bd62a-e8e7-4729-8d46-4c15803f8423"), "Create and update leases", "lease", true, "lease.manage" },
                { new Guid("56becab4-be20-492f-a68c-3af50de25591"), "View service-charge rules", "billing", true, "billing.rule.view" },
                { new Guid("8319d20e-22f3-4215-9356-cc0bedeade93"), "Create and update service-charge rules", "billing", true, "billing.rule.manage" },
                { new Guid("412439c6-b4b4-47a4-845c-197251a973fd"), "Run billing generation batches", "billing", true, "billing.generate" },
                { new Guid("2b03ef96-fc4b-4afd-8d1a-a0a952bd994a"), "View invoices", "invoice", true, "invoice.view" },
                { new Guid("14bb199d-ced0-4ace-b7f0-cae969a71476"), "Void invoices", "invoice", true, "invoice.void" },
                { new Guid("90ab4957-c9af-4254-9126-8f9714b92e25"), "View payments", "payment", true, "payment.view" },
                { new Guid("66ba4219-e415-40ed-845f-8a2fb38c9655"), "Record payments", "payment", true, "payment.record" },
                { new Guid("9fdbe6b3-15f4-4128-80f8-83e61f97f596"), "Reverse payments", "payment", true, "payment.reverse" },
                { new Guid("3e72ea1c-05f0-4c18-9139-092f26be76ce"), "View expenses", "expense", true, "expense.view" },
                { new Guid("0a7bb64b-acdb-4480-a927-5ef67eb5be45"), "Create and update expenses", "expense", true, "expense.manage" },
                { new Guid("c8258cc7-7f3d-44f5-89be-f3d43045e0a5"), "View complaints", "complaint", true, "complaint.view" },
                { new Guid("1c9b2c24-80b2-4d99-ab46-11715febebc3"), "Create complaints", "complaint", true, "complaint.create" },
                { new Guid("42329f3b-61a3-4bdd-97f8-5d3c81db4cd1"), "Assign complaints to staff", "complaint", true, "complaint.assign" },
                { new Guid("cd33ec6e-d1cd-4c55-989f-463703be4da3"), "Manage complaint lifecycle", "complaint", true, "complaint.manage" },
                { new Guid("10ee6afd-43ce-4996-87ea-11697dafbcf7"), "View work orders", "workorder", true, "workorder.view" },
                { new Guid("9e147667-e6b2-49ba-bc13-2f91980ecd1b"), "Create work orders", "workorder", true, "workorder.create" },
                { new Guid("e672922b-ad0e-4efe-8bd8-d87a74dffc98"), "Assign work orders to staff", "workorder", true, "workorder.assign" },
                { new Guid("bd8515e3-6f16-4ab7-98c9-c00b278eca2e"), "Mark work orders complete", "workorder", true, "workorder.complete" },
                { new Guid("fb803fe3-7ffe-410a-b98b-eddce4db8179"), "View notifications", "notification", false, "notification.view" },
                { new Guid("c8f3a659-b011-47f9-b202-4151c0333bb8"), "Manage notification templates and dispatch", "notification", false, "notification.manage" },
                { new Guid("494fc38c-7881-4377-8acf-4af5eecd37fd"), "View documents", "document", false, "document.view" },
                { new Guid("b11ae702-6f46-4b3e-ba7b-044bbb92b9b5"), "Upload documents", "document", false, "document.upload" },
                { new Guid("cd25c865-cdc3-41c2-8a3b-8be43bb1b2cf"), "Delete documents", "document", false, "document.delete" },
                { new Guid("6f87a675-5182-4b24-ade8-2154f8eb6d2d"), "View financial reports", "report", false, "report.financial.view" },
                { new Guid("b20b0bba-8c94-4efb-8478-704b798e95b9"), "View operational reports", "report", false, "report.operational.view" },
                { new Guid("c6d77026-9b90-46ed-960f-b75e6ca54256"), "View roles", "role", false, "role.view" },
                { new Guid("75a11555-16e8-4f38-bac0-9e5b1adc624b"), "Create roles and manage grants/assignments", "role", false, "role.manage" },
                { new Guid("421ed68e-90c5-4b35-91e4-c5e877723898"), "View the permission catalogue", "permission", false, "permission.view" },
                { new Guid("d3440448-1c01-44ba-b0ca-80faa4b205f0"), "View audit records", "audit", false, "audit.view" }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM identity.permissions;");
    }
}
