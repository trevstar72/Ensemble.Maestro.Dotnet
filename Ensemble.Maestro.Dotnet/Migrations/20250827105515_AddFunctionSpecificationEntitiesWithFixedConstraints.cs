using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensemble.Maestro.Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddFunctionSpecificationEntitiesWithFixedConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DesignerOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrossReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetLanguage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeploymentTarget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MarkdownOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuredData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchitectureOverview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComponentSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatabaseSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UiSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecuritySpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestingSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeploymentSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionSpecsCount = table.Column<int>(type: "int", nullable: false),
                    ComplexityRating = table.Column<int>(type: "int", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QualityScore = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    ExecutionCost = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpecsExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignerOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignerOutputs_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignerOutputs_CrossReferences_CrossReferenceId",
                        column: x => x.CrossReferenceId,
                        principalTable: "CrossReferences",
                        principalColumn: "PrimaryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DesignerOutputs_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignerOutputs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrossReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesignerOutputId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Namespace = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Inheritance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dependencies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Constructors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Patterns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Integrations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityConsiderations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceConsiderations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestingStrategy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplexityRating = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedController = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ControllerExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FunctionCount = table.Column<int>(type: "int", nullable: false),
                    SimpleFunctionCount = table.Column<int>(type: "int", nullable: false),
                    ComplexFunctionCount = table.Column<int>(type: "int", nullable: false),
                    MethodAgentCount = table.Column<int>(type: "int", nullable: false),
                    CompletionPercentage = table.Column<int>(type: "int", nullable: false),
                    QualityScore = table.Column<int>(type: "int", nullable: true),
                    GeneratedCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeSize = table.Column<long>(type: "bigint", nullable: true),
                    CodeStats = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ControllerAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImplementationStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImplementationCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeUnits_CrossReferences_CrossReferenceId",
                        column: x => x.CrossReferenceId,
                        principalTable: "CrossReferences",
                        principalColumn: "PrimaryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeUnits_DesignerOutputs_DesignerOutputId",
                        column: x => x.DesignerOutputId,
                        principalTable: "DesignerOutputs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CodeUnits_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CodeUnits_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CodeUnits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FunctionSpecifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrossReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FunctionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodeUnit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Namespace = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputParameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dependencies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessLogic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorHandling = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityConsiderations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestCases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplexityRating = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedByAgent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityScore = table.Column<int>(type: "int", nullable: true),
                    CompletenessPercentage = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImplementedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DesignerOutputId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_CodeUnits_CodeUnitId",
                        column: x => x.CodeUnitId,
                        principalTable: "CodeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_CrossReferences_CrossReferenceId",
                        column: x => x.CrossReferenceId,
                        principalTable: "CrossReferences",
                        principalColumn: "PrimaryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_DesignerOutputs_DesignerOutputId",
                        column: x => x.DesignerOutputId,
                        principalTable: "DesignerOutputs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FunctionSpecifications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_AssignedController",
                table: "CodeUnits",
                column: "AssignedController");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_ComplexityRating",
                table: "CodeUnits",
                column: "ComplexityRating");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_CreatedAt",
                table: "CodeUnits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_CrossReferenceId",
                table: "CodeUnits",
                column: "CrossReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_DesignerOutputId",
                table: "CodeUnits",
                column: "DesignerOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_Language",
                table: "CodeUnits",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_Language_UnitType",
                table: "CodeUnits",
                columns: new[] { "Language", "UnitType" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_ModuleId",
                table: "CodeUnits",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_Name",
                table: "CodeUnits",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_PipelineExecutionId",
                table: "CodeUnits",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_ProjectId",
                table: "CodeUnits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_ProjectId_Status",
                table: "CodeUnits",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_Status",
                table: "CodeUnits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CodeUnits_UnitType",
                table: "CodeUnits",
                column: "UnitType");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_AgentExecutionId",
                table: "DesignerOutputs",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_AgentType",
                table: "DesignerOutputs",
                column: "AgentType");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_CreatedAt",
                table: "DesignerOutputs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_CrossReferenceId",
                table: "DesignerOutputs",
                column: "CrossReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_PipelineExecutionId",
                table: "DesignerOutputs",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_ProjectId",
                table: "DesignerOutputs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_ProjectId_AgentType_Status",
                table: "DesignerOutputs",
                columns: new[] { "ProjectId", "AgentType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DesignerOutputs_Status",
                table: "DesignerOutputs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_AgentExecutionId",
                table: "FunctionSpecifications",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_CodeUnit",
                table: "FunctionSpecifications",
                column: "CodeUnit");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_CodeUnitId",
                table: "FunctionSpecifications",
                column: "CodeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_ComplexityRating",
                table: "FunctionSpecifications",
                column: "ComplexityRating");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_CreatedAt",
                table: "FunctionSpecifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_CrossReferenceId",
                table: "FunctionSpecifications",
                column: "CrossReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_DesignerOutputId",
                table: "FunctionSpecifications",
                column: "DesignerOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_FunctionName",
                table: "FunctionSpecifications",
                column: "FunctionName");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_Language",
                table: "FunctionSpecifications",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_Language_ComplexityRating",
                table: "FunctionSpecifications",
                columns: new[] { "Language", "ComplexityRating" });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_ModuleId",
                table: "FunctionSpecifications",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_PipelineExecutionId",
                table: "FunctionSpecifications",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_Priority",
                table: "FunctionSpecifications",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_ProjectId",
                table: "FunctionSpecifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_ProjectId_CodeUnit_Status",
                table: "FunctionSpecifications",
                columns: new[] { "ProjectId", "CodeUnit", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionSpecifications_Status",
                table: "FunctionSpecifications",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FunctionSpecifications");

            migrationBuilder.DropTable(
                name: "CodeUnits");

            migrationBuilder.DropTable(
                name: "DesignerOutputs");
        }
    }
}
