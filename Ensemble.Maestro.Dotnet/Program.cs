using Ensemble.Maestro.Dotnet.Components;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Agents;
using Ensemble.Maestro.Dotnet.Core.Agents.Building;
using Ensemble.Maestro.Dotnet.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using FastEndpoints;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTPS only
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        // Configure HTTPS options if needed
    });
});

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 5001;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add Entity Framework with SQL Server
builder.Services.AddDbContext<MaestroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository pattern services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IPipelineExecutionRepository, PipelineExecutionRepository>();
builder.Services.AddScoped<IAgentExecutionRepository, AgentExecutionRepository>();

// Add Service layer services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<TestbenchService>();
builder.Services.AddScoped<ExportService>();

// Add LLM services
builder.Services.AddScoped<ILLMService, LLMService>();

// Add Agent services
builder.Services.AddScoped<IAgentFactory, AgentFactory>();
builder.Services.AddScoped<AgentExecutionService>();

// Add Cross-database coordination services
builder.Services.AddScoped<ICrossReferenceService, CrossReferenceService>();
builder.Services.AddScoped<INeo4jService, Neo4jService>();
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

// Add Designer output storage service
builder.Services.AddScoped<IDesignerOutputStorageService, DesignerOutputStorageService>();

// Add Redis message queue service
builder.Services.AddScoped<IRedisMessageQueueService, RedisMessageQueueService>();

// Add Message Coordinator service
builder.Services.AddScoped<IMessageCoordinatorService, MessageCoordinatorService>();

// Add Swarm Configuration service
builder.Services.AddScoped<ISwarmConfigurationService, SwarmConfigurationService>();

// Add Code Unit Controller services
builder.Services.AddScoped<CodeUnitControllerService>();

// Add Code Document Storage service
builder.Services.AddSingleton<ICodeDocumentStorageService, CodeDocumentStorageService>();

// Add Build Execution service
builder.Services.AddScoped<IBuildExecutionService, BuildExecutionService>();

// Add Enhanced Builder Agent
builder.Services.AddScoped<EnhancedBuilderAgent>();
builder.Services.AddHostedService<CodeUnitControllerHostedService>();

// Configure SwarmConfiguration from appsettings
builder.Services.Configure<SwarmConfiguration>(
    builder.Configuration.GetSection(SwarmConfiguration.SectionName));

// Add Semantic Kernel
var kernelBuilder = builder.Services.AddKernel();

// Configure OpenAI service if API key is provided
var openAiApiKey = builder.Configuration["SemanticKernel:OpenAI:ApiKey"];
if (!string.IsNullOrEmpty(openAiApiKey) && openAiApiKey != "PLACEHOLDER_OPENAI_API_KEY")
{
    kernelBuilder.AddOpenAIChatCompletion(
        builder.Configuration["SemanticKernel:OpenAI:ModelId"] ?? "gpt-4o",
        openAiApiKey);
}

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Controllers disabled due to .NET 10 preview OpenAPI compatibility issues
// Will work with FastEndpoints only for now
// builder.Services.AddControllers(mvcOptions =>
// {
//     mvcOptions.AddResultConvention(resultStatusMap => resultStatusMap
//         .AddDefaultMap()
//         .For(ResultStatus.Ok, HttpStatusCode.OK, resultStatusOptions => resultStatusOptions
//             .For("POST", HttpStatusCode.Created)
//             .For("PUT", HttpStatusCode.NoContent)
//             .For("DELETE", HttpStatusCode.NoContent))
//         .For(ResultStatus.Invalid, HttpStatusCode.BadRequest, resultStatusOptions => resultStatusOptions
//             .With((controller, result) => new { errors = result.ValidationErrors }))
//         .For(ResultStatus.NotFound, HttpStatusCode.NotFound)
//         .For(ResultStatus.Error, HttpStatusCode.InternalServerError));
// });

// Add health checks for external dependencies
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MaestroDbContext>()
    .AddCheck("neo4j", () =>
    {
        try
        {
            // Simple TCP connection check to Neo4j port
            using var client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect("localhost", 7687, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            client.EndConnect(result);
            
            return success
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Neo4j connection successful")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Neo4j connection failed");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Neo4j health check failed: {ex.Message}");
        }
    })
    .AddCheck("qdrant", () =>
    {
        try
        {
            // Simple HTTP check to Qdrant health endpoint
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);
            var response = httpClient.GetAsync("http://localhost:6333/").GetAwaiter().GetResult();
            
            return response.IsSuccessStatusCode
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Qdrant API accessible")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Qdrant returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Qdrant health check failed: {ex.Message}");
        }
    })
    .AddCheck("redis", () =>
    {
        try
        {
            // Simple TCP connection check to Redis port
            using var client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect("localhost", 6379, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            client.EndConnect(result);
            
            return success
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis connection successful")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Redis connection failed");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Redis health check failed: {ex.Message}");
        }
    })
    .AddCheck("elasticsearch", () =>
    {
        try
        {
            // Simple HTTP check to Elasticsearch health endpoint
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);
            var response = httpClient.GetAsync("http://localhost:9200/_cluster/health").GetAwaiter().GetResult();
            
            return response.IsSuccessStatusCode
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Elasticsearch cluster accessible")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Elasticsearch returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Elasticsearch health check failed: {ex.Message}");
        }
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add detailed error pages in development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

// Add FastEndpoints middleware
app.UseFastEndpoints();

// Controllers disabled due to .NET 10 preview compatibility issues
// app.MapControllers();

// Add Swagger in development - disabled due to .NET 10 preview compatibility issues
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ensemble Maestro API v1");
//         c.RoutePrefix = "swagger";
//     });
// }

// Add health checks endpoint
app.MapHealthChecks("/health");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hubs (will add later)
// app.MapHub<OrchestrationHub>("/hubs/orchestration");

app.Run();
