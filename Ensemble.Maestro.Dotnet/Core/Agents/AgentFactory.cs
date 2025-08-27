using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Agents.Planning;
using Ensemble.Maestro.Dotnet.Core.Agents.Designing;
using Ensemble.Maestro.Dotnet.Core.Agents.Swarming;
using Ensemble.Maestro.Dotnet.Core.Agents.Building;
using Ensemble.Maestro.Dotnet.Core.Agents.Validating;

namespace Ensemble.Maestro.Dotnet.Core.Agents;

/// <summary>
/// Factory for creating and managing agent instances
/// </summary>
public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentFactory> _logger;
    
    private static readonly Dictionary<string, string[]> StageAgentMapping = new()
    {
        { "Planning", new[] { "Planner", "Architect", "Analyst" } },
        { "Designing", new[] { "Designer", "UIDesigner", "APIDesigner" } },
        { "Swarming", new[] { "MethodAgent" } },
        { "Building", new[] { "Builder", "CodeGenerator", "Compiler" } },
        { "Validating", new[] { "Validator", "Tester", "QualityAssurance" } }
    };
    
    private static readonly Dictionary<string, Type> AgentTypeMapping = new()
    {
        // Planning agents
        { "Planner", typeof(PlannerAgent) },
        { "Architect", typeof(ArchitectAgent) },
        { "Analyst", typeof(AnalystAgent) },
        
        // Designing agents
        { "Designer", typeof(DesignerAgent) },
        { "UIDesigner", typeof(UIDesignerAgent) },
        { "APIDesigner", typeof(APIDesignerAgent) },
        
        // Swarming agents
        { "MethodAgent", typeof(MethodAgent) },
        
        // Building agents
        { "Builder", typeof(BuilderAgent) },
        { "EnhancedBuilder", typeof(EnhancedBuilderAgent) },
        { "CodeGenerator", typeof(CodeGeneratorAgent) },
        { "Compiler", typeof(CompilerAgent) },
        
        // Validating agents
        { "Validator", typeof(ValidatorAgent) },
        { "Tester", typeof(TesterAgent) },
        { "QualityAssurance", typeof(QualityAssuranceAgent) }
    };
    
    public AgentFactory(IServiceProvider serviceProvider, ILogger<AgentFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public IAgent? CreateAgent(string agentType)
    {
        if (!AgentTypeMapping.TryGetValue(agentType, out var agentTypeClass))
        {
            _logger.LogWarning("Unknown agent type: {AgentType}", agentType);
            return null;
        }
        
        try
        {
            // Get logger for the specific agent type
            var loggerType = typeof(ILogger<>).MakeGenericType(typeof(BaseAgent));
            var logger = _serviceProvider.GetRequiredService(loggerType);
            
            // Get LLM service
            var llmService = _serviceProvider.GetRequiredService<ILLMService>();
            
            // Create agent instance - handle different constructor signatures
            IAgent agent;
            if (IsDesignerAgent(agentType))
            {
                // Designer agents need the additional storage service
                var designerStorageService = _serviceProvider.GetRequiredService<IDesignerOutputStorageService>();
                agent = (IAgent)Activator.CreateInstance(agentTypeClass, logger, llmService, designerStorageService)!;
            }
            else if (IsSwarmAgent(agentType))
            {
                // Swarm agents need additional services
                var messageCoordinator = _serviceProvider.GetRequiredService<IMessageCoordinatorService>();
                var swarmConfig = _serviceProvider.GetRequiredService<ISwarmConfigurationService>();
                
                if (agentType == "MethodAgent")
                {
                    agent = (IAgent)Activator.CreateInstance(agentTypeClass, logger, llmService, 
                        messageCoordinator, swarmConfig)!;
                }
                else
                {
                    // Legacy swarm agents use standard constructor
                    agent = (IAgent)Activator.CreateInstance(agentTypeClass, logger, llmService)!;
                }
            }
            else if (IsEnhancedBuilderAgent(agentType))
            {
                // Enhanced Builder agent needs additional services
                var codeDocumentStorageService = _serviceProvider.GetRequiredService<ICodeDocumentStorageService>();
                var messageCoordinatorService = _serviceProvider.GetRequiredService<IMessageCoordinatorService>();
                var buildExecutionService = _serviceProvider.GetRequiredService<IBuildExecutionService>();
                
                agent = (IAgent)Activator.CreateInstance(agentTypeClass, logger, llmService, 
                    codeDocumentStorageService, messageCoordinatorService, buildExecutionService)!;
            }
            else
            {
                // Other agents use the standard constructor
                agent = (IAgent)Activator.CreateInstance(agentTypeClass, logger, llmService)!;
            }
            
            _logger.LogDebug("Created agent instance: {AgentType}", agentType);
            return agent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent of type: {AgentType}", agentType);
            return null;
        }
    }
    
    public IEnumerable<string> GetAvailableAgentTypes()
    {
        return AgentTypeMapping.Keys;
    }
    
    public IEnumerable<IAgent> GetAgentsForStage(string stageName)
    {
        if (!StageAgentMapping.TryGetValue(stageName, out var agentTypes))
        {
            _logger.LogWarning("Unknown stage name: {StageName}", stageName);
            return Enumerable.Empty<IAgent>();
        }
        
        var agents = new List<IAgent>();
        
        foreach (var agentType in agentTypes)
        {
            var agent = CreateAgent(agentType);
            if (agent != null)
            {
                agents.Add(agent);
            }
        }
        
        return agents;
    }
    
    public bool IsAgentTypeAvailable(string agentType)
    {
        return AgentTypeMapping.ContainsKey(agentType);
    }
    
    /// <summary>
    /// Get agent types for a specific stage
    /// </summary>
    /// <param name="stageName">Stage name</param>
    /// <returns>Array of agent types for the stage</returns>
    public static string[] GetAgentTypesForStage(string stageName)
    {
        return StageAgentMapping.TryGetValue(stageName, out var agentTypes) 
            ? agentTypes 
            : new[] { "GenericAgent" };
    }
    
    /// <summary>
    /// Check if the agent type is a designer agent requiring additional services
    /// </summary>
    private static bool IsDesignerAgent(string agentType)
    {
        return agentType == "Designer" || agentType == "UIDesigner" || agentType == "APIDesigner";
    }
    
    /// <summary>
    /// Check if the agent type is a swarm agent requiring additional services
    /// </summary>
    private static bool IsSwarmAgent(string agentType)
    {
        return agentType == "MethodAgent";
    }
    
    /// <summary>
    /// Determines if an agent type requires Enhanced Builder dependencies
    /// </summary>
    private static bool IsEnhancedBuilderAgent(string agentType)
    {
        return agentType == "EnhancedBuilder";
    }
}