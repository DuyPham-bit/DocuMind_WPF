using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ProviderSetting> ProviderSettings => Set<ProviderSetting>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<EmbeddingProfile> EmbeddingProfiles => Set<EmbeddingProfile>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Document>().HasIndex(d => new { d.WorkspaceId, d.SourcePath }).IsUnique();
        modelBuilder.Entity<Document>().Property(d => d.SourcePath).UseCollation("NOCASE");
        modelBuilder.Entity<Document>().HasOne(d => d.Workspace).WithMany().HasForeignKey(d => d.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DocumentVersion>().HasOne(v => v.Document).WithMany(d => d.Versions).HasForeignKey(v => v.DocumentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DocumentVersion>().HasOne(v => v.EmbeddingProfile).WithMany().HasForeignKey(v => v.EmbeddingProfileId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentVersion>().HasIndex(v => v.DocumentId).IsUnique().HasFilter("\"Status\" = 1");
        modelBuilder.Entity<DocumentChunk>().HasOne(c => c.DocumentVersion).WithMany(v => v.Chunks).HasForeignKey(c => c.DocumentVersionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DocumentChunk>().HasIndex(c => new { c.DocumentVersionId, c.ChunkIndex }).IsUnique();
        modelBuilder.Entity<EmbeddingProfile>().HasIndex(p => new { p.Provider, p.Model, p.Endpoint, p.Dimension, p.Normalization, p.PipelineVersion }).IsUnique();
        modelBuilder.Entity<ProviderSetting>().HasIndex(p => p.IsActive).IsUnique().HasFilter("\"IsActive\" = 1");
        modelBuilder.Entity<ProviderSetting>().HasIndex(p => p.IsEmbeddingActive).IsUnique().HasFilter("\"IsEmbeddingActive\" = 1");
        modelBuilder.Entity<ChatMessage>().HasIndex(m => new { m.WorkspaceId, m.ConversationId, m.Timestamp });

        // Workspace configuration
        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Name).IsRequired().HasMaxLength(200);
            entity.Property(w => w.SystemPrompt).HasMaxLength(2000);

            entity.HasMany(w => w.Chunks)
                  .WithOne(c => c.Workspace)
                  .HasForeignKey(c => c.WorkspaceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(w => w.Messages)
                  .WithOne(m => m.Workspace)
                  .HasForeignKey(m => m.WorkspaceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentChunk configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FileName).IsRequired().HasMaxLength(300);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.EmbeddingJson).IsRequired();

            entity.HasIndex(c => c.WorkspaceId);
            entity.HasIndex(c => new { c.WorkspaceId, c.FileName });
        });

        // ProviderSetting configuration
        modelBuilder.Entity<ProviderSetting>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.ProviderType).IsUnique();
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired();
            entity.HasIndex(m => m.WorkspaceId);
        });

        // Seed default Workspace
        var defaultWorkspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        modelBuilder.Entity<Workspace>().HasData(new Workspace
        {
            Id = defaultWorkspaceId,
            ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Workspace Mặc Định",
            Description = "Không gian làm việc và tra cứu tài liệu thông minh cá nhân.",
            SystemPrompt = "Bạn là trợ lý học tập và nghiên cứu tài liệu chuẩn xác. Hãy trả lời súc tích, mạch lạc và bám sát tài liệu.",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
