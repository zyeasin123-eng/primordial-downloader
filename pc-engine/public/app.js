// System Theme Auto Switch
function applyTheme() {
  if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
}
applyTheme();
if (window.matchMedia) {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyTheme);
}

// Elements for Device Landing
const deviceAppLanding = document.getElementById('deviceAppLanding');
const webDownloaderWrapper = document.getElementById('webDownloaderWrapper');
const deviceHeroIcon = document.getElementById('deviceHeroIcon');
const deviceLandingTitle = document.getElementById('deviceLandingTitle');
const deviceLandingDesc = document.getElementById('deviceLandingDesc');
const deviceDownloadBtn = document.getElementById('deviceDownloadBtn');
const deviceBtnIcon = document.getElementById('deviceBtnIcon');
const deviceBtnText = document.getElementById('deviceBtnText');

// Elements for Web Downloader
const urlForm = document.getElementById('urlForm');
const videoUrlInput = document.getElementById('videoUrl');
const pasteBtn = document.getElementById('pasteBtn');
const submitBtn = document.getElementById('submitBtn');

const loadingState = document.getElementById('loadingState');
const loadingStepText = document.getElementById('loadingStepText');
const loadingPercentText = document.getElementById('loadingPercentText');
const loadingProgressBar = document.getElementById('loadingProgressBar');

const errorState = document.getElementById('errorState');
const errorText = document.getElementById('errorText');
const resultState = document.getElementById('resultState');

const resThumbnail = document.getElementById('resThumbnail');
const resDuration = document.getElementById('resDuration');
const resTitle = document.getElementById('resTitle');
const customFileNameInput = document.getElementById('customFileName');
const resetNameBtn = document.getElementById('resetNameBtn');
const formatsContainer = document.getElementById('formatsContainer');

const tabBoth = document.getElementById('tabBoth');
const tabVideoOnly = document.getElementById('tabVideoOnly');
const tabAudioOnly = document.getElementById('tabAudioOnly');

// Download Progress Modal Elements
const downloadModal = document.getElementById('downloadModal');
const cancelDlBtn = document.getElementById('cancelDlBtn');
const dlIcon = document.getElementById('dlIcon');
const dlFileName = document.getElementById('dlFileName');
const dlStatusMessage = document.getElementById('dlStatusMessage');
const dlDetailsText = document.getElementById('dlDetailsText');
const dlPercentText = document.getElementById('dlPercentText');
const dlProgressBar = document.getElementById('dlProgressBar');
const dlSpeedText = document.getElementById('dlSpeedText');
const dlEtaText = document.getElementById('dlEtaText');

// QR Modal Elements
const showQrBtn = document.getElementById('showQrBtn');
const closeQrBtn = document.getElementById('closeQrBtn');
const qrModal = document.getElementById('qrModal');
const qrImage = document.getElementById('qrImage');
const mobileUrlDisplay = document.getElementById('mobileUrlDisplay');
const copyUrlBtn = document.getElementById('copyUrlBtn');

let currentData = null;
let currentTab = 'both';
let activeJobInterval = null;
let loadingSimTimer = null;

// ==========================================
// SMART DEVICE DETECTION & ROUTING
// ==========================================
function detectAndRouteDevice() {
  const ua = navigator.userAgent || '';
  const isAndroidApp = ua.includes('MediaDownloaderApp');
  const isApple = /iPhone|iPad|iPod|Macintosh/i.test(ua) && !window.MSStream;
  const isLinux = /Linux/i.test(ua) && !/Android/i.test(ua);
  const isAndroidBrowser = /Android/i.test(ua) && !isAndroidApp;
  const isWindowsBrowser = /Windows/i.test(ua);

  // ONLY Apple devices, Linux desktop, OR inside Android App run Web Downloader directly!
  if (isApple || isLinux || isAndroidApp) {
    showWebDownloader();
    return;
  }

  // Android Browser -> ONLY Android App Download
  if (isAndroidBrowser) {
    showDeviceAppLanding('android');
    return;
  }

  // Windows PC Browser (and others) -> ONLY Windows PC Setup Download
  showDeviceAppLanding('windows');
}

function showDeviceAppLanding(type) {
  if (type === 'android') {
    deviceHeroIcon.className = 'fa-brands fa-android';
    deviceLandingTitle.innerText = 'Primordial Downloader for Android';
    deviceLandingDesc.innerText = 'Install the official Android application for high-speed background downloading, notifications, and ad-free experience.';
    deviceDownloadBtn.href = '/MediaDownloader.apk';
    deviceBtnIcon.className = 'fa-brands fa-android text-lg';
    deviceBtnText.innerText = 'Download Android APK';
  } else {
    deviceHeroIcon.className = 'fa-brands fa-windows text-blue-500';
    deviceLandingTitle.innerText = 'Primordial Downloader for Windows PC';
    deviceLandingDesc.innerText = 'Install the official Windows setup for ultra-fast downloads, 4K/2K conversions, and background service.';
    deviceDownloadBtn.href = '/MediaDownloader_Setup.exe';
    deviceBtnIcon.className = 'fa-brands fa-windows text-lg';
    deviceBtnText.innerText = 'Download Windows Setup (.exe)';
  }

  deviceAppLanding.classList.remove('hidden');
  deviceAppLanding.classList.add('flex');
  webDownloaderWrapper.classList.add('hidden');
}

window.showWebDownloader = function() {
  deviceAppLanding.classList.add('hidden');
  deviceAppLanding.classList.remove('flex');
  webDownloaderWrapper.classList.remove('hidden');
};

// Button Gray-out / Disabled Feedback Helper
function applyButtonGrayOut(btnElement, iconElement, textElement, defaultText, defaultIconClass) {
  if (!btnElement) return;
  btnElement.addEventListener('click', function() {
    const btn = this;
    
    // Immediately turn gray & disabled visually
    btn.classList.remove('bg-blue-600', 'hover:bg-blue-700', 'shadow-blue-600/25');
    btn.classList.add('bg-slate-400', 'dark:bg-slate-700', 'text-slate-200', 'cursor-wait', 'opacity-80', 'pointer-events-none');
    
    if (iconElement) iconElement.className = 'fa-solid fa-circle-notch fa-spin text-white';
    if (textElement) textElement.innerText = 'Starting download...';

    setTimeout(() => {
      if (iconElement) iconElement.className = 'fa-solid fa-check text-emerald-300';
      if (textElement) textElement.innerText = 'Download started! Check your files';
    }, 1200);

    setTimeout(() => {
      btn.classList.remove('bg-slate-400', 'dark:bg-slate-700', 'text-slate-200', 'cursor-wait', 'opacity-80', 'pointer-events-none');
      btn.classList.add('bg-blue-600', 'hover:bg-blue-700', 'shadow-blue-600/25');
      if (iconElement) iconElement.className = defaultIconClass;
      if (textElement) textElement.innerText = defaultText;
    }, 6000);
  });
}

// Bind gray-out to main landing download button
if (deviceDownloadBtn) {
  deviceDownloadBtn.addEventListener('click', function() {
    const btn = this;
    const origText = deviceBtnText.innerText;
    const origIcon = deviceBtnIcon.className;

    btn.classList.remove('bg-blue-600', 'hover:bg-blue-700', 'shadow-blue-600/25');
    btn.classList.add('bg-slate-400', 'dark:bg-slate-700', 'text-slate-200', 'cursor-wait', 'opacity-80', 'pointer-events-none');

    deviceBtnIcon.className = 'fa-solid fa-circle-notch fa-spin text-white';
    deviceBtnText.innerText = 'Starting download...';

    setTimeout(() => {
      deviceBtnIcon.className = 'fa-solid fa-check text-emerald-300';
      deviceBtnText.innerText = 'Download started! Check notifications';
    }, 1200);

    setTimeout(() => {
      btn.classList.remove('bg-slate-400', 'dark:bg-slate-700', 'text-slate-200', 'cursor-wait', 'opacity-80', 'pointer-events-none');
      btn.classList.add('bg-blue-600', 'hover:bg-blue-700', 'shadow-blue-600/25');
      deviceBtnIcon.className = origIcon;
      deviceBtnText.innerText = origText;
    }, 6000);
  });
}

// Bind gray-out to modal buttons
const directApkDownloadBtn = document.getElementById('directApkDownloadBtn');
const directSetupDownloadBtn = document.getElementById('directSetupDownloadBtn');

if (directApkDownloadBtn) {
  directApkDownloadBtn.addEventListener('click', function() {
    const btn = this;
    const origHtml = btn.innerHTML;
    btn.classList.remove('bg-blue-600', 'hover:bg-blue-700');
    btn.classList.add('bg-slate-400', 'dark:bg-slate-700', 'opacity-80', 'pointer-events-none');
    btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> <span>Starting download...</span>';
    setTimeout(() => {
      btn.innerHTML = '<i class="fa-solid fa-check text-emerald-300"></i> <span>Download started!</span>';
    }, 1200);
    setTimeout(() => {
      btn.classList.remove('bg-slate-400', 'dark:bg-slate-700', 'opacity-80', 'pointer-events-none');
      btn.classList.add('bg-blue-600', 'hover:bg-blue-700');
      btn.innerHTML = origHtml;
    }, 6000);
  });
}

if (directSetupDownloadBtn) {
  directSetupDownloadBtn.addEventListener('click', function() {
    const btn = this;
    const origHtml = btn.innerHTML;
    btn.classList.remove('bg-slate-100', 'dark:bg-slate-800');
    btn.classList.add('bg-slate-400', 'dark:bg-slate-700', 'text-white', 'opacity-80', 'pointer-events-none');
    btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> <span>Starting download...</span>';
    setTimeout(() => {
      btn.innerHTML = '<i class="fa-solid fa-check text-emerald-300"></i> <span>Download started!</span>';
    }, 1200);
    setTimeout(() => {
      btn.classList.remove('bg-slate-400', 'dark:bg-slate-700', 'text-white', 'opacity-80', 'pointer-events-none');
      btn.classList.add('bg-slate-100', 'dark:bg-slate-800');
      btn.innerHTML = origHtml;
    }, 6000);
  });
}

// Initialize Device Routing
detectAndRouteDevice();


// ==========================================
// FORMAT UTILITIES & EVENT HANDLERS
// ==========================================
function formatDuration(sec) {
  if (!sec) return '';
  const m = Math.floor(sec / 60);
  const s = Math.floor(sec % 60);
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

// Paste button handler
if (pasteBtn) {
  pasteBtn.addEventListener('click', async () => {
    try {
      const text = await navigator.clipboard.readText();
      if (text) {
        videoUrlInput.value = text.trim();
        videoUrlInput.focus();
      }
    } catch (err) {
      videoUrlInput.focus();
      videoUrlInput.select();
    }
  });
}

// Reset app
window.resetApp = function() {
  errorState.classList.add('hidden');
  resultState.classList.add('hidden');
  loadingState.classList.add('hidden');
  videoUrlInput.value = '';
  currentData = null;
  if (loadingSimTimer) clearInterval(loadingSimTimer);
  if (activeJobInterval) clearInterval(activeJobInterval);
};

// Reset custom name button
if (resetNameBtn) {
  resetNameBtn.addEventListener('click', () => {
    if (currentData) {
      customFileNameInput.value = currentData.title || '';
    }
  });
}

// ==========================================
// FORM SUBMIT: FETCH METADATA
// ==========================================
urlForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const url = videoUrlInput.value.trim();
  if (!url) return;

  errorState.classList.add('hidden');
  resultState.classList.add('hidden');
  loadingState.classList.remove('hidden');
  submitBtn.disabled = true;

  // Real-time animated loading progress
  let progress = 10;
  loadingProgressBar.style.width = `${progress}%`;
  loadingPercentText.innerText = `${progress}%`;
  loadingStepText.innerHTML = '<span class="w-2 h-2 rounded-full bg-blue-600 animate-ping"></span> Verifying media link...';

  if (loadingSimTimer) clearInterval(loadingSimTimer);
  loadingSimTimer = setInterval(() => {
    if (progress < 90) {
      progress += Math.floor(Math.random() * 8) + 4;
      if (progress > 90) progress = 90;
      loadingProgressBar.style.width = `${progress}%`;
      loadingPercentText.innerText = `${progress}%`;

      if (progress > 30 && progress < 70) {
        loadingStepText.innerHTML = '<span class="w-2 h-2 rounded-full bg-blue-600 animate-ping"></span> Analyzing media stream...';
      } else if (progress >= 70) {
        loadingStepText.innerHTML = '<span class="w-2 h-2 rounded-full bg-blue-600 animate-ping"></span> Preparing formats...';
      }
    }
  }, 250);

  try {
    const res = await fetch('/api/info', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ url })
    });

    const data = await res.json();
    clearInterval(loadingSimTimer);

    if (!data.success) {
      throw new Error(data.message || 'Failed to fetch media information.');
    }

    // Complete loading bar
    loadingProgressBar.style.width = '100%';
    loadingPercentText.innerText = '100%';
    loadingStepText.innerHTML = '<i class="fa-solid fa-check text-emerald-500"></i> Done!';

    setTimeout(() => {
      loadingState.classList.add('hidden');
      displayResult(data);
    }, 400);

  } catch (err) {
    clearInterval(loadingSimTimer);
    loadingState.classList.add('hidden');
    errorText.innerText = err.message || 'Unable to connect to download server.';
    errorState.classList.remove('hidden');
  } finally {
    submitBtn.disabled = false;
  }
});

// ==========================================
// DISPLAY RESULT & QUALITY OPTIONS
// ==========================================
function displayResult(data) {
  currentData = data;

  resThumbnail.src = data.thumbnail || '';
  resTitle.innerText = data.title || 'Unknown Media';
  resDuration.innerText = data.duration ? formatDuration(data.duration) : '';
  customFileNameInput.value = data.title || 'media';

  // Default to Video + Audio
  switchTab('both');
  resultState.classList.remove('hidden');
  resultState.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

window.switchTab = function(tab) {
  currentTab = tab;

  const activeClasses = 'flex-1 py-2.5 px-3 rounded-lg transition text-center flex items-center justify-center gap-1.5 bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-sm font-semibold';
  const inactiveClasses = 'flex-1 py-2.5 px-3 rounded-lg transition text-center flex items-center justify-center gap-1.5 text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white font-medium';

  tabBoth.className = tab === 'both' ? activeClasses : inactiveClasses;
  tabVideoOnly.className = tab === 'video_only' ? activeClasses : inactiveClasses;
  tabAudioOnly.className = tab === 'audio_only' ? activeClasses : inactiveClasses;

  renderFormats();
};

function renderFormats() {
  const formatsMap = currentData ? (currentData.formats || currentData.categories) : null;
  if (!formatsMap) return;

  const formats = formatsMap[currentTab] || [];
  formatsContainer.innerHTML = '';

  if (formats.length === 0) {
    formatsContainer.innerHTML = `
      <div class="p-6 text-center text-xs text-slate-400">
        No formats available in this category.
      </div>
    `;
    return;
  }

  formats.forEach((fmt) => {
    const row = document.createElement('div');
    row.className = 'p-3.5 sm:p-4 flex items-center justify-between gap-3 bg-white dark:bg-slate-900 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition';

    let badgeColor = 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300';
    if (fmt.quality.includes('4K') || fmt.quality.includes('2K')) {
      badgeColor = 'bg-purple-100 text-purple-700 dark:bg-purple-950 dark:text-purple-300 font-bold';
    } else if (fmt.quality.includes('1080p')) {
      badgeColor = 'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300 font-bold';
    } else if (fmt.quality.includes('720p')) {
      badgeColor = 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300';
    } else if (currentTab === 'audio_only') {
      badgeColor = 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300 font-bold';
    }

    const extUpper = (fmt.ext || 'MP4').toUpperCase();

    row.innerHTML = `
      <div class="flex items-center gap-2.5 sm:gap-3 min-w-0">
        <span class="text-xs px-2.5 py-1 rounded-lg ${badgeColor} whitespace-nowrap">
          ${fmt.quality}
        </span>
        <div class="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400 font-mono">
          <span class="font-semibold text-slate-700 dark:text-slate-300">${extUpper}</span>
          ${fmt.filesize ? `<span>•</span><span>${fmt.filesize}</span>` : ''}
          ${fmt.fps ? `<span>•</span><span>${fmt.fps}fps</span>` : ''}
        </div>
      </div>
      <button 
        type="button" 
        onclick="initiateDownload('${fmt.format_id}', '${fmt.ext || 'mp4'}')"
        class="px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-semibold text-xs flex items-center gap-1.5 transition active:scale-95 shadow-sm flex-shrink-0"
      >
        <i class="fa-solid fa-arrow-down text-[11px]"></i>
        <span>Download</span>
      </button>
    `;

    formatsContainer.appendChild(row);
  });
}

// ==========================================
// INITIATE DOWNLOAD WITH LIVE PROGRESS
// ==========================================
window.initiateDownload = async function(formatId, ext) {
  const url = videoUrlInput.value.trim();
  const customName = customFileNameInput.value.trim() || currentData.title || 'media';

  // Setup modal
  dlFileName.innerText = `${customName}.${ext}`;
  dlStatusMessage.innerText = 'Initializing download on server...';
  dlDetailsText.innerText = 'Preparing...';
  dlPercentText.innerText = '0%';
  dlProgressBar.style.width = '0%';
  dlSpeedText.innerText = '-- MB/s';
  dlEtaText.innerText = 'ETA --:--';
  dlIcon.className = 'w-12 h-12 rounded-2xl bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400 flex items-center justify-center text-xl flex-shrink-0';
  dlIcon.innerHTML = '<i class="fa-solid fa-arrow-down animate-bounce"></i>';
  dlProgressBar.classList.remove('bg-rose-500');
  dlProgressBar.classList.add('bg-blue-600');

  downloadModal.classList.remove('hidden');

  try {
    const res = await fetch('/api/start-download', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        url,
        format_id: formatId,
        custom_filename: customName,
        type: currentTab,
        ext
      })
    });

    const data = await res.json();

    if (!data.success || !data.jobId) {
      throw new Error(data.message || 'Could not start download.');
    }

    pollJobStatus(data.jobId);

  } catch (err) {
    // Direct download fallback
    dlStatusMessage.innerText = 'Downloading directly in browser...';
    setTimeout(() => {
      downloadModal.classList.add('hidden');
      const params = new URLSearchParams({
        url,
        format_id: formatId,
        custom_filename: customName,
        type: currentTab,
        ext
      });
      window.location.href = `/api/download?${params.toString()}`;
    }, 1200);
  }
};

function pollJobStatus(jobId) {
  if (activeJobInterval) clearInterval(activeJobInterval);

  activeJobInterval = setInterval(async () => {
    try {
      const res = await fetch(`/api/job-status/${jobId}`);
      if (!res.ok) throw new Error('Job status check failed');
      const job = await res.json();

      if (job.status === 'downloading') {
        const pct = Math.floor(job.percent || 0);
        dlProgressBar.style.width = `${pct}%`;
        dlPercentText.innerText = `${pct}%`;
        dlStatusMessage.innerText = 'Downloading video stream...';
        dlDetailsText.innerText = `${pct}% completed (${job.totalSize || ''})`;
        dlSpeedText.innerText = job.speed || '-- MB/s';
        dlEtaText.innerText = `ETA ${job.eta || '--:--'}`;
      } else if (job.status === 'ready') {
        clearInterval(activeJobInterval);
        dlProgressBar.style.width = '100%';
        dlPercentText.innerText = '100%';
        dlStatusMessage.innerText = 'Download Complete!';
        dlDetailsText.innerText = 'File is being saved to your device...';
        dlIcon.className = 'w-12 h-12 rounded-2xl bg-emerald-50 dark:bg-emerald-950 text-emerald-600 dark:text-emerald-400 flex items-center justify-center text-xl flex-shrink-0';
        dlIcon.innerHTML = '<i class="fa-solid fa-check"></i>';

        const fileDownloadUrl = `${window.location.origin}/api/get-file/${jobId}`;

        // 1. Automatic native file download
        try {
          const directLink = document.createElement('a');
          directLink.href = fileDownloadUrl;
          directLink.setAttribute('download', job.fileName || 'media');
          directLink.style.display = 'none';
          document.body.appendChild(directLink);
          directLink.click();
          setTimeout(() => { try { document.body.removeChild(directLink); } catch(e){} }, 2000);
        } catch(e) {}

        // 2. Fallback iframe trigger for universal mobile/webview compatibility
        try {
          const ifr = document.createElement('iframe');
          ifr.style.display = 'none';
          ifr.src = fileDownloadUrl;
          document.body.appendChild(ifr);
          setTimeout(() => { try { document.body.removeChild(ifr); } catch(e){} }, 8000);
        } catch(e) {}

        // 3. Auto-close modal after 2.5 seconds
        setTimeout(() => {
          downloadModal.classList.add('hidden');
        }, 2500);

      } else if (job.status === 'error' || job.status === 'failed') {
        clearInterval(activeJobInterval);
        dlProgressBar.style.width = '100%';
        dlProgressBar.classList.remove('bg-blue-600');
        dlProgressBar.classList.add('bg-rose-500');
        dlStatusMessage.innerText = 'Download failed';
        dlDetailsText.innerText = job.error || 'Server could not process this video stream.';
        dlIcon.className = 'w-12 h-12 rounded-2xl bg-rose-50 dark:bg-rose-950 text-rose-600 dark:text-rose-400 flex items-center justify-center text-xl flex-shrink-0';
        dlIcon.innerHTML = '<i class="fa-solid fa-triangle-exclamation"></i>';
      }
    } catch (e) {
      // Keep polling or fallback
    }
  }, 800);
}

if (cancelDlBtn) {
  cancelDlBtn.addEventListener('click', () => {
    if (activeJobInterval) clearInterval(activeJobInterval);
    downloadModal.classList.add('hidden');
  });
}

// ==========================================
// QR CODE / SHARE MODAL LOGIC
// ==========================================
let currentQrTarget = 'phone';

window.setQrTarget = function(target) {
  currentQrTarget = target;
  const qrTabPhone = document.getElementById('qrTabPhone');
  const qrTabPc = document.getElementById('qrTabPc');

  const activeClasses = 'flex-1 py-2 px-2.5 rounded-lg transition text-center flex items-center justify-center gap-1.5 bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-sm font-semibold';
  const inactiveClasses = 'flex-1 py-2 px-2.5 rounded-lg transition text-center flex items-center justify-center gap-1.5 text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white font-medium';

  const host = window.location.origin;
  const isLocal = host.includes('192.168.') || host.includes('localhost') || host.includes('127.0.0.1');
  const base = (isLocal && !host.startsWith('file:')) ? host : 'http://db.vegastar.top';

  let targetUrl = `${base}/MediaDownloader.apk`;
  if (target === 'pc') {
    targetUrl = `${base}/MediaDownloader_Setup.exe`;
    if (qrTabPc) qrTabPc.className = activeClasses;
    if (qrTabPhone) qrTabPhone.className = inactiveClasses;
  } else {
    if (qrTabPhone) qrTabPhone.className = activeClasses;
    if (qrTabPc) qrTabPc.className = inactiveClasses;
  }

  if (mobileUrlDisplay) mobileUrlDisplay.innerText = targetUrl;
  if (qrImage) {
    qrImage.src = `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(targetUrl)}`;
  }
};

window.openQrModal = function(defaultTarget = 'phone') {
  qrModal.classList.remove('hidden');
  setQrTarget(defaultTarget);
};

if (showQrBtn) {
  showQrBtn.addEventListener('click', () => openQrModal('phone'));
}

if (closeQrBtn) {
  closeQrBtn.addEventListener('click', () => {
    qrModal.classList.add('hidden');
  });
}

if (qrModal) {
  qrModal.addEventListener('click', (e) => {
    if (e.target === qrModal) {
      qrModal.classList.add('hidden');
    }
  });
}

if (copyUrlBtn) {
  copyUrlBtn.addEventListener('click', async () => {
    const url = (mobileUrlDisplay && mobileUrlDisplay.innerText.trim()) || 'http://db.vegastar.top';
    try {
      await navigator.clipboard.writeText(url);
      const originalText = copyUrlBtn.innerText;
      copyUrlBtn.innerText = 'Copied!';
      setTimeout(() => { copyUrlBtn.innerText = originalText; }, 1500);
    } catch (e) {}
  });
}

// ==========================================
// WELCOME & USAGE GUIDE BANNER / MODAL
// (Always available; dismisses when clicked outside or on X)
// ==========================================
window.dismissWelcomeGuide = function() {
  const modal = document.getElementById('welcomeGuideModal');
  if (modal) {
    modal.classList.add('hidden');
  }
};

window.openWelcomeGuide = function() {
  const modal = document.getElementById('welcomeGuideModal');
  if (modal) {
    modal.classList.remove('hidden');
  }
};

// Dismiss when clicked outside the card (on backdrop / side)
const welcomeModal = document.getElementById('welcomeGuideModal');
if (welcomeModal) {
  welcomeModal.addEventListener('click', (e) => {
    if (e.target === welcomeModal) {
      dismissWelcomeGuide();
    }
  });
}

// Show banner on page load
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    setTimeout(openWelcomeGuide, 300);
  });
} else {
  setTimeout(openWelcomeGuide, 300);
}
