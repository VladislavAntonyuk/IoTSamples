using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeManagement.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20260812083030_Workflows : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Workflows",
            columns: table => new
            {
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                TriggerType = table.Column<int>(type: "INTEGER", nullable: false),
                ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                TriggerDeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                TriggerMetric = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                TriggerOperator = table.Column<int>(type: "INTEGER", nullable: true),
                TriggerValue = table.Column<double>(type: "REAL", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Workflows", x => x.Name);
            });

        migrationBuilder.CreateTable(
            name: "WorkflowActions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WorkflowActions");

        migrationBuilder.DropTable(
            name: "Workflows");
    }
}
