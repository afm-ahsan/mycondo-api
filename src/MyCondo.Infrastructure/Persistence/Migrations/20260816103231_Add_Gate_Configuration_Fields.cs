using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Gate_Configuration_Fields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            schema: "property",
            table: "gates",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "display_order",
            schema: "property",
            table: "gates",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        // Existing gates already serve both entry and exit for every check-in/out flow today —
        // defaulting these to true (rather than EF's usual `false`) preserves current behavior for
        // pre-existing rows instead of silently locking every gate out of check-in/checkout.
        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            schema: "property",
            table: "gates",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_entry_allowed",
            schema: "property",
            table: "gates",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_exit_allowed",
            schema: "property",
            table: "gates",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "version",
            schema: "property",
            table: "gates",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        // `code` is added nullable first, backfilled from `name` for existing rows below, then
        // tightened to NOT NULL — a fresh column can't carry a meaningful per-row default.
        migrationBuilder.AddColumn<string>(
            name: "code",
            schema: "property",
            table: "gates",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        // Derive a Code for pre-existing gates from their Name (e.g. "Main Gate" -> "MAIN_GATE"),
        // deduped per building with a numeric suffix on collision, since Code is new and previously
        // only Name existed.
        migrationBuilder.Sql(
            """
            WITH candidates AS (
                SELECT
                    id,
                    COALESCE(
                        NULLIF(TRIM(BOTH '_' FROM UPPER(regexp_replace(name, '[^A-Za-z0-9]+', '_', 'g'))), ''),
                        'GATE'
                    ) AS base_code,
                    tenant_id,
                    building_id,
                    created_at_utc
                FROM property.gates
            ),
            numbered AS (
                SELECT
                    id,
                    base_code,
                    ROW_NUMBER() OVER (
                        PARTITION BY tenant_id, building_id, base_code ORDER BY created_at_utc, id
                    ) AS rn
                FROM candidates
            )
            UPDATE property.gates AS g
            SET code = LEFT(
                CASE WHEN numbered.rn = 1 THEN numbered.base_code ELSE numbered.base_code || '_' || numbered.rn END,
                20)
            FROM numbered
            WHERE g.id = numbered.id;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "code",
            schema: "property",
            table: "gates",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_gates_tenant_id_building_id_code",
            schema: "property",
            table: "gates",
            columns: new[] { "tenant_id", "building_id", "code" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_gates_tenant_id_building_id_code",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "code",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "description",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "display_order",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "is_active",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "is_entry_allowed",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "is_exit_allowed",
            schema: "property",
            table: "gates");

        migrationBuilder.DropColumn(
            name: "version",
            schema: "property",
            table: "gates");
    }
}
