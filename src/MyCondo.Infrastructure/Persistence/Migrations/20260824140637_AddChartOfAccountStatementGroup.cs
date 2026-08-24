using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddChartOfAccountStatementGroup : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "statement_group",
            schema: "finance",
            table: "chart_of_accounts",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "statement_group",
            schema: "finance",
            table: "chart_of_accounts");
    }
}
