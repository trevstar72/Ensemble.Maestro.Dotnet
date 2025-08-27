using Ensemble.Maestro.Dotnet.Components;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ensemble.Maestro.Dotnet.Core.Services;
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
    .AddCheck("neo4j", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Neo4j placeholder"))
    .AddCheck("qdrant", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Qdrant placeholder"))
    .AddCheck("redis", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis placeholder"));

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
