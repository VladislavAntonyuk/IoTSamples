using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeManagement.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20260812090612_WorkflowRuntimeState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "LastConditionMatched",
            table: "Workflows",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastTriggerDateLocal",
            table: "Workflows",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastTriggeredAtUtc",
            table: "Workflows",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LastConditionMatched",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "LastTriggerDateLocal",
            table: "Workflows");

        migrationBuilder.DropColumn(
            name: "LastTriggeredAtUtc",
            table: "Workflows");
    }
}
