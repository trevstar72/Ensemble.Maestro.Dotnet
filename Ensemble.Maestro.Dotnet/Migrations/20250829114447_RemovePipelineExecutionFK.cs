using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensemble.Maestro.Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class RemovePipelineExecutionFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignerOutputs_PipelineExecutions_PipelineExecutionId",
                table: "DesignerOutputs");

            migrationBuilder.DropIndex(
                name: "IX_DesignerOutputs_PipelineExecutionId",
                table: "DesignerOutputs");

            migrationBuilder.AlterColumn<Guid>(
                name: "PipelineExecutionId",
                table: "DesignerOutputs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PipelineExecutionId",
                table: "DesignerOutputs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_PipelineExecutionId",
                table: "DesignerOutputs",
                column: "PipelineExecutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignerOutputs_PipelineExecutions_PipelineExecutionId",
                table: "DesignerOutputs",
                column: "PipelineExecutionId",
                principalTable: "PipelineExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
