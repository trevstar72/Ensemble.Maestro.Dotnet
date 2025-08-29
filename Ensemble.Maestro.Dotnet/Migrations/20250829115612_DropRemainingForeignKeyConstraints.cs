using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensemble.Maestro.Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class DropRemainingForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint from FunctionSpecifications to AgentExecutions if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FunctionSpecifications_AgentExecutions_AgentExecutionId')
                BEGIN
                    ALTER TABLE [FunctionSpecifications] DROP CONSTRAINT [FK_FunctionSpecifications_AgentExecutions_AgentExecutionId]
                END");

            // Drop foreign key constraint from FunctionSpecifications to PipelineExecutions if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FunctionSpecifications_PipelineExecutions_PipelineExecutionId')
                BEGIN
                    ALTER TABLE [FunctionSpecifications] DROP CONSTRAINT [FK_FunctionSpecifications_PipelineExecutions_PipelineExecutionId]
                END");

            // Drop foreign key constraint from FunctionSpecifications to Projects if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FunctionSpecifications_Projects_ProjectId')
                BEGIN
                    ALTER TABLE [FunctionSpecifications] DROP CONSTRAINT [FK_FunctionSpecifications_Projects_ProjectId]
                END");

            // Drop foreign key constraint from FunctionSpecifications to Modules if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FunctionSpecifications_Modules_ModuleId')
                BEGIN
                    ALTER TABLE [FunctionSpecifications] DROP CONSTRAINT [FK_FunctionSpecifications_Modules_ModuleId]
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
