using System.Text.Json;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
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

        var pipeline = new PipelineExecution
        {
            ProjectId = projectId,
            Stage = "Planning",
            Status = "Running",
            TargetLanguage = config.TargetLanguage,
            DeploymentTarget = config.DeploymentTarget,
            AgentPoolSize = config.AgentPoolSize,
            EstimatedDurationSeconds = config.EstimatedDurationSeconds,
            ExecutionConfig = JsonSerializer.Serialize(config)
        };

        await _pipelineExecutionRepository.AddAsync(pipeline);
        await _pipelineExecutionRepository.SaveChangesAsync();

        _ = Task.Run(() => ExecutePipelineAsync(pipeline.Id, config));

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
            .SumAsync(a => a.TotalTokens.Value);
        var totalCost = await dbContext.AgentExecutions
            .Where(a => a.ExecutionCost.HasValue)
            .SumAsync(a => a.ExecutionCost.Value);

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
        try
        {
            var stages = new[] { "Planning", "Designing", "Swarming", "Building", "Validating" };
            
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                await ExecuteStageAsync(pipelineId, stage, i + 1, config);
                
                // Check if execution was cancelled
                var pipelineResult = await _pipelineExecutionRepository.GetByIdAsync(pipelineId);
                if (pipelineResult.IsSuccess && pipelineResult.Value?.Status == "Cancelled") return;
            }

            await CompletePipelineAsync(pipelineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline execution {PipelineId} failed", pipelineId);
            await FailPipelineAsync(pipelineId, ex.Message);
        }
    }

    private async Task ExecuteStageAsync(Guid pipelineId, string stageName, int order, TestConfiguration config)
    {
        var pipelineResult = await _pipelineExecutionRepository.GetByIdAsync(pipelineId);
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

        // Simulate stage execution with mock agent calls
        await SimulateStageExecution(pipelineId, stageExecution.Id, stageName, config);

        stageExecution.Status = "Completed";
        stageExecution.CompletedAt = DateTime.UtcNow;
        stageExecution.DurationSeconds = (int)(DateTime.UtcNow - stageExecution.StartedAt).TotalSeconds;
        stageExecution.ProgressPercentage = 100;

        dbContext.Update(stageExecution);
        await dbContext.SaveChangesAsync();
    }

    private async Task SimulateStageExecution(Guid pipelineId, Guid stageId, string stageName, TestConfiguration config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaestroDbContext>();
        
        var agentTypes = GetAgentTypesForStage(stageName);
        var random = new Random();

        foreach (var agentType in agentTypes)
        {
            var agentExecution = new AgentExecution
            {
                ProjectId = config.ProjectId,
                PipelineExecutionId = pipelineId,
                StageExecutionId = stageId,
                AgentType = agentType,
                AgentName = $"{agentType}Agent",
                Status = "Running",
                InputPrompt = $"Execute {stageName} stage with {agentType} specialization",
                Priority = "Medium",
                ModelUsed = "gpt-4o",
                Temperature = 0.7f,
                MaxTokens = 4000
            };

            dbContext.AgentExecutions.Add(agentExecution);
            await dbContext.SaveChangesAsync();

            // Simulate processing time
            await Task.Delay(random.Next(1000, 3000));

            // Complete the agent execution
            agentExecution.Status = "Completed";
            agentExecution.CompletedAt = DateTime.UtcNow;
            agentExecution.DurationSeconds = (int)(DateTime.UtcNow - agentExecution.StartedAt).TotalSeconds;
            agentExecution.OutputResponse = $"Successfully completed {stageName} stage processing";
            agentExecution.InputTokens = random.Next(500, 1500);
            agentExecution.OutputTokens = random.Next(200, 800);
            agentExecution.TotalTokens = agentExecution.InputTokens + agentExecution.OutputTokens;
            agentExecution.ExecutionCost = agentExecution.TotalTokens * 0.00002m; // Simulate cost
            agentExecution.QualityScore = random.Next(80, 95);
            agentExecution.ConfidenceScore = random.Next(75, 90);

            dbContext.Update(agentExecution);
            await dbContext.SaveChangesAsync();
        }
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

    private static string[] GetAgentTypesForStage(string stageName)
    {
        return stageName switch
        {
            "Planning" => new[] { "Planner", "Architect", "Analyst" },
            "Designing" => new[] { "Designer", "UIDesigner", "APIDesigner" },
            "Swarming" => new[] { "Coordinator", "Distributor", "Monitor" },
            "Building" => new[] { "Builder", "CodeGenerator", "Compiler" },
            "Validating" => new[] { "Validator", "Tester", "QualityAssurance" },
            _ => new[] { "GenericAgent" }
        };
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