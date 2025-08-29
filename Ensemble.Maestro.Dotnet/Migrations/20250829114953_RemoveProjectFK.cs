using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensemble.Maestro.Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignerOutputs_Projects_ProjectId",
                table: "DesignerOutputs");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
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
                name: "ProjectId",
                table: "DesignerOutputs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignerOutputs_Projects_ProjectId",
                table: "DesignerOutputs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
