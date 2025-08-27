using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// Quick test to verify LLM service works directly
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
    .Build();

var apiKey = config["SemanticKernel:OpenAI:ApiKey"];
var modelId = config["SemanticKernel:OpenAI:ModelId"];

Console.WriteLine($"Testing OpenAI API with model: {modelId}");

try 
{
    var builder = Kernel.CreateBuilder();
    builder.AddOpenAIChatCompletion(modelId, apiKey);
    var kernel = builder.Build();
    
    var chatService = kernel.GetRequiredService<IChatCompletionService>();
    var chatHistory = new ChatHistory();
    chatHistory.AddUserMessage("Generate a simple Hello World in C#. Just the code, no explanation.");
    
    Console.WriteLine("Making test API call...");
    
    var response = await chatService.GetChatMessageContentAsync(chatHistory);
    
    Console.WriteLine($"SUCCESS! Response: {response.Content}");
}
catch (Exception ex)
{
    Console.WriteLine($"FAILED: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}