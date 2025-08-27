using Ensemble.Maestro.Dotnet.Core.Agents;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Ensemble.Maestro.Dotnet.Core.Services;

public class CodeUnitControllerService
{
    private readonly IMessageCoordinatorService _messageCoordinator;
    private readonly IAgentFactory _agentFactory;
    private readonly ICodeDocumentStorageService _codeDocumentStorageService;
    private readonly ILogger<CodeUnitControllerService> _logger;
    private readonly Dictionary<string, int> _activeJobs = new();
    private readonly object _lockObject = new();

    public CodeUnitControllerService(
        IMessageCoordinatorService messageCoordinator,
        IAgentFactory agentFactory,
        ICodeDocumentStorageService codeDocumentStorageService,
        ILogger<CodeUnitControllerService> logger)
    {
        _messageCoordinator = messageCoordinator;
        _agentFactory = agentFactory;
        _codeDocumentStorageService = codeDocumentStorageService;
        _logger = logger;
    }

    public async Task ProcessCodeUnitAssignmentAsync(CodeUnitAssignmentMessage assignment)
    {
        try
        {
            _logger.LogInformation("🔧 CUCS Processing CodeUnit assignment: {CodeUnitName} with {FunctionCount} functions",
                assignment.Name, assignment.Functions?.Count ?? 0);

            if (assignment.Functions == null || assignment.Functions.Count == 0)
            {
                _logger.LogWarning("No functions found in CodeUnit assignment: {CodeUnitName}", assignment.Name);
                await NotifyBuilderIfQueueEmpty(assignment.CodeUnitId, assignment.Name);
                return;
            }

            // Track active jobs for this project/codeunit
            var jobKey = $"{assignment.CodeUnitId}:{assignment.Name}";
            lock (_lockObject)
            {
                _activeJobs[jobKey] = assignment.Functions.Count;
            }

            // Process each function individually
            var tasks = assignment.Functions.Select(function => 
                ProcessFunctionAsync(assignment.CodeUnitId, assignment.Name, function, jobKey));
            
            await Task.WhenAll(tasks);

            _logger.LogInformation("Completed processing CodeUnit: {CodeUnitName}", assignment.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CodeUnit assignment: {CodeUnitName}", assignment.Name);
            
            // Send error to Builder queue
            await _messageCoordinator.SendBuilderErrorAsync(new BuilderErrorMessage
            {
                ProjectId = assignment.CodeUnitId, // Use CodeUnitId as project context
                CodeUnitName = assignment.Name,
                ErrorType = "ProcessingError",
                ErrorMessage = $"Failed to process CodeUnit: {ex.Message}",
                Severity = 8
            });
        }
    }

    private async Task ProcessFunctionAsync(string projectId, string codeUnitName, FunctionAssignmentMessage function, string jobKey)
    {
        try
        {
            // Determine the appropriate MethodAgent type based on function characteristics
            var agentType = DetermineMethodAgentType(function);
            
            // Create job packet for the MethodAgent
            var jobPacket = CreateJobPacket(projectId, codeUnitName, function);
            
            // Spawn and execute the MethodAgent
            var methodAgent = _agentFactory.CreateAgent(agentType) as IMethodAgent;
            if (methodAgent == null)
            {
                throw new InvalidOperationException($"Failed to create MethodAgent of type: {agentType}");
            }

            _logger.LogInformation("⚡ CUCS Spawning {AgentType} for function: {FunctionName} (complexity: {Complexity})", 
                agentType, function.FunctionName, function.ComplexityRating);
            
            // Execute the agent with the job packet
            var result = await methodAgent.ExecuteAsync(jobPacket);
            
            // Store the individual code document result
            await StoreCodeDocument(projectId, codeUnitName, function.FunctionName ?? "Unknown", result);
            
            _logger.LogInformation("✅ CUCS Completed function: {FunctionName} in CodeUnit: {CodeUnitName}", 
                function.FunctionName, codeUnitName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing function: {FunctionName} in CodeUnit: {CodeUnitName}", 
                function.FunctionName, codeUnitName);
            
            // Send function-specific error to Builder queue
            await _messageCoordinator.SendBuilderErrorAsync(new BuilderErrorMessage
            {
                ProjectId = projectId,
                CodeUnitName = codeUnitName,
                FunctionName = function.FunctionName,
                ErrorType = "FunctionProcessingError",
                ErrorMessage = $"Failed to process function {function.FunctionName}: {ex.Message}",
                Severity = 6
            });
        }
        finally
        {
            // Decrement active job count and check if queue is empty
            await DecrementJobCountAndCheckQueue(jobKey, projectId, codeUnitName);
        }
    }

    private string DetermineMethodAgentType(FunctionAssignmentMessage function)
    {
        // Mechanical algorithm to determine agent type based on function characteristics
        // No AI involved - pure algorithmic decision making
        
        // Determine agent type based on function characteristics from FunctionAssignmentMessage
        var signature = function.Signature?.ToLower() ?? "";
        var description = function.Description?.ToLower() ?? "";
        
        if (signature.Contains("async") || signature.Contains("task"))
        {
            if (function.ComplexityRating > 5)
                return "ComplexAsyncMethodAgent";
            return "AsyncMethodAgent";
        }
        
        if (signature.Contains("task"))
            return "TaskMethodAgent";
        
        if (function.ComplexityRating > 8)
            return "ComplexMethodAgent";
        
        if (signature.Contains("static"))
            return "StaticMethodAgent";
        
        if (signature.Contains("private"))
            return "PrivateMethodAgent";
        
        // Default method agent
        return "StandardMethodAgent";
    }

    private MethodJobPacket CreateJobPacket(string projectId, string codeUnitName, FunctionAssignmentMessage function)
    {
        return new MethodJobPacket
        {
            JobId = Guid.NewGuid().ToString("N"),
            ProjectId = projectId,
            CodeUnitName = codeUnitName,
            Function = ConvertToFunctionSpec(function),
            CreatedAt = DateTime.UtcNow,
            Priority = CalculatePriority(function),
            Context = new Dictionary<string, object>
            {
                ["codeUnitName"] = codeUnitName,
                ["projectId"] = projectId,
                ["functionComplexity"] = function.ComplexityRating
            }
        };
    }

    private int CalculatePriority(FunctionAssignmentMessage function)
    {
        // Mechanical priority calculation based on FunctionAssignmentMessage
        int priority = 5; // Default
        
        var signature = function.Signature?.ToLower() ?? "";
        var functionName = function.FunctionName?.ToLower() ?? "";
        
        if (signature.Contains("public")) priority += 2;
        if (signature.Contains("async") || signature.Contains("task")) priority += 1;
        if (function.ComplexityRating > 5) priority += 1;
        if (functionName.Contains("main")) priority += 3;
        if (function.Priority?.ToLower() == "critical") priority += 2;
        if (function.Priority?.ToLower() == "high") priority += 1;
        
        return Math.Min(priority, 10);
    }

    private FunctionSpec ConvertToFunctionSpec(FunctionAssignmentMessage function)
    {
        return new FunctionSpec
        {
            Name = function.FunctionName ?? "Unknown",
            ReturnType = ExtractReturnType(function.Signature),
            Parameters = new List<ParameterSpec>(), // Would need to parse from signature
            AccessModifier = ExtractAccessModifier(function.Signature),
            IsStatic = function.Signature?.Contains("static") == true,
            IsAsync = function.Signature?.Contains("async") == true || function.Signature?.Contains("Task") == true,
            Description = function.Description ?? ""
        };
    }

    private string ExtractReturnType(string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return "void";
        
        // Simple extraction - in real implementation would use proper parsing
        if (signature.Contains("Task<")) return "Task<T>";
        if (signature.Contains("Task")) return "Task";
        if (signature.Contains("string")) return "string";
        if (signature.Contains("int")) return "int";
        if (signature.Contains("bool")) return "bool";
        
        return "object";
    }

    private string ExtractAccessModifier(string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return "public";
        
        if (signature.Contains("private")) return "private";
        if (signature.Contains("protected")) return "protected";
        if (signature.Contains("internal")) return "internal";
        
        return "public";
    }

    private async Task StoreCodeDocument(string projectId, string codeUnitName, string functionName, AgentExecutionResult result)
    {
        // Store individual code document using the CodeDocumentStorageService
        _logger.LogInformation("💾 CUCS Storing individual code document for function: {FunctionName}", functionName);
        
        try
        {
            var documentId = await _codeDocumentStorageService.StoreCodeDocumentAsync(
                projectId, codeUnitName, functionName, result);
                
            _logger.LogInformation("📄 Stored individual code document {DocumentId} for {CodeUnitName}.{FunctionName} ({Size} chars)",
                documentId, codeUnitName, functionName, result.OutputResponse?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to store code document for {CodeUnitName}.{FunctionName}", 
                codeUnitName, functionName);
        }
    }

    private async Task DecrementJobCountAndCheckQueue(string jobKey, string projectId, string codeUnitName)
    {
        bool isQueueEmpty = false;
        
        lock (_lockObject)
        {
            if (_activeJobs.ContainsKey(jobKey))
            {
                _activeJobs[jobKey]--;
                if (_activeJobs[jobKey] <= 0)
                {
                    _activeJobs.Remove(jobKey);
                    isQueueEmpty = true;
                }
            }
        }
        
        if (isQueueEmpty)
        {
            await NotifyBuilderIfQueueEmpty(projectId, codeUnitName);
        }
    }

    private async Task NotifyBuilderIfQueueEmpty(string projectId, string codeUnitName)
    {
        _logger.LogInformation("🏗️ CUCS Queue empty for CodeUnit: {CodeUnitName}, notifying Builder", codeUnitName);
        
        await _messageCoordinator.SendBuilderNotificationAsync(new BuilderNotificationMessage
        {
            ProjectId = projectId,
            CodeUnitName = codeUnitName,
            Status = "Complete",
            CompletedAt = DateTime.UtcNow
        });
        
        _logger.LogInformation("📨 CUCS Sent Builder notification for completed CodeUnit: {CodeUnitName}", codeUnitName);
    }
}

// Supporting classes
public class MethodJobPacket
{
    public string JobId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string CodeUnitName { get; set; } = string.Empty;
    public FunctionSpec Function { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class FunctionSpec
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterSpec>? Parameters { get; set; }
    public string AccessModifier { get; set; } = "public";
    public bool IsStatic { get; set; }
    public bool IsAsync { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ParameterSpec
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public interface IMethodAgent
{
    Task<AgentExecutionResult> ExecuteAsync(MethodJobPacket jobPacket);
}