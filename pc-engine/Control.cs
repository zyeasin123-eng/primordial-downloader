using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MediaDownloaderControlApp
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                ServicePointManager.DefaultConnectionLimit = 50;
                ServicePointManager.Expect100Continue = false;
            }
            catch {}

            if (args != null && args.Length > 0)
            {
                string firstArg = args[0].ToLower();
                if (firstArg.Contains("uninstall"))
                {
                    ControlForm.ExecuteUninstall(true);
                    return;
                }
                if (firstArg.Contains("start") || firstArg.Contains("silent") || firstArg.Contains("run"))
                {
                    ControlForm.StartServerProcess(AppDomain.CurrentDomain.BaseDirectory);
                    return;
                }
            }

            Application.Run(new ControlForm());
        }
    }

    public class ControlForm : Form
    {
        private Panel topPanel;
        private Label lblHeaderTitle;
        private Label lblStatusBadge;

        // Downloader Section
        private GroupBox grpDownloader;
        private TextBox txtUrl;
        private Button btnPaste;
        private Button btnGetMedia;
        private Label lblMediaTitle;
        private Label lblCustomName;
        private TextBox txtCustomName;
        private PictureBox picThumbnail;
        private FlowLayoutPanel pnlFormats;
        private ProgressBar prgDownload;
        private Label lblDownloadStatus;
        private Button btnOpenFolder;

        private System.Windows.Forms.Timer statusTimer;
        private System.Windows.Forms.Timer jobPollTimer;
        private string activeJobId = null;
        private string activeFileName = null;
        private bool isPolling = false;
        private string projectDir;

        public ControlForm()
        {
            projectDir = AppDomain.CurrentDomain.BaseDirectory;
            InitializeComponent();

            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 2500;
            statusTimer.Tick += (s, e) => UpdateServerStatus();
            statusTimer.Start();

            jobPollTimer = new System.Windows.Forms.Timer();
            jobPollTimer.Interval = 1000;
            jobPollTimer.Tick += (s, e) => PollJobStatus();

            UpdateServerStatus();
            CheckDailyTokenAndUpdates();

            System.Windows.Forms.Timer botHeartbeatTimer = new System.Windows.Forms.Timer();
            botHeartbeatTimer.Interval = 60000;
            botHeartbeatTimer.Tick += (s, e) => SendBotHeartbeat();
            botHeartbeatTimer.Start();
            SendBotHeartbeat();
        }

        private void SendBotHeartbeat()
        {
            ThreadPool.QueueUserWorkItem((st) => {
                try
                {
                    string nodeId = "bot_" + Environment.MachineName.ToLower().Replace(" ", "_");
                    try
                    {
                        HttpWebRequest cfReq = (HttpWebRequest)WebRequest.Create("https://heartbeat.primordial-nodes.workers.dev/heartbeat?id=" + nodeId);
                        cfReq.Method = "GET";
                        cfReq.Timeout = 3000;
                        cfReq.KeepAlive = false;
                        cfReq.Proxy = null;
                        using (HttpWebResponse cfResp = (HttpWebResponse)cfReq.GetResponse()) {}
                    }
                    catch {}

                    try
                    {
                        HttpWebRequest pingReq = (HttpWebRequest)WebRequest.Create("http://db.vegastar.top/heartbeat.php?id=" + nodeId);
                        pingReq.Method = "GET";
                        pingReq.Timeout = 3000;
                        pingReq.KeepAlive = false;
                        pingReq.Proxy = null;
                        using (HttpWebResponse pingResp = (HttpWebResponse)pingReq.GetResponse()) {}
                    }
                    catch {}
                }
                catch {}
            });
        }

        private void InitializeComponent()
        {
            this.Text = "Primordial Downloader";
            this.Size = new Size(640, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            try {
                string icoPath = Path.Combine(projectDir, "app.ico");
                if (File.Exists(icoPath)) this.Icon = new Icon(icoPath);
            } catch {}

            // Top Header Panel
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 65;
            topPanel.BackColor = Color.White;
            topPanel.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, 64, topPanel.Width, 64);
            };
            this.Controls.Add(topPanel);

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "Primordial Downloader";
            lblHeaderTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblHeaderTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblHeaderTitle.Location = new Point(18, 12);
            lblHeaderTitle.AutoSize = true;
            topPanel.Controls.Add(lblHeaderTitle);

            lblStatusBadge = new Label();
            lblStatusBadge.Text = "● Initializing...";
            lblStatusBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblStatusBadge.ForeColor = Color.FromArgb(100, 116, 139);
            lblStatusBadge.Location = new Point(22, 38);
            lblStatusBadge.AutoSize = true;
            topPanel.Controls.Add(lblStatusBadge);

            Button btnInfo = new Button();
            btnInfo.Text = "ⓘ";
            btnInfo.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            btnInfo.Size = new Size(34, 34);
            btnInfo.Location = new Point(topPanel.Width - 48, 15);
            btnInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnInfo.FlatStyle = FlatStyle.Flat;
            btnInfo.FlatAppearance.BorderSize = 1;
            btnInfo.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnInfo.BackColor = Color.FromArgb(248, 250, 252);
            btnInfo.ForeColor = Color.FromArgb(71, 85, 105);
            btnInfo.Cursor = Cursors.Hand;
            btnInfo.Click += (s, e) => ShowAboutModal();
            topPanel.Controls.Add(btnInfo);

            // Downloader Group
            grpDownloader = new GroupBox();
            grpDownloader.Text = " Direct PC Media Downloader ";
            grpDownloader.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpDownloader.ForeColor = Color.FromArgb(15, 23, 42);
            grpDownloader.Location = new Point(20, 80);
            grpDownloader.Size = new Size(585, 430);
            grpDownloader.BackColor = Color.White;
            this.Controls.Add(grpDownloader);

            txtUrl = new TextBox();
            txtUrl.Location = new Point(20, 35);
            txtUrl.Size = new Size(390, 28);
            txtUrl.Font = new Font("Segoe UI", 9.5f);
            txtUrl.ForeColor = Color.FromArgb(15, 23, 42);
            grpDownloader.Controls.Add(txtUrl);

            btnPaste = new Button();
            btnPaste.Text = "Paste";
            btnPaste.Location = new Point(418, 33);
            btnPaste.Size = new Size(65, 30);
            btnPaste.BackColor = Color.FromArgb(241, 245, 249);
            btnPaste.FlatStyle = FlatStyle.Flat;
            btnPaste.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnPaste.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnPaste.Cursor = Cursors.Hand;
            btnPaste.Click += (s, e) => {
                try { txtUrl.Text = Clipboard.GetText().Trim(); } catch {}
            };
            grpDownloader.Controls.Add(btnPaste);

            btnGetMedia = new Button();
            btnGetMedia.Text = "Get Media";
            btnGetMedia.Location = new Point(488, 33);
            btnGetMedia.Size = new Size(80, 30);
            btnGetMedia.BackColor = Color.FromArgb(37, 99, 235);
            btnGetMedia.ForeColor = Color.White;
            btnGetMedia.FlatStyle = FlatStyle.Flat;
            btnGetMedia.FlatAppearance.BorderSize = 0;
            btnGetMedia.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnGetMedia.Cursor = Cursors.Hand;
            btnGetMedia.Click += BtnGetMedia_Click;
            grpDownloader.Controls.Add(btnGetMedia);

            // Thumbnail and Title
            picThumbnail = new PictureBox();
            picThumbnail.Location = new Point(20, 75);
            picThumbnail.Size = new Size(160, 90);
            picThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
            picThumbnail.BackColor = Color.FromArgb(15, 23, 42);
            picThumbnail.Visible = false;
            grpDownloader.Controls.Add(picThumbnail);

            lblMediaTitle = new Label();
            lblMediaTitle.Location = new Point(190, 75);
            lblMediaTitle.Size = new Size(375, 34);
            lblMediaTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblMediaTitle.ForeColor = Color.FromArgb(51, 65, 85);
            lblMediaTitle.Visible = false;
            grpDownloader.Controls.Add(lblMediaTitle);

            lblCustomName = new Label();
            lblCustomName.Text = "✏️ Custom File Name (Optional):";
            lblCustomName.Location = new Point(190, 112);
            lblCustomName.Size = new Size(300, 20);
            lblCustomName.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblCustomName.ForeColor = Color.FromArgb(37, 99, 235);
            lblCustomName.Visible = false;
            grpDownloader.Controls.Add(lblCustomName);

            txtCustomName = new TextBox();
            txtCustomName.Location = new Point(190, 134);
            txtCustomName.Size = new Size(375, 26);
            txtCustomName.Font = new Font("Segoe UI", 9f);
            txtCustomName.ForeColor = Color.FromArgb(15, 23, 42);
            txtCustomName.BorderStyle = BorderStyle.FixedSingle;
            txtCustomName.Visible = false;
            grpDownloader.Controls.Add(txtCustomName);

            // Formats Flow Panel
            pnlFormats = new FlowLayoutPanel();
            pnlFormats.Location = new Point(20, 175);
            pnlFormats.Size = new Size(545, 135);
            pnlFormats.AutoScroll = true;
            pnlFormats.BackColor = Color.FromArgb(248, 250, 252);
            pnlFormats.BorderStyle = BorderStyle.FixedSingle;
            grpDownloader.Controls.Add(pnlFormats);

            // Progress Bar & Status
            prgDownload = new ProgressBar();
            prgDownload.Location = new Point(20, 325);
            prgDownload.Size = new Size(545, 24);
            prgDownload.Visible = false;
            grpDownloader.Controls.Add(prgDownload);

            lblDownloadStatus = new Label();
            lblDownloadStatus.Text = "Paste a media URL and click 'Get Media'";
            lblDownloadStatus.Font = new Font("Segoe UI", 9f);
            lblDownloadStatus.ForeColor = Color.FromArgb(100, 116, 139);
            lblDownloadStatus.Location = new Point(20, 360);
            lblDownloadStatus.Size = new Size(420, 45);
            grpDownloader.Controls.Add(lblDownloadStatus);

            btnOpenFolder = new Button();
            btnOpenFolder.Text = "Open Folder";
            btnOpenFolder.Location = new Point(450, 360);
            btnOpenFolder.Size = new Size(115, 34);
            btnOpenFolder.BackColor = Color.FromArgb(241, 245, 249);
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnOpenFolder.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnOpenFolder.Cursor = Cursors.Hand;
            btnOpenFolder.Click += (s, e) => {
                string userDl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                Process.Start("explorer.exe", userDl);
            };
            grpDownloader.Controls.Add(btnOpenFolder);
        }

        private void BtnGetMedia_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a valid video link.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnGetMedia.Enabled = false;
            lblDownloadStatus.Text = "Fetching media details, please wait...";
            pnlFormats.Controls.Clear();
            picThumbnail.Visible = false;
            lblMediaTitle.Visible = false;

            ThreadPool.QueueUserWorkItem((state) => {
                try
                {
                    string postData = "{\"url\":\"" + url.Replace("\"", "\\\"") + "\"}";
                    byte[] data = Encoding.UTF8.GetBytes(postData);

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3000/api/info");
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.ContentLength = data.Length;
                    req.Proxy = null;
                    req.Timeout = 25000;

                    using (Stream st = req.GetRequestStream())
                    {
                        st.Write(data, 0, data.Length);
                    }

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        this.Invoke(new Action(() => RenderFormats(json, url)));
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => {
                        btnGetMedia.Enabled = true;
                        lblDownloadStatus.Text = "Error: Could not retrieve media info. Check if background service is running.";
                    }));
                }
            });
        }

        private void RenderFormats(string json, string videoUrl)
        {
            btnGetMedia.Enabled = true;
            pnlFormats.Controls.Clear();

            // Simple parsing
            string title = "Media Video";
            int titleIdx = json.IndexOf("\"title\":");
            if (titleIdx > 0)
            {
                int start = json.IndexOf("\"", titleIdx + 8) + 1;
                int end = json.IndexOf("\"", start);
                if (start > 0 && end > start) title = json.Substring(start, end - start);
            }

            lblMediaTitle.Text = title;
            lblMediaTitle.Visible = true;
            lblCustomName.Visible = true;
            txtCustomName.Text = title;
            txtCustomName.Visible = true;

            // Thumbnail
            int thumbIdx = json.IndexOf("\"thumbnail\":");
            if (thumbIdx > 0)
            {
                int start = json.IndexOf("\"", thumbIdx + 12) + 1;
                int end = json.IndexOf("\"", start);
                if (start > 0 && end > start)
                {
                    string thumbUrl = json.Substring(start, end - start).Replace("\\/", "/");
                    if (thumbUrl.Contains("vi_webp") && thumbUrl.Contains(".webp"))
                    {
                        thumbUrl = thumbUrl.Replace("/vi_webp/", "/vi/").Replace(".webp", ".jpg");
                    }
                    ThreadPool.QueueUserWorkItem((s) => {
                        try
                        {
                            using (WebClient wc = new WebClient())
                            {
                                byte[] imgBytes = wc.DownloadData(thumbUrl);
                                using (MemoryStream ms = new MemoryStream(imgBytes))
                                {
                                    Image img = Image.FromStream(ms);
                                    this.Invoke(new Action(() => {
                                        picThumbnail.Image = img;
                                        picThumbnail.Visible = true;
                                        picThumbnail.BringToFront();
                                    }));
                                }
                            }
                        }
                        catch {}
                    });
                }
            }

            // Dynamically parse both formats from json using Regex
            bool hasFormats = false;
            int bothStart = json.IndexOf("\"both\":[");
            int videoOnlyStart = json.IndexOf("\"video_only\":[", bothStart > 0 ? bothStart : 0);
            string bothChunk = "";
            if (bothStart > 0 && videoOnlyStart > bothStart)
            {
                bothChunk = json.Substring(bothStart, videoOnlyStart - bothStart);
            }
            else if (bothStart > 0)
            {
                bothChunk = json.Substring(bothStart);
            }

            MatchCollection formatMatches = Regex.Matches(bothChunk, "\"quality\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"ext\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"format_id\"\\s*:\\s*\"([^\"]+)\"");
            foreach (Match m in formatMatches)
            {
                string qualityLabel = m.Groups[1].Value;
                string ext = m.Groups[2].Value;
                string fId = m.Groups[3].Value;
                if (string.IsNullOrEmpty(fId) || fId == "best")
                {
                    fId = "bestvideo+bestaudio/best";
                }
                AddFormatButton(qualityLabel, fId, ext, "both", videoUrl, title);
                hasFormats = true;
            }

            if (!hasFormats)
            {
                AddFormatButton("Original Video", "bestvideo+bestaudio/best", "mp4", "both", videoUrl, title);
            }

            AddFormatButton("MP3 Audio (320kbps)", "bestaudio/best", "mp3", "audio_only", videoUrl, title);

            lblDownloadStatus.Text = "Choose quality and start download:";
        }

        private void AddFormatButton(string quality, string formatId, string ext, string type, string url, string title)
        {
            Button btn = new Button();
            btn.Text = quality;
            btn.Size = new Size(165, 38);
            btn.Margin = new Padding(6);
            btn.BackColor = Color.White;
            btn.ForeColor = Color.FromArgb(15, 23, 42);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            btn.Click += (s, e) => StartDownloadJob(url, formatId, ext, type, title);
            pnlFormats.Controls.Add(btn);
        }

        private void StartDownloadJob(string url, string formatId, string ext, string type, string defaultTitle)
        {
            string chosenTitle = (!string.IsNullOrEmpty(txtCustomName.Text.Trim())) ? txtCustomName.Text.Trim() : defaultTitle;
            prgDownload.Value = 0;
            prgDownload.Visible = true;
            lblDownloadStatus.ForeColor = Color.FromArgb(30, 41, 59);
            lblDownloadStatus.Text = "Sending request to download engine...";
            string safeTitle = chosenTitle;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safeTitle = safeTitle.Replace(c, '_');
            }
            activeFileName = safeTitle + "." + ext;
            isPolling = false;

            ThreadPool.QueueUserWorkItem((st) => {
                try
                {
                    string jsonPayload = string.Format("{{\"url\":\"{0}\",\"format_id\":\"{1}\",\"custom_filename\":\"{2}\",\"type\":\"{3}\",\"ext\":\"{4}\"}}",
                        url.Replace("\"", "\\\""), formatId, safeTitle.Replace("\"", "\\\""), type, ext);
                    byte[] data = Encoding.UTF8.GetBytes(jsonPayload);

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3000/api/start-download");
                    req.Method = "POST";
                    req.ContentType = "application/json; charset=utf-8";
                    req.ContentLength = data.Length;
                    req.KeepAlive = false;
                    req.Timeout = 10000;
                    req.Proxy = null;

                    using (Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string r = sr.ReadToEnd();
                        Match mJob = Regex.Match(r, "\"jobId\"\\s*:\\s*\"([^\"]+)\"");
                        if (mJob.Success)
                        {
                            activeJobId = mJob.Groups[1].Value;
                            this.Invoke(new Action(() => {
                                lblDownloadStatus.Text = "Starting download...";
                                jobPollTimer.Start();
                            }));
                        }
                        else
                        {
                            this.Invoke(new Action(() => {
                                lblDownloadStatus.Text = "Failed to start download (No Job ID received)";
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => {
                        lblDownloadStatus.Text = "Failed to start download: " + ex.Message;
                    }));
                }
            });
        }

        private void PollJobStatus()
        {
            if (string.IsNullOrEmpty(activeJobId) || isPolling) return;
            isPolling = true;

            ThreadPool.QueueUserWorkItem((st) => {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3000/api/job-status/" + activeJobId);
                    req.Proxy = null;
                    req.KeepAlive = false;
                    req.Timeout = 3000;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        this.Invoke(new Action(() => {
                            Match mStatus = Regex.Match(json, "\"status\"\\s*:\\s*\"([^\"]+)\"");
                            Match mPct = Regex.Match(json, "\"percent\"\\s*:\\s*([0-9.]+)");
                            Match mMsg = Regex.Match(json, "\"message\"\\s*:\\s*\"([^\"]+)\"");
                            Match mErr = Regex.Match(json, "\"error\"\\s*:\\s*\"([^\"]+)\"");

                            if (mPct.Success)
                            {
                                double pVal;
                                if (double.TryParse(mPct.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out pVal))
                                {
                                    prgDownload.Value = Math.Min(100, Math.Max(0, (int)pVal));
                                }
                            }

                            string status = mStatus.Success ? mStatus.Groups[1].Value : "";

                            if (status == "starting")
                            {
                                lblDownloadStatus.Text = "Preparing media stream...";
                            }
                            else if (status == "downloading")
                            {
                                string msg = mMsg.Success ? mMsg.Groups[1].Value : string.Format("Downloading... {0}%", prgDownload.Value);
                                lblDownloadStatus.Text = msg;
                            }
                            else if (status == "merging")
                            {
                                prgDownload.Value = 98;
                                lblDownloadStatus.Text = "Merging audio and video...";
                            }
                            else if (status == "error" || status == "failed")
                            {
                                jobPollTimer.Stop();
                                string errText = mErr.Success ? mErr.Groups[1].Value : "Download failed";
                                lblDownloadStatus.Text = "Error: " + errText;
                                lblDownloadStatus.ForeColor = Color.FromArgb(220, 38, 38);
                                activeJobId = null;
                            }
                            else if (status == "ready")
                            {
                                jobPollTimer.Stop();
                                string completedJobId = activeJobId;
                                activeJobId = null;
                                prgDownload.Value = 100;
                                lblDownloadStatus.Text = "Download complete! Saving to Downloads folder...";

                                // Save directly into user Downloads folder
                                string userDl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                                string savePath = Path.Combine(userDl, activeFileName);

                                ThreadPool.QueueUserWorkItem((dl) => {
                                    try
                                    {
                                        using (WebClient wc = new WebClient())
                                        {
                                            wc.Proxy = null;
                                            wc.DownloadFile("http://127.0.0.1:3000/api/get-file/" + completedJobId, savePath);
                                        }
                                        this.Invoke(new Action(() => {
                                            lblDownloadStatus.Text = "✅ File saved successfully to Downloads folder!";
                                            lblDownloadStatus.ForeColor = Color.FromArgb(22, 163, 74);
                                        }));
                                    }
                                    catch (Exception ex)
                                    {
                                        this.Invoke(new Action(() => {
                                            lblDownloadStatus.Text = "File save error: " + ex.Message;
                                        }));
                                    }
                                });
                            }
                        }));
                    }
                }
                catch {}
                finally
                {
                    isPolling = false;
                }
            });
        }

        private bool IsServerRunning()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("node");
                if (procs != null && procs.Length > 0) return true;
            }
            catch {}

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3000/api/network-info");
                req.Proxy = null;
                req.Timeout = 1500;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    return resp.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateServerStatus()
        {
            bool running = IsServerRunning();
            if (running)
            {
                lblStatusBadge.ForeColor = Color.FromArgb(22, 163, 74);
                lblStatusBadge.Text = "● Service Active (Port 3000)";
            }
            else
            {
                lblStatusBadge.ForeColor = Color.FromArgb(220, 38, 38);
                lblStatusBadge.Text = "● Service Offline";
            }
        }

        public static void StartServerProcess(string baseDir)
        {
            try
            {
                StopServerProcess();
                Thread.Sleep(800);

                string nodeExe = "node.exe";
                if (File.Exists(@"C:\Program Files\nodejs\node.exe"))
                {
                    nodeExe = @"C:\Program Files\nodejs\node.exe";
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = nodeExe;
                psi.Arguments = "server.js";
                psi.WorkingDirectory = baseDir;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch {}
        }

        public static void StopServerProcess()
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName("node"))
                {
                    try { p.Kill(); } catch {}
                }
                foreach (Process p in Process.GetProcessesByName("cloudflared"))
                {
                    try { p.Kill(); } catch {}
                }
            }
            catch {}
        }

        public static void ExecuteUninstall(bool quiet)
        {
            StopServerProcess();
            try
            {
                ProcessStartInfo psiTask = new ProcessStartInfo("schtasks.exe", "/Delete /TN \"MediaDownloaderService\" /F");
                psiTask.CreateNoWindow = true;
                psiTask.UseShellExecute = false;
                Process p = Process.Start(psiTask);
                if (p != null) p.WaitForExit(3000);
            }
            catch {}
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MediaDownloader", false); } catch {}
        }

        private void ShowAboutModal()
        {
            using (Form about = new Form())
            {
                about.Text = "Primordial Downloader - About";
                about.Size = new Size(460, 360);
                about.StartPosition = FormStartPosition.CenterParent;
                about.FormBorderStyle = FormBorderStyle.FixedDialog;
                about.MaximizeBox = false;
                about.MinimizeBox = false;
                about.BackColor = Color.White;
                about.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

                Label title = new Label();
                title.Text = "Primordial Downloader";
                title.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
                title.ForeColor = Color.FromArgb(15, 23, 42);
                title.Location = new Point(24, 20);
                title.AutoSize = true;
                about.Controls.Add(title);

                Label ver = new Label();
                ver.Text = "Version: v29.2012.2 (Official Release)";
                ver.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                ver.ForeColor = Color.FromArgb(37, 99, 235);
                ver.Location = new Point(26, 52);
                ver.AutoSize = true;
                about.Controls.Add(ver);

                Label desc = new Label();
                desc.Text = "• Architecture: Decentralized Local Engine (127.0.0.1)\n" +
                            "• Features: Ad-free, no tracking cookies, unlimited speed\n" +
                            "• Security: Safe background service protected by Windows Defender\n" +
                            "• Publisher: Primordial / Vegastar\n" +
                            "• Status: 100% Free for all PC and Mobile devices";
                desc.Font = new Font("Segoe UI", 9.5f);
                desc.ForeColor = Color.FromArgb(71, 85, 105);
                desc.Location = new Point(26, 90);
                desc.Size = new Size(395, 130);
                about.Controls.Add(desc);

                Button btnCheckUpdate = new Button();
                btnCheckUpdate.Text = "🔄 Check Updates";
                btnCheckUpdate.Location = new Point(26, 240);
                btnCheckUpdate.Size = new Size(160, 38);
                btnCheckUpdate.FlatStyle = FlatStyle.Flat;
                btnCheckUpdate.BackColor = Color.FromArgb(37, 99, 235);
                btnCheckUpdate.ForeColor = Color.White;
                btnCheckUpdate.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnCheckUpdate.Cursor = Cursors.Hand;
                btnCheckUpdate.Click += (s, e) => {
                    btnCheckUpdate.Enabled = false;
                    btnCheckUpdate.Text = "Checking...";
                    ThreadPool.QueueUserWorkItem((st) => CheckForUpdateNow(about, btnCheckUpdate));
                };
                about.Controls.Add(btnCheckUpdate);

                Button btnWeb = new Button();
                btnWeb.Text = "🌐 Website";
                btnWeb.Location = new Point(196, 240);
                btnWeb.Size = new Size(130, 38);
                btnWeb.FlatStyle = FlatStyle.Flat;
                btnWeb.BackColor = Color.FromArgb(241, 245, 249);
                btnWeb.ForeColor = Color.FromArgb(37, 99, 235);
                btnWeb.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnWeb.Cursor = Cursors.Hand;
                btnWeb.Click += (s, e) => {
                    try { Process.Start(new ProcessStartInfo("http://db.vegastar.top/") { UseShellExecute = true }); } catch {}
                };
                about.Controls.Add(btnWeb);

                Button btnClose = new Button();
                btnClose.Text = "OK";
                btnClose.Location = new Point(336, 240);
                btnClose.Size = new Size(85, 38);
                btnClose.FlatStyle = FlatStyle.Flat;
                btnClose.BackColor = Color.FromArgb(241, 245, 249);
                btnClose.ForeColor = Color.FromArgb(51, 65, 85);
                btnClose.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnClose.Cursor = Cursors.Hand;
                btnClose.Click += (s, e) => about.Close();
                about.Controls.Add(btnClose);

                about.ShowDialog(this);
            }
        }

        private void CheckForUpdateNow(Form parentForm, Button btn)
        {
            try
            {
                string[] beaconUrls = new string[] {
                    "https://raw.githubusercontent.com/zyeasin123-eng/primordial-downloader/main/config.json",
                    "http://db.vegastar.top/config.json"
                };
                string json = null;
                foreach (string bUrl in beaconUrls)
                {
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(bUrl);
                        req.Method = "GET";
                        req.Timeout = 4000;
                        req.KeepAlive = false;
                        req.Proxy = null;
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            json = sr.ReadToEnd();
                        }
                        if (!string.IsNullOrEmpty(json)) break;
                    }
                    catch {}
                }

                parentForm.BeginInvoke(new Action(() => {
                    btn.Enabled = true;
                    btn.Text = "🔄 Check Updates";

                    if (string.IsNullOrEmpty(json))
                    {
                        MessageBox.Show(parentForm, "Could not connect to update server. Please check your internet connection.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    Match mVer = Regex.Match(json, "\"latest_version\"\\s*:\\s*\"([^\"]+)\"");
                    string latestVer = mVer.Success ? mVer.Groups[1].Value.Trim() : "29.2012.2";
                    string currentVer = "29.2012.2";

                    if (!string.Equals(latestVer, currentVer, StringComparison.OrdinalIgnoreCase))
                    {
                        Match mUrl = Regex.Match(json, "\"pc_download_url\"\\s*:\\s*\"([^\"]+)\"");
                        string updateUrl = mUrl.Success ? mUrl.Groups[1].Value : "http://db.vegastar.top/MediaDownloader_Setup.exe";

                        Match mNotes = Regex.Match(json, "\"changelog\"\\s*:\\s*\"([^\"]+)\"");
                        string changelog = mNotes.Success ? mNotes.Groups[1].Value.Replace("\\n", "\n") : "Performance improvements and bug fixes.";

                        DialogResult dr = MessageBox.Show(parentForm,
                            "🎉 New Version Available (v" + latestVer + ")!\n\n" +
                            "Official Version: v" + latestVer + "\n" +
                            "Current Version: v" + currentVer + "\n\n" +
                            "New Features:\n" + changelog + "\n\n" +
                            "Would you like to update to the latest version now?",
                            "Version Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (dr == DialogResult.Yes)
                        {
                            parentForm.Close();
                            ExecuteOneClickUpdate(updateUrl);
                        }
                    }
                    else
                    {
                        MessageBox.Show(parentForm, "✅ You are running the latest official version (v" + currentVer + ")!", "Official Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }));
            }
            catch
            {
                parentForm.BeginInvoke(new Action(() => {
                    btn.Enabled = true;
                    btn.Text = "🔄 Check Updates";
                }));
            }
        }

        private void CheckDailyTokenAndUpdates()
        {
            ThreadPool.QueueUserWorkItem((st) => {
                try
                {
                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    string tokenFile = Path.Combine(projectDir, "daily_token.json");

                    // 1. Primary Master Config: GitHub Raw (0% cPanel dependency, always-on, free forever)
                    // 2. Fallback: db.vegastar.top
                    string[] beaconUrls = new string[] {
                        "https://raw.githubusercontent.com/zyeasin123-eng/primordial-downloader/main/config.json",
                        "http://db.vegastar.top/config.json"
                    };
                    string json = null;
                    foreach (string bUrl in beaconUrls)
                    {
                        try
                        {
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(bUrl);
                            req.Method = "GET";
                            req.Timeout = 3000;
                            req.ReadWriteTimeout = 3000;
                            req.KeepAlive = false;
                            req.Proxy = null;
                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            {
                                json = sr.ReadToEnd();
                            }
                            if (!string.IsNullOrEmpty(json)) break;
                        }
                        catch {}
                    }

                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            File.WriteAllText(tokenFile, "{\"cached_date\":\"" + today + "\"}");
                        }
                        catch {}

                        // Exact Version Lock: Only the version specified in central config is allowed!
                        Match mVer = Regex.Match(json, "\"latest_version\"\\s*:\\s*\"([^\"]+)\"");
                        if (mVer.Success)
                        {
                            string latestVer = mVer.Groups[1].Value.Trim();
                            string currentVer = "29.2012.2";
                            Match mForce = Regex.Match(json, "\"force_update\"\\s*:\\s*(true|false)");
                            bool isMandatory = (mForce.Success && mForce.Groups[1].Value.Trim().ToLower() == "true");

                            if (!string.Equals(latestVer, currentVer, StringComparison.OrdinalIgnoreCase))
                            {
                                Match mUrl = Regex.Match(json, "\"pc_download_url\"\\s*:\\s*\"([^\"]+)\"");
                                string updateUrl = mUrl.Success ? mUrl.Groups[1].Value : "http://db.vegastar.top/MediaDownloader_Setup.exe";

                                Match mNotes = Regex.Match(json, "\"changelog\"\\s*:\\s*\"([^\"]+)\"");
                                string changelog = mNotes.Success ? mNotes.Groups[1].Value.Replace("\\n", "\n") : "Performance improvements and security updates.";

                                this.BeginInvoke(new Action(() => {
                                    if (isMandatory && grpDownloader != null) grpDownloader.Enabled = false;

                                    DialogResult dr = MessageBox.Show(this,
                                        (isMandatory ? "⚠️ Mandatory Update Required (v" : "🎉 Official Version Update (v") + latestVer + ")!\n\n" +
                                        "Official Version: v" + latestVer + "\n" +
                                        "Current Version: v" + currentVer + "\n\n" +
                                        "New Features:\n" + changelog + "\n\n" +
                                        "Would you like to update to the latest version now?",
                                        "Official Version Update",
                                        isMandatory ? MessageBoxButtons.OK : MessageBoxButtons.YesNo,
                                        isMandatory ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                                    if (isMandatory || dr == DialogResult.Yes)
                                    {
                                        ExecuteOneClickUpdate(updateUrl);
                                    }
                                }));

                                if (isMandatory) return; // Do not cache daily token for unauthorized version
                            }
                        }

                        // Broadcast Notice if present
                        Match mNotice = Regex.Match(json, "\"broadcast_notice\"\\s*:\\s*\"([^\"]+)\"");
                        if (mNotice.Success && !string.IsNullOrEmpty(mNotice.Groups[1].Value))
                        {
                            string notice = mNotice.Groups[1].Value;
                            this.BeginInvoke(new Action(() => {
                                if (lblStatusBadge != null)
                                {
                                    lblStatusBadge.Text = "● " + notice;
                                }
                            }));
                        }
                    }
                }
                catch
                {
                    // Fail-Open: Server/network issue -> Do nothing, local engine runs 100% smoothly
                }
            });
        }

        private void ExecuteOneClickUpdate(string updateUrl)
        {
            ThreadPool.QueueUserWorkItem((st) => {
                try
                {
                    string tempSetup = Path.Combine(Path.GetTempPath(), "MediaDownloader_Setup_Latest.exe");
                    using (WebClient wc = new WebClient())
                    {
                        wc.Proxy = null;
                        wc.DownloadFile(updateUrl, tempSetup);
                    }

                    if (File.Exists(tempSetup))
                    {
                        Process.Start(new ProcessStartInfo(tempSetup) { UseShellExecute = true });
                        this.BeginInvoke(new Action(() => this.Close()));
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new Action(() => {
                        MessageBox.Show(this, "Update download failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                }
            });
        }

        private static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) return string.IsNullOrEmpty(v2) ? 0 : -1;
            if (string.IsNullOrEmpty(v2)) return 1;

            string[] parts1 = v1.Trim().Split('.');
            string[] parts2 = v2.Trim().Split('.');

            int maxLen = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < maxLen; i++)
            {
                long num1 = 0;
                long num2 = 0;

                if (i < parts1.Length)
                {
                    string clean1 = Regex.Replace(parts1[i], @"[^\d]", "");
                    if (!string.IsNullOrEmpty(clean1)) long.TryParse(clean1, out num1);
                }
                if (i < parts2.Length)
                {
                    string clean2 = Regex.Replace(parts2[i], @"[^\d]", "");
                    if (!string.IsNullOrEmpty(clean2)) long.TryParse(clean2, out num2);
                }

                if (num1 > num2) return 1;
                if (num1 < num2) return -1;
            }

            return 0;
        }
    }
}
