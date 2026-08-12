using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeManagement.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20260812112330_WorkflowConditionsAndSteps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ConditionOperator",
            table: "Workflows",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "WorkflowSteps",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StepType = table.Column<int>(type: "INTEGER", nullable: false),
                DeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                DelaySeconds = table.Column<int>(type: "INTEGER", nullable: true),
                NotifyTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                NotifyMessageTemplate = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                WorkflowName = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkflowSteps_Workflows_WorkflowName",
                    column: x => x.WorkflowName,
                    principalTable: "Workflows",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkflowTriggerConditions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TriggerType = table.Column<int>(type: "INTEGER", nullable: false),
                ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                TriggerDeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                TriggerMetric = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                TriggerSourceActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                TriggerPropertyPath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                TriggerExpectedValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TriggerValueType = table.Column<int>(type: "INTEGER", nullable: true),
                TriggerOperator = table.Column<int>(type: "INTEGER", nullable: true),
                TriggerValue = table.Column<double>(type: "REAL", nullable: true),
                LastTriggeredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastTriggerDateLocal = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastConditionMatched = table.Column<bool>(type: "INTEGER", nullable: true),
                WorkflowName = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkflowTriggerConditions", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkflowTriggerConditions_Workflows_WorkflowName",
                    column: x => x.WorkflowName,
                    principalTable: "Workflows",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowSteps_WorkflowName",
            table: "WorkflowSteps",
            column: "WorkflowName");

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowTriggerConditions_WorkflowName",
            table: "WorkflowTriggerConditions",
            column: "WorkflowName");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WorkflowSteps");

        migrationBuilder.DropTable(
            name: "WorkflowTriggerConditions");

        migrationBuilder.DropColumn(
            name: "ConditionOperator",
            table: "Workflows");
    }
}
