using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Amenities_Facilities_Bookings_PoolSessions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_invoices_tenant_id_flat_id_period_source",
            schema: "billing",
            table: "invoices");

        migrationBuilder.EnsureSchema(
            name: "amenities");

        migrationBuilder.CreateTable(
            name: "blackout_dates",
            schema: "amenities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                date_from = table.Column<DateOnly>(type: "date", nullable: false),
                date_to = table.Column<DateOnly>(type: "date", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_blackout_dates", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "bookings",
            schema: "amenities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                building_id = table.Column<Guid>(type: "uuid", nullable: false),
                flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                setup_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                cleanup_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                expected_guest_count = table.Column<int>(type: "integer", nullable: false),
                booking_charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                cancellation_deadline_hours = table.Column<int>(type: "integer", nullable: false),
                cancellation_deduction_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                approval_required = table.Column<bool>(type: "boolean", nullable: false),
                payment_required = table.Column<bool>(type: "boolean", nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                deposit_collection_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                deposit_settlement_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                deposit_refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                deposit_deducted_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                terms_accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                rejected_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                cancelled_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                checked_in_by = table.Column<Guid>(type: "uuid", nullable: true),
                checked_in_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                inspected_by = table.Column<Guid>(type: "uuid", nullable: true),
                inspected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                inspection_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                damage_deduction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bookings", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "facilities",
            schema: "amenities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                building_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                facility_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                capacity = table.Column<int>(type: "integer", nullable: false),
                operating_hours_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                operating_hours_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                booking_charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                cancellation_deadline_hours = table.Column<int>(type: "integer", nullable: false),
                cancellation_deduction_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                guest_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                minimum_age_unaccompanied = table.Column<int>(type: "integer", nullable: true),
                requires_safety_acknowledgement = table.Column<bool>(type: "boolean", nullable: false),
                blocks_entry_if_account_overdue = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_facilities", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "pool_incidents",
            schema: "amenities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                pool_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reported_by = table.Column<Guid>(type: "uuid", nullable: true),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                action_taken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_pool_incidents", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "pool_sessions",
            schema: "amenities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                person_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                age_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                accompanied_by_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                entry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                exit_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                guest_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                safety_acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                checked_in_by = table.Column<Guid>(type: "uuid", nullable: true),
                checked_out_by = table.Column<Guid>(type: "uuid", nullable: true),
                override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_pool_sessions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_invoices_tenant_id_flat_id_period_source",
            schema: "billing",
            table: "invoices",
            columns: new[] { "tenant_id", "flat_id", "period_start", "period_end", "source" },
            unique: true,
            filter: "source <> 'FacilityBooking'");

        migrationBuilder.CreateIndex(
            name: "ix_blackout_dates_tenant_id_facility_id_is_active",
            schema: "amenities",
            table: "blackout_dates",
            columns: new[] { "tenant_id", "facility_id", "is_active" });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_tenant_id_facility_id_start_at_utc",
            schema: "amenities",
            table: "bookings",
            columns: new[] { "tenant_id", "facility_id", "start_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_tenant_id_flat_id_status",
            schema: "amenities",
            table: "bookings",
            columns: new[] { "tenant_id", "flat_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_facilities_tenant_id_building_id_facility_type",
            schema: "amenities",
            table: "facilities",
            columns: new[] { "tenant_id", "building_id", "facility_type" });

        migrationBuilder.CreateIndex(
            name: "ix_pool_incidents_tenant_id_facility_id_occurred_at_utc",
            schema: "amenities",
            table: "pool_incidents",
            columns: new[] { "tenant_id", "facility_id", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_pool_sessions_tenant_id_facility_id_exit_at_utc",
            schema: "amenities",
            table: "pool_sessions",
            columns: new[] { "tenant_id", "facility_id", "exit_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_pool_sessions_tenant_id_flat_id",
            schema: "amenities",
            table: "pool_sessions",
            columns: new[] { "tenant_id", "flat_id" });

        // Authoritative booking-overlap guard — same EXCLUDE/GiST pattern as ServiceChargeRule/
        // RatePlan (Slices E/F), but unlike those (which range over plain `date` columns with no
        // arithmetic), this one needs `timestamptz +/- interval` to compute the buffered slot.
        // `timestamptz_pl_interval`/`timestamptz_mi_interval` are STABLE, not IMMUTABLE (Postgres is
        // conservative because `interval` can carry calendar-relative components like months), so
        // Postgres rejects them directly inside a GiST index expression — this was never actually
        // caught until a real Postgres instance ran this migration for the first time (see
        // mycondo-phase1-final-postgresql-rls-verification-prompt.md's full-chain verification; not a
        // Phase-1/Platform defect, this table predates Phase 1 entirely). The wrapper function below
        // is safe to mark IMMUTABLE: setup/cleanup buffers are always `N * INTERVAL '1 minute'` — a
        // fixed-duration shift with no month/day component — so the result is a deterministic function
        // of its inputs regardless of session TimeZone, even though the underlying operators are
        // conservatively labeled STABLE. btree_gist is already created by the Slice E/F migrations,
        // but CREATE EXTENSION IF NOT EXISTS is idempotent and cheap, so it's repeated here rather than
        // assumed. A partial constraint: Cancelled/Rejected/NoShow bookings don't hold the slot, but
        // every other status does (including Draft/PendingApproval) — see Booking's doc comment for why.
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            CREATE OR REPLACE FUNCTION amenities.booking_slot_range(
                start_at_utc timestamptz,
                end_at_utc timestamptz,
                setup_buffer_minutes integer,
                cleanup_buffer_minutes integer
            ) RETURNS tstzrange
            LANGUAGE sql
            IMMUTABLE
            AS $$
                SELECT tstzrange(
                    start_at_utc - (setup_buffer_minutes * INTERVAL '1 minute'),
                    end_at_utc + (cleanup_buffer_minutes * INTERVAL '1 minute'),
                    '[]'
                );
            $$;

            ALTER TABLE amenities.bookings
            ADD CONSTRAINT ex_bookings_no_overlap
            EXCLUDE USING gist (
                tenant_id WITH =,
                facility_id WITH =,
                amenities.booking_slot_range(start_at_utc, end_at_utc, setup_buffer_minutes, cleanup_buffer_minutes) WITH &&
            )
            WHERE (status NOT IN ('Cancelled', 'Rejected', 'NoShow'));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE amenities.bookings DROP CONSTRAINT IF EXISTS ex_bookings_no_overlap;
            DROP FUNCTION IF EXISTS amenities.booking_slot_range(timestamptz, timestamptz, integer, integer);
            """);

        migrationBuilder.DropTable(
            name: "blackout_dates",
            schema: "amenities");

        migrationBuilder.DropTable(
            name: "bookings",
            schema: "amenities");

        migrationBuilder.DropTable(
            name: "facilities",
            schema: "amenities");

        migrationBuilder.DropTable(
            name: "pool_incidents",
            schema: "amenities");

        migrationBuilder.DropTable(
            name: "pool_sessions",
            schema: "amenities");

        migrationBuilder.DropIndex(
            name: "ux_invoices_tenant_id_flat_id_period_source",
            schema: "billing",
            table: "invoices");

        migrationBuilder.CreateIndex(
            name: "ux_invoices_tenant_id_flat_id_period_source",
            schema: "billing",
            table: "invoices",
            columns: new[] { "tenant_id", "flat_id", "period_start", "period_end", "source" },
            unique: true);
    }
}
