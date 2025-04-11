using Microsoft.EntityFrameworkCore;
using IntelligenceHub.DAL.Models;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.DAL
{
    /// <summary>
    /// Represents the database context for the Intelligence Hub application.
    /// </summary>
    public class IntelligenceHubDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntelligenceHubDbContext"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options used to configure the database context.</param>
        public IntelligenceHubDbContext(DbContextOptions<IntelligenceHubDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelligenceHubDbContext"/> class with a paremeterless 
        /// constructor for unit testing.
        /// </summary>
        public IntelligenceHubDbContext() : base()
        {
        }

        // Define DbSet properties for each entity type
        public DbSet<DbMessage> Messages { get; set; }
        public DbSet<DbProfile> Profiles { get; set; }
        public DbSet<DbProfileTool> ProfileTools { get; set; }
        public DbSet<DbTool> Tools { get; set; }
        public DbSet<DbProperty> Properties { get; set; }

        /// <summary>
        /// Configures the model for the Intelligence Hub database context.
        /// </summary>
        /// <param name="modelBuilder">The ModelBuilder used to configure EF Core entities, and their relationships.</param>
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
                entity.Property(e => e.User).HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Base64Image).HasColumnType("nvarchar(max)");
                entity.Property(e => e.TimeStamp).IsRequired();
                entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.TimeStamp).IsRequired().HasDefaultValueSql("GETDATE()");
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
                entity.Property(e => e.Host).HasMaxLength(255);
                entity.Property(e => e.ImageHost).HasMaxLength(255);
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
                entity.ToTable("ProfileTools");
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
