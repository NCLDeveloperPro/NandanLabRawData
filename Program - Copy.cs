//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace MedicalLabParser
//{
//    // 1. Data Models to hold the parsed information
//    public class TestResult
//    {
//        public string ParameterName { get; set; }
//        public string Value { get; set; }
//        public string Unit { get; set; }
//        public string ReferenceRange { get; set; }
//        public string Flag { get; set; } // e.g., N (Normal), H (High), L (Low)
//    }

//    public class PatientReport
//    {
//        public string SampleId { get; set; }
//        public List<TestResult> Results { get; set; } = new List<TestResult>();
//    }

//    // 2. The Parser Engine
//    public class YH500AstmParser
//    {
//        public PatientReport ParseRawData(string filePath)
//        {
//            var report = new PatientReport();

//            // Read all lines from the port monitor log text file
//            string[] lines = File.ReadAllLines(filePath);

//            foreach (var line in lines)
//            {
//                // Find where the actual machine message begins
//                int stxIndex = line.IndexOf("<STX>");
//                if (stxIndex == -1) continue; // Skip lines without data payload

//                // Find where the message ends
//                int crIndex = line.IndexOf("<CR>", stxIndex);
//                if (crIndex == -1) crIndex = line.Length;

//                // Extract the payload (e.g., "1R|32|^^^IMM#^X-IMM#|0.00|1E03/mm3|...")
//                string payload = line.Substring(stxIndex + 5, crIndex - stxIndex - 5);
//                if (string.IsNullOrEmpty(payload)) continue;

//                // The first character after <STX> is the frame number (0-7). We strip it.
//                string record = payload.Substring(1);

//                // Split the record by the standard ASTM delimiter '|'
//                string[] fields = record.Split('|');
//                if (fields.Length == 0) continue;

//                string recordType = fields[0];

//                // --- PARSE ORDER RECORD (To get Sample/Patient ID) ---
//                if (recordType == "O" && fields.Length > 2)
//                {
//                    report.SampleId = fields[2];
//                }
//                // --- PARSE RESULT RECORD (To get actual Blood Test values) ---
//                else if (recordType == "R" && fields.Length >= 7)
//                {
//                    var testResult = new TestResult();

//                    // Field 2: Universal Test ID (e.g., ^^^IMM#^X-IMM#)
//                    string[] testIdParts = fields[2].Split('^');
//                    // Usually, the parameter name is the 4th element in the caret-separated list
//                    testResult.ParameterName = testIdParts.Length >= 4 ? testIdParts[3] : fields[2];

//                    // Field 3 & 4: Value and Unit
//                    testResult.Value = fields[3];
//                    testResult.Unit = fields[4];

//                    // Field 5: Reference Ranges (e.g., 0.00 - 0.10^REFERENCE_RANGE)
//                    string[] refParts = fields[5].Split('^');
//                    testResult.ReferenceRange = refParts[0];

//                    // Field 6: Abnormal Flags
//                    testResult.Flag = fields[6];

//                    report.Results.Add(testResult);
//                }
//            }

//            return report;
//        }
//    }

//    // 3. Example of how to use this in your application
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            string rawFilePath = @"D:\Ankit\NandanLabRawData\NandanLabRawData\raw data of YH-500.txt";

//            var parser = new YH500AstmParser();
//            PatientReport report = parser.ParseRawData(rawFilePath);

//            Console.WriteLine($"Sample ID: {report.SampleId}");
//            Console.WriteLine("------------------------------------------------------");
//            Console.WriteLine($"{"Parameter",-10} | {"Value",-8} | {"Unit",-10} | {"Range",-15} | {"Flag"}");
//            Console.WriteLine("------------------------------------------------------");

//            foreach (var result in report.Results)
//            {
//                Console.WriteLine($"{result.ParameterName,-10} | {result.Value,-8} | {result.Unit,-10} | {result.ReferenceRange,-15} | {result.Flag}");
//            }
//        }
//    }
//}