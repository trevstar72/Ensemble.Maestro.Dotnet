using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Agents.Planning;

// Simple test to verify LLM integration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: false)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

// Add Semantic Kernel
var kernelBuilder = services.AddKernel();
var openAiApiKey = configuration["SemanticKernel:OpenAI:ApiKey"];
if (!string.IsNullOrEmpty(openAiApiKey) && openAiApiKey != "PLACEHOLDER_OPENAI_API_KEY")
{
    kernelBuilder.AddOpenAIChatCompletion(
        configuration["SemanticKernel:OpenAI:ModelId"] ?? "gpt-4o-mini",
        openAiApiKey);
}

// Add LLM service
services.AddScoped<ILLMService, LLMService>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var llmService = serviceProvider.GetRequiredService<ILLMService>();

Console.WriteLine("🔥 Testing LLM Integration with Real API Keys");
Console.WriteLine("==============================================");

try 
{
    // Test a simple LLM call
    var systemPrompt = "You are a helpful AI assistant. Respond briefly and clearly.";
    var userPrompt = "What is the capital of France? Please respond with just the city name.";
    
    Console.WriteLine($"📤 Sending test prompt: '{userPrompt}'");
    
    var response = await llmService.GenerateResponseAsync(
        systemPrompt, 
        userPrompt, 
        maxTokens: 100, 
        temperature: 0.3f);
        
    if (response.Success)
    {
        Console.WriteLine($"✅ LLM Response: {response.Content}");
        Console.WriteLine($"📊 Metrics:");
        Console.WriteLine($"   - Input tokens: {response.InputTokens}");
        Console.WriteLine($"   - Output tokens: {response.OutputTokens}");
        Console.WriteLine($"   - Total tokens: {response.TotalTokens}");
        Console.WriteLine($"   - Cost: ${response.Cost:F4}");
        Console.WriteLine($"   - Duration: {response.Duration.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   - Model: {response.ModelUsed}");
        Console.WriteLine("🎉 LLM Integration Test: SUCCESS!");
    }
    else 
    {
        Console.WriteLine($"❌ LLM Error: {response.ErrorMessage}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"💥 Exception: {ex.Message}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();