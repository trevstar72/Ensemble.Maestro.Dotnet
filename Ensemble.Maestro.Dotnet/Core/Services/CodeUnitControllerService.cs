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
        _logger.LogInformation("🚀 CUCS ProcessCodeUnitAssignmentAsync ENTRY - CodeUnit: {CodeUnitName}, ID: {CodeUnitId}", 
            assignment?.Name ?? "NULL", assignment?.CodeUnitId ?? "NULL");
        
        try
        {
            if (assignment == null)
            {
                _logger.LogError("🚨 CUCS ProcessCodeUnitAssignmentAsync received NULL assignment - this should never happen!");
                return;
            }
            
            _logger.LogInformation("🔧 CUCS Processing CodeUnit assignment: {CodeUnitName} with {FunctionCount} functions, Assignment ID: {AssignmentId}",
                assignment.Name, assignment.Functions?.Count ?? 0, assignment.AssignmentId);
            
            _logger.LogInformation("🔍 CUCS Assignment Details - Priority: {Priority}, Target Language: {Language}, Due: {DueAt}", 
                assignment.Priority, assignment.TargetLanguage, assignment.DueAt);

            if (assignment.Functions == null || assignment.Functions.Count == 0)
            {
                _logger.LogWarning("⚠️ CUCS No functions found in CodeUnit assignment: {CodeUnitName} - notifying builder", assignment.Name);
                await NotifyBuilderIfQueueEmpty(assignment.CodeUnitId, assignment.Name);
                return;
            }

            _logger.LogInformation("📋 CUCS Function list for {CodeUnitName}: [{Functions}]", assignment.Name, 
                string.Join(", ", assignment.Functions.Select(f => $"{f.FunctionName}(complexity:{f.ComplexityRating})")));

            // Track active jobs for this project/codeunit
            var jobKey = $"{assignment.CodeUnitId}:{assignment.Name}";
            _logger.LogInformation("🔒 CUCS Setting up job tracking with key: {JobKey} for {FunctionCount} functions", jobKey, assignment.Functions.Count);
            
            lock (_lockObject)
            {
                _activeJobs[jobKey] = assignment.Functions.Count;
                _logger.LogDebug("🔒 CUCS Job count set to {JobCount} for key {JobKey}", assignment.Functions.Count, jobKey);
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
        _logger.LogInformation("🔧 CUCS ProcessFunctionAsync ENTRY - Function: {FunctionName}, CodeUnit: {CodeUnitName}, JobKey: {JobKey}", 
            function?.FunctionName ?? "NULL", codeUnitName ?? "NULL", jobKey ?? "NULL");
        
        try
        {
            if (function == null)
            {
                _logger.LogError("🚨 CUCS ProcessFunctionAsync received NULL function - this should never happen!");
                return;
            }
            
            _logger.LogInformation("📝 CUCS Function details - Name: {FunctionName}, Signature: {Signature}, Complexity: {Complexity}, Priority: {Priority}", 
                function.FunctionName, function.Signature, function.ComplexityRating, function.Priority);
            
            // Determine the appropriate MethodAgent type based on function characteristics
            _logger.LogInformation("🎯 CUCS Determining MethodAgent type for function: {FunctionName}", function.FunctionName);
            var agentType = DetermineMethodAgentType(function);
            _logger.LogInformation("✅ CUCS Determined MethodAgent type: {AgentType} for function: {FunctionName}", agentType, function.FunctionName);
            
            // Create job packet for the MethodAgent
            _logger.LogInformation("📦 CUCS Creating job packet for function: {FunctionName}", function.FunctionName);
            var jobPacket = CreateJobPacket(projectId, codeUnitName, function);
            _logger.LogInformation("✅ CUCS Created job packet - JobId: {JobId}, Priority: {Priority}, Context keys: [{ContextKeys}]", 
                jobPacket.JobId, jobPacket.Priority, string.Join(", ", jobPacket.Context?.Keys ?? Enumerable.Empty<string>()));
            
            // Spawn and execute the MethodAgent
            _logger.LogInformation("🏭 CUCS Calling AgentFactory.CreateAgent for type: {AgentType}", agentType);
            var agent = _agentFactory.CreateAgent(agentType);
            _logger.LogInformation("🔍 CUCS AgentFactory returned agent: {AgentType} (IsNull: {IsNull})", 
                agent?.GetType().Name ?? "NULL", agent == null);
                
            var methodAgent = agent as IMethodAgent;
            _logger.LogInformation("🔄 CUCS Cast to IMethodAgent: {MethodAgentType} (IsNull: {IsNull})", 
                methodAgent?.GetType().Name ?? "NULL", methodAgent == null);
                
            if (methodAgent == null)
            {
                var errorMsg = $"Failed to create MethodAgent of type: {agentType} - AgentFactory returned: {agent?.GetType().Name ?? "NULL"}";
                _logger.LogError("❌ CUCS {ErrorMessage}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _logger.LogInformation("⚡ CUCS Successfully created {AgentType} for function: {FunctionName} (complexity: {Complexity})", 
                agentType, function.FunctionName, function.ComplexityRating);
            
            // Execute the agent with the job packet
            _logger.LogInformation("🚀 CUCS Executing MethodAgent.ExecuteAsync for function: {FunctionName} with JobId: {JobId}", 
                function.FunctionName, jobPacket.JobId);
            var result = await methodAgent.ExecuteAsync(jobPacket);
            _logger.LogInformation("✅ CUCS MethodAgent.ExecuteAsync completed for function: {FunctionName} - Success: {Success}, Output length: {OutputLength}", 
                function.FunctionName, result?.Success ?? false, result?.OutputResponse?.Length ?? 0);
            
            // Store the individual code document result
            _logger.LogInformation("💾 CUCS Calling StoreCodeDocument for function: {FunctionName}", function.FunctionName);
            await StoreCodeDocument(projectId, codeUnitName, function.FunctionName ?? "Unknown", result);
            
            _logger.LogInformation("✅ CUCS ProcessFunctionAsync COMPLETED successfully for function: {FunctionName} in CodeUnit: {CodeUnitName}", 
                function.FunctionName, codeUnitName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CUCS ProcessFunctionAsync EXCEPTION processing function: {FunctionName} in CodeUnit: {CodeUnitName} - Exception: {ExceptionType}, Message: {ExceptionMessage}, Stack: {StackTrace}", 
                function?.FunctionName ?? "NULL", codeUnitName, ex.GetType().Name, ex.Message, ex.StackTrace);
            
            // Send function-specific error to Builder queue
            _logger.LogInformation("📤 CUCS Sending BuilderErrorMessage for failed function: {FunctionName}", function?.FunctionName);
            await _messageCoordinator.SendBuilderErrorAsync(new BuilderErrorMessage
            {
                ProjectId = projectId,
                CodeUnitName = codeUnitName,
                FunctionName = function?.FunctionName,
                ErrorType = "FunctionProcessingError",
                ErrorMessage = $"Failed to process function {function?.FunctionName}: {ex.Message}",
                Severity = 6
            });
            _logger.LogInformation("✅ CUCS Sent BuilderErrorMessage for function: {FunctionName}", function?.FunctionName);
        }
        finally
        {
            // Decrement active job count and check if queue is empty
            _logger.LogInformation("🔻 CUCS ProcessFunctionAsync FINALLY block - calling DecrementJobCountAndCheckQueue for function: {FunctionName}, JobKey: {JobKey}", 
                function?.FunctionName ?? "NULL", jobKey);
            await DecrementJobCountAndCheckQueue(jobKey, projectId, codeUnitName);
        }
    }

    private string DetermineMethodAgentType(FunctionAssignmentMessage function)
    {
        _logger.LogInformation("🎯 CUCS DetermineMethodAgentType ENTRY - analyzing function: {FunctionName}", function?.FunctionName ?? "NULL");
        
        // For now, all method agents use the same MethodAgent type
        // The complexity and characteristics are passed in the job packet context
        // Future enhancement: Create specialized method agent types as needed
        var agentType = "MethodAgent";
        
        _logger.LogInformation("🎯 CUCS DetermineMethodAgentType determined type: {AgentType} for function: {FunctionName} (complexity: {Complexity})", 
            agentType, function?.FunctionName ?? "NULL", function?.ComplexityRating ?? 0);
            
        return agentType;
    }

    private MethodJobPacket CreateJobPacket(string projectId, string codeUnitName, FunctionAssignmentMessage function)
    {
        _logger.LogInformation("📦 CUCS CreateJobPacket ENTRY - ProjectId: {ProjectId}, CodeUnit: {CodeUnitName}, Function: {FunctionName}", 
            projectId ?? "NULL", codeUnitName ?? "NULL", function?.FunctionName ?? "NULL");
        
        var jobId = Guid.NewGuid().ToString("N");
        var priority = CalculatePriority(function);
        
        _logger.LogInformation("📦 CUCS Creating JobPacket - JobId: {JobId}, Priority: {Priority}, Complexity: {Complexity}", 
            jobId, priority, function?.ComplexityRating ?? 0);
        
        var functionSpec = ConvertToFunctionSpec(function);
        _logger.LogInformation("📦 CUCS Created FunctionSpec - Name: {Name}, ReturnType: {ReturnType}, IsAsync: {IsAsync}, IsStatic: {IsStatic}", 
            functionSpec.Name, functionSpec.ReturnType, functionSpec.IsAsync, functionSpec.IsStatic);
        
        var jobPacket = new MethodJobPacket
        {
            JobId = jobId,
            ProjectId = projectId,
            CodeUnitName = codeUnitName,
            Function = functionSpec,
            CreatedAt = DateTime.UtcNow,
            Priority = priority,
            Context = new Dictionary<string, object>
            {
                ["codeUnitName"] = codeUnitName,
                ["projectId"] = projectId,
                ["functionComplexity"] = function.ComplexityRating
            }
        };
        
        _logger.LogInformation("✅ CUCS CreateJobPacket COMPLETED - JobId: {JobId}, Context keys: [{ContextKeys}]", 
            jobPacket.JobId, string.Join(", ", jobPacket.Context.Keys));
            
        return jobPacket;
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
        _logger.LogInformation("🔻 CUCS DecrementJobCountAndCheckQueue ENTRY - JobKey: {JobKey}, ProjectId: {ProjectId}, CodeUnit: {CodeUnitName}", 
            jobKey ?? "NULL", projectId ?? "NULL", codeUnitName ?? "NULL");
        
        bool isQueueEmpty = false;
        int remainingJobs = 0;
        
        lock (_lockObject)
        {
            if (_activeJobs.ContainsKey(jobKey))
            {
                var previousCount = _activeJobs[jobKey];
                _activeJobs[jobKey]--;
                remainingJobs = _activeJobs[jobKey];
                
                _logger.LogInformation("🔻 CUCS Job count decremented for {JobKey}: {PreviousCount} → {RemainingJobs}", 
                    jobKey, previousCount, remainingJobs);
                
                if (_activeJobs[jobKey] <= 0)
                {
                    _activeJobs.Remove(jobKey);
                    isQueueEmpty = true;
                    _logger.LogInformation("🏁 CUCS Queue is now EMPTY for JobKey: {JobKey} - removed from active jobs", jobKey);
                }
                else
                {
                    _logger.LogInformation("⏳ CUCS Queue still has {RemainingJobs} jobs remaining for JobKey: {JobKey}", remainingJobs, jobKey);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ CUCS JobKey {JobKey} not found in active jobs dictionary - this may indicate a tracking issue", jobKey);
            }
        }
        
        if (isQueueEmpty)
        {
            _logger.LogInformation("🚀 CUCS Queue empty detected - calling NotifyBuilderIfQueueEmpty for CodeUnit: {CodeUnitName}", codeUnitName);
            await NotifyBuilderIfQueueEmpty(projectId, codeUnitName);
        }
        else
        {
            _logger.LogInformation("⏳ CUCS Not notifying Builder - queue still has jobs remaining for CodeUnit: {CodeUnitName}", codeUnitName);
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