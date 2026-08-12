using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeManagement.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20260812094048_WorkflowDynamicDeviceResult : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TriggerExpectedValue",
            table: "Workflows",
            type: "TEXT",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TriggerPropertyPath",
            table: "Workflows",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TriggerSourceActionName",
            table: "Workflows",
            type: "TEXT",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TriggerValueType",
            table: "Workflows",
            type: "INTEGER",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TriggerExpectedValue",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerPropertyPath",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerSourceActionName",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerValueType",
            table: "Workflows");
    }
}
