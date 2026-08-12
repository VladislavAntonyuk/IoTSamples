using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeManagement.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20260812115158_RemoveLegacyWorkflowFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WorkflowActions");

        migrationBuilder.DropColumn(
            name: "TriggerMetric",
            table: "WorkflowTriggerConditions");

        migrationBuilder.DropColumn(
            name: "TriggerValue",
            table: "WorkflowTriggerConditions");

        migrationBuilder.DropColumn(
            name: "LastTriggerDateLocal",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "ScheduledAt",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerDeviceName",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerExpectedValue",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerMetric",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerOperator",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerPropertyPath",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerSourceActionName",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerType",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerValue",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "TriggerValueType",
            table: "Workflows");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TriggerMetric",
            table: "WorkflowTriggerConditions",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "TriggerValue",
            table: "WorkflowTriggerConditions",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastTriggerDateLocal",
            table: "Workflows",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ScheduledAt",
            table: "Workflows",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TriggerDeviceName",
            table: "Workflows",
            type: "TEXT",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TriggerExpectedValue",
            table: "Workflows",
            type: "TEXT",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TriggerMetric",
            table: "Workflows",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TriggerOperator",
            table: "Workflows",
            type: "INTEGER",
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
            name: "TriggerType",
            table: "Workflows",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<double>(
            name: "TriggerValue",
            table: "Workflows",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TriggerValueType",
            table: "Workflows",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "WorkflowActions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                DeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                WorkflowName = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkflowActions", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkflowActions_Workflows_WorkflowName",
                    column: x => x.WorkflowName,
                    principalTable: "Workflows",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowActions_WorkflowName",
            table: "WorkflowActions",
            column: "WorkflowName");
    }
}
