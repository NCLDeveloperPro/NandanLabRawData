using NandanLabRawData.Logging;
using NandanLabRawData.Models;
using NandanLabRawData.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace NandanLabRawData
{
    public class FileMonitorService
    {
        private readonly string _folderPath;
        private readonly string _processedFolderPath;
        private FileSystemWatcher? _watcher;
        private readonly YumizenH500Parser _parser;
        private readonly DatabaseService? _databaseService;
        private readonly HashSet<string> _processedFileHashes = new();
        private bool _isRunning = false;

        public event Action<string, bool, string>? FileProcessed;
        public event Action<string>? StatusChanged;

        public FileMonitorService(string folderPath)
        {
            _folderPath = folderPath;
            _processedFolderPath = Path.Combine(folderPath, "Processed");
            _parser = new YumizenH500Parser();

            // Initialize database service
            try
            {
                _databaseService = new DatabaseService();
                StatusChanged?.Invoke("Database initialized successfully");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Warning: Database initialization failed: {ex.Message}");
                StatusChanged?.Invoke($"Warning: Database initialization failed: {ex.Message}");
                _databaseService = null;
            }

            // Create Processed folder if it doesn't exist
            EnsureProcessedFolderExists();
        }

        private void EnsureProcessedFolderExists()
        {
            try
            {
                if (!Directory.Exists(_processedFolderPath))
                {
                    Directory.CreateDirectory(_processedFolderPath);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Warning: Could not create Processed folder: {ex.Message}");
                StatusChanged?.Invoke($"Warning: Could not create Processed folder: {ex.Message}");
            }
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            StatusChanged?.Invoke("Starting folder monitor...");

            try
            {
                _watcher = new FileSystemWatcher(_folderPath)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    Filter = "*.txt"
                };

                _watcher.Created += OnFileCreated;
                _watcher.Changed += OnFileChanged;
                _watcher.Error += OnError;

                _watcher.EnableRaisingEvents = true;

                // Process existing files in the folder
                ProcessExistingFiles();

                StatusChanged?.Invoke("Monitoring folder for new files...");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error starting monitor: {ex.Message}");
                StatusChanged?.Invoke($"Error starting monitor: {ex.Message}");
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _watcher?.Dispose();
            _databaseService?.Dispose();
            StatusChanged?.Invoke("Monitor stopped");
        }

        private void ProcessExistingFiles()
        {
            try
            {
                var files = Directory.GetFiles(_folderPath, "*.txt");
                foreach (var file in files)
                {
                    // Skip files in the Processed folder
                    string directory = Path.GetDirectoryName(file) ?? _folderPath;
                    if (directory.Equals(_processedFolderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ProcessFile(file);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error processing existing files: {ex.Message}");
                StatusChanged?.Invoke($"Error processing existing files: {ex.Message}");
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            // Wait a moment to ensure file is fully written
            Thread.Sleep(900);
            ProcessFile(e.FullPath);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Process file on change (content-based detection will handle duplicates)
            Thread.Sleep(900);
            ProcessFile(e.FullPath);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            StatusChanged?.Invoke($"File watcher error: {exception?.Message}");
        }

        /// <summary>
        /// Calculates SHA256 hash of file content
        /// </summary>
        private string GetFileHash(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ProcessFile(string filePath)
        {
            try
            {
                if (!ResultHelper.IsInternetAvailableAsync().GetAwaiter().GetResult())
                {
                    FileProcessed?.Invoke(Path.GetFileName(filePath), false, "No internet connection - cannot process");
                    return;
                }
                // Skip if not a .txt file
                if (Path.GetExtension(filePath) != ".txt")
                    return;

                // Wait for file to be accessible
                if (!WaitForFileAccessible(filePath, 5))
                {
                    FileProcessed?.Invoke(Path.GetFileName(filePath), false, "File was locked - could not access");
                    return;
                }

                // Calculate content hash
                string contentHash = GetFileHash(filePath);
                if (string.IsNullOrEmpty(contentHash))
                {
                    FileProcessed?.Invoke(Path.GetFileName(filePath), false, "Could not read file content");
                    return;
                }
                
                // Parse the file
                var reports = _parser.ParseRawData(filePath);

                // Move file to Processed folder
                string fileName = Path.GetFileName(filePath);
                string processedFilePath = Path.Combine(_processedFolderPath, fileName);

                //// Handle file naming conflict in Processed folder
                //if (File.Exists(processedFilePath))
                //{
                //    // Add timestamp to avoid conflicts
                //    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                //    string extension = Path.GetExtension(fileName);
                //    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                //    fileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
                //    processedFilePath = Path.Combine(_processedFolderPath, fileName);
                //}

                ////File.Move(filePath, processedFilePath);
                ////// Store the hash of processed content
                //_processedFileHashes.Add(contentHash);

                // Save to database if service is available
                if (_databaseService != null)
                {
                    try
                    {
                        foreach (var report in reports)
                        {
                            // Skip if this exact content was already processed
                            if (_processedFileHashes.Contains(report.SampleId))
                            {
                                StatusChanged?.Invoke($"Skipped (duplicate content SampleId): {report.SampleId}");
                                return;
                            }

                            var results = report.Results.Select(r => (
                                r.ParameterName,
                                r.Value,
                                r.Unit,
                                r.ReferenceRange,
                                r.Flag
                            )).ToList();
                            FileProcessed?.Invoke($"SampleId: {report.SampleId}", true, "");
                            string header = string.Format("\n{0,-20}\t{1,-15}", "ParameterName", "Value");
                            FileProcessed?.Invoke(header, true, "");
                            var formFields = FormFieldsHelper.GetReportFormFields();
                            foreach (var item in report.Results)
                            {
                                var formField = formFields.FirstOrDefault(x => x.Value.Equals(item.ParameterName, StringComparison.OrdinalIgnoreCase));
                                if (formField != null)
                                {
                                    string row = string.Format("{0,-20}\t{1,-15}", item.ParameterName, item.Value);
                                    FileProcessed?.Invoke(row, true, "");
                                }
                            }
                            var dbReport = _databaseService.SaveAnalyzerReportAsync(
                                report.SampleId,
                                report.AnalysisDate,
                                Path.GetFileName(filePath),
                                report.RawData,
                                results
                            ).GetAwaiter().GetResult();
                            StatusChanged?.Invoke($"Database: Saved report ID {dbReport.Id}");
                            StatusChanged?.Invoke(new string('-', 70));
                            //// Store the hash of processed content
                            _processedFileHashes.Add(report.SampleId);
                            //// Log success
                            //string resultMessage = $"Parsed {report.Results.Count} parameters - Moved to Processed folder";
                            //FileProcessed?.Invoke(Path.GetFileName(filePath), true, resultMessage);
                            //StatusChanged?.Invoke($"Successfully processed and moved: {Path.GetFileName(filePath)}");
                        }
                    }
                    catch (Exception dbEx)
                    {
                        FileLogger.Log($"Error saving to database: {dbEx.Message}");
                        StatusChanged?.Invoke($"Warning: Could not save to database: {dbEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error processing file: {ex.Message}");
                string filename = Path.GetFileName(filePath);
                FileProcessed?.Invoke(filename, false, $"Error: {ex.Message}");
                StatusChanged?.Invoke($"Error processing file: {ex.Message}");
            }
        }

        private bool WaitForFileAccessible(string filePath, int maxWaitSeconds)
        {
            int attempts = 0;
            int maxAttempts = maxWaitSeconds * 2; // 500ms per attempt

            while (attempts < maxAttempts)
            {
                try
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // File is accessible
                        return true;
                    }
                }
                catch (IOException)
                {
                    attempts++;
                    Thread.Sleep(500);
                }
            }

            return false;
        }
    }
}