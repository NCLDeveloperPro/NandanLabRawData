using NandanLabRawData.Data;
using NandanLabRawData.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NandanLabRawData.Services
{
    /// <summary>
    /// Service for managing database operations for analyzer results
    /// </summary>
    public class DatabaseService
    {
        private readonly LabDataContext _context;
        private bool _databaseInitialized = false;

        public DatabaseService()
        {
            _context = new LabDataContext();
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the database connection and creates tables if needed
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                // Test connection and create/migrate database
                _context.Database.Migrate();
                _databaseInitialized = true;
            }
            catch (Exception ex)
            {
                // Log error but allow application to continue
                System.Diagnostics.Debug.WriteLine($"Database migration error: {ex.Message}");

                try
                {
                    // Try to ensure database is created at least
                    _context.Database.EnsureCreated();
                    _databaseInitialized = true;
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Database creation error: {innerEx.Message}");
                    _databaseInitialized = false;
                }
            }
        }

        /// <summary>
        /// Gets the database initialization status
        /// </summary>
        public bool IsDatabaseConnected => _databaseInitialized;

        /// <summary>
        /// Saves analyzer report and results to the database
        /// </summary>
        public async Task<AnalyzerReport> SaveAnalyzerReportAsync(
            string sampleId,
            DateTime? analysisDate,
            string sourceFileName,
            string rawData,
            List<(string parameterName, string value, string unit, string referenceRange, string flag)> results)
        {
            if (!_databaseInitialized)
            {
                throw new InvalidOperationException("Database is not initialized. Check your SQL Server connection.");
            }

            try
            {
                var report = new AnalyzerReport
                {
                    SampleId = sampleId ?? "Unknown",
                    AnalysisDate = analysisDate,
                    SourceFileName = sourceFileName,
                    RawData = rawData,
                    ProcessedDate = DateTime.Now
                };

                // Add results
                foreach (var result in results)
                {
                    report.Results.Add(new AnalyzerResult
                    {
                        ParameterName = result.parameterName ?? string.Empty,
                        Value = result.value ?? string.Empty,
                        Unit = result.unit ?? string.Empty,
                        ReferenceRange = result.referenceRange ?? string.Empty,
                        Flag = result.flag ?? string.Empty
                    });
                }

                _context.AnalyzerReports.Add(report);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Successfully saved report {report.Id} for sample {sampleId} to SQL Server");
                return report;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving analyzer report: {ex.Message}");
                throw new InvalidOperationException($"Error saving analyzer report to SQL Server: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets analyzer report by ID
        /// </summary>
        public async Task<AnalyzerReport> GetReportByIdAsync(int id)
        {
            try
            {
                return await _context.AnalyzerReports
                    .Where(r => r.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all analyzer reports
        /// </summary>
        public async Task<List<AnalyzerReport>> GetAllReportsAsync()
        {
            try
            {
                return await _context.AnalyzerReports
                    .OrderByDescending(r => r.ProcessedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving reports: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets reports by sample ID
        /// </summary>
        public async Task<List<AnalyzerReport>> GetReportsBySampleIdAsync(string sampleId)
        {
            try
            {
                return await _context.AnalyzerReports
                    .Where(r => r.SampleId == sampleId)
                    .OrderByDescending(r => r.ProcessedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving reports by sample ID: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets reports within a date range
        /// </summary>
        public async Task<List<AnalyzerReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.AnalyzerReports
                    .Where(r => r.ProcessedDate >= startDate && r.ProcessedDate <= endDate)
                    .OrderByDescending(r => r.ProcessedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving reports by date range: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets total number of reports in database
        /// </summary>
        public async Task<int> GetTotalReportCountAsync()
        {
            try
            {
                return await _context.AnalyzerReports.CountAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error counting reports: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes the DbContext
        /// </summary>
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
