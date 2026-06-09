namespace NandanLabRawData.Models
{
    /// <summary>
    /// Represents a lab analyzer report (parent entity for test results)
    /// </summary>
    public class AnalyzerReport
    {
        public int Id { get; set; }

        /// <summary>
        /// Sample/Patient ID from the lab analysis
        /// </summary>
        public string SampleId { get; set; }

        /// <summary>
        /// Date and time when the analysis was performed
        /// </summary>
        public DateTime? AnalysisDate { get; set; }

        /// <summary>
        /// Date and time when the file was processed and stored
        /// </summary>
        public DateTime ProcessedDate { get; set; }

        /// <summary>
        /// Original filename that was processed
        /// </summary>
        public string SourceFileName { get; set; }

        /// <summary>
        /// Raw data content from the analyzer file
        /// </summary>
        public string RawData { get; set; }

        /// <summary>
        /// Indicates whether the report has been processed
        /// </summary>
        public int? IsProcessed { get; set; }

        /// <summary>
        /// Collection of test results for this report
        /// </summary>
        public ICollection<AnalyzerResult> Results { get; set; } = new List<AnalyzerResult>();
    }

    /// <summary>
    /// Represents individual test results from an analyzer report
    /// </summary>
    public class AnalyzerResult
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to parent AnalyzerReport
        /// </summary>
        public int AnalyzerReportId { get; set; }

        /// <summary>
        /// Name of the test parameter (e.g., "WBC", "RBC", "Hemoglobin")
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// Measured value of the parameter
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Unit of measurement (e.g., "K/uL", "M/uL", "g/dL")
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Reference range for normal values
        /// </summary>
        public string ReferenceRange { get; set; }

        /// <summary>
        /// Flag indicating abnormality (e.g., "H" for high, "L" for low, blank for normal)
        /// </summary>
        public string Flag { get; set; }

        /// <summary>
        /// Navigation property to parent AnalyzerReport
        /// </summary>
        public AnalyzerReport AnalyzerReport { get; set; }
    }
}
