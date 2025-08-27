namespace Ensemble.Maestro.Dotnet.Core.Agents;

/// <summary>
/// Context information passed to agents during execution
/// </summary>
public class AgentExecutionContext
{
    /// <summary>
    /// Unique identifier for this execution
    /// </summary>
    public Guid ExecutionId { get; set; }
    
    /// <summary>
    /// Project identifier this execution belongs to
    /// </summary>
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// Pipeline execution identifier
    /// </summary>
    public Guid PipelineExecutionId { get; set; }
    
    /// <summary>
    /// Stage execution identifier
    /// </summary>
    public Guid StageExecutionId { get; set; }
    
    /// <summary>
    /// Current pipeline stage
    /// </summary>
    public string Stage { get; set; } = string.Empty;
    
    /// <summary>
    /// Input prompt or requirements for the agent
    /// </summary>
    public string InputPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Target programming language
    /// </summary>
    public string? TargetLanguage { get; set; }
    
    /// <summary>
    /// Deployment target environment
    /// </summary>
    public string? DeploymentTarget { get; set; }
    
    /// <summary>
    /// Agent pool size for parallel execution
    /// </summary>
    public int? AgentPoolSize { get; set; }
    
    /// <summary>
    /// Maximum tokens allowed for LLM calls
    /// </summary>
    public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// Temperature setting for LLM calls
    /// </summary>
    public float Temperature { get; set; } = 0.7f;
    
    /// <summary>
    /// Model to use for LLM calls
    /// </summary>
    public string ModelUsed { get; set; } = "gpt-4o";
    
    /// <summary>
    /// Additional parameters for agent execution
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// Results from previous agents in the pipeline
    /// </summary>
    public Dictionary<string, object> PreviousResults { get; set; } = new();
    
    /// <summary>
    /// Project files and context
    /// </summary>
    public List<ProjectFile> ProjectFiles { get; set; } = new();
    
    /// <summary>
    /// Metadata for agent execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Start time of the execution
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Represents a project file with content and metadata
    /// </summary>
    public class ProjectFile
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }
}