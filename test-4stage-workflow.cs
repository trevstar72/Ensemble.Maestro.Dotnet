using System.Net.Http.Json;
using System.Text.Json;

// Simple test to verify 4-stage workflow execution via API
Console.WriteLine("=== 4-Stage Workflow Test ===");
Console.WriteLine($"Test started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine();

// Test configuration
var baseUrl = "https://localhost:5001";
var client = new HttpClient();
client.BaseAddress = new Uri(baseUrl);

// Ignore SSL certificate issues for local testing
HttpClientHandler handler = new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
};
var httpsClient = new HttpClient(handler);
httpsClient.BaseAddress = new Uri(baseUrl);

try
{
    Console.WriteLine("Step 1: Testing health endpoint...");
    var healthResponse = await httpsClient.GetAsync("/health");
    Console.WriteLine($"Health check: {healthResponse.StatusCode}");
    
    if (!healthResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("❌ Health check failed - server may not be running");
        Console.WriteLine("Run: dotnet run --project Ensemble.Maestro.Dotnet");
        return;
    }
    
    Console.WriteLine("✅ Server is healthy");
    Console.WriteLine();

    Console.WriteLine("Step 2: Starting 4-stage workflow execution...");
    
    var testRequest = new
    {
        ProjectName = "Simple API Test Project",
        Description = "Testing the 4-stage pipeline: Planner → Designer → Swarm → Builder",
        TargetLanguage = "C#",
        DeploymentTarget = "Azure",
        AgentPoolSize = 2,
        MaxExecutionTimeMinutes = 5
    };

    var response = await httpsClient.PostAsJsonAsync("/api/executions/start", testRequest);
    
    if (response.IsSuccessStatusCode)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var executionResponse = JsonSerializer.Deserialize<dynamic>(responseContent);
        
        Console.WriteLine("✅ Test execution started successfully!");
        Console.WriteLine($"Execution ID: {executionResponse?.GetProperty("executionId")}");
        Console.WriteLine($"Status: {executionResponse?.GetProperty("status")}");
        Console.WriteLine($"Stage: {executionResponse?.GetProperty("stage")}");
        Console.WriteLine();
        
        // Monitor execution progress
        var executionId = executionResponse?.GetProperty("executionId").GetGuid();
        if (executionId.HasValue)
        {
            Console.WriteLine("Step 3: Monitoring 4-stage pipeline progress...");
            await MonitorExecution(httpsClient, executionId.Value);
        }
    }
    else
    {
        Console.WriteLine($"❌ Failed to start execution: {response.StatusCode}");
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error details: {errorContent}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
    Console.WriteLine($"Make sure the Maestro server is running on {baseUrl}");
}
finally
{
    httpsClient.Dispose();
    client.Dispose();
}

Console.WriteLine();
Console.WriteLine("=== Test Complete ===");

async Task MonitorExecution(HttpClient client, Guid executionId)
{
    var maxChecks = 12; // 60 seconds max
    var checkInterval = TimeSpan.FromSeconds(5);
    
    for (int i = 0; i < maxChecks; i++)
    {
        try
        {
            var statusResponse = await client.GetAsync($"/api/executions/{executionId}/status");
            if (statusResponse.IsSuccessStatusCode)
            {
                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<dynamic>(statusContent);
                
                var currentStage = status?.GetProperty("stage").GetString() ?? "Unknown";
                var currentStatus = status?.GetProperty("status").GetString() ?? "Unknown";
                var progress = status?.GetProperty("completedFunctions").GetInt32() ?? 0;
                var total = status?.GetProperty("totalFunctions").GetInt32() ?? 0;
                
                Console.WriteLine($"  [{DateTime.UtcNow:HH:mm:ss}] Stage: {currentStage} | Status: {currentStatus} | Progress: {progress}/{total}");
                
                // Check if completed or failed
                if (currentStatus == "Completed")
                {
                    Console.WriteLine("✅ 4-Stage pipeline completed successfully!");
                    Console.WriteLine("  Stage 1: PLANNER ✅");
                    Console.WriteLine("  Stage 2: DESIGNER ✅"); 
                    Console.WriteLine("  Stage 3: SWARM ✅");
                    Console.WriteLine("  Stage 4: BUILDER ✅");
                    break;
                }
                else if (currentStatus == "Failed")
                {
                    Console.WriteLine("❌ 4-Stage pipeline failed");
                    break;
                }
            }
            else
            {
                Console.WriteLine($"  Failed to get status: {statusResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Status check error: {ex.Message}");
        }
        
        if (i < maxChecks - 1)
        {
            await Task.Delay(checkInterval);
        }
    }
}