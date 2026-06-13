using System;
using System.Collections.Generic;

namespace NandanLabRawData.Models
{
    public class AnalyzerReport
    {
        public int Id { get; set; }
        public string SampleId { get; set; }
        public DateTime? AnalysisDate { get; set; }
        public DateTime ProcessedDate { get; set; }
        public string SourceFileName { get; set; }
        public string RawData { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? IsProcessed { get; set; }

        public ICollection<AnalyzerResult> Results { get; set; } = new List<AnalyzerResult>();
    }

    public class AnalyzerResult
    {
        public int Id { get; set; }
        public int AnalyzerReportId { get; set; }
        public string ParameterName { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Flag { get; set; }
        public DateTime CreatedDate { get; set; }

        public AnalyzerReport AnalyzerReport { get; set; }
    }

    public class Report
    {
        public int Id { get; set; }
        public int? RefDoctorId { get; set; }
        public int? RefLabId { get; set; }
        public int? LabId { get; set; }
        public int ReferedBy { get; set; }
        public DateTime? ReportPreparedDate { get; set; }
        public DateTime? SampleCollectionDate { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string DeletedBy { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string CompletedBy { get; set; }
        public bool IsDraft { get; set; }
        public string ReportUniqueNumberPerDay { get; set; }
        public string ReportPersonTitle { get; set; }
        public string ReportPersonFirstname { get; set; }
        public string ReportPersonLastname { get; set; }
        public int AgeYear { get; set; }
        public int AgeMonth { get; set; }
        public int AgeDays { get; set; }
        public string Gender { get; set; }
        public bool IsSelfReport { get; set; }
        public string ReportNote { get; set; }
        public string MobileNumber { get; set; }
        public string ReportPersonEmail { get; set; }
        public string ReportUrlId { get; set; }
        public bool IsVerifiedAndFinished { get; set; }
        public bool? IsInvoiceGenerated { get; set; }
        public int TotalPrice { get; set; }
        public int Discount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string HistoryPaymentMethod { get; set; }
        public bool IsReportMultiPage { get; set; }
        public bool IsOutSample { get; set; }
        public int PrintCount { get; set; }
        public bool? IsWhatsappSend { get; set; }
        public bool? IsReportReadyWhatsappSend { get; set; }
        public string? SampleId { get; set; }
        public virtual List<ReportDetail> ReportDetails { get; set; }
    }

    public class ReportDetail
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public int ReportFormId { get; set; }
        public bool IsReportOutsourced { get; set; }
        public int? OutsourceLabId { get; set; }
        public int CustomerPrice { get; set; }
        public virtual List<ReportFieldData> ReportFieldData { get; set; }
    }

    public class ReportFieldData
    {
        public int Id { get; set; }
        public int? FieldId { get; set; }
        public string FieldValue { get; set; }
        public int ReportDetailId { get; set; }
        public string ZoneSize { get; set; }
        public string RefZoneR { get; set; }
        public string RefZoneI { get; set; }
        public string RefZones { get; set; }
        public string SensitivityPattern { get; set; }
    }
}
