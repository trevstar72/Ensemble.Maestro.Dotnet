using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensemble.Maestro.Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Charter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetLanguage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetFramework = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeploymentTarget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ComplexityScore = table.Column<int>(type: "int", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModuleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TechnicalStack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplexityScore = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dependencies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    ModuleOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PipelineExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    StageStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetLanguage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeploymentTarget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AgentPoolSize = table.Column<int>(type: "int", nullable: true),
                    TotalFunctions = table.Column<int>(type: "int", nullable: true),
                    CompletedFunctions = table.Column<int>(type: "int", nullable: false),
                    FailedFunctions = table.Column<int>(type: "int", nullable: false),
                    EstimatedDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ActualDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionLogs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionMetrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionConfig = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineExecutions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrchestrationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrchestrationPattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrchestrationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    InitialInput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentCount = table.Column<int>(type: "int", nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    FunctionCallCount = table.Column<int>(type: "int", nullable: false),
                    TotalTokens = table.Column<int>(type: "int", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    SuccessRate = table.Column<int>(type: "int", nullable: false),
                    QualityScore = table.Column<int>(type: "int", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: true),
                    ParticipatingAgents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrchestrationConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuntimeConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionFlow = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceMetrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionLogs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Warnings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryAttempt = table.Column<int>(type: "int", nullable: false),
                    MaxRetryAttempts = table.Column<int>(type: "int", nullable: false),
                    CancellationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasCancelled = table.Column<bool>(type: "bit", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: true),
                    DidTimeout = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentOrchestrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InputTransformations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputTransformations = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrchestrationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrchestrationResults_OrchestrationResults_ParentOrchestrationId",
                        column: x => x.ParentOrchestrationId,
                        principalTable: "OrchestrationResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrchestrationResults_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FullPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    IsOverwritable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Template = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GenerationPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedByAgent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BuildOrder = table.Column<int>(type: "int", nullable: true),
                    Dependencies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityScore = table.Column<int>(type: "int", nullable: true),
                    ValidationResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StageExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    InputData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StageConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemsProcessed = table.Column<int>(type: "int", nullable: true),
                    ItemsCompleted = table.Column<int>(type: "int", nullable: false),
                    ItemsFailed = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionLogs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceMetrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryAttempt = table.Column<int>(type: "int", nullable: false),
                    MaxRetryAttempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageExecutions_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StageExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AgentSpecialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    InputPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    TotalTokens = table.Column<int>(type: "int", nullable: true),
                    ExecutionCost = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    ModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    MaxTokens = table.Column<int>(type: "int", nullable: true),
                    AgentConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionCalls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PluginsUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityScore = table.Column<int>(type: "int", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: true),
                    RetryAttempt = table.Column<int>(type: "int", nullable: false),
                    MaxRetryAttempts = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionLogs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceMetrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrchestrationPattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentExecutions_AgentExecutions_ParentExecutionId",
                        column: x => x.ParentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentExecutions_PipelineExecutions_PipelineExecutionId",
                        column: x => x.PipelineExecutionId,
                        principalTable: "PipelineExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentExecutions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentExecutions_StageExecutions_StageExecutionId",
                        column: x => x.StageExecutionId,
                        principalTable: "StageExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContentFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContentLength = table.Column<int>(type: "int", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: true),
                    TriggeredFunctionCall = table.Column<bool>(type: "bit", nullable: false),
                    FunctionCallData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionCallResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRetry = table.Column<bool>(type: "bit", nullable: false),
                    RetryAttempt = table.Column<int>(type: "int", nullable: false),
                    OrchestrationContext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsAIGenerated = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: true),
                    QualityScore = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentMessages_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentMessages_AgentMessages_ParentMessageId",
                        column: x => x.ParentMessageId,
                        principalTable: "AgentMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_AgentType",
                table: "AgentExecutions",
                column: "AgentType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_ParentExecutionId",
                table: "AgentExecutions",
                column: "ParentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_PipelineExecutionId",
                table: "AgentExecutions",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_ProjectId",
                table: "AgentExecutions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_ProjectId_AgentType_Status",
                table: "AgentExecutions",
                columns: new[] { "ProjectId", "AgentType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_StageExecutionId",
                table: "AgentExecutions",
                column: "StageExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_StartedAt",
                table: "AgentExecutions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_Status",
                table: "AgentExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_AgentExecutionId",
                table: "AgentMessages",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_AgentExecutionId_SequenceNumber",
                table: "AgentMessages",
                columns: new[] { "AgentExecutionId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_CreatedAt",
                table: "AgentMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_MessageType",
                table: "AgentMessages",
                column: "MessageType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_ParentMessageId",
                table: "AgentMessages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_Role",
                table: "AgentMessages",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_SequenceNumber",
                table: "AgentMessages",
                column: "SequenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_ThreadId",
                table: "AgentMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ModuleOrder",
                table: "Modules",
                column: "ModuleOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ModuleType",
                table: "Modules",
                column: "ModuleType");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Name",
                table: "Modules",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ProjectId",
                table: "Modules",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Status",
                table: "Modules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationResults_OrchestrationPattern",
                table: "OrchestrationResults",
                column: "OrchestrationPattern");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationResults_ParentOrchestrationId",
                table: "OrchestrationResults",
                column: "ParentOrchestrationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationResults_PipelineExecutionId",
                table: "OrchestrationResults",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationResults_StartedAt",
                table: "OrchestrationResults",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationResults_Status",
                table: "OrchestrationResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineExecutions_ProjectId",
                table: "PipelineExecutions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineExecutions_ProjectId_Status",
                table: "PipelineExecutions",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PipelineExecutions_Stage",
                table: "PipelineExecutions",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineExecutions_StartedAt",
                table: "PipelineExecutions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineExecutions_Status",
                table: "PipelineExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ContentType",
                table: "ProjectFiles",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_FileName",
                table: "ProjectFiles",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_IsActive",
                table: "ProjectFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ModuleId",
                table: "ProjectFiles",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_PipelineExecutionId",
                table: "ProjectFiles",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId",
                table: "ProjectFiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId_RelativePath",
                table: "ProjectFiles",
                columns: new[] { "ProjectId", "RelativePath" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_RelativePath",
                table: "ProjectFiles",
                column: "RelativePath");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_Status",
                table: "ProjectFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedAt",
                table: "Projects",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_ExecutionOrder",
                table: "StageExecutions",
                column: "ExecutionOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_PipelineExecutionId",
                table: "StageExecutions",
                column: "PipelineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_PipelineExecutionId_ExecutionOrder",
                table: "StageExecutions",
                columns: new[] { "PipelineExecutionId", "ExecutionOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_StageName",
                table: "StageExecutions",
                column: "StageName");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_Status",
                table: "StageExecutions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentMessages");

            migrationBuilder.DropTable(
                name: "OrchestrationResults");

            migrationBuilder.DropTable(
                name: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "AgentExecutions");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "StageExecutions");

            migrationBuilder.DropTable(
                name: "PipelineExecutions");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
