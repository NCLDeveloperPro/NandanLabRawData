using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using NandanLabRawData.Logging;

namespace NandanLabRawData
{
    public partial class MainForm : Form
    {
        private FileMonitorService? _monitorService;
        private bool _isMonitoring = false;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Lab Data File Monitor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeComponent()
        {
            // Panel for folder selection
            var folderPanel = new Panel();
            folderPanel.Dock = DockStyle.Top;
            folderPanel.Height = 80;
            folderPanel.Padding = new Padding(10);

            var labelFolder = new Label();
            labelFolder.Text = "Monitor Folder:";
            labelFolder.Location = new Point(10, 10);
            labelFolder.Size = new Size(100, 20);

            var textBoxFolderPath = new TextBox();
            textBoxFolderPath.Name = "textBoxFolderPath";
            textBoxFolderPath.Location = new Point(120, 10);
            textBoxFolderPath.Size = new Size(500, 20);
            textBoxFolderPath.ReadOnly = true;

            var buttonBrowse = new Button();
            buttonBrowse.Text = "Browse...";
            buttonBrowse.Location = new Point(630, 10);
            buttonBrowse.Size = new Size(100, 25);
            buttonBrowse.Click += (s, e) => BrowseFolder(textBoxFolderPath);

            var buttonStart = new Button();
            buttonStart.Name = "buttonStart";
            buttonStart.Text = "Start Monitoring";
            buttonStart.Location = new Point(120, 40);
            buttonStart.Size = new Size(120, 30);
            buttonStart.Click += (s, e) => StartMonitoring(textBoxFolderPath, buttonStart);

            var buttonStop = new Button();
            buttonStop.Name = "buttonStop";
            buttonStop.Text = "Stop Monitoring";
            buttonStop.Location = new Point(250, 40);
            buttonStop.Size = new Size(120, 30);
            buttonStop.Enabled = false;
            buttonStop.Click += (s, e) => StopMonitoring(buttonStart, buttonStop);

            folderPanel.Controls.Add(labelFolder);
            folderPanel.Controls.Add(textBoxFolderPath);
            folderPanel.Controls.Add(buttonBrowse);
            folderPanel.Controls.Add(buttonStart);
            folderPanel.Controls.Add(buttonStop);

            // Status label
            var statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 30;
            statusPanel.Padding = new Padding(10);

            var labelStatus = new Label();
            labelStatus.Name = "labelStatus";
            labelStatus.Text = "Status: Idle";
            labelStatus.Location = new Point(10, 5);
            labelStatus.Size = new Size(700, 20);
            labelStatus.ForeColor = Color.Blue;

            statusPanel.Controls.Add(labelStatus);

            // List of processed files
            var filesPanel = new Panel();
            filesPanel.Dock = DockStyle.Fill;
            filesPanel.Padding = new Padding(10);

            var labelFiles = new Label();
            labelFiles.Text = "Processed Files:";
            labelFiles.Location = new Point(10, 5);
            labelFiles.Size = new Size(100, 20);

            var listBoxFiles = new ListBox();
            listBoxFiles.Name = "listBoxFiles";
            listBoxFiles.Location = new Point(10, 30);
            listBoxFiles.Size = new Size(740, 300);
            listBoxFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            filesPanel.Controls.Add(labelFiles);
            filesPanel.Controls.Add(listBoxFiles);

            // Add all panels to form
            this.Controls.Add(filesPanel);
            this.Controls.Add(statusPanel);
            this.Controls.Add(folderPanel);
        }

        private void BrowseFolder(TextBox textBoxFolderPath)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder to monitor for lab data files";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxFolderPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void StartMonitoring(TextBox textBoxFolderPath, Button buttonStart)
        {
            if (string.IsNullOrWhiteSpace(textBoxFolderPath.Text))
            {
                MessageBox.Show("Please select a folder to monitor.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(textBoxFolderPath.Text))
            {
                MessageBox.Show("Selected folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var listBoxFiles = this.Controls.Find("listBoxFiles", true)[0] as ListBox;
                var labelStatus = this.Controls.Find("labelStatus", true)[0] as Label;
                var buttonStop = this.Controls.Find("buttonStop", true)[0] as Button;

                // Disable Start immediately
                buttonStart.Enabled = false;
                Cursor = Cursors.WaitCursor;  // show wait cursor

                _monitorService = new FileMonitorService(textBoxFolderPath.Text);

                _monitorService.FileProcessed += (filename, success, message) =>
                {
                    this.Invoke(() =>
                    {
                        string status = success ? "[READ]" : "[ERROR]";
                        listBoxFiles?.Items.Add($"{status} {filename} - {message}");
                        listBoxFiles.TopIndex = listBoxFiles.Items.Count - 1;
                    });
                };

                _monitorService.StatusChanged += (message) =>
                {
                    this.Invoke(() =>
                    {
                        if (labelStatus != null)
                        {
                            labelStatus.Text = $"Status: {message}";
                            labelStatus.ForeColor = message.Contains("Error") ? Color.Red : Color.Green;
                        }

                        // When monitoring is active, finalize UI
                        if (message.Contains("Monitoring active"))
                        {
                            Cursor = Cursors.Default;   // reset cursor
                            buttonStop.Enabled = true;
                            textBoxFolderPath.ReadOnly = true;
                        }
                    });
                };

                _monitorService.Start();
                _isMonitoring = true;
                buttonStop.Enabled = true;
                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                buttonStart.Enabled = true;
                MessageBox.Show($"Error starting monitoring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                FileLogger.Log($"Error starting monitoring: {ex.Message}");
            }
        }


        private void StopMonitoring(Button buttonStart, Button buttonStop)
        {
            try
            {
                _monitorService?.Stop();
                _isMonitoring = false;

                buttonStart.Enabled = true;
                var textBoxFolderPath = this.Controls.Find("textBoxFolderPath", true)[0] as TextBox;
                if (textBoxFolderPath != null)
                    textBoxFolderPath.ReadOnly = false;

                buttonStop.Enabled = false;

                var labelStatus = this.Controls.Find("labelStatus", true)[0] as Label;
                if (labelStatus != null)
                {
                    labelStatus.Text = "Status: Monitoring stopped";
                    labelStatus.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error stopping monitoring: {ex.Message}");
                MessageBox.Show($"Error stopping monitoring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isMonitoring)
            {
                _monitorService?.Stop();
            }
        }
    }
}
