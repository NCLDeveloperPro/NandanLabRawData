using Microsoft.EntityFrameworkCore;
using NandanLabRawData.Models;

namespace NandanLabRawData.Data
{
    public class LabDataContext : DbContext
    {
        public DbSet<AnalyzerReport> AnalyzerReports { get; set; }
        public DbSet<AnalyzerResult> AnalyzerResults { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportDetail> ReportDetails { get; set; }
        public DbSet<ReportFieldData> ReportFieldDatas { get; set; }


        public LabDataContext(DbContextOptions<LabDataContext> options) : base(options) { }
        public LabDataContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer("Data Source=103.191.208.18;Initial Catalog=NandanLabDbDev;Integrated Security=False;User ID=developer;Password=p@$$w0rd;Encrypt=True;TrustServerCertificate=True;",
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                      // how many times to retry
                maxRetryDelay: TimeSpan.FromSeconds(10), // wait between retries
                errorNumbersToAdd: null                 // retry all transient errors
            ));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AnalyzerReports
            modelBuilder.Entity<AnalyzerReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SampleId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SourceFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.RawData).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ProcessedDate).IsRequired().HasColumnType("datetime2");
                entity.Property(e => e.AnalysisDate).HasColumnType("datetime2");
                entity.Property(e => e.IsProcessed).HasDefaultValue(0);

                entity.HasMany(e => e.Results)
                      .WithOne(r => r.AnalyzerReport)
                      .HasForeignKey(r => r.AnalyzerReportId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("AnalyzerReports");
            });

            // AnalyzerResults
            modelBuilder.Entity<AnalyzerResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).HasMaxLength(50);
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.ReferenceRange).HasMaxLength(100);
                entity.Property(e => e.Flag).HasMaxLength(10);
                entity.HasIndex(e => e.AnalyzerReportId);
                entity.ToTable("AnalyzerResults");
            });

            // Reports
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Reports");
            });

            // ReportDetails
            modelBuilder.Entity<ReportDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("ReportDetails");
            });

            // ReportFieldDatas
            modelBuilder.Entity<ReportFieldData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("ReportFieldDatas");
            });


            // Indexes for AnalyzerReports
            modelBuilder.Entity<AnalyzerReport>().HasIndex(e => e.SampleId).HasDatabaseName("IX_AnalyzerReports_SampleId");
            modelBuilder.Entity<AnalyzerReport>().HasIndex(e => e.ProcessedDate).HasDatabaseName("IX_AnalyzerReports_ProcessedDate");
        }
    }
}
