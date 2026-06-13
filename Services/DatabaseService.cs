using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NandanLabRawData.Data;
using NandanLabRawData.Logging;
using NandanLabRawData.Models;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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
                FileLogger.Log($"Database migration error: {ex.Message}");

                try
                {
                    // Try to ensure database is created at least
                    _context.Database.EnsureCreated();
                    _databaseInitialized = true;
                }
                catch (Exception innerEx)
                {
                    FileLogger.Log($"Database creation error: {innerEx.Message}");
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
        public async Task<Report> SaveAnalyzerReportAsync(
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
                var finalResult = ResultHelper.RemoveSpecificResults(results);
                var objReport = new Report();
                var currentUserId = "a5e5522d-3ce6-4e14-8b03-c915c9b58f86";                
                objReport.SampleId = sampleId;
                objReport.ReportUrlId = Guid.NewGuid().ToString();
                objReport.IsVerifiedAndFinished = false;
                objReport.CreatedBy = currentUserId;
                objReport.CreatedOn = DateTime.Now;
                objReport.UpdatedOn = DateTime.Now;
                objReport.UpdatedBy = currentUserId;
                objReport.LabId = 3;
                objReport.IsSelfReport = true;
                List<ReportDetail> lstReportDetails = new List<ReportDetail>();
                List<ReportFieldData> lstReportFieldData = new List<ReportFieldData>();
                int reportFormId = 130;
                var formFields = FormFieldsHelper.GetReportFormFields();               
                foreach (var item in finalResult)
                {
                    var formField = formFields.FirstOrDefault(x => x.Value.Equals(item.parameterName, StringComparison.OrdinalIgnoreCase));
                    if (formField != null)
                    {
                        ReportFieldData objReportFieldData = new ReportFieldData
                        {
                            FieldId = formField?.Id ?? 0,
                            FieldValue = item.value
                        };
                        lstReportFieldData.Add(objReportFieldData);                       
                    }
                }
                ReportDetail objReportDetail = new ReportDetail
                {
                    IsReportOutsourced = false,
                    ReportFormId = reportFormId,
                    ReportFieldData = lstReportFieldData
                };
                lstReportDetails.Add(objReportDetail);

                if (lstReportDetails.Count > 0)
                {
                    objReport.ReportDetails = lstReportDetails;
                }

                _context.Reports.Add(objReport);
                await _context.SaveChangesAsync();
                int reportId = objReport.Id;
                UpdateReportUniqueNumber(reportId);
                return objReport;
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error saving analyzer report: {ex.Message}");
                throw new InvalidOperationException($"Error saving analyzer report to SQL Server: {ex.Message}", ex);
            }
        }


        public static void UpdateReportUniqueNumber(int reportId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection("Data Source=103.191.208.18;Initial Catalog=NandanLabDbDev;Integrated Security=False;User ID=developer;Password=p@$$w0rd;Encrypt=True;TrustServerCertificate=True;"))
                using (SqlCommand cmd = new SqlCommand("UpdateReportUniqueNumberPerDay", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@reportId", SqlDbType.Int) { Value = reportId });

                    connection.Open();
                    int rowsAffected = cmd.ExecuteNonQuery(); // Use this for UPDATE/INSERT/DELETE
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
