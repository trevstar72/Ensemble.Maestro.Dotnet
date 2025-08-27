using Microsoft.EntityFrameworkCore;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;

namespace Ensemble.Maestro.Dotnet.Core.Data;

/// <summary>
/// Entity Framework database context for the Maestro application
/// </summary>
public class MaestroDbContext : DbContext
{
    public MaestroDbContext(DbContextOptions<MaestroDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<PipelineExecution> PipelineExecutions { get; set; } = null!;
    public DbSet<StageExecution> StageExecutions { get; set; } = null!;
    public DbSet<ProjectFile> ProjectFiles { get; set; } = null!;
    public DbSet<Module> Modules { get; set; } = null!;

    // Semantic Kernel specific entities
    public DbSet<AgentExecution> AgentExecutions { get; set; } = null!;
    public DbSet<AgentMessage> AgentMessages { get; set; } = null!;
    public DbSet<OrchestrationResult> OrchestrationResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Project entity
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure PipelineExecution entity
        modelBuilder.Entity<PipelineExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Stage);
            entity.HasIndex(e => e.StartedAt);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.StageStartedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Foreign key relationships
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.PipelineExecutions)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure StageExecution entity
        modelBuilder.Entity<StageExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PipelineExecutionId);
            entity.HasIndex(e => e.StageName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExecutionOrder);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.PipelineExecution)
                  .WithMany(p => p.StageExecutions)
                  .HasForeignKey(e => e.PipelineExecutionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure ProjectFile entity
        modelBuilder.Entity<ProjectFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ModuleId);
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.RelativePath);
            entity.HasIndex(e => e.ContentType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Files)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Module)
                  .WithMany(m => m.Files)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PipelineExecution)
                  .WithMany()
                  .HasForeignKey(e => e.PipelineExecutionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure Module entity
        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ModuleType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ModuleOrder);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Modules)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure AgentExecution entity
        modelBuilder.Entity<AgentExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.PipelineExecutionId);
            entity.HasIndex(e => e.StageExecutionId);
            entity.HasIndex(e => e.AgentType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.ParentExecutionId);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.AgentExecutions)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.PipelineExecution)
                  .WithMany(p => p.AgentExecutions)
                  .HasForeignKey(e => e.PipelineExecutionId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.StageExecution)
                  .WithMany(s => s.AgentExecutions)
                  .HasForeignKey(e => e.StageExecutionId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ParentExecution)
                  .WithMany(p => p.ChildExecutions)
                  .HasForeignKey(e => e.ParentExecutionId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes for hierarchies
        });

        // Configure AgentMessage entity
        modelBuilder.Entity<AgentMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AgentExecutionId);
            entity.HasIndex(e => e.SequenceNumber);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.MessageType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ThreadId);
            entity.HasIndex(e => e.ParentMessageId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.AgentExecution)
                  .WithMany(a => a.Messages)
                  .HasForeignKey(e => e.AgentExecutionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentMessage)
                  .WithMany(m => m.Replies)
                  .HasForeignKey(e => e.ParentMessageId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes for threaded messages
        });

        // Configure OrchestrationResult entity
        modelBuilder.Entity<OrchestrationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PipelineExecutionId);
            entity.HasIndex(e => e.OrchestrationPattern);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.ParentOrchestrationId);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.PipelineExecution)
                  .WithMany(p => p.OrchestrationResults)
                  .HasForeignKey(e => e.PipelineExecutionId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ParentOrchestration)
                  .WithMany(o => o.ChildOrchestrations)
                  .HasForeignKey(e => e.ParentOrchestrationId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes for nested orchestrations
        });

        // Add some useful composite indexes
        modelBuilder.Entity<PipelineExecution>()
            .HasIndex(e => new { e.ProjectId, e.Status });

        modelBuilder.Entity<StageExecution>()
            .HasIndex(e => new { e.PipelineExecutionId, e.ExecutionOrder });

        modelBuilder.Entity<ProjectFile>()
            .HasIndex(e => new { e.ProjectId, e.RelativePath });

        modelBuilder.Entity<AgentExecution>()
            .HasIndex(e => new { e.ProjectId, e.AgentType, e.Status });

        modelBuilder.Entity<AgentMessage>()
            .HasIndex(e => new { e.AgentExecutionId, e.SequenceNumber });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically update UpdatedAt timestamp for entities that have it
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Project or ProjectFile or Module && 
                       (e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is Project project)
                project.UpdatedAt = DateTime.UtcNow;
            else if (entityEntry.Entity is ProjectFile file)
                file.UpdatedAt = DateTime.UtcNow;
            else if (entityEntry.Entity is Module module)
                module.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}