using Microsoft.EntityFrameworkCore;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL
{
    public class IntelligenceHubDbContext : DbContext
    {
        public IntelligenceHubDbContext(DbContextOptions<IntelligenceHubDbContext> options) : base(options)
        {
        }

        // Define DbSet properties for each entity type
        public DbSet<DbMessage> Messages { get; set; }
        public DbSet<DbIndexMetadata> IndexMetadata { get; set; }
        public DbSet<DbIndexDocument> IndexDocuments { get; set; }
        public DbSet<DbProfile> Profiles { get; set; }
        public DbSet<DbProfileTool> ProfileTools { get; set; }
        public DbSet<DbTool> Tools { get; set; }
        public DbSet<DbProperty> Properties { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity properties and relationships
            modelBuilder.Entity<DbMessage>(entity =>
            {
                entity.ToTable("MessageHistory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.ConversationId).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Base64Image).HasColumnType("nvarchar(max)");
                entity.Property(e => e.TimeStamp).IsRequired();
                entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<DbIndexMetadata>(entity =>
            {
                entity.ToTable("IndexMetadata");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.QueryType).HasMaxLength(255);
                entity.Property(e => e.IndexingInterval).IsRequired();
                entity.Property(e => e.EmbeddingModel).HasMaxLength(255);
                entity.Property(e => e.MaxRagAttachments).HasDefaultValue(3);
                entity.Property(e => e.ChunkOverlap).HasDefaultValue(0.1f);
                entity.Property(e => e.GenerateTopic).IsRequired();
                entity.Property(e => e.GenerateKeywords).IsRequired();
                entity.Property(e => e.GenerateTitleVector).IsRequired();
                entity.Property(e => e.GenerateContentVector).IsRequired();
                entity.Property(e => e.GenerateTopicVector).IsRequired();
                entity.Property(e => e.GenerateKeywordVector).IsRequired();
                entity.Property(e => e.DefaultScoringProfile).HasMaxLength(255);
                entity.Property(e => e.ScoringAggregation).HasMaxLength(255);
                entity.Property(e => e.ScoringInterpolation).HasMaxLength(255);
                entity.Property(e => e.ScoringFreshnessBoost).HasDefaultValue(0.0);
                entity.Property(e => e.ScoringBoostDurationDays).HasDefaultValue(0);
                entity.Property(e => e.ScoringTagBoost).HasDefaultValue(0.0);
                entity.Property(e => e.ScoringWeights).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<DbIndexDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Topic).HasMaxLength(255);
                entity.Property(e => e.Keywords).HasMaxLength(255);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(510);
                entity.Property(e => e.Created).IsRequired();
                entity.Property(e => e.Modified).IsRequired();
            });

            modelBuilder.Entity<DbProfile>(entity =>
            {
                entity.ToTable("Profiles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Model).HasMaxLength(255);
                entity.Property(e => e.FrequencyPenalty);
                entity.Property(e => e.PresencePenalty);
                entity.Property(e => e.Temperature);
                entity.Property(e => e.TopP);
                entity.Property(e => e.TopLogprobs);
                entity.Property(e => e.MaxTokens);
                entity.Property(e => e.MaxMessageHistory);
                entity.Property(e => e.ResponseFormat).HasMaxLength(255);
                entity.Property(e => e.User).HasMaxLength(255);
                entity.Property(e => e.SystemMessage).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Stop).HasMaxLength(255);
                entity.Property(e => e.ReferenceProfiles).HasMaxLength(2040);
                entity.Property(e => e.ReferenceDescription).HasMaxLength(2040);
                entity.Property(e => e.ReturnRecursion);
            });

            modelBuilder.Entity<DbTool>(entity =>
            {
                entity.ToTable("Tools");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Required).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ExecutionUrl).HasMaxLength(255);
                entity.Property(e => e.ExecutionMethod).HasMaxLength(255);
                entity.Property(e => e.ExecutionBase64Key).HasMaxLength(255);

                // Define relationships with ProfileTools
                entity.HasMany(e => e.ProfileTools)
                      .WithOne(pt => pt.Tool)
                      .HasForeignKey(pt => pt.ToolID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DbProperty>(entity =>
            {
                entity.ToTable("Properties");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ToolId).IsRequired();

                // Define foreign key relationship with Tools table
                entity.HasOne<DbTool>()
                      .WithMany()
                      .HasForeignKey(e => e.ToolId);
            });

            modelBuilder.Entity<DbProfileTool>(entity =>
            {
                entity.HasKey(e => new { e.ProfileID, e.ToolID });

                entity.HasOne(e => e.Profile)
                    .WithMany(p => p.ProfileTools)
                    .HasForeignKey(e => e.ProfileID);

                entity.HasOne(e => e.Tool)
                    .WithMany(t => t.ProfileTools)
                    .HasForeignKey(e => e.ToolID);
            });
        }
    }
}
