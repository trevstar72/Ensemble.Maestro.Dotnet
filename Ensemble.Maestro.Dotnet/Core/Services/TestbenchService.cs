using System.Text.Json;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ensemble.Maestro.Dotnet.Core.Agents;
using Microsoft.EntityFrameworkCore;

namespace Ensemble.Maestro.Dotnet.Core.Services;

public class TestbenchService
{
    private readonly IPipelineExecutionRepository _pipelineExecutionRepository;
    private readonly IAgentExecutionRepository _agentExecutionRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TestbenchService> _logger;

    public TestbenchService(
        IPipelineExecutionRepository pipelineExecutionRepository,
        IAgentExecutionRepository agentExecutionRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TestbenchService> logger)
    {
        _pipelineExecutionRepository = pipelineExecutionRepository;
        _agentExecutionRepository = agentExecutionRepository;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task<PipelineExecution> StartTestExecution(TestConfiguration config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        // Create or use existing project
        var projectId = config.ProjectId;
        if (projectId == Guid.Empty)
        {
            // Create a new test project automatically
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = $"Testbench Experiment {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                Requirements = "Generated test execution for pipeline validation",
                Status = "Active",
                TargetLanguage = config.TargetLanguage ?? "Auto-detect",
                DeploymentTarget = config.DeploymentTarget ?? "Not specified",
                Priority = "High",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
            projectId = project.Id;
        }

        // Calculate total functions based on agent types across all stages
        var totalAgentCount = new[] { "Planning", "Designing", "Swarming", "Building", "Validating" }
            .SelectMany(stage => GetAgentTypesForStage(stage))
            .Count();

        var pipeline = new PipelineExecution
        {
            ProjectId = projectId,
            Stage = "Planning",
            Status = "Running",
            TargetLanguage = config.TargetLanguage,
            DeploymentTarget = config.DeploymentTarget,
            AgentPoolSize = config.AgentPoolSize,
            EstimatedDurationSeconds = config.EstimatedDurationSeconds,
            TotalFunctions = totalAgentCount,
            CompletedFunctions = 0,
            FailedFunctions = 0,
            ExecutionConfig = JsonSerializer.Serialize(config)
        };

        await _pipelineExecutionRepository.AddAsync(pipeline);
        await _pipelineExecutionRepository.SaveChangesAsync();

        // Fix: Update config with the correct project ID before passing to pipeline execution
        var updatedConfig = config with { ProjectId = projectId };
        
        _logger.LogInformation("Starting pipeline execution {PipelineId} for project {ProjectId} with {TotalAgents} total agents", 
            pipeline.Id, projectId, totalAgentCount);

        _ = Task.Run(async () =>
        {
            try
            {
                await ExecutePipelineAsync(pipeline.Id, updatedConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline execution {PipelineId} failed with exception: {ErrorMessage}", pipeline.Id, ex.Message);
                await FailPipelineAsync(pipeline.Id, ex.Message);
            }
        });

        return pipeline;
    }

    public async Task<List<PipelineExecution>> GetActiveExecutions()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        return await dbContext.PipelineExecutions
            .Include(p => p.Project)
            .Include(p => p.StageExecutions)
            .Include(p => p.AgentExecutions)
            .Where(p => p.Status == "Running" || p.Status == "Pending")
            .OrderByDescending(p => p.StartedAt)
            .ToListAsync();
    }

    public async Task<List<PipelineExecution>> GetRecentExecutions(int count = 50)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        return await dbContext.PipelineExecutions
            .Include(p => p.Project)
            .Include(p => p.StageExecutions)
            .Include(p => p.AgentExecutions)
            .Include(p => p.OrchestrationResults)
            .OrderByDescending(p => p.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<PipelineExecution?> GetExecutionDetails(Guid executionId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        return await dbContext.PipelineExecutions
            .Include(p => p.Project)
            .Include(p => p.StageExecutions.OrderBy(s => s.ExecutionOrder))
                .ThenInclude(s => s.AgentExecutions)
            .Include(p => p.AgentExecutions)
                .ThenInclude(a => a.Messages)
            .Include(p => p.OrchestrationResults)
                .ThenInclude(o => o.ChildOrchestrations)
            .FirstOrDefaultAsync(p => p.Id == executionId);
    }

    public async Task<List<AgentExecution>> GetAgentExecutionsByPipeline(Guid pipelineExecutionId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        return await dbContext.AgentExecutions
            .Include(a => a.Messages)
            .Where(a => a.PipelineExecutionId == pipelineExecutionId)
            .OrderBy(a => a.StartedAt)
            .ToListAsync();
    }

    public async Task<TestbenchStats> GetTestbenchStats()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        var totalExecutions = await dbContext.PipelineExecutions.CountAsync();
        var activeExecutions = await dbContext.PipelineExecutions
            .CountAsync(p => p.Status == "Running" || p.Status == "Pending");
        var completedExecutions = await dbContext.PipelineExecutions
            .CountAsync(p => p.Status == "Completed");
        var failedExecutions = await dbContext.PipelineExecutions
            .CountAsync(p => p.Status == "Failed" || p.Status == "Error");

        var totalAgentExecutions = await dbContext.AgentExecutions.CountAsync();
        var totalTokens = await dbContext.AgentExecutions
            .Where(a => a.TotalTokens.HasValue)
            .SumAsync(a => a.TotalTokens!.Value);
        var totalCost = await dbContext.AgentExecutions
            .Where(a => a.ExecutionCost.HasValue)
            .SumAsync(a => a.ExecutionCost!.Value);

        var avgExecutionTime = await dbContext.PipelineExecutions
            .Where(p => p.ActualDurationSeconds.HasValue)
            .AverageAsync(p => (double?)p.ActualDurationSeconds);

        return new TestbenchStats
        {
            TotalExecutions = totalExecutions,
            ActiveExecutions = activeExecutions,
            CompletedExecutions = completedExecutions,
            FailedExecutions = failedExecutions,
            SuccessRate = totalExecutions > 0 ? (double)completedExecutions / totalExecutions * 100 : 0,
            TotalAgentExecutions = totalAgentExecutions,
            TotalTokensConsumed = totalTokens,
            TotalCostUSD = totalCost,
            AverageExecutionTimeSeconds = avgExecutionTime ?? 0
        };
    }

    public async Task<bool> CancelExecution(Guid executionId)
    {
        var executionResult = await _pipelineExecutionRepository.GetByIdAsync(executionId);
        if (!executionResult.IsSuccess || executionResult.Value == null || 
            executionResult.Value.Status == "Completed" || executionResult.Value.Status == "Failed")
        {
            return false;
        }

        var execution = executionResult.Value;
        execution.Status = "Cancelled";
        execution.CompletedAt = DateTime.UtcNow;
        execution.ActualDurationSeconds = (int)(DateTime.UtcNow - execution.StartedAt).TotalSeconds;

        await _pipelineExecutionRepository.UpdateAsync(execution);
        await _pipelineExecutionRepository.SaveChangesAsync();

        return true;
    }

    private async Task ExecutePipelineAsync(Guid pipelineId, TestConfiguration config)
    {
        _logger.LogInformation("🚀 ExecutePipelineAsync STARTED for pipeline {PipelineId}", pipelineId);
        _logger.LogInformation("🔍 DEBUG: About to enter try block for pipeline {PipelineId}", pipelineId);
        try
        {
            _logger.LogInformation("🔍 DEBUG: Inside try block, creating stages array for pipeline {PipelineId}", pipelineId);
            var stages = new[] { "Planning", "Designing", "Swarming", "Building", "Validating" };
            _logger.LogInformation("🔍 DEBUG: Stages array created successfully for pipeline {PipelineId}", pipelineId);
            _logger.LogInformation("📋 Pipeline {PipelineId} will execute {StageCount} stages: {Stages}", 
                pipelineId, stages.Length, string.Join(", ", stages));
            
            _logger.LogInformation("🔍 DEBUG: About to start for-loop for {StageCount} stages", stages.Length);
            
            for (int i = 0; i < stages.Length; i++)
            {
                _logger.LogInformation("🔍 DEBUG: For-loop iteration {Iteration}, processing stage index {StageIndex}", i + 1, i);
                var stage = stages[i];
                _logger.LogInformation("🔍 DEBUG: Stage name retrieved: {StageName}", stage);
                _logger.LogInformation("▶️ Pipeline {PipelineId} starting stage {StageIndex}/{TotalStages}: {StageName}", 
                    pipelineId, i + 1, stages.Length, stage);
                
                _logger.LogInformation("🔍 DEBUG: About to call ExecuteStageAsync for stage {StageName}", stage);
                await ExecuteStageAsync(pipelineId, stage, i + 1, config);
                _logger.LogInformation("🔍 DEBUG: ExecuteStageAsync COMPLETED for stage {StageName}", stage);
                
                _logger.LogInformation("✅ Pipeline {PipelineId} completed stage {StageIndex}/{TotalStages}: {StageName}", 
                    pipelineId, i + 1, stages.Length, stage);
                
                // Check if execution was cancelled
                var pipelineResult = await _pipelineExecutionRepository.GetByIdAsync(pipelineId);
                if (pipelineResult.IsSuccess && pipelineResult.Value?.Status == "Cancelled") 
                {
                    _logger.LogWarning("⏹️ Pipeline {PipelineId} was cancelled, stopping execution", pipelineId);
                    return;
                }
            }

            _logger.LogInformation("🏁 Pipeline {PipelineId} completing - all stages finished", pipelineId);
            await CompletePipelineAsync(pipelineId);
            _logger.LogInformation("✨ Pipeline {PipelineId} completed successfully", pipelineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline execution {PipelineId} failed", pipelineId);
            await FailPipelineAsync(pipelineId, ex.Message);
        }
    }

    private async Task ExecuteStageAsync(Guid pipelineId, string stageName, int order, TestConfiguration config)
    {
        _logger.LogInformation("🎯 ExecuteStageAsync ENTRY - Pipeline: {PipelineId}, Stage: {StageName}, Order: {Order}", 
            pipelineId, stageName, order);
        _logger.LogInformation("🔍 DEBUG: About to call _pipelineExecutionRepository.GetByIdAsync for pipeline {PipelineId}", pipelineId);
        
        var pipelineResult = await _pipelineExecutionRepository.GetByIdAsync(pipelineId);
        
        _logger.LogInformation("🔍 DEBUG: _pipelineExecutionRepository.GetByIdAsync COMPLETED - Success: {IsSuccess}", pipelineResult.IsSuccess);
        if (!pipelineResult.IsSuccess || pipelineResult.Value == null) return;

        var pipeline = pipelineResult.Value;
        pipeline.Stage = stageName;
        pipeline.StageStartedAt = DateTime.UtcNow;
        await _pipelineExecutionRepository.UpdateAsync(pipeline);

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        var stageExecution = new StageExecution
        {
            PipelineExecutionId = pipelineId,
            StageName = stageName,
            ExecutionOrder = order,
            Status = "Running"
        };

        dbContext.StageExecutions.Add(stageExecution);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("🚀 STAGE EXECUTION: Starting ExecuteStageAgents for {StageName} - StageId: {StageId}, Pipeline: {PipelineId}", 
            stageName, stageExecution.Id, pipelineId);

        // Execute real agents for the stage
        await ExecuteStageAgents(pipelineId, stageExecution.Id, stageName, config);

        _logger.LogInformation("✅ STAGE EXECUTION: ExecuteStageAgents COMPLETED for {StageName} - updating status to Completed", stageName);

        stageExecution.Status = "Completed";
        stageExecution.CompletedAt = DateTime.UtcNow;
        stageExecution.DurationSeconds = (int)(DateTime.UtcNow - stageExecution.StartedAt).TotalSeconds;
        stageExecution.ProgressPercentage = 100;

        dbContext.Update(stageExecution);
        await dbContext.SaveChangesAsync();
    }

    private async Task ExecuteStageAgents(Guid pipelineId, Guid stageId, string stageName, TestConfiguration config)
    {
        _logger.LogInformation("🔥 === ExecuteStageAgents ENTRY - Stage: {StageName} ===", stageName);
        _logger.LogInformation("🔥 Pipeline: {PipelineId}, StageId: {StageId}, ProjectId: {ProjectId}", 
            pipelineId, stageId, config.ProjectId);
            
        using var scope = _serviceScopeFactory.CreateScope();
        var agentExecutionService = scope.ServiceProvider.GetRequiredService<AgentExecutionService>();
        
        // Validate configuration
        if (config.ProjectId == Guid.Empty)
        {
            _logger.LogError("CRITICAL: ProjectId is Empty for stage {StageName}! This will cause agent validation failures.", stageName);
        }
        
        // Create execution context
        var context = new AgentExecutionContext
        {
            ProjectId = config.ProjectId,
            PipelineExecutionId = pipelineId,
            StageExecutionId = stageId,
            Stage = stageName,
            InputPrompt = $"Execute {stageName} stage for project with requirements: {GetStageRequirements(stageName)}",
            TargetLanguage = config.TargetLanguage,
            DeploymentTarget = config.DeploymentTarget,
            AgentPoolSize = config.AgentPoolSize,
            Parameters = config.Parameters ?? new Dictionary<string, object>()
        };

        _logger.LogInformation("Context created - ProjectId: {ProjectId}, InputPrompt: {InputPrompt}", 
            context.ProjectId, context.InputPrompt);

        try
        {
            _logger.LogInformation("🔀 STAGE ROUTING: Determining execution path for stage '{StageName}'", stageName);
            
            // Special handling for Swarming stage - spawn Code Unit Controllers dynamically
            if (stageName == "Swarming")
            {
                _logger.LogInformation("🐝 SWARMING PATH: Executing ExecuteSwarmStageAsync for {StageName}", stageName);
                await ExecuteSwarmStageAsync(pipelineId, stageId, config);
            }
            // Special handling for Building stage - message-based EnhancedBuilder trigger
            else if (stageName == "Building")
            {
                _logger.LogInformation("🏗️ BUILDING PATH: Executing ExecuteBuildingStageAsync for {StageName}", stageName);
                await ExecuteBuildingStageAsync(pipelineId, stageId, config);
            }
            // Special handling for Designing stage - individual agent execution to trigger ProcessDesignerOutputAsync hook
            else if (stageName == "Designing")
            {
                _logger.LogInformation("🎨 DESIGNING PATH: Executing ExecuteDesigningStageAsync for {StageName}", stageName);
                await ExecuteDesigningStageAsync(pipelineId, stageId, config);
            }
            else
            {
                _logger.LogInformation("🔄 TRADITIONAL PATH: Executing ExecuteStageAgentsAsync for {StageName}", stageName);
                // Execute all agents for this stage using traditional method
                var executions = await agentExecutionService.ExecuteStageAgentsAsync(
                    config.ProjectId,
                    pipelineId,
                    stageId,
                    stageName,
                    context.InputPrompt,
                    context);
                
                _logger.LogInformation("✅ TRADITIONAL PATH COMPLETED: Stage {StageName} completed with {ExecutionCount} agent executions", 
                    stageName, executions?.Count ?? 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage {StageName} execution failed with error: {ErrorMessage}", stageName, ex.Message);
            throw;
        }
    }
    
    private string GetStageRequirements(string stageName)
    {
        return stageName switch
        {
            "Planning" => "Analyze requirements, create project plan, design system architecture, and assess technical feasibility",
            "Designing" => "Create detailed system design, UI/UX specifications, and API documentation",
            "Swarming" => "Coordinate agent execution, distribute tasks efficiently, and monitor swarm performance",
            "Building" => "Generate source code, compile binaries, and create deployment packages",
            "Validating" => "Validate system quality, execute comprehensive tests, and perform quality assurance",
            _ => "Execute stage-specific tasks with high quality and reliability"
        };
    }

    private async Task CompletePipelineAsync(Guid pipelineId)
    {
        var pipelineResult = await _pipelineExecutionRepository.GetByIdAsync(pipelineId);
        if (!pipelineResult.IsSuccess || pipelineResult.Value == null) return;

        var pipeline = pipelineResult.Value;
        pipeline.Status = "Completed";
        pipeline.CompletedAt = DateTime.UtcNow;
        pipeline.ActualDurationSeconds = (int)(DateTime.UtcNow - pipeline.StartedAt).TotalSeconds;
        pipeline.ProgressPercentage = 100;

        await _pipelineExecutionRepository.UpdateAsync(pipeline);
        await _pipelineExecutionRepository.SaveChangesAsync();
    }

    private async Task FailPipelineAsync(Guid pipelineId, string errorMessage)
    {
        var pipelineResult = await _pipelineExecutionRepository.GetByIdAsync(pipelineId);
        if (!pipelineResult.IsSuccess || pipelineResult.Value == null) return;

        var pipeline = pipelineResult.Value;
        pipeline.Status = "Failed";
        pipeline.CompletedAt = DateTime.UtcNow;
        pipeline.ActualDurationSeconds = (int)(DateTime.UtcNow - pipeline.StartedAt).TotalSeconds;
        pipeline.ErrorMessage = errorMessage;

        await _pipelineExecutionRepository.UpdateAsync(pipeline);
        await _pipelineExecutionRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Execute Swarming stage by spawning Code Unit Controllers based on Designer output
    /// </summary>
    private async Task ExecuteSwarmStageAsync(Guid pipelineId, Guid stageId, TestConfiguration config)
    {
        _logger.LogInformation("=== Starting Swarming stage with Code Unit Controller spawning ===");
        _logger.LogInformation("Pipeline: {PipelineId}, Stage: {StageId}, Project: {ProjectId}", 
            pipelineId, stageId, config.ProjectId);

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        var messageCoordinatorService = scope.ServiceProvider.GetRequiredService<IMessageCoordinatorService>();

        try
        {
            // Step 1: Query function specifications from Designer stage output
            _logger.LogInformation("Querying function specifications from Designer output for pipeline {PipelineId}", pipelineId);
            
            var functionSpecs = await dbContext.FunctionSpecifications
                .Where(fs => fs.PipelineExecutionId == pipelineId)
                .ToListAsync();

            if (functionSpecs.Count == 0)
            {
                _logger.LogWarning("No function specifications found for pipeline {PipelineId}. Swarming stage will complete immediately.", pipelineId);
                return;
            }

            _logger.LogInformation("Found {SpecCount} function specifications to process", functionSpecs.Count);

            // Step 2: Group specifications by CodeUnit (UserController, ProductController, etc.)
            var codeUnitGroups = functionSpecs
                .GroupBy(fs => fs.CodeUnit)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToList();

            _logger.LogInformation("Grouped specifications into {GroupCount} code units: [{CodeUnits}]", 
                codeUnitGroups.Count, 
                string.Join(", ", codeUnitGroups.Select(g => g.Key)));

            // Step 3: Send CodeUnitAssignmentMessage for each group via MessageCoordinatorService
            foreach (var codeUnitGroup in codeUnitGroups)
            {
                var codeUnitName = codeUnitGroup.Key!;
                var functions = codeUnitGroup.ToList();

                _logger.LogInformation("Spawning Code Unit Controller for {CodeUnitName} with {FunctionCount} functions", 
                    codeUnitName, functions.Count);

                // Create CodeUnitAssignmentMessage with function specifications
                var assignment = new Core.Messages.CodeUnitAssignmentMessage
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    CodeUnitId = Guid.NewGuid().ToString("N"),
                    Name = codeUnitName,
                    UnitType = "Controller", // Default type, could be enhanced later
                    Namespace = functions.FirstOrDefault()?.Namespace,
                    Description = $"Code unit controller for {codeUnitName} with {functions.Count} functions",
                    
                    // Convert FunctionSpecifications to FunctionAssignmentMessages
                    Functions = functions.Select(fs => new Core.Messages.FunctionAssignmentMessage
                    {
                        FunctionSpecificationId = fs.Id.ToString(),
                        FunctionName = fs.FunctionName,
                        CodeUnit = fs.CodeUnit,
                        Signature = fs.Signature,
                        Description = fs.Description,
                        BusinessLogic = fs.BusinessLogic,
                        ValidationRules = fs.ValidationRules,
                        ErrorHandling = fs.ErrorHandling,
                        SecurityConsiderations = fs.SecurityConsiderations,
                        TestCases = fs.TestCases,
                        ComplexityRating = fs.ComplexityRating,
                        EstimatedMinutes = fs.EstimatedMinutes ?? 15,
                        Priority = fs.Priority,
                        TargetLanguage = fs.Language ?? config.TargetLanguage ?? "CSharp"
                    }).ToList(),
                    
                    SimpleFunctionCount = functions.Count(f => f.ComplexityRating < 4),
                    ComplexFunctionCount = functions.Count(f => f.ComplexityRating >= 7),
                    ComplexityRating = (int)functions.Average(f => f.ComplexityRating),
                    EstimatedMinutes = functions.Sum(f => f.EstimatedMinutes ?? 15),
                    Priority = "High",
                    TargetLanguage = config.TargetLanguage ?? "CSharp"
                };

                // Send the assignment message to spawn the Code Unit Controller
                var success = await messageCoordinatorService.SendCodeUnitAssignmentAsync(assignment);
                
                if (success)
                {
                    _logger.LogInformation("Successfully sent Code Unit Controller assignment for {CodeUnitName}", codeUnitName);
                }
                else
                {
                    _logger.LogError("Failed to send Code Unit Controller assignment for {CodeUnitName}", codeUnitName);
                }
            }

            _logger.LogInformation("=== Swarming stage initialization completed. Spawned {ControllerCount} Code Unit Controllers ===", 
                codeUnitGroups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Swarming stage execution failed: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private async Task ExecuteBuildingStageAsync(Guid pipelineId, Guid stageId, TestConfiguration config)
    {
        _logger.LogInformation("=== Starting Building stage with EnhancedBuilder ===");
        _logger.LogInformation("Pipeline: {PipelineId}, Stage: {StageId}, Project: {ProjectId}", 
            pipelineId, stageId, config.ProjectId);

        using var scope = _serviceScopeFactory.CreateScope();
        var agentExecutionService = scope.ServiceProvider.GetRequiredService<AgentExecutionService>();

        try
        {
            // Create execution context for EnhancedBuilder
            var context = new AgentExecutionContext
            {
                ProjectId = config.ProjectId,
                PipelineExecutionId = pipelineId,
                StageExecutionId = stageId,
                Stage = "Building",
                InputPrompt = "Aggregate individual MethodAgent code documents and build the complete project",
                TargetLanguage = config.TargetLanguage,
                DeploymentTarget = config.DeploymentTarget,
                Parameters = config.Parameters ?? new Dictionary<string, object>()
            };

            _logger.LogInformation("Executing EnhancedBuilder agent to aggregate MethodAgent outputs...");
            
            // Execute only the EnhancedBuilder agent (not the old monolithic agents)
            var execution = await agentExecutionService.ExecuteAgentAsync(
                config.ProjectId,
                pipelineId,
                stageId,
                "EnhancedBuilder",
                context.InputPrompt,
                context);

            _logger.LogInformation("EnhancedBuilder execution completed with status: {Status}", execution.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Building stage execution failed: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Execute Designing stage with individual agent execution to trigger ProcessDesignerOutputAsync hook
    /// </summary>
    private async Task ExecuteDesigningStageAsync(Guid pipelineId, Guid stageId, TestConfiguration config)
    {
        _logger.LogInformation("=== Starting Designing stage with individual agent execution ===");
        _logger.LogInformation("Pipeline: {PipelineId}, Stage: {StageId}, Project: {ProjectId}", 
            pipelineId, stageId, config.ProjectId);

        using var scope = _serviceScopeFactory.CreateScope();
        var agentExecutionService = scope.ServiceProvider.GetRequiredService<AgentExecutionService>();

        try
        {
            // Get Designer agent types for this stage
            var agentTypes = AgentFactory.GetAgentTypesForStage("Designing");
            _logger.LogInformation("Executing {AgentCount} Designer agents: [{AgentTypes}]", 
                agentTypes.Length, string.Join(", ", agentTypes));

            foreach (var agentType in agentTypes)
            {
                // Create execution context for each Designer agent
                var context = new AgentExecutionContext
                {
                    ProjectId = config.ProjectId,
                    PipelineExecutionId = pipelineId,
                    StageExecutionId = stageId,
                    Stage = "Designing",
                    InputPrompt = $"Create detailed {agentType} specifications with function definitions for requirements: {GetStageRequirements("Designing")}",
                    TargetLanguage = config.TargetLanguage,
                    DeploymentTarget = config.DeploymentTarget,
                    Parameters = config.Parameters ?? new Dictionary<string, object>()
                };

                _logger.LogInformation("Executing {AgentType} agent individually to trigger ProcessDesignerOutputAsync hook...", agentType);
                
                // Execute each Designer agent individually through ExecuteAgentAsync to trigger the hook
                var execution = await agentExecutionService.ExecuteAgentAsync(
                    config.ProjectId,
                    pipelineId,
                    stageId,
                    agentType,
                    context.InputPrompt,
                    context);

                _logger.LogInformation("{AgentType} execution completed with status: {Status}", agentType, execution.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Designing stage execution failed: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private static string[] GetAgentTypesForStage(string stageName)
    {
        return AgentFactory.GetAgentTypesForStage(stageName);
    }
}

public record TestConfiguration(
    Guid ProjectId,
    string? TargetLanguage = null,
    string? DeploymentTarget = null,
    int? AgentPoolSize = null,
    int? EstimatedDurationSeconds = null,
    Dictionary<string, object>? Parameters = null
);

public record TestbenchStats
{
    public int TotalExecutions { get; init; }
    public int ActiveExecutions { get; init; }
    public int CompletedExecutions { get; init; }
    public int FailedExecutions { get; init; }
    public double SuccessRate { get; init; }
    public int TotalAgentExecutions { get; init; }
    public int TotalTokensConsumed { get; init; }
    public decimal TotalCostUSD { get; init; }
    public double AverageExecutionTimeSeconds { get; init; }
}