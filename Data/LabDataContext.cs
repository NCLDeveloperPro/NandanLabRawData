using Microsoft.EntityFrameworkCore;
using NandanLabRawData.Models;
using NandanLabRawData.Configuration;

namespace NandanLabRawData.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for lab analyzer data using SQL Server
    /// </summary>
    public class LabDataContext : DbContext
    {
        public DbSet<AnalyzerReport> AnalyzerReports { get; set; }
        public DbSet<AnalyzerResult> AnalyzerResults { get; set; }

        /// <summary>
        /// Constructor with options for dependency injection
        /// </summary>
        public LabDataContext(DbContextOptions<LabDataContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Default constructor for design-time support (migrations)
        /// </summary>
        public LabDataContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Only configure if not already configured by dependency injection
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(DatabaseConfiguration.ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AnalyzerReport entity
            modelBuilder.Entity<AnalyzerReport>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.SampleId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.SourceFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.RawData)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ProcessedDate)
                    .IsRequired()
                    .HasColumnType("datetime2");

                entity.Property(e => e.AnalysisDate)
                    .HasColumnType("datetime2");

                // One-to-many relationship: AnalyzerReport -> AnalyzerResults
                entity.HasMany(e => e.Results)
                    .WithOne(r => r.AnalyzerReport)
                    .HasForeignKey(r => r.AnalyzerReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set table name
                entity.ToTable("AnalyzerReports");
            });

            // Configure AnalyzerResult entity
            modelBuilder.Entity<AnalyzerResult>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ParameterName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Value)
                    .HasMaxLength(50);

                entity.Property(e => e.Unit)
                    .HasMaxLength(50);

                entity.Property(e => e.ReferenceRange)
                    .HasMaxLength(100);

                entity.Property(e => e.Flag)
                    .HasMaxLength(10);

                // Index for common queries
                entity.HasIndex(e => e.AnalyzerReportId);

                // Set table name
                entity.ToTable("AnalyzerResults");
            });

            // Create index for sample lookup
            modelBuilder.Entity<AnalyzerReport>()
                .HasIndex(e => e.SampleId)
                .HasName("IX_AnalyzerReports_SampleId");

            // Create index for date range queries
            modelBuilder.Entity<AnalyzerReport>()
                .HasIndex(e => e.ProcessedDate)
                .HasName("IX_AnalyzerReports_ProcessedDate");
        }
    }
}
