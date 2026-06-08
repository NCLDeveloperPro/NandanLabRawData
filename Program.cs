using NandanLabRawData;
using System;
using System.Windows.Forms;

namespace MedicalLabParser
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Publish cmd -            
            // dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:IncludeNativeLibrariesForSelfExtract=true
            // select * from dbo.AnalyzerResults
            // select* from dbo.AnalyzerReports

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}