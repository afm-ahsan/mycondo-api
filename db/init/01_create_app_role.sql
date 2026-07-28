-- Creates the restricted runtime role the API/DbMigrator connect as. The container's bootstrap
-- role (POSTGRES_USER, see docker-compose.yml) is mycondo_migrator — the DDL/migrations role, and
-- Postgres always makes its initdb bootstrap role a superuser, so it must never be the role the
-- running application uses (superusers unconditionally bypass Row-Level Security regardless of
-- FORCE ROW LEVEL SECURITY — see mycondo-docs ADR-009 and this fix's ADR for the incident that found
-- this the hard way).
--
-- Privileges on schemas/tables/sequences are granted separately by the
-- Grant_App_Role_Runtime_Privileges EF migration (runs as mycondo_migrator, after this role exists).
-- This script only creates the role itself — docker-entrypoint-initdb.d scripts run once, before any
-- schema exists, so there is nothing to grant privileges on yet.
CREATE ROLE mycondo_app WITH LOGIN PASSWORD 'mycondo_dev'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
