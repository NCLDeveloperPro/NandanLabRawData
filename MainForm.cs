using NandanLabRawData.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NandanLabRawData
{
    public class MainForm : Form
    {
        private FileMonitorService _monitorService;
        private bool _isMonitoring = false;
        private ProgressBar progressBar;

        public MainForm()
        {
            BuildUI();

            Text = "Lab Data File Monitor";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormClosing += MainForm_FormClosing;
        }

        private void BuildUI()
        {
            // Folder panel
            Panel folderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            Label labelFolder = new Label
            {
                Text = "Monitor Folder:",
                Location = new Point(10, 10),
                Size = new Size(100, 20)
            };

            TextBox textBoxFolderPath = new TextBox
            {
                Name = "textBoxFolderPath",
                Location = new Point(120, 10),
                Size = new Size(500, 20),
                ReadOnly = true
            };

            Button buttonBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(630, 10),
                Size = new Size(100, 25)
            };

            buttonBrowse.Click += (s, e) => BrowseFolder(textBoxFolderPath);

            Button buttonStart = new Button
            {
                Name = "buttonStart",
                Text = "Start Monitoring",
                Location = new Point(120, 40),
                Size = new Size(120, 30)
            };

            buttonStart.Click += (s, e) => StartMonitoring(textBoxFolderPath, buttonStart);

            Button buttonStop = new Button
            {
                Name = "buttonStop",
                Text = "Stop Monitoring",
                Location = new Point(250, 40),
                Size = new Size(120, 30),
                Enabled = false
            };

            buttonStop.Click += (s, e) => StopMonitoring(buttonStart, buttonStop);

            folderPanel.Controls.Add(labelFolder);
            folderPanel.Controls.Add(textBoxFolderPath);
            folderPanel.Controls.Add(buttonBrowse);
            folderPanel.Controls.Add(buttonStart);
            folderPanel.Controls.Add(buttonStop);

            // Status panel
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(10)
            };

            Label labelStatus = new Label
            {
                Name = "labelStatus",
                Text = "Status: Idle",
                Location = new Point(10, 5),
                Size = new Size(700, 20),
                ForeColor = Color.Blue
            };

            statusPanel.Controls.Add(labelStatus);

            // Progress panel
            Panel progressPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(10)
            };

            progressBar = new ProgressBar
            {
                Name = "progressBar",
                Location = new Point(10, 5),
                Size = new Size(740, 20),
                Visible = false
            };

            progressPanel.Controls.Add(progressBar);

            // Files panel
            Panel filesPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label labelFiles = new Label
            {
                Text = "Processed Files:",
                Dock = DockStyle.Top,
                Height = 25
            };

            DataGridView dgvFiles = new DataGridView
            {
                Name = "dgvFiles",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = true,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.FixedSingle,               
            };
            dgvFiles.Columns.Add("Parameter", "File Name");
            filesPanel.Controls.Add(labelFiles);
            filesPanel.Controls.Add(dgvFiles);

            Controls.Add(filesPanel);
            Controls.Add(progressPanel);
            Controls.Add(statusPanel);
            Controls.Add(folderPanel);
        }

        private void BrowseFolder(TextBox textBoxFolderPath)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder to monitor";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxFolderPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void StartMonitoring(TextBox textBoxFolderPath, Button buttonStart)
        {
            if (string.IsNullOrWhiteSpace(textBoxFolderPath.Text))
            {
                MessageBox.Show(
                    "Please select a folder.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Directory.Exists(textBoxFolderPath.Text))
            {
                MessageBox.Show(
                    "Folder does not exist.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                DataGridView dgvFiles =
                    Controls.Find("dgvFiles", true)[0] as DataGridView;

                Label labelStatus =
                    Controls.Find("labelStatus", true)[0] as Label;

                Button buttonStop =
                    Controls.Find("buttonStop", true)[0] as Button;

                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

                buttonStart.Enabled = false;

                _monitorService = new FileMonitorService(textBoxFolderPath.Text);

                _monitorService.FileProcessed += (filename, success, message) =>
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (dgvFiles != null)
                        {
                            string status = success ? "[READ]" : "[ERROR]";
                            message = !string.IsNullOrEmpty(message) ? $"{filename} - {message}" : $"{filename}";
                            dgvFiles.Rows.Add(message);
                        }
                    });
                };              

                _monitorService.StatusChanged += (message) =>
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (labelStatus != null)
                        {
                            labelStatus.Text = "Status: " + message;
                            labelStatus.ForeColor =
                                message.Contains("Error")
                                ? Color.Red
                                : Color.Green;
                        }
                    });
                };

                _monitorService.Start();

                _isMonitoring = true;

                buttonStop.Enabled = true;

                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = progressBar.Maximum;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                FileLogger.Log(ex.ToString());
            }
        }

        private void StopMonitoring(Button buttonStart, Button buttonStop)
        {
            try
            {
                if (_monitorService != null)
                {
                    _monitorService.Stop();
                }

                _isMonitoring = false;

                buttonStart.Enabled = true;
                buttonStop.Enabled = false;

                Label labelStatus =
                    Controls.Find("labelStatus", true)[0] as Label;

                if (labelStatus != null)
                {
                    labelStatus.Text = "Status: Monitoring stopped";
                    labelStatus.ForeColor = Color.Orange;
                }

                progressBar.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                FileLogger.Log(ex.ToString());
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isMonitoring && _monitorService != null)
            {
                _monitorService.Stop();
            }
        }

    }
}