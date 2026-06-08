using MedicalLabParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NandanLabRawData
{
    public class YumizenH500Parser
    {
        public PatientReport ParseRawData(string filePath)
        {
            var report = new PatientReport();

            string[] lines = File.ReadAllLines(filePath);

            report.RawData = File.ReadAllText(filePath);

            foreach (var line in lines)
            {
                int stxIndex = line.IndexOf("<STX>");

                if (stxIndex < 0)
                    continue;

                int crIndex = line.IndexOf("<CR>", stxIndex);

                if (crIndex < 0)
                    crIndex = line.Length;

                string payload = line.Substring(
                    stxIndex + 5,
                    crIndex - stxIndex - 5);

                if (string.IsNullOrWhiteSpace(payload))
                    continue;

                if (payload.Length < 2)
                    continue;

                string record = payload.Substring(1);

                string[] fields = record.Split('|');

                if (fields.Length == 0)
                    continue;

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

            return report;
        }

        private void ParseOrder(string[] fields, PatientReport report)
        {
            if (fields.Length > 2)
                report.SampleId = fields[2];

            if (fields.Length > 6)
            {
                if (DateTime.TryParseExact(
                        fields[6],
                        "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime dt))
                {
                    report.AnalysisDate = dt;
                }
            }
        }

        private void ParseResult(string[] fields, PatientReport report)
        {
            if (fields.Length < 7)
                return;

            string[] testParts = fields[2].Split('^');

            string parameterName =
                testParts.Length >= 4
                    ? testParts[3]
                    : fields[2];

            string referenceRange = "";

            if (!string.IsNullOrWhiteSpace(fields[5]))
            {
                referenceRange = fields[5].Split('^')[0];
            }

            string formattedUnit = FormatUnit(fields[4]);

            report.Results.Add(new TestResult
            {
                ParameterName = parameterName,
                Value = fields[3],
                Unit = formattedUnit,
                ReferenceRange = referenceRange,
                Flag = fields[6]
            });
        }

        private string FormatUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return unit;

            // Replace scientific notation patterns like 1E03, 1E06, etc. with superscript equivalents
            // 1E03 → 10³, 1E06 → 10⁶, etc.
            unit = System.Text.RegularExpressions.Regex.Replace(unit, @"1E(\d+)", m =>
            {
                // Remove leading zeros by parsing as int and converting back to string
                int exponent = int.Parse(m.Groups[1].Value);
                string superscript = ConvertToSuperscript(exponent.ToString());
                return "10" + superscript;
            });

            // Replace mm3 with mm³
            unit = unit.Replace("mm3", "mm³");

            return unit;
        }

        private string ConvertToSuperscript(string number)
        {
            // Map digits to their superscript equivalents using Unicode escape sequences
            var superscriptMap = new Dictionary<char, char>
            {
                { '0', '\u2070' }, // ⁰
                { '1', '\u00B9' }, // ¹
                { '2', '\u00B2' }, // ²
                { '3', '\u00B3' }, // ³
                { '4', '\u2074' }, // ⁴
                { '5', '\u2075' }, // ⁵
                { '6', '\u2076' }, // ⁶
                { '7', '\u2077' }, // ⁷
                { '8', '\u2078' }, // ⁸
                { '9', '\u2079' }  // ⁹
            };

            var result = new StringBuilder();
            foreach (char digit in number)
            {
                if (superscriptMap.ContainsKey(digit))
                    result.Append(superscriptMap[digit]);
                else
                    result.Append(digit);
            }

            return result.ToString();
        }

        private void ParseCondition(string[] fields, PatientReport report)
        {
            if (fields.Length < 4)
                return;

            string[] conditions =
                fields[3].Split('\\');

            foreach (string item in conditions)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    report.Conditions.Add(item);
                }
            }
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
