using System;
using System.IO;
using System.Collections.Generic;

namespace NandanLabRawData.Logging
{
    public static class FileLogger
    {
        private static readonly string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string logFile = Path.Combine(logDirectory, "app_log.txt");

        static FileLogger()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void Log(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
            File.AppendAllText(logFile, logEntry);
        }
    }

    public static class FormFieldsHelper
    {
        public static List<ParameterItem> GetReportFormFields()
        {
            return new List<ParameterItem>
            {
                new ParameterItem { Id = 483, Value = "IML" },
                new ParameterItem { Id = 482, Value = "IMM" },
                new ParameterItem { Id = 474, Value = "BAS" },//"Basophils" },
                new ParameterItem { Id = 481, Value = "IMG" },
                new ParameterItem { Id = 484, Value = "ALY" },
                new ParameterItem { Id = 466, Value = "Mic" },
                new ParameterItem { Id = 485, Value = "LIC" },
                new ParameterItem { Id = 477, Value = "PCT" },
                new ParameterItem { Id = 473, Value = "EOS"},//"Eosinophils" },
                new ParameterItem { Id = 494, Value = "NLR" },
                new ParameterItem { Id = 478, Value = "PDW" },
                new ParameterItem { Id = 458, Value = "HGB" },// "HAEMOGLOBIN" },
                new ParameterItem { Id = 464, Value = "RDW-CV" },
                new ParameterItem { Id = 475, Value = "PLT" },//"Platelet Count" },
                new ParameterItem { Id = 471, Value = "LYM" },//"Lymphocytes" },
                new ParameterItem { Id = 480, Value = "P-LCR" },
                new ParameterItem { Id = 467, Value = "MAC" },
                new ParameterItem { Id = 462, Value = "MCH" },
                new ParameterItem { Id = 459, Value = "RBC" },//"Red Blood Cells" },
                new ParameterItem { Id = 463, Value = "MCHC" },
                new ParameterItem { Id = 460, Value = "HCT" },
                new ParameterItem { Id = 465, Value = "RDW-SD" },
                new ParameterItem { Id = 472, Value = "MON" },//"Monocytes" },
                new ParameterItem { Id = 468, Value = "WBC" }, //"TOTAL WBC COUNT" },
                new ParameterItem { Id = 470, Value = "NEU" },//"Polymorphs" },
                new ParameterItem { Id = 479, Value = "P-LCC" },
                new ParameterItem { Id = 476, Value = "MPV" },
                new ParameterItem { Id = 461, Value = "MCV" }
            };
        }

    }
    public static class ResultHelper
    {
        public static List<(string parameterName, string value, string unit, string referenceRange, string flag)>
            RemoveSpecificResults(List<(string parameterName, string value, string unit, string referenceRange, string flag)> results)
        {
            // Define the items you want to remove
            var toRemove = new HashSet<(string parameterName, string unit, string referenceRange)>
            {
                ("LIC", "10³/mm³", "0.00 - 0.20"),
                ("IML", "10³/mm³", "0.00 - 0.05"),
                ("BAS", "10³/mm³", "0.00 - 0.10"),
                ("IMM", "10³/mm³", "0.00 - 0.10"),
                ("ALY", "10³/mm³", "0.00 - 0.20"),
                ("EOS", "10³/mm³", "0.00 - 0.40"),
                ("IMG", "10³/mm³", "0.00 - 0.50"),
                ("MON", "10³/mm³", "0.20 - 0.80"),
                ("LYM", "10³/mm³", "1.25 - 4.00"),
                ("NEU", "10³/mm³", "1.50 - 7.50")
            };

            // Filter out matches
            return results
                .Where(r => !toRemove.Contains((r.parameterName, r.unit, r.referenceRange)))
                .ToList();
        }

        public static async Task<bool> IsInternetAvailableAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var response = await client.GetAsync("http://www.msftconnecttest.com");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    public class ParameterItem
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
