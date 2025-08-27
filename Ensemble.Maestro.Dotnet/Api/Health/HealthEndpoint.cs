using FastEndpoints;

namespace Ensemble.Maestro.Dotnet.Api.Health;

/// <summary>
/// Basic health check endpoint for the application
/// </summary>
public class HealthEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
        Description(b => b
            .WithName("Health Check")
            .WithSummary("Returns the health status of the application")
            .WithDescription("Basic health check endpoint that returns application status and version information")
            .Produces<HealthResponse>(200, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Application = "Ensemble Maestro .NET",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        await Send.OkAsync(response, ct);
    }
}

/// <summary>
/// Health check response model
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "Unknown";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string Application { get; set; } = "Ensemble Maestro";
    public string Environment { get; set; } = "Unknown";
}