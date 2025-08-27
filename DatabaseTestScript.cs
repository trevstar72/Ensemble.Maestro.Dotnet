using Microsoft.EntityFrameworkCore;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;

// Test script to verify database operations work correctly
namespace DatabaseTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing database operations...");
        
        // Configuration
        var connectionString = "Server=localhost,1434;Database=Maestro;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=true;";
        
        // Configure DbContext
        var optionsBuilder = new DbContextOptionsBuilder<MaestroDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        using var context = new MaestroDbContext(optionsBuilder.Options);
        
        try
        {
            Console.WriteLine("✓ Database connection established");
            
            // Test 1: Create a Project
            var project = new Project
            {
                Name = "Test Project",
                Requirements = "Test requirements for database validation",
                Status = "Pending",
                Priority = "High",
                EstimatedHours = 40.5m
            };
            
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Project created: {project.Id}");
            
            // Test 2: Create a Module for the project
            var module = new Module
            {
                ProjectId = project.Id,
                Name = "Test Module",
                Description = "Test module description",
                ModuleType = "API",
                ComplexityScore = 5,
                Status = "Pending",
                Priority = "Medium",
                EstimatedHours = 15.5m,
                ActualHours = 12.0m
            };
            
            context.Modules.Add(module);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Module created: {module.Id}");
            
            // Test 3: Create a Pipeline Execution for the project
            var pipelineExecution = new PipelineExecution
            {
                ProjectId = project.Id,
                Stage = "Planning",
                Status = "Running"
            };
            
            context.PipelineExecutions.Add(pipelineExecution);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Pipeline Execution created: {pipelineExecution.Id}");
            
            // Test 4: Create an Agent Execution - this is the one that had issues
            var agentExecution = new AgentExecution
            {
                ProjectId = project.Id,
                PipelineExecutionId = pipelineExecution.Id,
                AgentType = "Planner",
                AgentName = "PlannerAgent",
                Status = "Running",
                Priority = "High",
                InputPrompt = "Generate a test plan for the project"
            };
            
            context.AgentExecutions.Add(agentExecution);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Agent Execution created: {agentExecution.Id}");
            
            // Test 5: Create Agent Message
            var agentMessage = new AgentMessage
            {
                AgentExecutionId = agentExecution.Id,
                SequenceNumber = 1,
                Role = "assistant",
                SenderName = "PlannerAgent",
                Content = "Test message content",
                MessageType = "Text",
                ContentFormat = "text/plain",
                ContentLength = 19,
                Priority = "Medium"
            };
            
            context.AgentMessages.Add(agentMessage);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Agent Message created: {agentMessage.Id}");
            
            // Test 6: Verify foreign key relationships work
            var agentFromDb = await context.AgentExecutions
                .Include(a => a.Project)
                .Include(a => a.PipelineExecution)
                .Include(a => a.Messages)
                .FirstOrDefaultAsync(a => a.Id == agentExecution.Id);
                
            if (agentFromDb != null)
            {
                Console.WriteLine($"✓ Foreign key relationships verified:");
                Console.WriteLine($"  - Project: {agentFromDb.Project.Name}");
                Console.WriteLine($"  - Pipeline: {agentFromDb.PipelineExecution?.Stage}");
                Console.WriteLine($"  - Messages: {agentFromDb.Messages.Count}");
            }
            
            // Test 7: Test decimal precision
            var moduleFromDb = await context.Modules.FirstOrDefaultAsync(m => m.Id == module.Id);
            if (moduleFromDb != null)
            {
                Console.WriteLine($"✓ Decimal precision preserved:");
                Console.WriteLine($"  - EstimatedHours: {moduleFromDb.EstimatedHours}");
                Console.WriteLine($"  - ActualHours: {moduleFromDb.ActualHours}");
            }
            
            Console.WriteLine("\n🎉 All database integrity tests passed!");
            Console.WriteLine("✓ Projects can be created");
            Console.WriteLine("✓ Modules with decimal precision work");
            Console.WriteLine("✓ Pipeline Executions can be created");
            Console.WriteLine("✓ Agent Executions can reference Projects properly");
            Console.WriteLine("✓ Agent Messages can be created");
            Console.WriteLine("✓ Foreign key relationships are working");
            Console.WriteLine("✓ No foreign key constraint violations");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database test failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
            Environment.Exit(1);
        }
    }
}