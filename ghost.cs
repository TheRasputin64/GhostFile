using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace GhostFile
{
    public partial class MainForm : Form
    {
        private TextBox txtSourcePath;
        private TextBox txtDestPath;
        private Button btnSourceBrowse;
        private Button btnDestBrowse;
        private Button btnExecute;
        private Button btnCancel;
        private ListView lstFiles;
        private RichTextBox txtLog;
        private ProgressBar prgProgress;
        private Label lblStatus;
        private Panel pnlHeader;
        private TableLayoutPanel tblMain;

        private CancellationTokenSource cts;
        private bool isProcessing = false;
        private int totalItems = 0;
        private int completedItems = 0;
        private Stopwatch operationTimer = new Stopwatch();

        public MainForm()
        {
            InitializeComponent();
            this.Icon = CreateProgramIcon();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings
            this.Text = "GhostFile Pro";
            this.Size = new Size(900, 700);
            this.MinimumSize = new Size(700, 600);
            this.BackColor = Color.FromArgb(24, 24, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Header Panel
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(40, 20, 60),
                Padding = new Padding(15)
            };

            Label titleLabel = new Label
            {
                Text = "GhostFile Pro",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(25, 25)
            };
            pnlHeader.Controls.Add(titleLabel);

            // Main Layout
            tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(32, 32, 36)
            };

            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            // Source path controls
            TableLayoutPanel sourcePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            sourcePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85));
            sourcePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            txtSourcePath = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                Text = "Source Directory..."
            };
            txtSourcePath.GotFocus += (s, e) => { if (txtSourcePath.Text == "Source Directory...") txtSourcePath.Text = ""; };
            txtSourcePath.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSourcePath.Text)) txtSourcePath.Text = "Source Directory..."; };

            btnSourceBrowse = new Button
            {
                Text = "Browse",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 40, 100),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnSourceBrowse.Click += SelectSource;

            sourcePanel.Controls.Add(txtSourcePath, 0, 0);
            sourcePanel.Controls.Add(btnSourceBrowse, 1, 0);

            // Destination path controls
            TableLayoutPanel destPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            destPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85));
            destPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            txtDestPath = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                Text = "Destination Directory..."
            };
            txtDestPath.GotFocus += (s, e) => { if (txtDestPath.Text == "Destination Directory...") txtDestPath.Text = ""; };
            txtDestPath.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtDestPath.Text)) txtDestPath.Text = "Destination Directory..."; };

            btnDestBrowse = new Button
            {
                Text = "Browse",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 40, 100),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnDestBrowse.Click += SelectDestination;

            destPanel.Controls.Add(txtDestPath, 0, 0);
            destPanel.Controls.Add(btnDestBrowse, 1, 0);

            // File list view
            lstFiles = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(38, 38, 42),
                ForeColor = Color.White,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                MultiSelect = false
            };
            lstFiles.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Name", Width = 300 },
                new ColumnHeader { Text = "Type", Width = 100 },
                new ColumnHeader { Text = "Size", Width = 100 },
                new ColumnHeader { Text = "Modified", Width = 150 }
            });

            // Log view
            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 34),
                ForeColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Font = new Font("Consolas", 9F)
            };

            // Action buttons panel
            TableLayoutPanel actionsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            btnExecute = new Button
            {
                Text = "EXECUTE",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 40, 130),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExecute.Click += ExecuteButton_Click;

            btnCancel = new Button
            {
                Text = "CANCEL",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnCancel.Click += CancelButton_Click;

            actionsPanel.Controls.Add(btnExecute, 0, 0);
            actionsPanel.Controls.Add(btnCancel, 1, 0);

            // Progress panel
            TableLayoutPanel progressPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            prgProgress = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(128, 255, 128),
                Margin = new Padding(0)
            };

            lblStatus = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.LightGray,
                BackColor = Color.FromArgb(35, 35, 38),
                Margin = new Padding(5, 0, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            progressPanel.Controls.Add(prgProgress, 0, 0);
            progressPanel.Controls.Add(lblStatus, 1, 0);

            // Add all components to main layout
            tblMain.Controls.Add(sourcePanel, 0, 0);
            tblMain.Controls.Add(destPanel, 0, 1);
            tblMain.Controls.Add(lstFiles, 0, 2);
            tblMain.Controls.Add(txtLog, 0, 3);
            tblMain.Controls.Add(actionsPanel, 0, 4);
            tblMain.Controls.Add(progressPanel, 0, 5);

            // Add panels to form
            this.Controls.Add(tblMain);
            this.Controls.Add(pnlHeader);

            this.ResumeLayout(false);
        }

        private Icon CreateProgramIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(60, 20, 80));

                // Draw a ghost silhouette
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Ghost body
                g.FillEllipse(Brushes.White, 6, 4, 20, 20);

                // Ghost bottom curve
                Point[] points = {
                    new Point(6, 24),
                    new Point(10, 28),
                    new Point(14, 24),
                    new Point(18, 28),
                    new Point(22, 24),
                    new Point(26, 28),
                    new Point(26, 24)
                };
                g.FillPolygon(Brushes.White, points);

                // Ghost eyes
                g.FillEllipse(Brushes.Black, 10, 10, 4, 4);
                g.FillEllipse(Brushes.Black, 18, 10, 4, 4);
            }

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private void SelectSource(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Source Directory";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtSourcePath.Text = dialog.SelectedPath;
                    LogMessage($"Source path set to: {dialog.SelectedPath}");
                    UpdateFileList(dialog.SelectedPath);
                }
            }
        }

        private void SelectDestination(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Destination Directory";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtDestPath.Text = dialog.SelectedPath;
                    LogMessage($"Destination path set to: {dialog.SelectedPath}");
                }
            }
        }

        private void UpdateFileList(string path)
        {
            lstFiles.Items.Clear();
            lblStatus.Text = "Loading files...";

            try
            {
                Thread updateThread = new Thread(new ThreadStart(() => {
                    try
                    {
                        var dirInfo = new DirectoryInfo(path);
                        var directories = dirInfo.GetDirectories();
                        var files = dirInfo.GetFiles();

                        this.Invoke((MethodInvoker)delegate
                        {
                            foreach (var dir in directories)
                            {
                                var item = new ListViewItem(dir.Name);
                                item.SubItems.AddRange(new[] {
                                    "Folder",
                                    "<DIR>",
                                    dir.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                });
                                lstFiles.Items.Add(item);
                            }

                            foreach (var file in files)
                            {
                                var item = new ListViewItem(file.Name);
                                item.SubItems.AddRange(new[] {
                                    file.Extension,
                                    FormatFileSize(file.Length),
                                    file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                });
                                lstFiles.Items.Add(item);
                            }

                            lblStatus.Text = $"{directories.Length} folders, {files.Length} files";
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)delegate {
                            LogMessage($"Error listing files: {ex.Message}");
                            lblStatus.Text = "Error loading files";
                        });
                    }
                }));

                updateThread.IsBackground = true;
                updateThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage($"Error initiating file listing: {ex.Message}");
                lblStatus.Text = "Error";
            }
        }

        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (txtSourcePath.Text == "Source Directory..." || txtDestPath.Text == "Destination Directory..." ||
                string.IsNullOrWhiteSpace(txtSourcePath.Text) || string.IsNullOrWhiteSpace(txtDestPath.Text))
            {
                MessageBox.Show("Please select both source and destination folders.", "Missing Paths",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isProcessing) return;

            if (txtSourcePath.Text == txtDestPath.Text)
            {
                MessageBox.Show("Source and destination cannot be the same directory.", "Invalid Paths",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isProcessing = true;
            cts = new CancellationTokenSource();
            btnExecute.Enabled = false;
            btnCancel.Enabled = true;
            prgProgress.Value = 0;
            totalItems = 0;
            completedItems = 0;

            Thread processThread = new Thread(new ThreadStart(() => {
                try
                {
                    operationTimer.Reset();
                    operationTimer.Start();

                    ProcessDirectory(txtSourcePath.Text, txtDestPath.Text);

                    operationTimer.Stop();
                    this.Invoke((MethodInvoker)delegate {
                        LogMessage($"Operation completed successfully in {operationTimer.ElapsedMilliseconds / 1000.0:F2} seconds");
                        lblStatus.Text = "Completed";
                    });
                }
                catch (OperationCanceledException)
                {
                    this.Invoke((MethodInvoker)delegate {
                        LogMessage("Operation was cancelled by user");
                        lblStatus.Text = "Cancelled";
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate {
                        LogMessage($"Error: {ex.Message}");
                        lblStatus.Text = "Error";
                    });
                }
                finally
                {
                    this.Invoke((MethodInvoker)delegate {
                        isProcessing = false;
                        btnExecute.Enabled = true;
                        btnCancel.Enabled = false;
                    });

                    if (cts != null)
                    {
                        cts.Dispose();
                        cts = null;
                    }
                }
            }));

            processThread.IsBackground = true;
            processThread.Start();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (isProcessing && cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                LogMessage("Cancelling operation...");
                lblStatus.Text = "Cancelling...";
            }
        }

        private void ProcessDirectory(string sourceDir, string destDir)
        {
            // Count items first to enable progress tracking
            this.Invoke((MethodInvoker)delegate {
                LogMessage("Scanning directory structure...");
                lblStatus.Text = "Scanning...";
            });

            CountItems(sourceDir);

            this.Invoke((MethodInvoker)delegate {
                LogMessage($"Found {totalItems} items to process");
                lblStatus.Text = $"{totalItems} items to process";
            });

            // Create base destination directory
            Directory.CreateDirectory(destDir);

            // Create all directories first
            this.Invoke((MethodInvoker)delegate {
                LogMessage("Creating directory structure...");
                lblStatus.Text = "Creating directories...";
            });

            var directories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories)
                .OrderBy(dir => dir.Length)
                .ToArray();

            int dirCount = 0;
            foreach (var dir in directories)
            {
                if (cts.IsCancellationRequested)
                {
                    cts.Token.ThrowIfCancellationRequested();
                }

                string targetDir = dir.Replace(sourceDir, destDir);
                Directory.CreateDirectory(targetDir);

                dirCount++;
                completedItems++;

                if (dirCount % 20 == 0)
                {
                    UpdateProgress();
                    Thread.Sleep(1); // Allow UI to breathe
                }
            }

            UpdateProgress();

            // Now create all files (empty) using batched processing for speed
            this.Invoke((MethodInvoker)delegate {
                LogMessage("Creating file structure...");
                lblStatus.Text = "Creating files...";
            });

            var filePaths = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            // Process files in batches to improve performance while keeping UI responsive
            int batchSize = 100;
            int totalBatches = (filePaths.Length + batchSize - 1) / batchSize;

            for (int batch = 0; batch < totalBatches; batch++)
            {
                if (cts.IsCancellationRequested)
                {
                    cts.Token.ThrowIfCancellationRequested();
                }

                int startIdx = batch * batchSize;
                int endIdx = Math.Min(startIdx + batchSize, filePaths.Length);

                List<Thread> batchThreads = new List<Thread>();
                for (int i = startIdx; i < endIdx; i++)
                {
                    string filePath = filePaths[i];
                    Thread fileThread = new Thread(new ThreadStart(() => {
                        try
                        {
                            string newPath = filePath.Replace(sourceDir, destDir);
                            File.Create(newPath).Dispose(); // Just create empty file
                            Interlocked.Increment(ref completedItems);
                        }
                        catch { /* Skip on error */ }
                    }));

                    fileThread.IsBackground = true;
                    fileThread.Start();
                    batchThreads.Add(fileThread);
                }

                // Wait for all files in this batch to finish
                foreach (var thread in batchThreads)
                {
                    thread.Join();
                }

                UpdateProgress();
            }

            // Ensure final progress update
            UpdateProgress();
        }

        private void CountItems(string path)
        {
            try
            {
                totalItems = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length +
                             Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Length;
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate {
                    LogMessage($"Error counting items: {ex.Message}");
                });
                totalItems = 1; // Prevent division by zero
            }
        }

        private void UpdateProgress()
        {
            if (prgProgress.InvokeRequired)
            {
                prgProgress.Invoke(new Action(UpdateProgress));
                return;
            }

            int percentage = totalItems > 0 ? (int)((double)completedItems / totalItems * 100) : 0;
            prgProgress.Value = Math.Min(percentage, 100);
            lblStatus.Text = $"Processing: {completedItems}/{totalItems} ({percentage}%)";
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(LogMessage), message);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
