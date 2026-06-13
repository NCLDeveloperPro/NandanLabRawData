using MedicalLabParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NandanLabRawData
{
    public class YumizenH500Parser
    {
        public List<PatientReport> ParseRawData(string filePath)
        {
            var reports = new List<PatientReport>();

            string rawText = File.ReadAllText(filePath);
            string[] segments = rawText.Split(new[] { "<ENQ>" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                // Only process if segment contains <EOT>
                int eotIndex = segment.IndexOf("<EOT>");
                if (eotIndex < 0)
                    continue;

                string reportText = segment.Substring(0, eotIndex);

                var report = new PatientReport
                {
                    RawData = reportText
                };

                string[] lines = reportText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    int stxIndex = line.IndexOf("<STX>");
                    if (stxIndex < 0) continue;

                    int crIndex = line.IndexOf("<CR>", stxIndex);
                    if (crIndex < 0) crIndex = line.Length;

                    string payload = line.Substring(stxIndex + 5, crIndex - stxIndex - 5);
                    if (string.IsNullOrWhiteSpace(payload) || payload.Length < 2) continue;

                    string record = payload.Substring(1);
                    string[] fields = record.Split('|');
                    if (fields.Length == 0) continue;

                    string recordType = fields[0];
                    switch (recordType)
                    {
                        case "O":
                            ParseOrder(fields, report);
                            break;
                        case "R":
                            ParseResult(fields, report);
                            break;
                        case "C":
                            ParseCondition(fields, report);
                            break;
                    }
                }

                reports.Add(report);
            }

            return reports;
        }

        private void ParseOrder(string[] fields, PatientReport report)
        {
            if (fields.Length > 2)
                report.SampleId = fields[2];

            if (fields.Length > 6 &&
                DateTime.TryParseExact(fields[6], "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                report.AnalysisDate = dt;
            }
        }

        private void ParseResult(string[] fields, PatientReport report)
        {
            if (fields.Length < 7) return;

            string[] testParts = fields[2].Split('^');
            string parameterName = testParts.Length >= 4 ? testParts[3] : fields[2];

            string referenceRange = "";
            if (!string.IsNullOrWhiteSpace(fields[5]))
                referenceRange = fields[5].Split('^')[0];

            string formattedUnit = FormatUnit(fields[4]);

            report.Results.Add(new TestResult
            {
                ParameterName = parameterName.Replace("%", "").Replace("#", ""),
                Value = fields[3],
                Unit = formattedUnit,
                ReferenceRange = referenceRange,
                Flag = fields[6]
            });
        }

        private string FormatUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return unit;

            unit = System.Text.RegularExpressions.Regex.Replace(unit, @"1E(\d+)", m =>
            {
                int exponent = int.Parse(m.Groups[1].Value);
                string superscript = ConvertToSuperscript(exponent.ToString());
                return "10" + superscript;
            });

            return unit.Replace("mm3", "mm³");
        }

        private string ConvertToSuperscript(string number)
        {
            var superscriptMap = new Dictionary<char, char>
            {
                { '0', '\u2070' }, { '1', '\u00B9' }, { '2', '\u00B2' },
                { '3', '\u00B3' }, { '4', '\u2074' }, { '5', '\u2075' },
                { '6', '\u2076' }, { '7', '\u2077' }, { '8', '\u2078' },
                { '9', '\u2079' }
            };

            var result = new StringBuilder();
            foreach (char digit in number)
                result.Append(superscriptMap.ContainsKey(digit) ? superscriptMap[digit] : digit);

            return result.ToString();
        }

        private void ParseCondition(string[] fields, PatientReport report)
        {
            if (fields.Length < 4) return;

            string[] conditions = fields[3].Split('\\');
            foreach (string item in conditions)
                if (!string.IsNullOrWhiteSpace(item))
                    report.Conditions.Add(item);
        }
    }

    public class TestResult
    {
        public string ParameterName { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Flag { get; set; }
    }

    public class PatientReport
    {
        public string SampleId { get; set; }
        public DateTime? AnalysisDate { get; set; }
        public List<TestResult> Results { get; set; } = new();
        public List<string> Conditions { get; set; } = new();
        public string RawData { get; set; }
    }
}
