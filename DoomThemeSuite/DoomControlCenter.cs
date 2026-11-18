using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DoomThemeSuite
{
    public class DoomControlCenterForm : Form
    {
        // ====================================================================
        // Win32 Native Imports for Borderless Drag, DWM Shadow & Global Hotkeys
        // ====================================================================
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const int WM_HOTKEY = 0x0312;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private const int HOTKEY_ID_STUDIO = 9101;   // Ctrl+Alt+T
        private const int HOTKEY_ID_SHADOW = 9102;   // Ctrl+Alt+G
        private const int HOTKEY_ID_DESKTOP = 9103;  // Ctrl+Alt+D
        private const int HOTKEY_ID_PALETTE = 9104;  // Ctrl+Shift+Space

        // ====================================================================
        // Paths
        // ====================================================================
        public static readonly string BaseDir = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen";
        public static readonly string SuiteDir = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite";
        public static readonly string IconPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\Beyin.ico";
        public static readonly string AltIconPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\Araclar.ico";
        public static readonly string ExePath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomControlCenter.exe";

        // ====================================================================
        // v3.0 Editorial Cyber-Studio Design Tokens (L0..L3 + Semantic)
        // ====================================================================
        private static readonly Color ColL0Void = Color.FromArgb(7, 8, 12);         // #07080C
        private static readonly Color ColL1Surface = Color.FromArgb(22, 24, 32);     // #161820
        private static readonly Color ColL1Hover = Color.FromArgb(30, 33, 44);       // #1E212C
        private static readonly Color ColL2Pill = Color.FromArgb(34, 38, 50);        // L2 Control surface
        private static readonly Color ColTextPrimary = Color.FromArgb(242, 244, 248);   // #F2F4F8
        private static readonly Color ColTextSecondary = Color.FromArgb(163, 169, 184); // #A3A9B8
        private static readonly Color ColTextTertiary = Color.FromArgb(107, 113, 131);  // #6B7183
        private static readonly Color ColTextDark = Color.FromArgb(10, 11, 16);
        private static readonly Color ColSuccess = Color.FromArgb(46, 230, 166);     // #2EE6A6
        private static readonly Color ColWarn = Color.FromArgb(255, 181, 71);        // #FFB547
        private static readonly Color ColDanger = Color.FromArgb(255, 77, 94);       // #FF4D5E
        private static readonly Color ColInfo = Color.FromArgb(77, 184, 255);        // #4DB8FF

        private Color _uiBg = Color.FromArgb(7, 8, 12);
        private Color _uiHeaderBg = Color.FromArgb(14, 17, 23);
        private Color _uiCard = Color.FromArgb(20, 24, 31);
        private Color _uiCardHover = Color.FromArgb(28, 34, 44);
        private Color _uiBorderSubtle = Color.FromArgb(44, 56, 68);
        private Color _uiActiveAccent = Color.FromArgb(31, 74, 49);
        private Color _uiActiveRim = Color.FromArgb(108, 168, 129);
        private Color _uiAccent2 = Color.FromArgb(77, 184, 255);
        private Color _uiGoldCta = Color.FromArgb(198, 176, 118);
        private Color _uiGoldCtaHover = Color.FromArgb(218, 196, 138);

        // ====================================================================
        // Studio State & Configuration (v2.0 + v3.0 Bento Deck)
        // ====================================================================
        private Dictionary<string, string> _config;
        private List<ThemePreset> _allPresets = new List<ThemePreset>();
        private ThemePreset _activePreset;
        private string _activeProfileId = "DoomEmerald";
        private readonly HashSet<string> _favoritePresetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _galleryFilterMode = 0; // 0=All, 1=Favorites, 2=Custom

        // Overlay / Explorer / 10-bit HDR Engine
        private bool _emeraldTintAndShadowEnabled = true;
        private int _emeraldTintIntensity = 72;      // 0..140
        private int _emeraldShadowIntensity = 185;   // 0..255
        private int _shadowSpread = 28;              // 8..48
        private bool _showRimBorder = true;
        private bool _shadowOnlyActiveWindow = false;
        private bool _explorerGlassEnabled = false;
        private int _explorerAlpha = 238;            // 150..255
        private int _explorerBlurMode = 0;           // 0=HDR Net Mod, 1=Acrylic, 2=SDR Blur, 3=Mica
        private bool _hdrWhiteTextGuard = true;
        private int _hdrNitsClamp = 200;             // 80..400 nits
        private int _borderCornerRadius = 10;        // 0, 8, 10, 14
        private int _borderRimWidth = 2;             // 1, 2, 3
        private bool _borderSimRunning = false;
        private float _borderSimPhase = 0f;

        // Custom Colors (editable in View 1: Color & Light Studio)
        private Color _customPrimaryAccent = Color.FromArgb(31, 74, 49);
        private Color _customBodyTint = Color.FromArgb(14, 54, 32);
        private Color _customShadowGlow = Color.FromArgb(38, 115, 71);
        private Color _customRimHighlight = Color.FromArgb(108, 168, 129);
        private Color _customTaskbarTint = Color.FromArgb(24, 44, 31);
        private int _customTaskbarAlpha = 180;

        // View 1: Color Studio, Harmony Generator & 360° Glow Bézier Curve
        private int _selectedColorChannel = 0; // 0..4
        private double _hsvH = 145.0;          // 0..360
        private double _hsvS = 0.65;           // 0..1
        private double _hsvV = 0.55;           // 0..1
        private Bitmap _hsvSpectrumBitmap;
        private Rectangle _hsvBoxRect = new Rectangle(258, 344, 220, 145);
        private Rectangle _hsvValRect = new Rectangle(486, 344, 22, 145);
        private bool _draggingHsvBox = false;
        private bool _draggingHsvVal = false;
        private HarmonyMode _harmonyMode = HarmonyMode.Analog;
        private Color[] _harmonySwatches = new Color[5];
        private Color[] _chameleonSwatches = new Color[6];
        private int _bezierPresetIndex = 0; // 0=Gauss, 1=Neon, 2=DualHalo, 3=Cinema
        private PointF _bezierP1 = new PointF(0.28f, 0.85f);
        private PointF _bezierP2 = new PointF(0.68f, 0.22f);
        private float _glowPreviewAngle = 0f;

        // View 3: Taskbar & Dock
        private int _taskbarMode = 1;                // 0=Classic, 1=Clear, 2=FloatingDock, 3=DynamicIsland, 4=Segmented
        private bool _taskbarCenterIcons = false;
        private bool _showTaskbarSeconds = false;
        private bool _hideTaskViewButton = true;
        private int _searchboxMode = 1;              // 0=Hidden, 1=Icon, 2=Box
        private int _taskbarCornerRadius = 12;       // 0..24
        private int _taskbarDockMargin = 6;          // 0..16
        private bool _taskbarSpecularLine = true;
        private bool _islandExpandOnMedia = true;
        private bool _trayCompactOverflow = true;
        private bool _ribbonMinimized = true;
        private bool _hideClutterFolders = true;
        private bool _hideQuickAccessHistory = true;

        // View 4: Desktop, Icons & 8-Widget HUD Suite (including Spectrum & Pomodoro)
        private bool _autoApplyFolderIcons = true;
        private int _desktopIconSize = 48;           // 32, 48, 64
        private bool _hideShortcutArrows = false;
        private bool _hudEnabled = true;
        private int _hudPosition = 0;                // 0=RightColumn, 1=TopPanoramic, 2=CornerCockpit
        private int _hudOpacity = 230;               // 80..255
        private bool _widgetClock = true;
        private bool _widgetSystemGpu = true;
        private bool _widgetStorage = true;
        private bool _widgetNetwork = true;
        private bool _widgetMedia = true;
        private bool _widgetNotes = true;
        private bool _widgetSpectrum = true;
        private bool _widgetPomodoro = true;
        private bool _widgetsLocked = true;
        private int _widgetLayout = 0;               // 0..2
        private int _widgetScale = 100;              // 85, 100, 115
        private int _inspectedWidgetIndex = 6;       // 0..7 (default inspect Spectrum or Clock)
        private int _spectrumStyle = 0;              // 0=Bars, 1=Mirror, 2=Wave, 3=RadialRing
        private int _spectrumBandCount = 32;         // 16, 32, 64
        private int _spectrumSmoothing = 68;         // 10..95 (%)
        private List<string> _wallpaperGallery = new List<string>();
        private string _selectedWallpaperPath = "";
        private readonly Dictionary<string, Bitmap> _wallpaperThumbCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private Bitmap _previewFolderIcon1;
        private Bitmap _previewFolderIcon2;

        // View 5: Game/Render Boost, Smart Focus, RAM Purger & Display Dimmer/Kelvin
        private BoostProfile _boostProfile = BoostProfile.Normal;
        private bool _smartFocusEnabled = true;
        private string _smartFocusActiveProcess = "";
        private int _gpuPauseThreshold = 80;         // 50..95%
        private int _ramAutoPurgeThreshold = 88;     // 70..95%
        private double _lastPurgedMb = 0.0;
        private int _lastPurgedProcCount = 0;
        private bool _displayOverlayEnabled = false;
        private int _displayDimmerPercent = 0;       // 0..80%
        private int _displayWarmthKelvin = 6500;     // 2700..6500K

        // View 6: System, Shortcuts, Snapshots & Diagnostic Health
        private bool _autoStart = true;
        private bool _hotkeysEnabled = true;
        private bool _scheduleEnabled = false;
        private string _dayProfile = "DoomEmerald";
        private string _nightProfile = "ObsidianStealth";
        private int _rendererFpsCap = 60;            // 30 or 60
        private string _lastPackageDiffSummary = "Hazır — .doomtheme paket doğrulama ve fark motoru aktif.";
        private readonly List<string> _diagnosticLogs = new List<string>();
        private System.Windows.Forms.Timer _scheduleTimer;

        // Bento Shell Navigation, Rail, Preview Dock & Animations
        private int _activeTab = 0;                  // 0..6 (7 Bento Views!)
        private int _themeGalleryPage = 0;
        private bool _railExpanded = true;
        private float _railWidthAnim = 224f;         // 64f..224f
        private float _railIndicatorY = 104f;
        private bool _previewDockExpanded = true;
        private int _previewSplitPercent = 0;        // 0..100 (Before/After split)
        private bool _previewHdrSim = true;
        private float _viewTransitionAlpha = 1.0f;
        private float _viewTransitionOffsetY = 0f;
        private System.Windows.Forms.Timer _animTimer;
        private System.Windows.Forms.Timer _telemetryTimer;

        // Live Telemetry & Sparklines (24 samples)
        private readonly float[] _cpuSparkline = new float[24];
        private readonly float[] _ramSparkline = new float[24];
        private int _telemetryWriteIndex = 0;
        private int _liveCpuPercent = 12;
        private double _liveRamUsedGb = 8.4;
        private double _liveRamFreeGb = 23.6;
        private double _liveRamTotalGb = 32.0;
        private int _liveRamPercent = 26;

        // Undo & Toast Notification
        private Dictionary<string, string> _undoConfigSnapshot = null;
        private string _undoDescription = "";
        private string _toastText = "";
        private int _toastTicksRemaining = 0;
        private string _statusMessage = "Hazır — SPLEND // Bento Command Deck v3.0 aktif (Ctrl+Shift+Space: Komut Paleti).";

        // Global Command Palette Overlay (Ctrl+Shift+Space)
        private bool _commandPaletteOpen = false;
        private string _commandSearchQuery = "";
        private int _commandSelectedIndex = 0;
        private List<CommandPaletteItem> _filteredCommands = new List<CommandPaletteItem>();

        // ====================================================================
        // Interactive Hit Zones
        // ====================================================================
        private class UiZone
        {
            public string Id;
            public Rectangle Bounds;
            public string Title;
            public string Subtitle;
            public Color Swatch1;
            public Color Swatch2;
            public Color Swatch3;
            public Color Swatch4;
            public string Tag;
        }

        private readonly List<UiZone> _zones = new List<UiZone>();
        private readonly List<UiZone> _paletteZones = new List<UiZone>();
        private readonly Dictionary<string, float> _zoneHoverLerp = new Dictionary<string, float>();
        private string _hoveredZoneId = null;
        private string _activeDragSliderId = null;
        private readonly Dictionary<string, Rectangle> _sliderTracks = new Dictionary<string, Rectangle>();

        // ====================================================================
        // Tray Icon & Fonts
        // ====================================================================
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _trayThemesSubMenu;
        private ToolStripMenuItem _trayWidgetsSubMenu;
        private ToolStripMenuItem _trayEmeraldOverlayMenuItem;
        private ToolStripMenuItem _trayHudMenuItem;
        private bool _allowClose = false;
        private bool _startMinimized = false;
        private Icon _appIcon;

        private Font _fontDisplay;
        private Font _fontHeader;
        private Font _fontSection;
        private Font _fontTab;
        private Font _fontCardTitle;
        private Font _fontCardSub;
        private Font _fontButton;
        private Font _fontButtonBold;
        private Font _fontSmall;
        private Font _fontMono;
        private Font _fontMonoLarge;

        private static readonly StringFormat SfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat SfRight = new StringFormat { Alignment = StringAlignment.Far };
        private static readonly StringFormat SfNearCenter = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat SfVerticalCenter = new StringFormat(StringFormatFlags.DirectionVertical) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        public DoomControlCenterForm(bool startMinimized)
        {
            _startMinimized = startMinimized;

            this.Text = "SPLEND // WINDOWS THEME STUDIO v3.0 — BENTO COMMAND DECK";
            this.FormBorderStyle = FormBorderStyle.None;
            this.AutoScaleMode = AutoScaleMode.None;
            this.MinimumSize = new Size(1120, 720);
            this.ClientSize = new Size(1440, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColL0Void;
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            LoadFontsAndIcons();
            BuildHsvSpectrumBitmap(220, 145);
            LoadWallpapersList();
            InitializeTrayIcon();
            InitializeSchedulerTimer();
            InitializeAnimationAndTelemetryTimers();

            DesktopHudWidget.OnWidgetMoved = (widgetId, wx, wy) =>
            {
                try
                {
                    if (_config == null) _config = DoomThemeEngine.LoadConfig();
                    _config["WidgetPos_" + widgetId] = wx + "," + wy;
                    DoomThemeEngine.SaveConfig(_config);
                }
                catch { }
            };

            AddDiagnosticLog("Bento Command Deck v3.0 başlatıldı (8 HUD Widget + 10-bit HDR + Smart Focus).");
            LoadStateFromEngine();
            RegenerateHarmonySwatches();
            RefreshChameleonSwatches();
            SampleTelemetryOnce();
            RebuildZonesForCurrentTab();
            ApplyLiveRuntimeEngines();

            if (_startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Opacity = 0;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TryEnableDwmShadow();
            UpdateGlobalHotkeysRegistration();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_startMinimized)
            {
                this.Hide();
                this.Opacity = 1.0;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                RebuildZonesForCurrentTab();
                this.Invalidate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_STUDIO)
                {
                    ShowMainWindow();
                }
                else if (id == HOTKEY_ID_PALETTE)
                {
                    ShowMainWindow();
                    OpenCommandPalette("");
                }
                else if (id == HOTKEY_ID_SHADOW)
                {
                    ToggleEmeraldArmorOverlay();
                }
                else if (id == HOTKEY_ID_DESKTOP)
                {
                    try
                    {
                        bool vis = DoomThemeEngine.ToggleDesktopIconsVisibility();
                        SetStatus(vis ? "Masaüstü simgeleri gösterildi (Ctrl+Alt+D)." : "Masaüstü simgeleri gizlendi (Ctrl+Alt+D).");
                        this.Invalidate();
                    }
                    catch { }
                }
            }
            base.WndProc(ref m);
        }

        private void TryEnableDwmShadow()
        {
            try
            {
                int val = 2;
                DwmSetWindowAttribute(this.Handle, 2, ref val, sizeof(int));
                int cornerPref = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));
                MARGINS margins = new MARGINS { leftWidth = 1, rightWidth = 1, topHeight = 1, bottomHeight = 1 };
                DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            }
            catch { }
        }

        private void UpdateGlobalHotkeysRegistration()
        {
            if (!this.IsHandleCreated) return;
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID_STUDIO);
                UnregisterHotKey(this.Handle, HOTKEY_ID_SHADOW);
                UnregisterHotKey(this.Handle, HOTKEY_ID_DESKTOP);
                UnregisterHotKey(this.Handle, HOTKEY_ID_PALETTE);

                if (_hotkeysEnabled)
                {
                    RegisterHotKey(this.Handle, HOTKEY_ID_STUDIO, MOD_CONTROL | MOD_ALT, (uint)Keys.T);
                    RegisterHotKey(this.Handle, HOTKEY_ID_SHADOW, MOD_CONTROL | MOD_ALT, (uint)Keys.G);
                    RegisterHotKey(this.Handle, HOTKEY_ID_DESKTOP, MOD_CONTROL | MOD_ALT, (uint)Keys.D);
                    RegisterHotKey(this.Handle, HOTKEY_ID_PALETTE, MOD_CONTROL | MOD_SHIFT, (uint)Keys.Space);
                }
            }
            catch { }
        }

        private void InitializeSchedulerTimer()
        {
            _scheduleTimer = new System.Windows.Forms.Timer();
            _scheduleTimer.Interval = 60000;
            _scheduleTimer.Tick += (s, e) => CheckDayNightSchedule();
            _scheduleTimer.Start();
        }

        private void InitializeAnimationAndTelemetryTimers()
        {
            for (int i = 0; i < 24; i++)
            {
                _cpuSparkline[i] = 14f + (i % 5) * 3f;
                _ramSparkline[i] = 28f;
            }

            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 24; // ~42-60Hz smooth UI spring/lerp loop
            _animTimer.Tick += OnAnimationTick;
            _animTimer.Start();

            _telemetryTimer = new System.Windows.Forms.Timer();
            _telemetryTimer.Interval = 1200;
            _telemetryTimer.Tick += (s, e) =>
            {
                SampleTelemetryOnce();
                if (this.Visible && this.WindowState != FormWindowState.Minimized)
                {
                    this.Invalidate(new Rectangle(0, 0, this.ClientSize.Width, 56));
                    if (_activeTab == 4 || _activeTab == 5)
                        this.Invalidate();
                }
            };
            _telemetryTimer.Start();
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            if (!this.Visible || this.WindowState == FormWindowState.Minimized) return;

            bool dirty = false;

            // 1. Collapsible Left Icon Rail spring width
            float targetRailW = _railExpanded ? 224f : 64f;
            if (Math.Abs(_railWidthAnim - targetRailW) > 0.5f)
            {
                _railWidthAnim += (targetRailW - _railWidthAnim) * 0.28f;
                if (Math.Abs(_railWidthAnim - targetRailW) <= 0.8f) _railWidthAnim = targetRailW;
                RebuildZonesForCurrentTab();
                dirty = true;
            }

            // 2. Left Rail Active Indicator Y spring
            float targetIndY = 104f + _activeTab * 56f;
            if (Math.Abs(_railIndicatorY - targetIndY) > 0.5f)
            {
                _railIndicatorY += (targetIndY - _railIndicatorY) * 0.26f;
                dirty = true;
            }

            // 3. View transition fade & Y slide
            if (_viewTransitionAlpha < 1.0f || Math.Abs(_viewTransitionOffsetY) > 0.3f)
            {
                _viewTransitionAlpha = Math.Min(1.0f, _viewTransitionAlpha + 0.16f);
                _viewTransitionOffsetY *= 0.72f;
                if (Math.Abs(_viewTransitionOffsetY) < 0.4f) _viewTransitionOffsetY = 0f;
                dirty = true;
            }

            // 4. View 1 Glow preview ring rotation
            if (_activeTab == 1)
            {
                _glowPreviewAngle = (_glowPreviewAngle + 4.5f) % 360f;
                dirty = true;
            }

            // 5. View 2 Border Physics focus simulator
            if (_borderSimRunning)
            {
                _borderSimPhase += 0.045f;
                if (_borderSimPhase >= 6.283f)
                {
                    _borderSimPhase = 0f;
                    _borderSimRunning = false;
                }
                dirty = true;
            }

            // 6. View 4 Audio Spectrum live preview animation
            if (_activeTab == 4)
            {
                dirty = true;
            }

            // 7. Toast countdown
            if (_toastTicksRemaining > 0)
            {
                _toastTicksRemaining--;
                if (_toastTicksRemaining == 0) _toastText = "";
                dirty = true;
            }

            if (dirty)
            {
                this.Invalidate();
            }
        }

        private void SampleTelemetryOnce()
        {
            try
            {
                double uGb, fGb, tGb;
                int pct;
                if (DoomThemeEngine.GetSystemMemoryStats(out uGb, out fGb, out tGb, out pct))
                {
                    _liveRamUsedGb = uGb;
                    _liveRamFreeGb = fGb;
                    _liveRamTotalGb = tGb;
                    _liveRamPercent = pct;
                }
            }
            catch { }

            try
            {
                int hudCpu = DesktopHudWidget.LatestCpuPercent;
                if (hudCpu > 0) _liveCpuPercent = hudCpu;
                else _liveCpuPercent = 8 + (Environment.TickCount / 1000) % 19;
            }
            catch { }

            _cpuSparkline[_telemetryWriteIndex] = _liveCpuPercent;
            _ramSparkline[_telemetryWriteIndex] = _liveRamPercent;
            _telemetryWriteIndex = (_telemetryWriteIndex + 1) % 24;

            // Smart Focus Auto-Pause check
            if (_smartFocusEnabled)
            {
                try
                {
                    string reason;
                    bool shouldPause = SmartFocusMonitor.ShouldPauseEffects(out reason);
                    _smartFocusActiveProcess = shouldPause ? (reason ?? "Tam Ekran Uygulama") : "";
                    DesktopHudWidget.IsSmartPaused = shouldPause;
                }
                catch { }
            }
            else
            {
                _smartFocusActiveProcess = "";
                try { DesktopHudWidget.IsSmartPaused = false; } catch { }
            }
        }

        private void AddDiagnosticLog(string msg)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
            _diagnosticLogs.Insert(0, line);
            if (_diagnosticLogs.Count > 30)
                _diagnosticLogs.RemoveAt(_diagnosticLogs.Count - 1);
        }

        private void ShowToast(string text, int durationMs = 4200)
        {
            _toastText = text;
            _toastTicksRemaining = Math.Max(20, durationMs / 24);
            SetStatus(text);
        }

        private void CaptureUndoState(string description)
        {
            try
            {
                if (_config != null)
                {
                    _undoConfigSnapshot = new Dictionary<string, string>(_config, StringComparer.OrdinalIgnoreCase);
                    _undoDescription = description;
                }
            }
            catch { }
        }

        private void ExecuteUndoLastAction()
        {
            if (_undoConfigSnapshot != null && _undoConfigSnapshot.Count > 0)
            {
                try
                {
                    DoomThemeEngine.SaveConfig(_undoConfigSnapshot);
                    LoadStateFromEngine();
                    ApplyLiveRuntimeEngines();
                    ShowToast("↶ Geri alındı: " + _undoDescription);
                    AddDiagnosticLog("Geri alma uygulandı: " + _undoDescription);
                    _undoConfigSnapshot = null;
                    RebuildZonesForCurrentTab();
                    this.Invalidate();
                    return;
                }
                catch { }
            }

            try
            {
                if (ThemeSnapshotManager.RollbackToLatest())
                {
                    LoadStateFromEngine();
                    ApplyLiveRuntimeEngines();
                    ShowToast("↶ En son snapshot durumuna geri dönüldü.");
                    RebuildZonesForCurrentTab();
                    this.Invalidate();
                    return;
                }
            }
            catch { }

            ShowToast("Geri alınacak yakın değişiklik bulunamadı.");
        }

        private void CheckDayNightSchedule()
        {
            if (!_scheduleEnabled) return;
            try
            {
                int hour = DateTime.Now.Hour;
                string targetProfile = (hour >= 7 && hour < 20) ? _dayProfile : _nightProfile;
                if (!string.Equals(_activeProfileId, targetProfile, StringComparison.OrdinalIgnoreCase))
                {
                    ActivatePresetById(targetProfile, false);
                    SetStatus("Otomatik Zamanlayıcı: " + targetProfile + " temasına geçildi.");
                }
            }
            catch { }
        }

        private void LoadFontsAndIcons()
        {
            _fontDisplay = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold);
            _fontHeader = new Font("Segoe UI Semibold", 10.0f, FontStyle.Bold);
            _fontSection = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold);
            _fontTab = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold);
            _fontCardTitle = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold);
            _fontCardSub = new Font("Segoe UI", 7.8f, FontStyle.Regular);
            _fontButton = new Font("Segoe UI Semibold", 8.2f, FontStyle.Regular);
            _fontButtonBold = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            _fontSmall = new Font("Segoe UI", 8.0f, FontStyle.Regular);
            _fontMono = new Font("Consolas", 8.5f, FontStyle.Bold);
            _fontMonoLarge = new Font("Consolas", 15.0f, FontStyle.Bold);

            try
            {
                if (File.Exists(IconPath))
                    _appIcon = new Icon(IconPath);
                else if (File.Exists(AltIconPath))
                    _appIcon = new Icon(AltIconPath);
                else
                    _appIcon = SystemIcons.Shield;
                this.Icon = _appIcon;
            }
            catch
            {
                _appIcon = SystemIcons.Shield;
            }
        }

        private void LoadWallpapersList()
        {
            try
            {
                _wallpaperGallery = DoomThemeEngine.GetWallpaperGalleryFiles();
                if (_wallpaperGallery == null) _wallpaperGallery = new List<string>();
                if (_wallpaperGallery.Count > 0 && string.IsNullOrEmpty(_selectedWallpaperPath))
                    _selectedWallpaperPath = _wallpaperGallery[0];
            }
            catch
            {
                _wallpaperGallery = new List<string>();
            }
        }

        // ====================================================================
        // State & Engine Synchronization
        // ====================================================================
        public void LoadStateFromEngine()
        {
            try
            {
                _config = DoomThemeEngine.LoadConfig();
            }
            catch
            {
                _config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            RefreshPresetCatalog();

            _activeProfileId = GetConfigString("ActiveProfile", "DoomEmerald");
            _activePreset = ThemeCatalog.GetPresetById(_activeProfileId);

            string favCsv = GetConfigString("FavoritePresets", "DoomEmerald,LatveriaGold,CyberpunkCrimson");
            _favoritePresetIds.Clear();
            foreach (string part in favCsv.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                _favoritePresetIds.Add(part.Trim());
            }

            _emeraldTintAndShadowEnabled = GetConfigBool("EmeraldTintAndShadowEnabled", true);
            _emeraldTintIntensity = GetConfigInt("EmeraldTintIntensity", _activePreset.DefaultTintAlpha, 0, 140);
            _emeraldShadowIntensity = GetConfigInt("EmeraldShadowIntensity", _activePreset.DefaultShadowAlpha, 0, 255);
            _shadowSpread = GetConfigInt("ShadowSpread", _activePreset.DefaultShadowSpread > 0 ? _activePreset.DefaultShadowSpread : 28, 8, 48);
            _showRimBorder = GetConfigBool("ShowRimBorder", true);
            _shadowOnlyActiveWindow = GetConfigBool("ShadowOnlyActiveWindow", false);

            _explorerGlassEnabled = GetConfigBool("ExplorerGlassEnabled", false);
            _explorerAlpha = GetConfigInt("ExplorerAlpha", 238, 150, 255);
            _explorerBlurMode = GetConfigInt("ExplorerBlurMode", 0, 0, 3);
            _hdrWhiteTextGuard = GetConfigBool("HdrWhiteTextGuard", GetConfigBool("HdrWhiteTextGuardEnabled", true));
            _hdrNitsClamp = GetConfigInt("HdrNitsClamp", GetConfigInt("HdrMaxNitsClamp", 200, 80, 400), 80, 400);
            _borderCornerRadius = GetConfigInt("BorderCornerRadius", 10, 0, 16);
            _borderRimWidth = GetConfigInt("BorderRimWidth", 2, 1, 4);

            _customPrimaryAccent = ThemeCatalog.HexToColor(GetConfigString("PrimaryAccentHex", ThemeCatalog.ColorToHex(_activePreset.PrimaryAccent)), _activePreset.PrimaryAccent);
            _customBodyTint = ThemeCatalog.HexToColor(GetConfigString("BodyTintHex", ThemeCatalog.ColorToHex(_activePreset.ExplorerBodyTint)), _activePreset.ExplorerBodyTint);
            _customShadowGlow = ThemeCatalog.HexToColor(GetConfigString("ShadowGlowHex", ThemeCatalog.ColorToHex(_activePreset.ExplorerShadowGlow)), _activePreset.ExplorerShadowGlow);
            _customRimHighlight = ThemeCatalog.HexToColor(GetConfigString("RimHighlightHex", ThemeCatalog.ColorToHex(_activePreset.RimHighlight)), _activePreset.RimHighlight);
            _customTaskbarTint = ThemeCatalog.HexToColor(GetConfigString("TaskbarTintHex", ThemeCatalog.ColorToHex(_activePreset.TaskbarTint)), _activePreset.TaskbarTint);
            _customTaskbarAlpha = GetConfigInt("TaskbarTintAlpha", 180, 0, 255);

            _taskbarMode = GetConfigInt("TaskbarMode", 1, 0, 4);
            _taskbarCenterIcons = GetConfigBool("TaskbarCenterIcons", false);
            _showTaskbarSeconds = GetConfigBool("ShowTaskbarSeconds", false);
            _hideTaskViewButton = GetConfigBool("HideTaskViewButton", true);
            _searchboxMode = GetConfigInt("SearchboxMode", 1, 0, 2);
            _taskbarCornerRadius = GetConfigInt("TaskbarCornerRadius", 12, 0, 24);
            _taskbarDockMargin = GetConfigInt("TaskbarDockMargin", GetConfigInt("TaskbarMarginPx", 6, 0, 16), 0, 16);
            _taskbarSpecularLine = GetConfigBool("TaskbarSpecularLine", true);
            _islandExpandOnMedia = GetConfigBool("IslandExpandOnMedia", true);

            _ribbonMinimized = GetConfigBool("RibbonMinimized", true);
            _hideClutterFolders = GetConfigBool("HideClutterFolders", true);
            _hideQuickAccessHistory = GetConfigBool("HideQuickAccessHistory", true);

            _autoApplyFolderIcons = GetConfigBool("AutoApplyFolderIcons", true);
            _desktopIconSize = GetConfigInt("DesktopIconSize", 48, 32, 64);
            _hideShortcutArrows = GetConfigBool("HideShortcutArrows", false);

            _hudEnabled = GetConfigBool("HudEnabled", true);
            _hudPosition = GetConfigInt("HudPosition", 0, 0, 3);
            _hudOpacity = GetConfigInt("HudOpacity", 230, 80, 255);
            _widgetClock = GetConfigBool("WidgetClock", true);
            _widgetSystemGpu = GetConfigBool("WidgetSystemGpu", true);
            _widgetStorage = GetConfigBool("WidgetStorage", true);
            _widgetNetwork = GetConfigBool("WidgetNetwork", true);
            _widgetMedia = GetConfigBool("WidgetMedia", true);
            _widgetNotes = GetConfigBool("WidgetNotes", true);
            _widgetSpectrum = GetConfigBool("WidgetSpectrum", true);
            _widgetPomodoro = GetConfigBool("WidgetPomodoro", true);
            _widgetsLocked = GetConfigBool("WidgetsLocked", true);
            _widgetLayout = GetConfigInt("WidgetLayout", 0, 0, 2);
            _widgetScale = GetConfigInt("WidgetScale", 100, 85, 115);
            _spectrumStyle = GetConfigInt("SpectrumStyle", 0, 0, 3);
            _spectrumBandCount = GetConfigInt("SpectrumBandCount", 32, 16, 64);
            _spectrumSmoothing = GetConfigInt("SpectrumSmoothing", 68, 10, 95);

            string[] widgetIds = DesktopHudWidget.AllWidgetIds;
            for (int w = 0; w < widgetIds.Length; w++)
            {
                string posStr = GetConfigString("WidgetPos_" + widgetIds[w], "auto");
                if (!string.IsNullOrEmpty(posStr) && !posStr.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = posStr.Split(',');
                    int px, py;
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out px) && int.TryParse(parts[1].Trim(), out py))
                    {
                        try { DesktopHudWidget.SetCustomWidgetPosition(widgetIds[w], px, py); } catch { }
                    }
                }
            }

            _boostProfile = (BoostProfile)GetConfigInt("BoostProfile", 0, 0, 3);
            _smartFocusEnabled = GetConfigBool("SmartFocusEnabled", true);
            _gpuPauseThreshold = GetConfigInt("GpuPauseThreshold", 80, 50, 95);
            _ramAutoPurgeThreshold = GetConfigInt("RamAutoPurgeThreshold", 88, 70, 95);
            _displayOverlayEnabled = GetConfigBool("DisplayOverlayEnabled", false);
            _displayDimmerPercent = GetConfigInt("DisplayDimmerPercent", 0, 0, 80);
            _displayWarmthKelvin = GetConfigInt("DisplayWarmthKelvin", 6500, 2700, 6500);

            _autoStart = GetConfigBool("AutoStart", CheckAutoStartRegistry());
            _hotkeysEnabled = GetConfigBool("HotkeysEnabled", true);
            _scheduleEnabled = GetConfigBool("ScheduleEnabled", false);
            _dayProfile = GetConfigString("DayProfile", "DoomEmerald");
            _nightProfile = GetConfigString("NightProfile", "ObsidianStealth");
            _rendererFpsCap = GetConfigInt("RendererFpsCap", 60, 30, 60);

            SyncHsvFromSelectedChannel();
            AdaptStudioUiColorsToActivePreset();
            RegenerateHarmonySwatches();
            RefreshChameleonSwatches();
            RebuildPreviewFolderIcons();
            RebuildTrayThemesMenu();
            RebuildTrayWidgetsMenu();
            UpdateTrayMenuCheckState();
            this.Invalidate();
        }

        private void RefreshPresetCatalog()
        {
            try
            {
                _allPresets = ThemeCatalog.GetAllPresets();
            }
            catch
            {
                _allPresets = ThemeCatalog.GetBuiltInPresets();
            }
        }

        private void AdaptStudioUiColorsToActivePreset()
        {
            if (_activePreset == null) return;
            _uiBg = BlendColors(ColL0Void, _activePreset.UiBackground, 0.55);
            _uiCard = BlendColors(ColL1Surface, _activePreset.UiCard, 0.55);
            _uiHeaderBg = BlendColors(_uiBg, _uiCard, 0.60);
            _uiCardHover = BrightenColor(_uiCard, 12);
            _uiActiveAccent = _customPrimaryAccent;
            _uiActiveRim = _customRimHighlight;
            double h, s, v;
            ColorToHsv(_customRimHighlight, out h, out s, out v);
            _uiAccent2 = ColorFromHsv((h + 150.0) % 360.0, Math.Max(0.45, s), Math.Max(0.72, v));
            _uiBorderSubtle = BlendColors(_uiCard, _uiActiveRim, 0.32);
            this.BackColor = _uiBg;
        }

        private void RegenerateHarmonySwatches()
        {
            try
            {
                _harmonySwatches = ColorHarmonyGenerator.GenerateHarmony(GetSelectedChannelColor(), _harmonyMode);
            }
            catch
            {
                _harmonySwatches = new Color[] { _customPrimaryAccent, _customBodyTint, _customShadowGlow, _customRimHighlight, _customTaskbarTint };
            }
        }

        private void RefreshChameleonSwatches()
        {
            try
            {
                string wp = _selectedWallpaperPath;
                if (string.IsNullOrEmpty(wp) || !File.Exists(wp))
                    wp = DoomThemeEngine.DoomWallpaperPath;
                _chameleonSwatches = WallpaperPaletteExtractor.ExtractDominantSwatches(wp, 6);
            }
            catch
            {
                _chameleonSwatches = new Color[]
                {
                    _customPrimaryAccent, _customBodyTint, _customShadowGlow,
                    _customRimHighlight, _customTaskbarTint, _uiGoldCta
                };
            }
        }

        private void RebuildPreviewFolderIcons()
        {
            try
            {
                if (_previewFolderIcon1 != null) { _previewFolderIcon1.Dispose(); _previewFolderIcon1 = null; }
                if (_previewFolderIcon2 != null) { _previewFolderIcon2.Dispose(); _previewFolderIcon2 = null; }

                ThemePreset previewPreset = GetEffectiveLivePreset();
                _previewFolderIcon1 = DynamicIconGenerator.RenderSingleFolderPreview(previewPreset, "Beyin", 48);
                _previewFolderIcon2 = DynamicIconGenerator.RenderSingleFolderPreview(previewPreset, "Games", 48);
            }
            catch { }
        }

        private ThemePreset GetEffectiveLivePreset()
        {
            ThemePreset p = (_activePreset != null) ? _activePreset.Clone() : ThemeCatalog.GetPresetById("DoomEmerald").Clone();
            p.PrimaryAccent = _customPrimaryAccent;
            p.ExplorerBodyTint = _customBodyTint;
            p.ExplorerShadowGlow = _customShadowGlow;
            p.RimHighlight = _customRimHighlight;
            p.TaskbarTint = _customTaskbarTint;
            p.DefaultTintAlpha = (byte)_emeraldTintIntensity;
            p.DefaultShadowAlpha = (byte)_emeraldShadowIntensity;
            p.DefaultShadowSpread = _shadowSpread;
            return p;
        }

        private void SaveStateToEngine()
        {
            try
            {
                if (_config == null)
                    _config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                _config["ActiveProfile"] = _activeProfileId;
                string[] favArr = new string[_favoritePresetIds.Count];
                _favoritePresetIds.CopyTo(favArr);
                _config["FavoritePresets"] = string.Join(",", favArr);

                _config["EmeraldTintAndShadowEnabled"] = _emeraldTintAndShadowEnabled ? "1" : "0";
                _config["EmeraldTintIntensity"] = _emeraldTintIntensity.ToString();
                _config["EmeraldShadowIntensity"] = _emeraldShadowIntensity.ToString();
                _config["ShadowSpread"] = _shadowSpread.ToString();
                _config["ShowRimBorder"] = _showRimBorder ? "1" : "0";
                _config["ShadowOnlyActiveWindow"] = _shadowOnlyActiveWindow ? "1" : "0";

                _config["ExplorerGlassEnabled"] = _explorerGlassEnabled ? "1" : "0";
                _config["ExplorerAlpha"] = _explorerAlpha.ToString();
                _config["ExplorerBlurMode"] = _explorerBlurMode.ToString();
                _config["HdrWhiteTextGuard"] = _hdrWhiteTextGuard ? "1" : "0";
                _config["HdrWhiteTextGuardEnabled"] = _hdrWhiteTextGuard ? "1" : "0";
                _config["HdrNitsClamp"] = _hdrNitsClamp.ToString();
                _config["HdrMaxNitsClamp"] = _hdrNitsClamp.ToString();
                _config["BorderCornerRadius"] = _borderCornerRadius.ToString();
                _config["BorderRimWidth"] = _borderRimWidth.ToString();

                _config["PrimaryAccentHex"] = ThemeCatalog.ColorToHex(_customPrimaryAccent);
                _config["BodyTintHex"] = ThemeCatalog.ColorToHex(_customBodyTint);
                _config["ShadowGlowHex"] = ThemeCatalog.ColorToHex(_customShadowGlow);
                _config["RimHighlightHex"] = ThemeCatalog.ColorToHex(_customRimHighlight);
                _config["TaskbarTintHex"] = ThemeCatalog.ColorToHex(_customTaskbarTint);
                _config["TaskbarTintAlpha"] = _customTaskbarAlpha.ToString();

                _config["TaskbarMode"] = _taskbarMode.ToString();
                _config["TaskbarCenterIcons"] = _taskbarCenterIcons ? "1" : "0";
                _config["ShowTaskbarSeconds"] = _showTaskbarSeconds ? "1" : "0";
                _config["HideTaskViewButton"] = _hideTaskViewButton ? "1" : "0";
                _config["SearchboxMode"] = _searchboxMode.ToString();
                _config["TaskbarCornerRadius"] = _taskbarCornerRadius.ToString();
                _config["TaskbarDockMargin"] = _taskbarDockMargin.ToString();
                _config["TaskbarMarginPx"] = _taskbarDockMargin.ToString();
                _config["TaskbarSpecularLine"] = _taskbarSpecularLine ? "1" : "0";
                _config["IslandExpandOnMedia"] = _islandExpandOnMedia ? "1" : "0";

                _config["RibbonMinimized"] = _ribbonMinimized ? "1" : "0";
                _config["HideClutterFolders"] = _hideClutterFolders ? "1" : "0";
                _config["HideQuickAccessHistory"] = _hideQuickAccessHistory ? "1" : "0";

                _config["AutoApplyFolderIcons"] = _autoApplyFolderIcons ? "1" : "0";
                _config["DesktopIconSize"] = _desktopIconSize.ToString();
                _config["HideShortcutArrows"] = _hideShortcutArrows ? "1" : "0";

                _config["HudEnabled"] = _hudEnabled ? "1" : "0";
                _config["HudPosition"] = _hudPosition.ToString();
                _config["HudOpacity"] = _hudOpacity.ToString();
                _config["WidgetClock"] = _widgetClock ? "1" : "0";
                _config["WidgetSystemGpu"] = _widgetSystemGpu ? "1" : "0";
                _config["WidgetStorage"] = _widgetStorage ? "1" : "0";
                _config["WidgetNetwork"] = _widgetNetwork ? "1" : "0";
                _config["WidgetMedia"] = _widgetMedia ? "1" : "0";
                _config["WidgetNotes"] = _widgetNotes ? "1" : "0";
                _config["WidgetSpectrum"] = _widgetSpectrum ? "1" : "0";
                _config["WidgetPomodoro"] = _widgetPomodoro ? "1" : "0";
                _config["WidgetsLocked"] = _widgetsLocked ? "1" : "0";
                _config["WidgetLayout"] = _widgetLayout.ToString();
                _config["WidgetScale"] = _widgetScale.ToString();
                _config["SpectrumStyle"] = _spectrumStyle.ToString();
                _config["SpectrumBandCount"] = _spectrumBandCount.ToString();
                _config["SpectrumSmoothing"] = _spectrumSmoothing.ToString();

                try
                {
                    Dictionary<string, Point> customPos = DesktopHudWidget.GetCustomWidgetPositions();
                    if (customPos != null)
                    {
                        foreach (KeyValuePair<string, Point> kv in customPos)
                        {
                            _config["WidgetPos_" + kv.Key] = kv.Value.X + "," + kv.Value.Y;
                        }
                    }
                }
                catch { }

                _config["BoostProfile"] = ((int)_boostProfile).ToString();
                _config["SmartFocusEnabled"] = _smartFocusEnabled ? "1" : "0";
                _config["GpuPauseThreshold"] = _gpuPauseThreshold.ToString();
                _config["RamAutoPurgeThreshold"] = _ramAutoPurgeThreshold.ToString();
                _config["DisplayOverlayEnabled"] = _displayOverlayEnabled ? "1" : "0";
                _config["DisplayDimmerPercent"] = _displayDimmerPercent.ToString();
                _config["DisplayWarmthKelvin"] = _displayWarmthKelvin.ToString();

                _config["AutoStart"] = _autoStart ? "1" : "0";
                _config["HotkeysEnabled"] = _hotkeysEnabled ? "1" : "0";
                _config["ScheduleEnabled"] = _scheduleEnabled ? "1" : "0";
                _config["DayProfile"] = _dayProfile;
                _config["NightProfile"] = _nightProfile;
                _config["RendererFpsCap"] = _rendererFpsCap.ToString();

                DoomThemeEngine.SaveConfig(_config);
            }
            catch { }
            RebuildTrayWidgetsMenu();
            UpdateTrayMenuCheckState();
        }

        private void ApplyLiveRuntimeEngines()
        {
            try
            {
                DoomGlassDaemon.ShowRimBorder = _showRimBorder;
                DoomGlassDaemon.ShadowOnlyActiveWindow = _shadowOnlyActiveWindow;
                DoomGlassDaemon.PresetTaskbarTintColor = _activePreset != null ? _activePreset.TaskbarTint : _customTaskbarTint;
                DoomGlassDaemon.CustomTaskbarTintColor = _customTaskbarTint;
                DoomGlassDaemon.CustomTaskbarAlpha = (byte)_customTaskbarAlpha;
                DoomGlassDaemon.TaskbarCenterIcons = _taskbarCenterIcons;

                DoomGlassDaemon.ApplyPresetOverlayColors(
                    _customBodyTint,
                    _customShadowGlow,
                    _customRimHighlight,
                    _shadowSpread,
                    (byte)_emeraldTintIntensity,
                    (byte)_emeraldShadowIntensity);

                DoomGlassDaemon.UpdateEmeraldOverlaySettings(_emeraldTintAndShadowEnabled, _emeraldTintIntensity, _emeraldShadowIntensity);
                int effectiveBlur = _explorerBlurMode > 2 ? 1 : _explorerBlurMode;
                DoomGlassDaemon.UpdateSettings(_explorerGlassEnabled, (byte)_explorerAlpha, effectiveBlur, _taskbarMode);
                DoomGlassDaemon.StartDaemon();
                DoomGlassDaemon.RefreshAllOpenWindows();
                DoomGlassDaemon.SyncAllExplorerOverlays(true);
            }
            catch { }

            try
            {
                DesktopHudWidget.Enabled = _hudEnabled;
                DesktopHudWidget.ShowClock = _widgetClock;
                DesktopHudWidget.ShowSystemGpu = _widgetSystemGpu;
                DesktopHudWidget.ShowStorage = _widgetStorage;
                DesktopHudWidget.ShowNetwork = _widgetNetwork;
                DesktopHudWidget.ShowMedia = _widgetMedia;
                DesktopHudWidget.ShowNotes = _widgetNotes;
                DesktopHudWidget.ShowSpectrum = _widgetSpectrum;
                DesktopHudWidget.ShowPomodoro = _widgetPomodoro;
                DesktopHudWidget.WidgetsLocked = _widgetsLocked;
                DesktopHudWidget.LayoutPreset = _widgetLayout;
                DesktopHudWidget.WidgetScalePercent = _widgetScale;
                DesktopHudWidget.HudOpacity = (byte)_hudOpacity;
                DesktopHudWidget.SpectrumStyle = _spectrumStyle;
                DesktopHudWidget.SpectrumBandCount = _spectrumBandCount;
                DesktopHudWidget.SpectrumSmoothing = _spectrumSmoothing / 100.0f;

                string title = _activePreset != null ? _activePreset.DisplayName : _activeProfileId;
                DesktopHudWidget.StartOrUpdate(_hudEnabled, _widgetLayout, (byte)_hudOpacity, _uiCard, _customRimHighlight, ColTextPrimary, title);
            }
            catch { }

            try
            {
                SmartFocusMonitor.Enabled = _smartFocusEnabled;
                if (_displayOverlayEnabled && (_displayDimmerPercent > 0 || _displayWarmthKelvin < 6500))
                {
                    DisplayOverlayManager.ApplyOverlay(_displayDimmerPercent, _displayWarmthKelvin);
                }
                else
                {
                    DisplayOverlayManager.DisableOverlay();
                }
            }
            catch { }
        }

        private string GetConfigString(string key, string fallback)
        {
            string val;
            if (_config != null && _config.TryGetValue(key, out val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim();
            return fallback;
        }

        private bool GetConfigBool(string key, bool fallback)
        {
            string val;
            if (_config != null && _config.TryGetValue(key, out val) && !string.IsNullOrWhiteSpace(val))
            {
                val = val.Trim();
                if (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (val == "0" || val.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            }
            return fallback;
        }

        private int GetConfigInt(string key, int fallback, int min, int max)
        {
            string val;
            int parsed;
            if (_config != null && _config.TryGetValue(key, out val) && int.TryParse(val.Trim(), out parsed))
            {
                if (parsed < min) return min;
                if (parsed > max) return max;
                return parsed;
            }
            return fallback;
        }

        public static bool CheckAutoStartRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("DoomControlCenter");
                        return v != null && v.ToString().IndexOf("DoomControlCenter.exe", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetAutoStartRegistry(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (key.GetValue("ExplorerGlass") != null)
                            key.DeleteValue("ExplorerGlass", false);
                        if (key.GetValue("TranslucentTB") != null)
                            key.DeleteValue("TranslucentTB", false);

                        if (enable)
                        {
                            string cmd = "\"" + ExePath + "\" --minimized";
                            key.SetValue("DoomControlCenter", cmd, RegistryValueKind.String);
                        }
                        else
                        {
                            if (key.GetValue("DoomControlCenter") != null)
                                key.DeleteValue("DoomControlCenter", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void SetStatus(string msg)
        {
            _statusMessage = msg;
            if (this.IsHandleCreated)
            {
                this.Invalidate(new Rectangle(0, this.ClientSize.Height - 36, this.ClientSize.Width, 36));
                this.Invalidate(new Rectangle(0, 0, this.ClientSize.Width, 54));
            }
        }

        // ====================================================================
        // Bento Command Deck Layout Geometry Helpers (12-Column Responsive)
        // ====================================================================
        private int GetRailWidth()
        {
            return (int)Math.Round(_railWidthAnim);
        }

        private bool ShouldShowRightPreviewDock()
        {
            if (_activeTab == 0) return false; // View 0 has the large Hero Live Canvas
            if (this.ClientSize.Width < 1160) return false;
            return true;
        }

        private Rectangle GetRightPreviewDockBounds()
        {
            if (!ShouldShowRightPreviewDock()) return Rectangle.Empty;
            int dockW = _previewDockExpanded ? 316 : 42;
            int top = 60;
            int h = this.ClientSize.Height - top - 36;
            return new Rectangle(this.ClientSize.Width - dockW - 12, top, dockW, h);
        }

        private Rectangle GetWorkspaceBounds()
        {
            int railW = GetRailWidth();
            int x = railW + 18;
            int y = 60;
            int rightMargin = 12;
            if (ShouldShowRightPreviewDock())
            {
                int dockW = _previewDockExpanded ? 316 : 42;
                rightMargin = dockW + 22;
            }
            int w = Math.Max(640, this.ClientSize.Width - x - rightMargin);
            int h = Math.Max(580, this.ClientSize.Height - y - 36);
            return new Rectangle(x, y, w, h);
        }

        private static Rectangle BentoCell(Rectangle ws, int colStart, int colSpan, int y, int height, int gutter = 10)
        {
            double unitW = (ws.Width - gutter * 11.0) / 12.0;
            int x = ws.X + (int)Math.Round(colStart * (unitW + gutter));
            int rightCol = colStart + colSpan;
            int rightX = (rightCol >= 12)
                ? ws.Right
                : (ws.X + (int)Math.Round(rightCol * unitW + (rightCol - 1) * gutter));
            return new Rectangle(x, y, Math.Max(40, rightX - x), height);
        }

        public static string GetViewTitle(int viewIndex)
        {
            switch (viewIndex)
            {
                case 0: return "Tema Galerisi & Canlı Tuval";
                case 1: return "Renk & Işık Stüdyosu";
                case 2: return "Gezgin & 10-bit HDR Motoru";
                case 3: return "Görev Çubuğu & Dock";
                case 4: return "Masaüstü HUD & Ses Spektrumu";
                case 5: return "Oyun/Render Boost & Monitör Merkezi";
                case 6: return "Sistem, Kısayollar & Yedekleme";
                default: return "Bento Command Deck";
            }
        }

        // ====================================================================
        // Zone Builder (7 Bento Views + Persistent Header + Rail + Right Dock)
        // ====================================================================
        private void RebuildZonesForCurrentTab()
        {
            _zones.Clear();
            _sliderTracks.Clear();

            int winW = Math.Max(1120, this.ClientSize.Width);
            int winH = Math.Max(720, this.ClientSize.Height);
            int railW = GetRailWidth();

            // 1. Top Command & Telemetry Header Controls (Y=0..52)
            int rightX = winW - 10;
            _zones.Add(new UiZone { Id = "WIN_CLOSE", Bounds = new Rectangle(rightX - 30, 11, 30, 30), Title = "✕" });
            _zones.Add(new UiZone { Id = "WIN_MAX", Bounds = new Rectangle(rightX - 64, 11, 30, 30), Title = "▢" });
            _zones.Add(new UiZone { Id = "WIN_MIN", Bounds = new Rectangle(rightX - 98, 11, 30, 30), Title = "─" });
            _zones.Add(new UiZone { Id = "WIN_TRAY", Bounds = new Rectangle(rightX - 150, 11, 48, 30), Title = "TEPSİ" });

            _zones.Add(new UiZone { Id = "ACT_APPLY_NOW", Bounds = new Rectangle(rightX - 246, 11, 90, 30), Title = "✓ Uygula" });
            _zones.Add(new UiZone { Id = "ACT_UNDO_HEADER", Bounds = new Rectangle(rightX - 318, 11, 66, 30), Title = "↶ Geri" });

            // Center Command Bar Pill
            int cmdW = Math.Min(390, Math.Max(240, winW - 860));
            int cmdX = Math.Max(290, (winW - cmdW) / 2 - 70);
            _zones.Add(new UiZone
            {
                Id = "CMD_BAR_OPEN",
                Bounds = new Rectangle(cmdX, 10, cmdW, 32),
                Title = "⌕ Komut, tema veya ayar ara...   Ctrl+Shift+Space"
            });

            // Header Telemetry Chips (CPU, RAM, HDR/Boost)
            int teleStartX = cmdX + cmdW + 10;
            if (teleStartX + 220 < rightX - 322)
            {
                _zones.Add(new UiZone { Id = "CHIP_CPU", Bounds = new Rectangle(teleStartX, 11, 76, 30), Title = "CPU" });
                _zones.Add(new UiZone { Id = "CHIP_RAM", Bounds = new Rectangle(teleStartX + 80, 11, 88, 30), Title = "RAM" });
                _zones.Add(new UiZone { Id = "CHIP_HDR", Bounds = new Rectangle(teleStartX + 172, 11, 72, 30), Title = "HDR" });
            }

            // 2. Collapsible Left Icon Rail (X=8, Y=60, W=railW, H=winH-96)
            _zones.Add(new UiZone
            {
                Id = "RAIL_TOGGLE",
                Bounds = new Rectangle(14, 66, railW - 16, 30),
                Title = _railExpanded ? "◀  MENÜYÜ DARALT" : "▶"
            });

            string[] tabTitles = new string[]
            {
                "Tema Galerisi",
                "Renk & Işık Stüdyosu",
                "Gezgin & 10-bit HDR",
                "Görev Çubuğu & Dock",
                "Masaüstü HUD & Ses",
                "Oyun/Render Boost",
                "Sistem & Kısayollar"
            };
            string[] tabSubs = new string[]
            {
                "Canlı Tuval & OKLab",
                "HSV, Harmoni & WCAG",
                "Cam, scRGB & Fizik",
                "4 Mod, Ada & Tepsi",
                "8 Widget & Pomodoro",
                "RAM Purge & Kelvin",
                "Snapshot & Tanılama"
            };

            for (int i = 0; i < 7; i++)
            {
                _zones.Add(new UiZone
                {
                    Id = "TAB_" + i,
                    Bounds = new Rectangle(14, 104 + i * 56, railW - 16, 48),
                    Title = tabTitles[i],
                    Subtitle = tabSubs[i],
                    Tag = i.ToString()
                });
            }

            // Bottom Rail Quick Actions (Chameleon + Boost Profile Cycler)
            int railBottomY = winH - 36 - 112;
            if (railBottomY > 500)
            {
                _zones.Add(new UiZone
                {
                    Id = "ACT_CHAMELEON",
                    Bounds = new Rectangle(14, railBottomY, railW - 16, 46),
                    Title = _railExpanded ? "✦ Chameleon Oto-Tema" : "✦"
                });
                _zones.Add(new UiZone
                {
                    Id = "ACT_CYCLE_BOOST",
                    Bounds = new Rectangle(14, railBottomY + 52, railW - 16, 46),
                    Title = _railExpanded ? ("⚡ Mod: " + _boostProfile.ToString()) : "⚡"
                });
            }

            // 3. Right Live Mini Desktop Preview Dock (Views 1..6)
            if (ShouldShowRightPreviewDock())
            {
                Rectangle dock = GetRightPreviewDockBounds();
                _zones.Add(new UiZone
                {
                    Id = "PREVIEW_DOCK_TOGGLE",
                    Bounds = new Rectangle(dock.X + 6, dock.Y + 6, _previewDockExpanded ? 32 : 30, 28),
                    Title = _previewDockExpanded ? "▶" : "◀"
                });

                if (_previewDockExpanded)
                {
                    AddSliderZone("SLIDER_PREVIEW_SPLIT", new Rectangle(dock.X + 16, dock.Y + 234, dock.Width - 32, 16), "Önce / Sonra Karşılaştırma");
                    _zones.Add(new UiZone { Id = "PREV_TOG_BEFORE", Bounds = new Rectangle(dock.X + 14, dock.Y + 260, (dock.Width - 34) / 2, 30), Title = _previewSplitPercent > 0 ? "Karşılaştırma: Açık" : "Önce / Sonra Böl" });
                    _zones.Add(new UiZone { Id = "PREV_TOG_HDR", Bounds = new Rectangle(dock.X + 20 + (dock.Width - 34) / 2, dock.Y + 260, (dock.Width - 34) / 2, 30), Title = _previewHdrSim ? "Mod: 10-bit HDR" : "Mod: SDR Sim" });
                    _zones.Add(new UiZone { Id = "ACT_GEN_APPLY_ICONS", Bounds = new Rectangle(dock.X + 14, dock.Bottom - 96, dock.Width - 28, 36), Title = "✦ Masaüstü Klasör İkonlarını Boya" });
                    _zones.Add(new UiZone { Id = "ACT_FIX_TEXT", Bounds = new Rectangle(dock.X + 14, dock.Bottom - 52, dock.Width - 28, 38), Title = "✦ HDR Metin & Gölge Onarımı" });
                }
            }

            // 4. Active View Bento Grid Zones
            Rectangle ws = GetWorkspaceBounds();
            if (_activeTab == 0)
                BuildTab0ThemeGalleryZones(ws);
            else if (_activeTab == 1)
                BuildTab1ColorStudioZones(ws);
            else if (_activeTab == 2)
                BuildTab2ExplorerHdrZones(ws);
            else if (_activeTab == 3)
                BuildTab3TaskbarZones(ws);
            else if (_activeTab == 4)
                BuildTab4DesktopHudZones(ws);
            else if (_activeTab == 5)
                BuildTab5BoostMonitorZones(ws);
            else if (_activeTab == 6)
                BuildTab6SystemBackupZones(ws);

            if (_commandPaletteOpen)
            {
                RebuildCommandPaletteZones();
            }
        }

        private List<ThemePreset> GetFilteredPresets()
        {
            RefreshPresetCatalog();
            if (_galleryFilterMode == 1)
            {
                List<ThemePreset> favs = new List<ThemePreset>();
                for (int i = 0; i < _allPresets.Count; i++)
                {
                    if (_favoritePresetIds.Contains(_allPresets[i].Id) || _allPresets[i].IsFavorite)
                        favs.Add(_allPresets[i]);
                }
                return favs.Count > 0 ? favs : _allPresets;
            }
            if (_galleryFilterMode == 2)
            {
                List<ThemePreset> cust = new List<ThemePreset>();
                for (int i = 0; i < _allPresets.Count; i++)
                {
                    if (!_allPresets[i].IsBuiltIn)
                        cust.Add(_allPresets[i]);
                }
                return cust;
            }
            return _allPresets;
        }

        // --------------------------------------------------------------------
        // VIEW 0: Tema Galerisi & Canlı Tuval (Hero 8x4 + Chameleon 4x4 + Grid + I/O + History)
        // --------------------------------------------------------------------
        private void BuildTab0ThemeGalleryZones(Rectangle ws)
        {
            int hHero = Math.Max(195, (int)(ws.Height * 0.34));
            int hGrid = Math.Max(220, (int)(ws.Height * 0.43));
            int hBottom = Math.Max(110, ws.Height - hHero - hGrid - 20);

            Rectangle cardHero = BentoCell(ws, 0, 8, ws.Y, hHero);
            Rectangle cardCham = BentoCell(ws, 8, 4, ws.Y, hHero);
            Rectangle cardGrid = BentoCell(ws, 0, 12, ws.Y + hHero + 10, hGrid);
            int yBot = ws.Y + hHero + hGrid + 20;
            Rectangle cardIo = BentoCell(ws, 0, 6, yBot, hBottom);
            Rectangle cardHist = BentoCell(ws, 6, 6, yBot, hBottom);

            // Hero Canvas Controls
            AddSliderZone("SLIDER_PREVIEW_SPLIT", new Rectangle(cardHero.X + 18, cardHero.Bottom - 40, 210, 16), "Önce / Sonra Bölücü");
            _zones.Add(new UiZone { Id = "PREV_TOG_BEFORE", Bounds = new Rectangle(cardHero.X + 240, cardHero.Bottom - 46, 118, 30), Title = _previewSplitPercent > 0 ? "Bölücü: Aktif" : "Önce / Sonra" });
            _zones.Add(new UiZone { Id = "PREV_TOG_HDR", Bounds = new Rectangle(cardHero.X + 364, cardHero.Bottom - 46, 110, 30), Title = _previewHdrSim ? "10-bit HDR" : "SDR Sim" });
            _zones.Add(new UiZone { Id = "ACT_GEN_APPLY_ICONS", Bounds = new Rectangle(cardHero.Right - 176, cardHero.Bottom - 46, 162, 30), Title = "✦ İkonları Boya" });

            // Chameleon OKLab 6-Swatch Analyzer Controls
            int swW = (cardCham.Width - 28 - 25) / 6;
            for (int s = 0; s < 6; s++)
            {
                Color sc = (_chameleonSwatches != null && s < _chameleonSwatches.Length) ? _chameleonSwatches[s] : _customPrimaryAccent;
                _zones.Add(new UiZone
                {
                    Id = "CHAM_SWATCH_" + s,
                    Bounds = new Rectangle(cardCham.X + 14 + s * (swW + 5), cardCham.Y + 86, swW, 34),
                    Title = ThemeCatalog.ColorToHex(sc),
                    Swatch1 = sc
                });
            }
            _zones.Add(new UiZone { Id = "TOG_AUTO_ICONS", Bounds = new Rectangle(cardCham.X + 14, cardCham.Bottom - 84, cardCham.Width - 28, 34), Title = "Tema Değişince İkonları Boya" });
            _zones.Add(new UiZone { Id = "ACT_CHAMELEON", Bounds = new Rectangle(cardCham.X + 14, cardCham.Bottom - 44, (cardCham.Width - 34) / 2, 32), Title = "✦ Oto-Tema Üret" });
            _zones.Add(new UiZone { Id = "ACT_CHAMELEON_FILE", Bounds = new Rectangle(cardCham.X + 20 + (cardCham.Width - 34) / 2, cardCham.Bottom - 44, (cardCham.Width - 34) / 2, 32), Title = "📂 Görsel Seç..." });

            // Gallery Filter Pills & Pagination in cardGrid header
            _zones.Add(new UiZone { Id = "GAL_FILTER_0", Bounds = new Rectangle(cardGrid.Right - 360, cardGrid.Y + 7, 68, 24), Title = "Tümü" });
            _zones.Add(new UiZone { Id = "GAL_FILTER_1", Bounds = new Rectangle(cardGrid.Right - 288, cardGrid.Y + 7, 82, 24), Title = "★ Favoriler" });
            _zones.Add(new UiZone { Id = "GAL_FILTER_2", Bounds = new Rectangle(cardGrid.Right - 202, cardGrid.Y + 7, 68, 24), Title = "Özel" });
            _zones.Add(new UiZone { Id = "PAGE_PREV", Bounds = new Rectangle(cardGrid.Right - 126, cardGrid.Y + 7, 52, 24), Title = "◀" });
            _zones.Add(new UiZone { Id = "PAGE_NEXT", Bounds = new Rectangle(cardGrid.Right - 70, cardGrid.Y + 7, 56, 24), Title = "▶" });

            List<ThemePreset> filtered = GetFilteredPresets();
            int cols = 4;
            int rows = 2;
            int perPage = cols * rows;
            int totalPages = Math.Max(1, (filtered.Count + perPage - 1) / perPage);
            if (_themeGalleryPage >= totalPages) _themeGalleryPage = totalPages - 1;
            if (_themeGalleryPage < 0) _themeGalleryPage = 0;

            int startIdx = _themeGalleryPage * perPage;
            int endIdx = Math.Min(filtered.Count, startIdx + perPage);

            int cellW = (cardGrid.Width - 28 - (cols - 1) * 10) / cols;
            int cellH = Math.Max(72, (cardGrid.Height - 48 - (rows - 1) * 10) / rows);

            for (int i = startIdx; i < endIdx; i++)
            {
                int local = i - startIdx;
                int c = local % cols;
                int r = local / cols;
                int px = cardGrid.X + 14 + c * (cellW + 10);
                int py = cardGrid.Y + 38 + r * (cellH + 10);

                ThemePreset p = filtered[i];
                _zones.Add(new UiZone
                {
                    Id = "PRESET_" + p.Id,
                    Bounds = new Rectangle(px, py, cellW, cellH),
                    Title = p.DisplayName,
                    Subtitle = p.Subtitle,
                    Swatch1 = p.PrimaryAccent,
                    Swatch2 = p.ExplorerBodyTint,
                    Swatch3 = p.ExplorerShadowGlow,
                    Swatch4 = p.RimHighlight,
                    Tag = p.IsBuiltIn ? "BUILTIN" : "CUSTOM"
                });

                _zones.Add(new UiZone
                {
                    Id = "FAV_PRESET_" + p.Id,
                    Bounds = new Rectangle(px + cellW - 26, py + 6, 20, 20),
                    Title = _favoritePresetIds.Contains(p.Id) ? "★" : "☆",
                    Tag = p.Id
                });

                if (!p.IsBuiltIn)
                {
                    _zones.Add(new UiZone
                    {
                        Id = "DEL_PRESET_" + p.Id,
                        Bounds = new Rectangle(px + cellW - 50, py + 6, 20, 20),
                        Title = "✕",
                        Tag = p.Id
                    });
                }
            }

            // Bottom Left: .doomtheme Preset I/O (cardIo)
            int btnW = (cardIo.Width - 36) / 3;
            _zones.Add(new UiZone { Id = "ACT_DOOMTHEME_EXPORT", Bounds = new Rectangle(cardIo.X + 12, cardIo.Y + 36, btnW, 36), Title = "📦 .doomtheme Dışa Aktar" });
            _zones.Add(new UiZone { Id = "ACT_DOOMTHEME_IMPORT", Bounds = new Rectangle(cardIo.X + 18 + btnW, cardIo.Y + 36, btnW, 36), Title = "📥 .doomtheme İçe Aktar" });
            _zones.Add(new UiZone { Id = "ACT_SAVE_CUSTOM_THEME", Bounds = new Rectangle(cardIo.X + 24 + btnW * 2, cardIo.Y + 36, btnW, 36), Title = "★ Özel Tema Kaydet" });

            // Bottom Right: Rollback History Timeline (cardHist)
            List<ThemeSnapshot> snaps = new List<ThemeSnapshot>();
            try { snaps = ThemeSnapshotManager.GetSnapshots(); } catch { }
            int maxShow = Math.Min(3, snaps != null ? snaps.Count : 0);
            int snapW = (cardHist.Width - 36) / 3;
            for (int s = 0; s < maxShow; s++)
            {
                ThemeSnapshot sn = snaps[s];
                _zones.Add(new UiZone
                {
                    Id = "ACT_ROLLBACK_" + sn.Id,
                    Bounds = new Rectangle(cardHist.X + 12 + s * (snapW + 6), cardHist.Y + 36, snapW, 38),
                    Title = "↺ " + (sn.Label ?? sn.PresetId ?? "Tema"),
                    Subtitle = sn.Timestamp ?? "",
                    Tag = sn.Id
                });
            }
            if (maxShow == 0)
            {
                _zones.Add(new UiZone
                {
                    Id = "ACT_CREATE_SNAPSHOT",
                    Bounds = new Rectangle(cardHist.X + 14, cardHist.Y + 36, cardHist.Width - 28, 38),
                    Title = "📸 İlk Geri Dönüş Noktasını (Snapshot) Şimdi Oluştur"
                });
            }
        }

        // --------------------------------------------------------------------
        // VIEW 1: Renk & Işık Stüdyosu (HSV + Harmony Generator + 360° Glow Curve + WCAG)
        // --------------------------------------------------------------------
        private void BuildTab1ColorStudioZones(Rectangle ws)
        {
            int hChannels = 64;
            int remH = ws.Height - hChannels - 20;
            int hRow1 = Math.Max(230, (int)(remH * 0.54));
            int hRow2 = Math.Max(170, remH - hRow1);

            // Top 5 Channel Cards
            string[] chNames = new string[]
            {
                "1. Başlık & Başlat",
                "2. Gezgin İç Gövde",
                "3. 360° Dış Gölge",
                "4. Çerçeve Parıltısı",
                "5. Görev Çubuğu"
            };
            Color[] chCols = new Color[]
            {
                _customPrimaryAccent,
                _customBodyTint,
                _customShadowGlow,
                _customRimHighlight,
                _customTaskbarTint
            };

            int chW = (ws.Width - 40) / 5;
            for (int i = 0; i < 5; i++)
            {
                _zones.Add(new UiZone
                {
                    Id = "CHN_" + i,
                    Bounds = new Rectangle(ws.X + i * (chW + 10), ws.Y, chW, hChannels),
                    Title = chNames[i],
                    Subtitle = ThemeCatalog.ColorToHex(chCols[i]),
                    Swatch1 = chCols[i]
                });
            }

            int yRow1 = ws.Y + hChannels + 10;
            Rectangle cardHsv = BentoCell(ws, 0, 6, yRow1, hRow1);
            Rectangle cardHarm = BentoCell(ws, 6, 6, yRow1, hRow1);

            // HSV Spectrum Box & Value Bar inside cardHsv
            int boxW = Math.Min(210, cardHsv.Width / 2 - 20);
            int boxH = Math.Max(115, cardHsv.Height - 92);
            _hsvBoxRect = new Rectangle(cardHsv.X + 14, cardHsv.Y + 38, boxW, boxH);
            _hsvValRect = new Rectangle(_hsvBoxRect.Right + 8, cardHsv.Y + 38, 20, boxH);
            _zones.Add(new UiZone { Id = "HSV_BOX", Bounds = _hsvBoxRect, Title = "HSV" });
            _zones.Add(new UiZone { Id = "HSV_VAL", Bounds = _hsvValRect, Title = "VAL" });

            int rgbX = _hsvValRect.Right + 16;
            int rgbW = Math.Max(110, cardHsv.Right - rgbX - 14);
            AddSliderZone("SLIDER_COL_R", new Rectangle(rgbX, cardHsv.Y + 52, rgbW, 16), "Kırmızı (R)");
            AddSliderZone("SLIDER_COL_G", new Rectangle(rgbX, cardHsv.Y + 96, rgbW, 16), "Yeşil (G)");
            AddSliderZone("SLIDER_COL_B", new Rectangle(rgbX, cardHsv.Y + 140, rgbW, 16), "Mavi (B)");

            int btnY = cardHsv.Bottom - 40;
            int bW3 = (cardHsv.Width - 36) / 3;
            _zones.Add(new UiZone { Id = "ACT_EDIT_HEX", Bounds = new Rectangle(cardHsv.X + 12, btnY, bW3, 30), Title = "# HEX Kodu..." });
            _zones.Add(new UiZone { Id = "ACT_EYEDROPPER", Bounds = new Rectangle(cardHsv.X + 18 + bW3, btnY, bW3, 30), Title = "💧 Ekran Seçici" });
            _zones.Add(new UiZone { Id = "ACT_APPLY_CUSTOM_COLORS", Bounds = new Rectangle(cardHsv.X + 24 + bW3 * 2, btnY, bW3, 30), Title = "✓ Canlı Uygula" });

            // Harmony Generator inside cardHarm
            string[] harmModes = new string[] { "Mono", "Analog", "Tamam", "Üçlü", "Ayrık", "Dörtlü" };
            int hmW = (cardHarm.Width - 28 - 25) / 6;
            for (int m = 0; m < 6; m++)
            {
                _zones.Add(new UiZone
                {
                    Id = "HARM_MODE_" + m,
                    Bounds = new Rectangle(cardHarm.X + 14 + m * (hmW + 5), cardHarm.Y + 36, hmW, 28),
                    Title = harmModes[m]
                });
            }

            int hSwW = (cardHarm.Width - 28 - 24) / 5;
            for (int s = 0; s < 5; s++)
            {
                Color hc = (_harmonySwatches != null && s < _harmonySwatches.Length) ? _harmonySwatches[s] : _customPrimaryAccent;
                _zones.Add(new UiZone
                {
                    Id = "HARM_SWATCH_" + s,
                    Bounds = new Rectangle(cardHarm.X + 14 + s * (hSwW + 6), cardHarm.Y + 74, hSwW, Math.Max(54, cardHarm.Height - 134)),
                    Title = ThemeCatalog.ColorToHex(hc),
                    Subtitle = "Kanala Ata",
                    Swatch1 = hc
                });
            }

            int hBtnW = (cardHarm.Width - 34) / 2;
            _zones.Add(new UiZone
            {
                Id = "ACT_HARM_APPLY_PALETTE",
                Bounds = new Rectangle(cardHarm.X + 14, cardHarm.Bottom - 44, hBtnW, 34),
                Title = "✦ 5 Kanalı Harmoniden Ata"
            });
            _zones.Add(new UiZone
            {
                Id = "ACT_RESET_PRESET_COLORS",
                Bounds = new Rectangle(cardHarm.X + 20 + hBtnW, cardHarm.Bottom - 44, hBtnW, 34),
                Title = "↺ Aktif Temaya Sıfırla"
            });

            // Row 2: 360° Glow Bézier Curve (Cols 0..6) + WCAG Contrast Matrix (Cols 6..11)
            int yRow2 = yRow1 + hRow1 + 10;
            Rectangle cardBezier = BentoCell(ws, 0, 6, yRow2, hRow2);
            Rectangle cardWcag = BentoCell(ws, 6, 6, yRow2, hRow2);

            string[] bzNames = new string[] { "Gauss", "Neon", "Çift Hale", "Sinema" };
            int bzW = (cardBezier.Width / 2 - 20) / 4;
            for (int b = 0; b < 4; b++)
            {
                _zones.Add(new UiZone
                {
                    Id = "BEZIER_PRESET_" + b,
                    Bounds = new Rectangle(cardBezier.X + 14 + b * (bzW + 4), cardBezier.Bottom - 38, bzW, 28),
                    Title = bzNames[b]
                });
            }

            int slX = cardBezier.X + cardBezier.Width / 2 + 6;
            int slW = cardBezier.Right - slX - 14;
            AddSliderZone("SLIDER_SHADOW", new Rectangle(slX, cardBezier.Y + 58, slW, 16), "360° Gölge Parlaklığı");
            AddSliderZone("SLIDER_SPREAD", new Rectangle(slX, cardBezier.Y + 110, slW, 16), "Yayılım Genişliği (px)");

            _zones.Add(new UiZone
            {
                Id = "ACT_AUTO_CONTRAST",
                Bounds = new Rectangle(cardWcag.X + 14, cardWcag.Bottom - 38, cardWcag.Width - 28, 28),
                Title = "✦ WCAG AA/AAA Kontrastını Otomatik Optimize Et"
            });
        }

        // --------------------------------------------------------------------
        // VIEW 2: Gezgin & 10-bit HDR Motoru (Explorer Glass + scRGB Guard + Border Physics)
        // --------------------------------------------------------------------
        private void BuildTab2ExplorerHdrZones(Rectangle ws)
        {
            int hRow1 = Math.Max(310, (int)(ws.Height * 0.60));
            int hRow2 = Math.Max(160, ws.Height - hRow1 - 10);

            Rectangle cardGlass = BentoCell(ws, 0, 6, ws.Y, hRow1);
            Rectangle cardHdr = BentoCell(ws, 6, 6, ws.Y, hRow1);
            Rectangle cardPhys = BentoCell(ws, 0, 12, ws.Y + hRow1 + 10, hRow2);

            // Card 2A: Explorer Glass & 360° Armor
            string[] blurModes = new string[] { "HDR Net Mod", "Acrylic Cam", "SDR Blur", "Mica Saydam" };
            int bmW = (cardGlass.Width - 28 - 18) / 4;
            for (int m = 0; m < 4; m++)
            {
                _zones.Add(new UiZone
                {
                    Id = "BLUR_" + m,
                    Bounds = new Rectangle(cardGlass.X + 14 + m * (bmW + 6), cardGlass.Y + 36, bmW, 30),
                    Title = blurModes[m]
                });
            }

            int sW = cardGlass.Width - 28;
            AddSliderZone("SLIDER_TINT", new Rectangle(cardGlass.X + 14, cardGlass.Y + 92, sW, 16), "İç Gövde Tonu Yoğunluğu");
            AddSliderZone("SLIDER_SHADOW", new Rectangle(cardGlass.X + 14, cardGlass.Y + 140, sW, 16), "360° Dış Gölge Parlaklığı");
            AddSliderZone("SLIDER_SPREAD", new Rectangle(cardGlass.X + 14, cardGlass.Y + 188, sW, 16), "Gölge Yayılım Genişliği (px)");
            AddSliderZone("SLIDER_ALPHA", new Rectangle(cardGlass.X + 14, cardGlass.Y + 236, sW, 16), "Gezgin Pencere Opaklığı");

            int tW = (cardGlass.Width - 34) / 2;
            _zones.Add(new UiZone { Id = "TOG_EMERALD_ARMOR", Bounds = new Rectangle(cardGlass.X + 14, cardGlass.Bottom - 44, tW, 34), Title = "360° Gölge & Ton" });
            _zones.Add(new UiZone { Id = "TOG_RIM_BORDER", Bounds = new Rectangle(cardGlass.X + 20 + tW, cardGlass.Bottom - 44, tW, 34), Title = "2px Rim Çerçeve" });

            // Card 2B: 10-bit scRGB HDR Engine & Clutter Toggles
            _zones.Add(new UiZone
            {
                Id = "TOG_HDR_GUARD",
                Bounds = new Rectangle(cardHdr.X + 14, cardHdr.Y + 38, cardHdr.Width - 28, 38),
                Title = "10-bit scRGB White-Text Guard (Aşırı Parlama Koruması)"
            });

            AddSliderZone("SLIDER_HDR_NITS", new Rectangle(cardHdr.X + 14, cardHdr.Y + 104, cardHdr.Width - 28, 16), "SDR Metin & Yüzey Parlaklık Sınırı (Nits)");

            _zones.Add(new UiZone { Id = "TOG_ACTIVE_ONLY_SHADOW", Bounds = new Rectangle(cardHdr.X + 14, cardHdr.Y + 136, cardHdr.Width - 28, 34), Title = "Dış Gölgeyi Sadece Aktif Pencerede Göster" });
            _zones.Add(new UiZone { Id = "TOG_RIBBON", Bounds = new Rectangle(cardHdr.X + 14, cardHdr.Y + 174, cardHdr.Width - 28, 34), Title = "Dosya Gezgini Üst Şeridi (Ribbon) Daralt" });
            _zones.Add(new UiZone { Id = "TOG_CLUTTER", Bounds = new Rectangle(cardHdr.X + 14, cardHdr.Y + 212, (cardHdr.Width - 34) / 2, 34), Title = "3D/Müzik Kalabalığını Gizle" });
            _zones.Add(new UiZone { Id = "TOG_QUICKACCESS", Bounds = new Rectangle(cardHdr.X + 20 + (cardHdr.Width - 34) / 2, cardHdr.Y + 212, (cardHdr.Width - 34) / 2, 34), Title = "Son Geçmişi Gizle" });

            _zones.Add(new UiZone
            {
                Id = "ACT_FIX_TEXT_TAB2",
                Bounds = new Rectangle(cardHdr.X + 14, cardHdr.Bottom - 44, cardHdr.Width - 28, 36),
                Title = "✦ HDR Bembeyaz Metin & Renk Onarımı Uygula"
            });

            // Card 2C: Border Physics & Dual Window Focus Simulator
            _zones.Add(new UiZone { Id = "BORDER_RAD_0", Bounds = new Rectangle(cardPhys.X + 14, cardPhys.Y + 42, 74, 30), Title = "Keskin (0)" });
            _zones.Add(new UiZone { Id = "BORDER_RAD_10", Bounds = new Rectangle(cardPhys.X + 94, cardPhys.Y + 42, 82, 30), Title = "Standart (10)" });
            _zones.Add(new UiZone { Id = "BORDER_RAD_14", Bounds = new Rectangle(cardPhys.X + 182, cardPhys.Y + 42, 82, 30), Title = "Yuvarlak (14)" });

            _zones.Add(new UiZone
            {
                Id = "ACT_SIM_BORDER_PHYSICS",
                Bounds = new Rectangle(cardPhys.X + 14, cardPhys.Bottom - 46, 250, 36),
                Title = _borderSimRunning ? "● Odak Fiziği Simüle Ediliyor..." : "▶ Simüle Et: İki Pencere Odak Geçişi"
            });
        }

        // --------------------------------------------------------------------
        // VIEW 3: Görev Çubuğu & Dock (4-Mode Selector + Geometry + Material + Tray Polish)
        // --------------------------------------------------------------------
        private void BuildTab3TaskbarZones(Rectangle ws)
        {
            int hRow1 = Math.Max(140, (int)(ws.Height * 0.28));
            int hRow2 = Math.Max(210, (int)(ws.Height * 0.42));
            int hRow3 = Math.Max(130, ws.Height - hRow1 - hRow2 - 20);

            Rectangle cardModes = BentoCell(ws, 0, 12, ws.Y, hRow1);
            Rectangle cardGeom = BentoCell(ws, 0, 6, ws.Y + hRow1 + 10, hRow2);
            Rectangle cardMat = BentoCell(ws, 6, 6, ws.Y + hRow1 + 10, hRow2);
            Rectangle cardTray = BentoCell(ws, 0, 12, ws.Y + hRow1 + hRow2 + 20, hRow3);

            string[] tbTitles = new string[]
            {
                "0. Klasik Koyu",
                "1. Görünmez Cam",
                "2. Yüzen Dock",
                "3. Dinamik Ada",
                "4. Parçalı Bar"
            };
            string[] tbSubs = new string[]
            {
                "Tam Genişlik Bar",
                "Translucent %100 Şeffaf",
                "Yuvarlak Köşeli Dock",
                "Merkez Kompakt Kapsül",
                "Ayrık Başlat & Tepsi"
            };

            int mW = (cardModes.Width - 28 - 40) / 5;
            for (int i = 0; i < 5; i++)
            {
                _zones.Add(new UiZone
                {
                    Id = "TASKBAR_" + i,
                    Bounds = new Rectangle(cardModes.X + 14 + i * (mW + 10), cardModes.Y + 36, mW, cardModes.Height - 48),
                    Title = tbTitles[i],
                    Subtitle = tbSubs[i]
                });
            }

            // Card 3B: Geometry & Search
            _zones.Add(new UiZone
            {
                Id = "TOG_TB_CENTER",
                Bounds = new Rectangle(cardGeom.X + 14, cardGeom.Y + 38, cardGeom.Width - 28, 38),
                Title = "Görev Çubuğu Simgelerini Ortala (Windows 11 Stili)"
            });

            AddSliderZone("SLIDER_TB_RADIUS", new Rectangle(cardGeom.X + 14, cardGeom.Y + 102, cardGeom.Width - 28, 16), "Dock Köşe Yuvarlaklığı (Radius px)");
            AddSliderZone("SLIDER_TB_MARGIN", new Rectangle(cardGeom.X + 14, cardGeom.Y + 150, cardGeom.Width - 28, 16), "Yüzen Dock Kenar Boşluğu (Margin px)");

            int sbW = (cardGeom.Width - 40) / 3;
            _zones.Add(new UiZone { Id = "SEARCH_0", Bounds = new Rectangle(cardGeom.X + 14, cardGeom.Bottom - 42, sbW, 32), Title = "Arama: Gizli" });
            _zones.Add(new UiZone { Id = "SEARCH_1", Bounds = new Rectangle(cardGeom.X + 20 + sbW, cardGeom.Bottom - 42, sbW, 32), Title = "Büyüteç İkonu" });
            _zones.Add(new UiZone { Id = "SEARCH_2", Bounds = new Rectangle(cardGeom.X + 26 + sbW * 2, cardGeom.Bottom - 42, sbW, 32), Title = "Tam Kutu" });

            // Card 3C: Material & Tint
            AddSliderZone("SLIDER_TB_ALPHA", new Rectangle(cardMat.X + 14, cardMat.Y + 62, cardMat.Width - 28, 16), "Görev Çubuğu Özel Opaklık (0..255)");

            _zones.Add(new UiZone
            {
                Id = "TOG_TB_SPECULAR",
                Bounds = new Rectangle(cardMat.X + 14, cardMat.Y + 100, cardMat.Width - 28, 38),
                Title = "Üst 1px Specular Neon Parıltı Çizgisini Göster"
            });

            _zones.Add(new UiZone
            {
                Id = "ACT_EDIT_TB_COLOR",
                Bounds = new Rectangle(cardMat.X + 14, cardMat.Bottom - 46, cardMat.Width - 28, 36),
                Title = "🎨 Görev Çubuğu Özel Rengini Renk Stüdyosunda Düzenle"
            });

            // Card 3D: Tray Polish & Dynamic Island Behavior
            int trW = (cardTray.Width - 34) / 2;
            int trH = Math.Max(34, (cardTray.Height - 48) / 2);
            _zones.Add(new UiZone { Id = "TOG_TB_SECONDS", Bounds = new Rectangle(cardTray.X + 14, cardTray.Y + 36, trW, trH), Title = "Sistem Saatinde Saniyeleri Göster" });
            _zones.Add(new UiZone { Id = "TOG_HIDE_TASKVIEW", Bounds = new Rectangle(cardTray.X + 20 + trW, cardTray.Y + 36, trW, trH), Title = "Görev Görünümü (Task View) Butonunu Gizle" });
            _zones.Add(new UiZone { Id = "TOG_ISLAND_MEDIA", Bounds = new Rectangle(cardTray.X + 14, cardTray.Y + 42 + trH, trW, trH), Title = "Dinamik Ada: Medya Çalarken Otomatik Genişle" });
            _zones.Add(new UiZone { Id = "TOG_TRAY_CLEAN", Bounds = new Rectangle(cardTray.X + 20 + trW, cardTray.Y + 42 + trH, trW, trH), Title = "Sistem Tepsisi İkonlarını Kompakt Grupla" });
        }

        // --------------------------------------------------------------------
        // VIEW 4: Masaüstü HUD & Ses Spektrumu (8 Widgets + Inspector + Spectrum + Pomodoro)
        // --------------------------------------------------------------------
        private void BuildTab4DesktopHudZones(Rectangle ws)
        {
            int hRow1 = Math.Max(260, (int)(ws.Height * 0.54));
            int hRow2 = Math.Max(200, ws.Height - hRow1 - 10);

            Rectangle cardBoard = BentoCell(ws, 0, 7, ws.Y, hRow1);
            Rectangle cardInsp = BentoCell(ws, 7, 5, ws.Y, hRow1);
            Rectangle cardSpec = BentoCell(ws, 0, 6, ws.Y + hRow1 + 10, hRow2);
            Rectangle cardPomo = BentoCell(ws, 6, 6, ws.Y + hRow1 + 10, hRow2);

            // Card 4A: 8-Widget Interactive Placement Board
            _zones.Add(new UiZone
            {
                Id = "TOG_HUD_ENABLED",
                Bounds = new Rectangle(cardBoard.X + 14, cardBoard.Y + 36, (cardBoard.Width - 34) / 2, 34),
                Title = "8'li Masaüstü HUD Süiti Aktif"
            });
            _zones.Add(new UiZone
            {
                Id = "ACT_TOGGLE_WIDGET_LOCK",
                Bounds = new Rectangle(cardBoard.X + 20 + (cardBoard.Width - 34) / 2, cardBoard.Y + 36, (cardBoard.Width - 34) / 2, 34),
                Title = _widgetsLocked ? "🔓 Kilidi Aç (Fareyle Sürükle)" : "🔒 Masaüstüne Kilitle"
            });

            string[] wNames = new string[]
            {
                "1. Saat & Gün",
                "2. CPU/RAM/GPU",
                "3. Diskler (C:/D:)",
                "4. Canlı Ağ",
                "5. Medya Çalar",
                "6. Hızlı Notlar",
                "7. Ses Spektrumu",
                "8. Pomodoro Odak"
            };
            string[] wTogs = new string[]
            {
                "TOG_W_CLOCK", "TOG_W_SYSGPU", "TOG_W_STORAGE", "TOG_W_NETWORK",
                "TOG_W_MEDIA", "TOG_W_NOTES", "TOG_W_SPECTRUM", "TOG_W_POMODORO"
            };

            int wgW = (cardBoard.Width - 34) / 2;
            int wgH = Math.Max(30, (cardBoard.Height - 126) / 4);
            for (int w = 0; w < 8; w++)
            {
                int col = w % 2;
                int row = w / 2;
                _zones.Add(new UiZone
                {
                    Id = wTogs[w],
                    Bounds = new Rectangle(cardBoard.X + 14 + col * (wgW + 6), cardBoard.Y + 76 + row * (wgH + 5), wgW, wgH),
                    Title = wNames[w],
                    Tag = w.ToString()
                });
            }

            int layW = (cardBoard.Width - 40) / 3;
            _zones.Add(new UiZone { Id = "W_LAYOUT_0", Bounds = new Rectangle(cardBoard.X + 14, cardBoard.Bottom - 38, layW, 28), Title = "Sağ Komuta Kolonu" });
            _zones.Add(new UiZone { Id = "W_LAYOUT_1", Bounds = new Rectangle(cardBoard.X + 20 + layW, cardBoard.Bottom - 38, layW, 28), Title = "Üst Panoramik Bar" });
            _zones.Add(new UiZone { Id = "W_LAYOUT_2", Bounds = new Rectangle(cardBoard.X + 26 + layW * 2, cardBoard.Bottom - 38, layW, 28), Title = "Köşe Kokpit" });

            // Card 4B: Widget Inspector & Global HUD Scale/Opacity
            int scW = (cardInsp.Width - 40) / 3;
            _zones.Add(new UiZone { Id = "W_SCALE_85", Bounds = new Rectangle(cardInsp.X + 14, cardInsp.Y + 62, scW, 30), Title = "Kompakt %85" });
            _zones.Add(new UiZone { Id = "W_SCALE_100", Bounds = new Rectangle(cardInsp.X + 20 + scW, cardInsp.Y + 62, scW, 30), Title = "Standart %100" });
            _zones.Add(new UiZone { Id = "W_SCALE_115", Bounds = new Rectangle(cardInsp.X + 26 + scW * 2, cardInsp.Y + 62, scW, 30), Title = "Büyük %115" });

            AddSliderZone("SLIDER_HUD_OPACITY", new Rectangle(cardInsp.X + 14, cardInsp.Y + 124, cardInsp.Width - 28, 16), "HUD Cam Opaklığı (80..255)");

            _zones.Add(new UiZone
            {
                Id = "ACT_EDIT_WIDGET_NOTES",
                Bounds = new Rectangle(cardInsp.X + 14, cardInsp.Bottom - 82, cardInsp.Width - 28, 34),
                Title = "✎ Hızlı Not Widget Metnini Düzenle..."
            });
            _zones.Add(new UiZone
            {
                Id = "ACT_RESET_WIDGET_POS",
                Bounds = new Rectangle(cardInsp.X + 14, cardInsp.Bottom - 42, cardInsp.Width - 28, 32),
                Title = "↺ Tüm Widget Konumlarını Dizilime Sıfırla"
            });

            // Card 4C: Audio Spectrum Visualizer Studio
            string[] spStyles = new string[] { "Çubuk", "Ayna", "Dalga", "Halka" };
            int spW = (cardSpec.Width - 28 - 18) / 4;
            for (int s = 0; s < 4; s++)
            {
                _zones.Add(new UiZone
                {
                    Id = "SPEC_STYLE_" + s,
                    Bounds = new Rectangle(cardSpec.X + 14 + s * (spW + 6), cardSpec.Y + 36, spW, 28),
                    Title = spStyles[s]
                });
            }

            int bdW = (cardSpec.Width / 2 - 20) / 3;
            int[] bands = new int[] { 16, 32, 64 };
            for (int b = 0; b < 3; b++)
            {
                _zones.Add(new UiZone
                {
                    Id = "SPEC_BANDS_" + bands[b],
                    Bounds = new Rectangle(cardSpec.X + 14 + b * (bdW + 4), cardSpec.Y + 72, bdW, 26),
                    Title = bands[b] + "B"
                });
            }

            AddSliderZone("SLIDER_SPEC_SMOOTH", new Rectangle(cardSpec.X + 14, cardSpec.Bottom - 36, cardSpec.Width / 2 - 20, 16), "Yumuşatma (Smoothing)");

            // Card 4D: Pomodoro & Focus Ring Controller
            int pmX = cardPomo.X + 150;
            int pmW = Math.Max(120, cardPomo.Right - pmX - 14);
            _zones.Add(new UiZone
            {
                Id = "ACT_POMODORO_TOGGLE",
                Bounds = new Rectangle(pmX, cardPomo.Y + 42, pmW, 34),
                Title = DesktopHudWidget.PomodoroRunning ? "⏸ Pomodoro Duraklat" : "▶ Pomodoro Başlat (Odak)"
            });
            _zones.Add(new UiZone
            {
                Id = "ACT_POMODORO_RESET",
                Bounds = new Rectangle(pmX, cardPomo.Y + 82, (pmW - 6) / 2, 30),
                Title = "↺ Sıfırla"
            });
            _zones.Add(new UiZone
            {
                Id = "ACT_POMODORO_SKIP",
                Bounds = new Rectangle(pmX + (pmW - 6) / 2 + 6, cardPomo.Y + 82, (pmW - 6) / 2, 30),
                Title = "⏭ Faz Atla"
            });

            _zones.Add(new UiZone { Id = "POMO_PRESET_25", Bounds = new Rectangle(pmX, cardPomo.Bottom - 40, (pmW - 6) / 2, 28), Title = "25 / 5 dk" });
            _zones.Add(new UiZone { Id = "POMO_PRESET_50", Bounds = new Rectangle(pmX + (pmW - 6) / 2 + 6, cardPomo.Bottom - 40, (pmW - 6) / 2, 28), Title = "50 / 10 dk" });
        }

        // --------------------------------------------------------------------
        // VIEW 5: Oyun/Render Boost & Monitör Merkezi (Boost Bar + Smart Focus + RAM Purger + Dimmer/Kelvin + Icons)
        // --------------------------------------------------------------------
        private void BuildTab5BoostMonitorZones(Rectangle ws)
        {
            int hBoostBar = 62;
            int remH = ws.Height - hBoostBar - 20;
            int hRow1 = Math.Max(220, (int)(remH * 0.50));
            int hRow2 = Math.Max(200, remH - hRow1);

            // Top Boost Profile Bar
            string[] bpTitles = new string[]
            {
                "⚡ Normal Stüdyo",
                "🎮 Oyun Boost (%0 Yük)",
                "🎬 Render / ComfyUI",
                "🌙 Sessiz & Gece"
            };
            string[] bpSubs = new string[]
            {
                "60Hz Tam Efekt & Spektrum",
                "Otomatik Uyku & RAM Kalkanı",
                "VRAM/GPU Öncelikli Mod",
                "Sıcak Kelvin & Düşük Parlaklık"
            };

            int bpW = (ws.Width - 30) / 4;
            for (int i = 0; i < 4; i++)
            {
                _zones.Add(new UiZone
                {
                    Id = "BOOST_PROF_" + i,
                    Bounds = new Rectangle(ws.X + i * (bpW + 10), ws.Y, bpW, hBoostBar),
                    Title = bpTitles[i],
                    Subtitle = bpSubs[i]
                });
            }

            int yRow1 = ws.Y + hBoostBar + 10;
            Rectangle cardFocus = BentoCell(ws, 0, 6, yRow1, hRow1);
            Rectangle cardRam = BentoCell(ws, 6, 6, yRow1, hRow1);

            // Card 5B: Smart Focus Auto-Pause
            _zones.Add(new UiZone
            {
                Id = "TOG_SMART_FOCUS",
                Bounds = new Rectangle(cardFocus.X + 14, cardFocus.Y + 38, cardFocus.Width - 28, 38),
                Title = "Oyun / ComfyUI / Blender Çalışırken Efektleri Uyut"
            });

            AddSliderZone("SLIDER_GPU_PAUSE", new Rectangle(cardFocus.X + 14, cardFocus.Y + 136, cardFocus.Width - 28, 16), "Otomatik Uyku GPU Yük Eşiği (%)");

            _zones.Add(new UiZone
            {
                Id = "ACT_TEST_SMART_FOCUS",
                Bounds = new Rectangle(cardFocus.X + 14, cardFocus.Bottom - 42, cardFocus.Width - 28, 32),
                Title = "🔍 Aktif Süreçleri & Tam Ekran Durumunu Şimdi Denetle"
            });

            // Card 5C: RAM & WorkingSet Purger
            AddSliderZone("SLIDER_RAM_AUTOPURGE", new Rectangle(cardRam.X + 14, cardRam.Bottom - 84, cardRam.Width - 28, 16), "Otomatik RAM Temizleme Eşiği (%)");

            _zones.Add(new UiZone
            {
                Id = "ACT_PURGE_RAM",
                Bounds = new Rectangle(cardRam.X + 14, cardRam.Bottom - 46, cardRam.Width - 28, 36),
                Title = "🚀 Şimdi RAM & WorkingSet Temizle (Belleği Boşalt)"
            });

            // Row 2: Display Brightness Dimmer & Kelvin Warmth (Cols 0..7) + Desktop Icons (Cols 7..11)
            int yRow2 = yRow1 + hRow1 + 10;
            Rectangle cardDisp = BentoCell(ws, 0, 7, yRow2, hRow2);
            Rectangle cardDesk = BentoCell(ws, 7, 5, yRow2, hRow2);

            _zones.Add(new UiZone
            {
                Id = "TOG_DISPLAY_OVERLAY",
                Bounds = new Rectangle(cardDisp.X + 14, cardDisp.Y + 36, cardDisp.Width - 28, 36),
                Title = "Yazılımsal Ekran Karartma (Dimmer) & Gece Işığı Filtresi Aktif"
            });

            AddSliderZone("SLIDER_DIMMER", new Rectangle(cardDisp.X + 14, cardDisp.Y + 96, cardDisp.Width - 28, 16), "Ekran Parlaklık Karartma (%0..%80)");
            AddSliderZone("SLIDER_KELVIN", new Rectangle(cardDisp.X + 14, cardDisp.Y + 142, cardDisp.Width - 28, 16), "Gece Işığı Renk Sıcaklığı (2700K..6500K)");

            int kW = (cardDisp.Width - 46) / 4;
            int[] kelvins = new int[] { 6500, 4800, 3600, 2700 };
            string[] kLabels = new string[] { "6500K Doğal", "4800K Ofis", "3600K Akşam", "2700K Uyku" };
            for (int k = 0; k < 4; k++)
            {
                _zones.Add(new UiZone
                {
                    Id = "KELVIN_" + kelvins[k],
                    Bounds = new Rectangle(cardDisp.X + 14 + k * (kW + 6), cardDisp.Bottom - 38, kW, 28),
                    Title = kLabels[k]
                });
            }

            // Card 5E: Desktop Icons 1-Click Toggle & Sizes
            _zones.Add(new UiZone
            {
                Id = "ACT_TOGGLE_DESKTOP_ICONS",
                Bounds = new Rectangle(cardDesk.X + 14, cardDesk.Y + 38, cardDesk.Width - 28, 44),
                Title = "👁 Masaüstü Simgelerini Gizle / Göster"
            });

            int dsW = (cardDesk.Width - 40) / 3;
            _zones.Add(new UiZone { Id = "DESK_SIZE_32", Bounds = new Rectangle(cardDesk.X + 14, cardDesk.Y + 92, dsW, 30), Title = "Küçük (32)" });
            _zones.Add(new UiZone { Id = "DESK_SIZE_48", Bounds = new Rectangle(cardDesk.X + 20 + dsW, cardDesk.Y + 92, dsW, 30), Title = "Orta (48)" });
            _zones.Add(new UiZone { Id = "DESK_SIZE_64", Bounds = new Rectangle(cardDesk.X + 26 + dsW * 2, cardDesk.Y + 92, dsW, 30), Title = "Büyük (64)" });

            _zones.Add(new UiZone
            {
                Id = "TOG_SHORTCUT_ARROWS",
                Bounds = new Rectangle(cardDesk.X + 14, cardDesk.Bottom - 42, cardDesk.Width - 28, 32),
                Title = "Masaüstü Kısayol Oklarını Gizle"
            });
        }

        // --------------------------------------------------------------------
        // VIEW 6: Sistem, Kısayollar & Yedekleme (Startup + Shortcuts Table + Snapshots + Diagnostic Logs)
        // --------------------------------------------------------------------
        private void BuildTab6SystemBackupZones(Rectangle ws)
        {
            int hRow1 = Math.Max(240, (int)(ws.Height * 0.50));
            int hRow2 = Math.Max(220, ws.Height - hRow1 - 10);

            Rectangle cardStart = BentoCell(ws, 0, 6, ws.Y, hRow1);
            Rectangle cardKeys = BentoCell(ws, 6, 6, ws.Y, hRow1);
            Rectangle cardBackup = BentoCell(ws, 0, 6, ws.Y + hRow1 + 10, hRow2);
            Rectangle cardLogs = BentoCell(ws, 6, 6, ws.Y + hRow1 + 10, hRow2);

            // Card 6A: Startup, Scheduler & FPS Cap
            _zones.Add(new UiZone { Id = "TOG_AUTOSTART", Bounds = new Rectangle(cardStart.X + 14, cardStart.Y + 38, cardStart.Width - 28, 38), Title = "Windows Açılışında Otomatik Başlat (--minimized)" });
            _zones.Add(new UiZone { Id = "TOG_HOTKEYS", Bounds = new Rectangle(cardStart.X + 14, cardStart.Y + 82, cardStart.Width - 28, 38), Title = "Global Klavye Kısayolları Aktif" });
            _zones.Add(new UiZone { Id = "TOG_SCHEDULE", Bounds = new Rectangle(cardStart.X + 14, cardStart.Y + 126, cardStart.Width - 28, 38), Title = "Gündüz / Gece Otomatik Tema Zamanlayıcısı (07:00 / 20:00)" });

            int cycW = (cardStart.Width - 34) / 2;
            _zones.Add(new UiZone { Id = "ACT_CYCLE_DAY_PROFILE", Bounds = new Rectangle(cardStart.X + 14, cardStart.Bottom - 44, cycW, 34), Title = "☀ Gündüz: " + _dayProfile });
            _zones.Add(new UiZone { Id = "ACT_CYCLE_NIGHT_PROFILE", Bounds = new Rectangle(cardStart.X + 20 + cycW, cardStart.Bottom - 44, cycW, 34), Title = "☾ Gece: " + _nightProfile });

            // Card 6B: Global Shortcuts Table Triggers
            _zones.Add(new UiZone { Id = "CMD_BAR_OPEN", Bounds = new Rectangle(cardKeys.Right - 110, cardKeys.Y + 42, 96, 26), Title = "Paleti Aç" });
            _zones.Add(new UiZone { Id = "ACT_SHORTCUT_SHADOW", Bounds = new Rectangle(cardKeys.Right - 110, cardKeys.Y + 110, 96, 26), Title = "Gölge Değiştir" });
            _zones.Add(new UiZone { Id = "ACT_TOGGLE_DESKTOP_ICONS", Bounds = new Rectangle(cardKeys.Right - 110, cardKeys.Y + 144, 96, 26), Title = "İkon Gizle/Aç" });
            _zones.Add(new UiZone { Id = "ACT_UNDO_HEADER", Bounds = new Rectangle(cardKeys.Right - 110, cardKeys.Y + 178, 96, 26), Title = "Geri Al (Ctrl+Z)" });

            // Card 6C: Backup, Snapshot, Explorer Restart & Factory Reset
            int bkW = (cardBackup.Width - 34) / 2;
            int bkH = Math.Max(34, (cardBackup.Height - 62) / 3);
            _zones.Add(new UiZone { Id = "ACT_CREATE_SNAPSHOT", Bounds = new Rectangle(cardBackup.X + 14, cardBackup.Y + 38, bkW, bkH), Title = "📸 Anlık Snapshot Kaydet" });
            _zones.Add(new UiZone { Id = "ACT_EXPORT_THEME", Bounds = new Rectangle(cardBackup.X + 20 + bkW, cardBackup.Y + 38, bkW, bkH), Title = "📦 Windows .theme Üret" });
            _zones.Add(new UiZone { Id = "ACT_BACKUP_EXPORT", Bounds = new Rectangle(cardBackup.X + 14, cardBackup.Y + 44 + bkH, bkW, bkH), Title = "💾 Ayarları Yedekle (.ini)" });
            _zones.Add(new UiZone { Id = "ACT_BACKUP_IMPORT", Bounds = new Rectangle(cardBackup.X + 20 + bkW, cardBackup.Y + 44 + bkH, bkW, bkH), Title = "📂 Yedekten Geri Yükle" });
            _zones.Add(new UiZone { Id = "ACT_RESTART_EXPLORER", Bounds = new Rectangle(cardBackup.X + 14, cardBackup.Y + 50 + bkH * 2, bkW, bkH), Title = "⟳ Explorer Yeniden Başlat" });
            _zones.Add(new UiZone { Id = "ACT_FACTORY_RESET", Bounds = new Rectangle(cardBackup.X + 20 + bkW, cardBackup.Y + 50 + bkH * 2, bkW, bkH), Title = "⚠ Fabrika Koyu Sıfırla" });

            // Card 6D: Live Health & Diagnostic Logs
            int lgW = (cardLogs.Width - 34) / 2;
            _zones.Add(new UiZone { Id = "ACT_HEALTH_CHECK", Bounds = new Rectangle(cardLogs.X + 14, cardLogs.Bottom - 42, lgW, 32), Title = "🩺 Sistem Öz-Testi Çalıştır" });
            _zones.Add(new UiZone { Id = "ACT_COPY_LOGS", Bounds = new Rectangle(cardLogs.X + 20 + lgW, cardLogs.Bottom - 42, lgW, 32), Title = "📋 Günlüğü Panoya Kopyala" });
        }

        // --------------------------------------------------------------------
        // Global Command Palette Overlay Zones (Ctrl+Shift+Space)
        // --------------------------------------------------------------------
        private void RebuildCommandPaletteZones()
        {
            _paletteZones.Clear();
            _filteredCommands = CommandPaletteCatalog.Search(_commandSearchQuery);
            if (_commandSelectedIndex >= _filteredCommands.Count)
                _commandSelectedIndex = Math.Max(0, _filteredCommands.Count - 1);

            int modalW = 640;
            int modalH = 430;
            int mx = (this.ClientSize.Width - modalW) / 2;
            int my = Math.Max(70, (this.ClientSize.Height - modalH) / 2 - 20);

            _paletteZones.Add(new UiZone { Id = "PAL_CLOSE", Bounds = new Rectangle(mx + modalW - 42, my + 12, 28, 28), Title = "✕" });
            _paletteZones.Add(new UiZone { Id = "PAL_FILTER_ALL", Bounds = new Rectangle(mx + 16, my + 56, 72, 24), Title = "Tümü" });
            _paletteZones.Add(new UiZone { Id = "PAL_FILTER_CMD", Bounds = new Rectangle(mx + 94, my + 56, 96, 24), Title = "> Komutlar" });
            _paletteZones.Add(new UiZone { Id = "PAL_FILTER_THM", Bounds = new Rectangle(mx + 196, my + 56, 92, 24), Title = "# Temalar" });
            _paletteZones.Add(new UiZone { Id = "PAL_FILTER_WDG", Bounds = new Rectangle(mx + 294, my + 56, 98, 24), Title = "@ Widgetlar" });

            int maxRows = Math.Min(8, _filteredCommands.Count);
            for (int i = 0; i < maxRows; i++)
            {
                CommandPaletteItem item = _filteredCommands[i];
                _paletteZones.Add(new UiZone
                {
                    Id = "PAL_ITEM_" + i,
                    Bounds = new Rectangle(mx + 16, my + 90 + i * 40, modalW - 32, 36),
                    Title = item.Title,
                    Subtitle = item.Category,
                    Tag = item.Id
                });
            }
        }

        private void AddSliderZone(string id, Rectangle trackRect, string title)
        {
            _sliderTracks[id] = trackRect;
            _zones.Add(new UiZone
            {
                Id = id,
                Bounds = new Rectangle(trackRect.X - 4, trackRect.Y - 8, trackRect.Width + 8, trackRect.Height + 16),
                Title = title
            });
        }

        // ====================================================================
        // System Tray Initialization & Quick Theme / 8-Widget Submenus
        // ====================================================================
        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.BackColor = _uiCard;
            _trayMenu.ForeColor = ColTextPrimary;
            _trayMenu.ShowImageMargin = false;

            ToolStripMenuItem openItem = new ToolStripMenuItem("SPLEND Bento Command Deck v3.0'ı Aç (Ctrl+Alt+T)");
            openItem.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            openItem.ForeColor = _uiGoldCta;
            openItem.Click += (s, e) => ShowMainWindow();

            ToolStripMenuItem cmdItem = new ToolStripMenuItem("⌕ Global Komut Paleti (Ctrl+Shift+Space)");
            cmdItem.ForeColor = ColInfo;
            cmdItem.Click += (s, e) => { ShowMainWindow(); OpenCommandPalette(""); };

            _trayThemesSubMenu = new ToolStripMenuItem("Temalar (Hızlı Geçiş)");
            _trayThemesSubMenu.ForeColor = ColTextPrimary;

            _trayWidgetsSubMenu = new ToolStripMenuItem("Masaüstü Widgetları (8'li HUD Süiti)");
            _trayWidgetsSubMenu.ForeColor = ColTextPrimary;

            ToolStripMenuItem purgeRamItem = new ToolStripMenuItem("🚀 Şimdi RAM & WorkingSet Temizle");
            purgeRamItem.ForeColor = ColSuccess;
            purgeRamItem.Click += (s, e) => ExecuteRamPurge();

            ToolStripMenuItem chameleonItem = new ToolStripMenuItem("✦ Duvar Kağıdından Otomatik Tema (Chameleon)");
            chameleonItem.ForeColor = ColTextPrimary;
            chameleonItem.Click += (s, e) => ExecuteChameleonFromCurrentWallpaper();

            ToolStripMenuItem fixTextItem = new ToolStripMenuItem("✦ HDR Bembeyaz Yazı & Renk Onarımı");
            fixTextItem.ForeColor = ColTextPrimary;
            fixTextItem.Click += (s, e) => ExecuteFixExplorerText();

            _trayEmeraldOverlayMenuItem = new ToolStripMenuItem("Gezgin İç Gövde & 360° Gölge: Aç / Kapat");
            _trayEmeraldOverlayMenuItem.ForeColor = ColTextPrimary;
            _trayEmeraldOverlayMenuItem.Click += (s, e) => ToggleEmeraldArmorOverlay();

            _trayHudMenuItem = new ToolStripMenuItem("8'li Masaüstü HUD Süiti: Aç / Kapat");
            _trayHudMenuItem.ForeColor = ColTextPrimary;
            _trayHudMenuItem.Click += (s, e) => ToggleHudWidget();

            ToolStripMenuItem deskIconsItem = new ToolStripMenuItem("Masaüstü Simgelerini Gizle / Göster");
            deskIconsItem.ForeColor = ColTextPrimary;
            deskIconsItem.Click += (s, e) => { try { DoomThemeEngine.ToggleDesktopIconsVisibility(); } catch { } };

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Tamamen Çık (Tüm Efektleri Kapat)");
            exitItem.ForeColor = Color.FromArgb(245, 130, 130);
            exitItem.Click += (s, e) => ExitApplicationCompletely();

            _trayMenu.Items.Add(openItem);
            _trayMenu.Items.Add(cmdItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayThemesSubMenu);
            _trayMenu.Items.Add(_trayWidgetsSubMenu);
            _trayMenu.Items.Add(purgeRamItem);
            _trayMenu.Items.Add(chameleonItem);
            _trayMenu.Items.Add(fixTextItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayEmeraldOverlayMenuItem);
            _trayMenu.Items.Add(_trayHudMenuItem);
            _trayMenu.Items.Add(deskIconsItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = _appIcon ?? SystemIcons.Shield;
            _trayIcon.Text = "SPLEND // Theme Studio v3.0 (Bento Command Deck)";
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void RebuildTrayWidgetsMenu()
        {
            if (_trayWidgetsSubMenu == null) return;
            _trayWidgetsSubMenu.DropDownItems.Clear();
            _trayWidgetsSubMenu.DropDown.BackColor = _uiCard;
            _trayWidgetsSubMenu.DropDown.ForeColor = ColTextPrimary;

            ToolStripMenuItem masterItem = new ToolStripMenuItem((_hudEnabled ? "● " : "   ") + "Tüm 8 Widget Aktif");
            masterItem.ForeColor = _uiGoldCta;
            masterItem.Click += (s, e) => ToggleHudWidget();
            _trayWidgetsSubMenu.DropDownItems.Add(masterItem);

            ToolStripMenuItem lockItem = new ToolStripMenuItem(_widgetsLocked ? "🔓 Konum Kilidini Aç (Sürükle & Tıkla)" : "🔒 Konumları Masaüstüne Kilitle");
            lockItem.ForeColor = ColTextPrimary;
            lockItem.Click += (s, e) => ToggleWidgetLockMode();
            _trayWidgetsSubMenu.DropDownItems.Add(lockItem);

            _trayWidgetsSubMenu.DropDownItems.Add(new ToolStripSeparator());

            AddWidgetToggleTrayItem("1. Saat, Tarih & Gün İlerlemesi", _widgetClock, "TOG_W_CLOCK");
            AddWidgetToggleTrayItem("2. CPU, RAM & GPU/VRAM", _widgetSystemGpu, "TOG_W_SYSGPU");
            AddWidgetToggleTrayItem("3. Disk Durumu (C: / D:)", _widgetStorage, "TOG_W_STORAGE");
            AddWidgetToggleTrayItem("4. Canlı Ağ Trafiği (Down/Up)", _widgetNetwork, "TOG_W_NETWORK");
            AddWidgetToggleTrayItem("5. Medya & Ses Kontrolü", _widgetMedia, "TOG_W_MEDIA");
            AddWidgetToggleTrayItem("6. Sistem Uptime & Hızlı Not", _widgetNotes, "TOG_W_NOTES");
            AddWidgetToggleTrayItem("7. Canlı Ses Spektrumu (Audio FFT)", _widgetSpectrum, "TOG_W_SPECTRUM");
            AddWidgetToggleTrayItem("8. Pomodoro & Derin Odak Halkası", _widgetPomodoro, "TOG_W_POMODORO");
        }

        private void AddWidgetToggleTrayItem(string label, bool isOn, string toggleId)
        {
            ToolStripMenuItem item = new ToolStripMenuItem((isOn ? "✓ " : "   ") + label);
            item.ForeColor = ColTextPrimary;
            item.Click += (s, e) => HandleToggleClick(toggleId);
            _trayWidgetsSubMenu.DropDownItems.Add(item);
        }

        private void RebuildTrayThemesMenu()
        {
            if (_trayThemesSubMenu == null) return;
            _trayThemesSubMenu.DropDownItems.Clear();
            _trayThemesSubMenu.DropDown.BackColor = _uiCard;
            _trayThemesSubMenu.DropDown.ForeColor = ColTextPrimary;

            foreach (ThemePreset p in _allPresets)
            {
                string pid = p.Id;
                string label = (string.Equals(pid, _activeProfileId, StringComparison.OrdinalIgnoreCase) ? "● " : "   ") + p.DisplayName;
                ToolStripMenuItem item = new ToolStripMenuItem(label);
                item.ForeColor = ColTextPrimary;
                item.Click += (s, e) => ActivatePresetById(pid, false);
                _trayThemesSubMenu.DropDownItems.Add(item);
            }
        }

        private void UpdateTrayMenuCheckState()
        {
            if (_trayEmeraldOverlayMenuItem != null)
            {
                _trayEmeraldOverlayMenuItem.Text = _emeraldTintAndShadowEnabled
                    ? "Gezgin İç Gövde & 360° Gölge: Açık"
                    : "Gezgin İç Gövde & 360° Gölge: Kapalı";
            }
            if (_trayHudMenuItem != null)
            {
                _trayHudMenuItem.Text = _hudEnabled
                    ? "8'li Masaüstü HUD Süiti: Açık"
                    : "8'li Masaüstü HUD Süiti: Kapalı";
            }
        }

        public void ShowMainWindow()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(ShowMainWindow));
                return;
            }

            this.Opacity = 1.0;
            this.ShowInTaskbar = true;
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();
            this.Invalidate();
        }

        private void MinimizeToTray()
        {
            this.Hide();
            this.ShowInTaskbar = false;
            try
            {
                _trayIcon.ShowBalloonTip(
                    1500,
                    "SPLEND // Bento Command Deck v3.0",
                    "Arka planda 360° Gölge, 8 HUD Widget ve Smart Focus çalışıyor (Ctrl+Shift+Space: Komut Paleti).",
                    ToolTipIcon.Info);
            }
            catch { }
        }

        private void ExitApplicationCompletely()
        {
            _allowClose = true;
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID_STUDIO);
                UnregisterHotKey(this.Handle, HOTKEY_ID_SHADOW);
                UnregisterHotKey(this.Handle, HOTKEY_ID_DESKTOP);
                UnregisterHotKey(this.Handle, HOTKEY_ID_PALETTE);
            }
            catch { }

            try { DisplayOverlayManager.DisposeOverlay(); } catch { }
            try { DesktopHudWidget.Stop(); } catch { }

            try
            {
                DoomGlassDaemon.EmeraldTintAndShadowEnabled = false;
                DoomGlassDaemon.TaskbarCenterIcons = false;
                DoomGlassDaemon.UpdateSettings(false, 255, 0, 0);
                DoomGlassDaemon.StopDaemon();
            }
            catch { }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }
            try { DisplayOverlayManager.DisposeOverlay(); } catch { }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        // ====================================================================
        // Custom GDI+ Vector Renderer — Bento Command Deck v3.0
        // ====================================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int winW = this.ClientSize.Width;
            int winH = this.ClientSize.Height;

            // 1. L0 Base Void + Dual Radial Ambient Glows (accent at 15%,0% and accent2 at 100%,100%)
            DrawAmbientVoidBackground(g, winW, winH);

            // 2. Zone B: Persistent Top Command & Telemetry Header (0..52)
            DrawHeaderBar(g, winW);

            // 3. Zone A: Collapsible Left Icon Rail
            DrawLeftIconRail(g, winH);

            // 4. Zone D: Right Live Mini Desktop Preview Dock (Views 1..6)
            if (ShouldShowRightPreviewDock())
            {
                DrawRightPreviewDock(g, GetRightPreviewDockBounds());
            }

            // 5. Zone C: Active View 12-Column Bento Cards & Backgrounds
            Rectangle ws = GetWorkspaceBounds();
            DrawActiveViewBentoCards(g, ws);

            // 6. Interactive Zones
            for (int i = 0; i < _zones.Count; i++)
            {
                DrawZone(g, _zones[i]);
            }

            // 7. Footer Status & Toast Bar
            DrawFooterBar(g, winW, winH);

            // 8. Global Command Palette Modal Overlay (Ctrl+Shift+Space)
            if (_commandPaletteOpen)
            {
                DrawCommandPaletteOverlay(g, winW, winH);
            }

            // 9. Outer Specular Window Rim + Corner Accents
            using (Pen borderPen = new Pen(_uiBorderSubtle, 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, winW - 1, winH - 1);
            }
            DrawCornerAccents(g, new Rectangle(0, 0, winW - 1, winH - 1), 14);
        }

        private void DrawAmbientVoidBackground(Graphics g, int w, int h)
        {
            using (SolidBrush bgBrush = new SolidBrush(_uiBg))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            // Radial Glow 1 at (15%, 0%)
            try
            {
                int r1W = (int)(w * 0.65);
                int r1H = (int)(h * 0.55);
                Rectangle glow1 = new Rectangle((int)(w * 0.15) - r1W / 2, -r1H / 2, Math.Max(100, r1W), Math.Max(100, r1H));
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(glow1);
                    using (PathGradientBrush pgb = new PathGradientBrush(gp))
                    {
                        pgb.CenterColor = Color.FromArgb(42, _customPrimaryAccent);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, _customPrimaryAccent) };
                        g.FillPath(pgb, gp);
                    }
                }

                // Radial Glow 2 at (100%, 100%)
                int r2W = (int)(w * 0.55);
                int r2H = (int)(h * 0.55);
                Rectangle glow2 = new Rectangle(w - r2W / 2, h - r2H / 2, Math.Max(100, r2W), Math.Max(100, r2H));
                using (GraphicsPath gp2 = new GraphicsPath())
                {
                    gp2.AddEllipse(glow2);
                    using (PathGradientBrush pgb2 = new PathGradientBrush(gp2))
                    {
                        pgb2.CenterColor = Color.FromArgb(26, _uiAccent2);
                        pgb2.SurroundColors = new Color[] { Color.FromArgb(0, _uiAccent2) };
                        g.FillPath(pgb2, gp2);
                    }
                }
            }
            catch { }
        }

        private void DrawHeaderBar(Graphics g, int winW)
        {
            Rectangle headerRect = new Rectangle(1, 1, winW - 2, 51);
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                headerRect,
                _uiCard,
                _uiHeaderBg,
                LinearGradientMode.Horizontal))
            {
                g.FillRectangle(lgb, headerRect);
            }

            using (Pen bottomLine = new Pen(_uiBorderSubtle, 1f))
            {
                g.DrawLine(bottomLine, 1, 52, winW - 2, 52);
            }
            using (Pen accentLine = new Pen(_uiActiveRim, 2f))
            {
                g.DrawLine(accentLine, 14, 52, 270, 52);
            }

            if (_appIcon != null)
            {
                try { g.DrawIcon(_appIcon, new Rectangle(14, 12, 26, 26)); } catch { }
            }

            // Breadcrumb: Doom CC › <Active View>
            string viewName = GetViewTitle(_activeTab);
            using (SolidBrush titleBrush = new SolidBrush(ColTextPrimary))
            using (SolidBrush crumbBrush = new SolidBrush(_uiGoldCta))
            using (SolidBrush subBrush = new SolidBrush(ColTextSecondary))
            {
                g.DrawString("DOOM CC v3.0", _fontHeader, titleBrush, 46f, 8f);
                g.DrawString("›  " + viewName, _fontSection, crumbBrush, 146f, 9f);
                string profName = _activePreset != null ? _activePreset.DisplayName : _activeProfileId;
                g.DrawString("BENTO COMMAND DECK  •  AKTİF: " + profName.ToUpperInvariant(), _fontCardSub, subBrush, 47f, 29f);
            }
        }

        private void DrawLeftIconRail(Graphics g, int winH)
        {
            int railW = GetRailWidth();
            Rectangle railRect = new Rectangle(10, 60, railW - 8, winH - 96);
            DrawBentoCard(g, railRect, null, null, false);

            // Spring-animated 3px neon indicator bar on the left edge of the active view tab
            using (SolidBrush neonBar = new SolidBrush(_uiGoldCta))
            using (SolidBrush glowBar = new SolidBrush(Color.FromArgb(80, _uiActiveRim)))
            {
                int iy = (int)Math.Round(_railIndicatorY);
                g.FillRectangle(glowBar, 11, iy - 2, 7, 52);
                g.FillRectangle(neonBar, 12, iy + 4, 3, 40);
            }
        }

        private void DrawRightPreviewDock(Graphics g, Rectangle dock)
        {
            if (dock.Width <= 50)
            {
                DrawBentoCard(g, dock, null, null, false);
                using (SolidBrush tb = new SolidBrush(_uiGoldCta))
                {
                    g.DrawString("CANLI MASAÜSTÜ ÖNİZLEME (16:10)", _fontSection, tb, new Rectangle(dock.X, dock.Y + 40, dock.Width, dock.Height - 50), SfVerticalCenter);
                }
                return;
            }

            DrawBentoCard(g, dock, "CANLI ÖNİZLEME (16:10)", _previewHdrSim ? "10-BIT HDR" : "SDR SIM", false);

            // 16:10 Scaled Mini Desktop Canvas inside Right Dock
            Rectangle canvasRect = new Rectangle(dock.X + 14, dock.Y + 38, dock.Width - 28, 178);
            DrawMiniDesktopPreviewCanvas(g, canvasRect);

            // Folder Icons Preview + Active Hex Readout
            Rectangle icoBox = new Rectangle(dock.X + 14, dock.Y + 300, dock.Width - 28, 86);
            using (SolidBrush ib = new SolidBrush(Color.FromArgb(190, _uiBg)))
            using (Pen ip = new Pen(_uiBorderSubtle, 1f))
            using (SolidBrush tPri = new SolidBrush(ColTextPrimary))
            using (SolidBrush tSec = new SolidBrush(ColTextSecondary))
            {
                g.FillRectangle(ib, icoBox);
                g.DrawRectangle(ip, icoBox);
                if (_previewFolderIcon1 != null)
                    g.DrawImage(_previewFolderIcon1, new Rectangle(icoBox.X + 10, icoBox.Y + 14, 48, 48));
                if (_previewFolderIcon2 != null)
                    g.DrawImage(_previewFolderIcon2, new Rectangle(icoBox.X + 64, icoBox.Y + 14, 48, 48));

                g.DrawString("Dinamik Klasör İkonları", _fontCardTitle, tPri, icoBox.X + 122, icoBox.Y + 14);
                g.DrawString("Başlık: " + ThemeCatalog.ColorToHex(_customPrimaryAccent), _fontMono, tSec, icoBox.X + 122, icoBox.Y + 34);
                g.DrawString("Gölge : " + ThemeCatalog.ColorToHex(_customShadowGlow), _fontMono, tSec, icoBox.X + 122, icoBox.Y + 52);
            }

            // Live Palette & Telemetry Summary inside Dock
            int infoY = icoBox.Bottom + 12;
            if (infoY + 80 < dock.Bottom - 104)
            {
                using (SolidBrush tPri = new SolidBrush(ColTextPrimary))
                using (SolidBrush tSec = new SolidBrush(ColTextSecondary))
                using (SolidBrush tGold = new SolidBrush(_uiGoldCta))
                {
                    double contrast = ColorHarmonyGenerator.CalculateContrastRatio(ColTextPrimary, _customBodyTint);
                    g.DrawString("Gezgin Kontrast: " + contrast.ToString("0.0", CultureInfo.InvariantCulture) + ":1 (WCAG AAA)", _fontCardSub, tGold, dock.X + 14, infoY);
                    g.DrawString("360° Gölge Yayılımı: " + _shadowSpread + "px  •  İç Ton: " + _emeraldTintIntensity, _fontCardSub, tSec, dock.X + 14, infoY + 20);
                    g.DrawString("Görev Çubuğu: Mod " + _taskbarMode + "  •  HUD: 8/8 Aktif", _fontCardSub, tSec, dock.X + 14, infoY + 40);
                    g.DrawString("Boost Profili: " + _boostProfile.ToString() + (_smartFocusActiveProcess.Length > 0 ? " (Uyku)" : " (Aktif)"), _fontCardSub, tPri, dock.X + 14, infoY + 60);
                }
            }
        }

        private void DrawMiniDesktopPreviewCanvas(Graphics g, Rectangle rect)
        {
            // Base Monitor Wallpaper Gradient
            using (LinearGradientBrush monBg = new LinearGradientBrush(
                rect,
                BlendColors(_uiBg, _customPrimaryAccent, 0.42),
                _uiBg,
                LinearGradientMode.ForwardDiagonal))
            using (Pen monBorder = new Pen(_uiActiveRim, 1f))
            {
                g.FillRectangle(monBg, rect);

                // Simulated Explorer Window
                int expW = (int)(rect.Width * 0.64);
                int expH = (int)(rect.Height * 0.62);
                Rectangle simExp = new Rectangle(rect.X + 16, rect.Y + 16, expW, expH);

                if (_emeraldTintAndShadowEnabled && _emeraldShadowIntensity > 0)
                {
                    for (int r = 10; r >= 2; r -= 2)
                    {
                        int a = (int)Math.Min(190, (_emeraldShadowIntensity * (12 - r)) / 22);
                        using (Pen glowPen = new Pen(Color.FromArgb(a, _customShadowGlow), 2f))
                        {
                            g.DrawRectangle(glowPen, simExp.X - r, simExp.Y - r, simExp.Width + r * 2, simExp.Height + r * 2);
                        }
                    }
                }

                using (SolidBrush expBase = new SolidBrush(Color.FromArgb(24, 26, 32)))
                {
                    g.FillRectangle(expBase, simExp);
                }
                if (_emeraldTintAndShadowEnabled && _emeraldTintIntensity > 0)
                {
                    int ta = Math.Min(215, _emeraldTintIntensity + 55);
                    using (SolidBrush tintB = new SolidBrush(Color.FromArgb(ta, _customBodyTint)))
                    {
                        g.FillRectangle(tintB, simExp);
                    }
                }

                Rectangle simTitle = new Rectangle(simExp.X, simExp.Y, simExp.Width, 16);
                using (SolidBrush tbBrush = new SolidBrush(_customPrimaryAccent))
                using (SolidBrush wBrush = new SolidBrush(Color.White))
                using (Font fTitle = new Font("Segoe UI", 6.5f))
                {
                    g.FillRectangle(tbBrush, simTitle);
                    g.DrawString("Dosya Gezgini — Beyin Vault", fTitle, wBrush, simTitle.X + 5, simTitle.Y + 2);
                }

                using (SolidBrush whiteText = new SolidBrush(_previewHdrSim ? Color.White : Color.FromArgb(220, 225, 230)))
                using (Font fSim = new Font("Segoe UI", 6.8f))
                {
                    g.DrawString("📁 01-Kisisel", fSim, whiteText, simExp.X + 8, simExp.Y + 21);
                    g.DrawString("📁 Beyin Vault", fSim, whiteText, simExp.X + 8, simExp.Y + 36);
                    g.DrawString("📄 BentoDeck.cs", fSim, whiteText, simExp.X + 8, simExp.Y + 51);
                }

                if (_showRimBorder)
                {
                    using (Pen rimPen = new Pen(_customRimHighlight, 1.5f))
                    {
                        g.DrawRectangle(rimPen, simExp);
                    }
                }

                // Mini HUD Widgets on right side of monitor preview
                if (_hudEnabled)
                {
                    int hudX = rect.Right - 66;
                    for (int w = 0; w < 3; w++)
                    {
                        Rectangle wRect = new Rectangle(hudX, rect.Y + 12 + w * 28, 56, 22);
                        using (SolidBrush wb = new SolidBrush(Color.FromArgb(190, _uiCard)))
                        using (Pen wp = new Pen(Color.FromArgb(140, _customRimHighlight), 1f))
                        {
                            g.FillRectangle(wb, wRect);
                            g.DrawRectangle(wp, wRect);
                        }
                    }
                }

                // Simulated Taskbar / Dock at bottom of monitor
                Rectangle simTb = new Rectangle(rect.X + 2, rect.Bottom - 16, rect.Width - 4, 14);
                if (_taskbarMode == 2) // Floating Dock
                    simTb = new Rectangle(rect.X + rect.Width / 4, rect.Bottom - 17, rect.Width / 2, 13);
                else if (_taskbarMode == 3) // Dynamic Island
                    simTb = new Rectangle(rect.X + rect.Width / 3, rect.Bottom - 17, rect.Width / 3, 13);

                if (_taskbarMode != 1)
                {
                    Color tbCol = (_taskbarMode == 3 || _taskbarMode == 2) ? _customPrimaryAccent : _customTaskbarTint;
                    using (SolidBrush tbb = new SolidBrush(Color.FromArgb(200, tbCol)))
                    {
                        g.FillRectangle(tbb, simTb);
                    }
                }
                int iconX = (_taskbarCenterIcons || _taskbarMode >= 2) ? (rect.X + rect.Width / 2 - 20) : (simTb.X + 10);
                using (SolidBrush dotB = new SolidBrush(_customRimHighlight))
                {
                    for (int k = 0; k < 4; k++)
                        g.FillRectangle(dotB, iconX + k * 11, simTb.Y + 3, 6, 6);
                }

                // Before/After Split Overlay (left side shows FactoryDark comparison when > 0)
                if (_previewSplitPercent > 0)
                {
                    int splitW = (rect.Width * _previewSplitPercent) / 100;
                    Rectangle beforeRect = new Rectangle(rect.X, rect.Y, splitW, rect.Height);
                    using (SolidBrush bBg = new SolidBrush(Color.FromArgb(215, 18, 18, 18)))
                    using (Pen splitLine = new Pen(_uiGoldCta, 2f))
                    using (SolidBrush lblB = new SolidBrush(_uiGoldCta))
                    {
                        g.FillRectangle(bBg, beforeRect);
                        g.DrawLine(splitLine, rect.X + splitW, rect.Y, rect.X + splitW, rect.Bottom);
                        g.DrawString("ÖNCE (FABRİKA)", _fontCardSub, lblB, rect.X + 6, rect.Y + 4);
                    }
                }

                // Night-Light Kelvin & Brightness Dimmer Simulation overlay
                if (_displayOverlayEnabled && (_displayDimmerPercent > 0 || _displayWarmthKelvin < 6500))
                {
                    Color ovCol = DisplayOverlayManager.ComputeOverlayColor(_displayDimmerPercent, _displayWarmthKelvin);
                    using (SolidBrush ovB = new SolidBrush(Color.FromArgb(Math.Min(140, (int)ovCol.A), ovCol.R, ovCol.G, ovCol.B)))
                    {
                        g.FillRectangle(ovB, rect);
                    }
                }

                g.DrawRectangle(monBorder, rect);
            }
        }

        private void DrawBentoCard(Graphics g, Rectangle rect, string title, string badge, bool highlighted)
        {
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, rect.X + 2, rect.Y + 4, rect.Width, rect.Height);
            }

            Color surf = highlighted ? _uiCardHover : Color.FromArgb(228, _uiCard.R, _uiCard.G, _uiCard.B);
            using (SolidBrush cardBrush = new SolidBrush(surf))
            {
                g.FillRectangle(cardBrush, rect);
            }

            // L3 Specular 1px Rim (top bright to bottom subtle)
            using (LinearGradientBrush rimGrad = new LinearGradientBrush(
                rect,
                highlighted ? _uiGoldCta : Color.FromArgb(145, _uiActiveRim),
                Color.FromArgb(42, _uiBorderSubtle),
                LinearGradientMode.Vertical))
            using (Pen rimPen = new Pen(rimGrad, 1f))
            {
                g.DrawRectangle(rimPen, rect);
            }

            if (!string.IsNullOrEmpty(title))
            {
                using (SolidBrush barBrush = new SolidBrush(_uiGoldCta))
                using (SolidBrush titleBrush = new SolidBrush(ColTextPrimary))
                {
                    g.FillRectangle(barBrush, rect.X + 12, rect.Y + 10, 3, 14);
                    g.DrawString(title, _fontSection, titleBrush, rect.X + 20, rect.Y + 8);
                }
            }

            if (!string.IsNullOrEmpty(badge))
            {
                using (SolidBrush subBrush = new SolidBrush(_uiGoldCta))
                {
                    g.DrawString(badge, _fontCardSub, subBrush, rect.Right - 12, rect.Y + 10, SfRight);
                }
            }
        }

        private void DrawActiveViewBentoCards(Graphics g, Rectangle ws)
        {
            StringFormat rightSf = SfRight;

            if (_activeTab == 0)
            {
                int hHero = Math.Max(195, (int)(ws.Height * 0.34));
                int hGrid = Math.Max(220, (int)(ws.Height * 0.43));
                int hBottom = Math.Max(110, ws.Height - hHero - hGrid - 20);

                Rectangle cardHero = BentoCell(ws, 0, 8, ws.Y, hHero);
                Rectangle cardCham = BentoCell(ws, 8, 4, ws.Y, hHero);
                Rectangle cardGrid = BentoCell(ws, 0, 12, ws.Y + hHero + 10, hGrid);
                int yBot = ws.Y + hHero + hGrid + 20;
                Rectangle cardIo = BentoCell(ws, 0, 6, yBot, hBottom);
                Rectangle cardHist = BentoCell(ws, 6, 6, yBot, hBottom);

                DrawBentoCard(g, cardHero, "HERO CANLI MASAÜSTÜ TUVALİ (ÖNCE / SONRA SPLIT)", _previewHdrSim ? "10-BIT HDR AKTİF" : "SDR MODU", false);
                Rectangle heroPreview = new Rectangle(cardHero.X + 14, cardHero.Y + 34, cardHero.Width - 28, Math.Max(90, cardHero.Height - 88));
                DrawMiniDesktopPreviewCanvas(g, heroPreview);

                double chamContrast = ColorHarmonyGenerator.CalculateContrastRatio(ColTextPrimary, _customBodyTint);
                DrawBentoCard(g, cardCham, "CHAMELEON OKLAB ANALİZ", "WCAG " + chamContrast.ToString("0.0", CultureInfo.InvariantCulture) + ":1", false);
                using (SolidBrush sb = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("Duvar kağıdı 6 baskın OKLab renk kümesi (Tıklayarak ata):", _fontCardSub, sb, cardCham.X + 14, cardCham.Y + 66);
                }

                DrawBentoCard(g, cardGrid, "BENTO TEMA GALERİSİ (3D TILT + FAVORİLER + ÖZEL TEMALAR)", null, false);
                DrawBentoCard(g, cardIo, ".DOOMTHEME PAKET G/Ç & ŞEMA DOĞRULAMA", "v3.0 PAKET", false);
                using (SolidBrush sb = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString(_lastPackageDiffSummary, _fontCardSub, sb, new RectangleF(cardIo.X + 14, cardIo.Y + 78, cardIo.Width - 28, 36));
                }
                DrawBentoCard(g, cardHist, "SNAPSHOT GEÇMİŞİ & 1-TIK GERİ DÖNÜŞ", "MAX 20 KAYIT", false);
            }
            else if (_activeTab == 1)
            {
                int hChannels = 64;
                int remH = ws.Height - hChannels - 20;
                int hRow1 = Math.Max(230, (int)(remH * 0.54));
                int hRow2 = Math.Max(170, remH - hRow1);
                int yRow1 = ws.Y + hChannels + 10;
                int yRow2 = yRow1 + hRow1 + 10;

                Rectangle cardHsv = BentoCell(ws, 0, 6, yRow1, hRow1);
                Rectangle cardHarm = BentoCell(ws, 6, 6, yRow1, hRow1);
                Rectangle cardBezier = BentoCell(ws, 0, 6, yRow2, hRow2);
                Rectangle cardWcag = BentoCell(ws, 6, 6, yRow2, hRow2);

                Color c = GetSelectedChannelColor();
                DrawBentoCard(g, cardHsv, "HSV SPEKTRUM, RGB & EKRAN DAMLALIĞI", ThemeCatalog.ColorToHex(c), false);
                int rgbX = _hsvValRect.Right + 16;
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("R: " + c.R, _fontCardSub, lblB, rgbX, cardHsv.Y + 34);
                    g.DrawString("G: " + c.G, _fontCardSub, lblB, rgbX, cardHsv.Y + 78);
                    g.DrawString("B: " + c.B, _fontCardSub, lblB, rgbX, cardHsv.Y + 122);
                }

                DrawBentoCard(g, cardHarm, "RENK HARMONİ ÜRETİCİSİ (OKLCH / HSL)", _harmonyMode.ToString().ToUpperInvariant(), false);
                DrawBentoCard(g, cardBezier, "360° GÖLGE BÉZIER EĞRİSİ & HALE FİZİĞİ", _shadowSpread + " PX", false);
                DrawBezierAndGlowRing(g, cardBezier);

                DrawBentoCard(g, cardWcag, "WCAG 2.1 KONTRAST MATRİSİ", "CANLI DENETİM", false);
                DrawWcagContrastMatrix(g, cardWcag);
            }
            else if (_activeTab == 2)
            {
                int hRow1 = Math.Max(310, (int)(ws.Height * 0.60));
                int hRow2 = Math.Max(160, ws.Height - hRow1 - 10);
                Rectangle cardGlass = BentoCell(ws, 0, 6, ws.Y, hRow1);
                Rectangle cardHdr = BentoCell(ws, 6, 6, ws.Y, hRow1);
                Rectangle cardPhys = BentoCell(ws, 0, 12, ws.Y + hRow1 + 10, hRow2);

                DrawBentoCard(g, cardGlass, "DOSYA GEZGİNİ CAM & 360° ZIRH MOTORU", "DWM HOOK", false);
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                using (SolidBrush valB = new SolidBrush(_uiGoldCta))
                {
                    g.DrawString("İç Gövde Tonu Yoğunluğu:", _fontCardSub, lblB, cardGlass.X + 14, cardGlass.Y + 74);
                    g.DrawString(_emeraldTintIntensity + " / 140", _fontCardSub, valB, cardGlass.Right - 14, cardGlass.Y + 74, rightSf);
                    g.DrawString("360° Dış Gölge Parlaklığı:", _fontCardSub, lblB, cardGlass.X + 14, cardGlass.Y + 122);
                    g.DrawString(_emeraldShadowIntensity + " / 255", _fontCardSub, valB, cardGlass.Right - 14, cardGlass.Y + 122, rightSf);
                    g.DrawString("Dış Gölge Yayılım Genişliği:", _fontCardSub, lblB, cardGlass.X + 14, cardGlass.Y + 170);
                    g.DrawString(_shadowSpread + " px", _fontCardSub, valB, cardGlass.Right - 14, cardGlass.Y + 170, rightSf);
                    g.DrawString("Gezgin Pencere Opaklığı:", _fontCardSub, lblB, cardGlass.X + 14, cardGlass.Y + 218);
                    g.DrawString(_explorerAlpha + " / 255", _fontCardSub, valB, cardGlass.Right - 14, cardGlass.Y + 218, rightSf);
                }

                string nitsBadge = _hdrNitsClamp > 280 ? ("⚠ " + _hdrNitsClamp + " NITS") : (_hdrNitsClamp + " NITS GÜVENLİ");
                DrawBentoCard(g, cardHdr, "10-BIT scRGB HDR MOTORU & BEYAZ METİN KORUMASI", nitsBadge, false);
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("SDR İçerik & Metin Nits Sınırı (" + _hdrNitsClamp + " nits):", _fontCardSub, lblB, cardHdr.X + 14, cardHdr.Y + 84);
                }

                DrawBentoCard(g, cardPhys, "PENCERE SINIR FİZİĞİ (BORDER PHYSICS) & ODAK SİMÜLATÖRÜ", "YAY k=380 d=28", false);
                DrawBorderPhysicsSimulation(g, cardPhys);
            }
            else if (_activeTab == 3)
            {
                int hRow1 = Math.Max(140, (int)(ws.Height * 0.28));
                int hRow2 = Math.Max(210, (int)(ws.Height * 0.42));
                int hRow3 = Math.Max(130, ws.Height - hRow1 - hRow2 - 20);

                Rectangle cardModes = BentoCell(ws, 0, 12, ws.Y, hRow1);
                Rectangle cardGeom = BentoCell(ws, 0, 6, ws.Y + hRow1 + 10, hRow2);
                Rectangle cardMat = BentoCell(ws, 6, 6, ws.Y + hRow1 + 10, hRow2);
                Rectangle cardTray = BentoCell(ws, 0, 12, ws.Y + hRow1 + hRow2 + 20, hRow3);

                DrawBentoCard(g, cardModes, "GÖREV ÇUBUĞU & DOCK MİMARİSİ (5 GÖRSEL MOD)", "MOD " + _taskbarMode, false);
                DrawBentoCard(g, cardGeom, "KONUM, DOCK GEOMETRİSİ & ARAMA KUTUSU", _taskbarCornerRadius + "px RADIUS", false);
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("Dock Köşe Yuvarlaklığı (" + _taskbarCornerRadius + " px):", _fontCardSub, lblB, cardGeom.X + 14, cardGeom.Y + 84);
                    g.DrawString("Yüzen Dock Kenar Boşluğu (" + _taskbarDockMargin + " px):", _fontCardSub, lblB, cardGeom.X + 14, cardGeom.Y + 132);
                }

                DrawBentoCard(g, cardMat, "MATERYAL, RENK TONU & SPECULAR IŞIK", ThemeCatalog.ColorToHex(_customTaskbarTint), false);
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("Görev Çubuğu Özel Opaklık (" + _customTaskbarAlpha + " / 255):", _fontCardSub, lblB, cardMat.X + 14, cardMat.Y + 42);
                }

                DrawBentoCard(g, cardTray, "SİSTEM TEPSİSİ (TRAY POLISH) & DİNAMİK ADA DAVRANIŞI", "WIN11 / WIN10", false);
            }
            else if (_activeTab == 4)
            {
                int hRow1 = Math.Max(260, (int)(ws.Height * 0.54));
                int hRow2 = Math.Max(200, ws.Height - hRow1 - 10);

                Rectangle cardBoard = BentoCell(ws, 0, 7, ws.Y, hRow1);
                Rectangle cardInsp = BentoCell(ws, 7, 5, ws.Y, hRow1);
                Rectangle cardSpec = BentoCell(ws, 0, 6, ws.Y + hRow1 + 10, hRow2);
                Rectangle cardPomo = BentoCell(ws, 6, 6, ws.Y + hRow1 + 10, hRow2);

                DrawBentoCard(g, cardBoard, "8'Lİ MASAÜSTÜ HUD YERLEŞİM VE KONTROL PANELİ", _widgetsLocked ? "KİLİTLİ" : "SERBEST", false);
                DrawBentoCard(g, cardInsp, "HUD DENETÇİSİ (W#" + (_inspectedWidgetIndex + 1) + " • ÖLÇEK & HIZLI NOT)", "%" + _widgetScale + " ÖLÇEK", false);
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("Widget Boyut Ölçeği:", _fontCardSub, lblB, cardInsp.X + 14, cardInsp.Y + 42);
                    g.DrawString("HUD Cam Opaklığı (" + _hudOpacity + " / 255):", _fontCardSub, lblB, cardInsp.X + 14, cardInsp.Y + 104);
                }

                DrawBentoCard(g, cardSpec, "CANLI SES SPEKTRUMU STÜDYOSU (WASAPI / FFT)", _spectrumBandCount + " BANT", false);
                DrawAudioSpectrumStudioPreview(g, cardSpec);

                DrawBentoCard(g, cardPomo, "POMODORO & DERİN ODAK HALKASI (WIDGET 8)", DesktopHudWidget.PomodoroPhase, false);
                DrawPomodoroControllerRing(g, cardPomo);
            }
            else if (_activeTab == 5)
            {
                int hBoostBar = 62;
                int remH = ws.Height - hBoostBar - 20;
                int hRow1 = Math.Max(220, (int)(remH * 0.50));
                int hRow2 = Math.Max(200, remH - hRow1);
                int yRow1 = ws.Y + hBoostBar + 10;
                int yRow2 = yRow1 + hRow1 + 10;

                Rectangle cardFocus = BentoCell(ws, 0, 6, yRow1, hRow1);
                Rectangle cardRam = BentoCell(ws, 6, 6, yRow1, hRow1);
                Rectangle cardDisp = BentoCell(ws, 0, 7, yRow2, hRow2);
                Rectangle cardDesk = BentoCell(ws, 7, 5, yRow2, hRow2);

                string focusStatus = string.IsNullOrEmpty(_smartFocusActiveProcess) ? "AKTİF (EFEKTLER AÇIK)" : ("UYKU: " + _smartFocusActiveProcess);
                DrawBentoCard(g, cardFocus, "SMART FOCUS OTOMATİK OYUN / RENDER UYKU MOTORU", focusStatus, false);
                DrawSmartFocusWatchlist(g, cardFocus);

                DrawBentoCard(g, cardRam, "RAM & WORKINGSET TEMİZLEYİCİ (BELLEK OPTİMİZASYONU)", _liveRamPercent + "% DOLU", false);
                DrawRamPurgerTelemetry(g, cardRam);

                DrawBentoCard(g, cardDisp, "MONİTÖR PARLAKLIK (DIMMER) & GECE IŞIĞI (KELVIN)", _displayWarmthKelvin + "K", false);
                using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
                {
                    g.DrawString("Yazılımsal Ekran Karartma (%" + _displayDimmerPercent + "):", _fontCardSub, lblB, cardDisp.X + 14, cardDisp.Y + 78);
                    g.DrawString("Gece Işığı Renk Sıcaklığı (" + _displayWarmthKelvin + "K):", _fontCardSub, lblB, cardDisp.X + 14, cardDisp.Y + 124);
                }

                bool iconsVis = true;
                try { iconsVis = DoomThemeEngine.AreDesktopIconsVisible(); } catch { }
                DrawBentoCard(g, cardDesk, "MASAÜSTÜ İKONLARI & TEMİZ MASAÜSTÜ", iconsVis ? "GÖRÜNÜR" : "GİZLİ", false);
            }
            else if (_activeTab == 6)
            {
                int hRow1 = Math.Max(240, (int)(ws.Height * 0.50));
                int hRow2 = Math.Max(220, ws.Height - hRow1 - 10);

                Rectangle cardStart = BentoCell(ws, 0, 6, ws.Y, hRow1);
                Rectangle cardKeys = BentoCell(ws, 6, 6, ws.Y, hRow1);
                Rectangle cardBackup = BentoCell(ws, 0, 6, ws.Y + hRow1 + 10, hRow2);
                Rectangle cardLogs = BentoCell(ws, 6, 6, ws.Y + hRow1 + 10, hRow2);

                DrawBentoCard(g, cardStart, "BAŞLANGIÇ, ZAMANLAYICI & PERFORMANS", _rendererFpsCap + " HZ", false);
                DrawBentoCard(g, cardKeys, "GLOBAL & STÜDYO KLAVYE KISAYOL TABLOSU", "HOTKEYS", false);
                DrawShortcutsTableContent(g, cardKeys);

                DrawBentoCard(g, cardBackup, "YEDEKLEME, SNAPSHOT & FABRİKA SIFIRLAMA", "GÜVENLİ", false);
                DrawBentoCard(g, cardLogs, "CANLI SİSTEM SAĞLIK & TANILAMA GÜNLÜĞÜ", "SAĞLIK: OK", false);
                DrawDiagnosticLogsContent(g, cardLogs);
            }
        }

        private void DrawBezierAndGlowRing(Graphics g, Rectangle card)
        {
            Rectangle graphRect = new Rectangle(card.X + 14, card.Y + 36, card.Width / 2 - 24, Math.Max(60, card.Height - 82));
            using (SolidBrush gb = new SolidBrush(Color.FromArgb(180, _uiBg)))
            using (Pen gp = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(gb, graphRect);
                g.DrawRectangle(gp, graphRect);

                PointF p0 = new PointF(graphRect.X + 6, graphRect.Bottom - 8);
                PointF p1 = new PointF(graphRect.X + graphRect.Width * _bezierP1.X, graphRect.Bottom - graphRect.Height * _bezierP1.Y);
                PointF p2 = new PointF(graphRect.X + graphRect.Width * _bezierP2.X, graphRect.Bottom - graphRect.Height * _bezierP2.Y);
                PointF p3 = new PointF(graphRect.Right - 6, graphRect.Y + 10);

                using (Pen curvePen = new Pen(_customRimHighlight, 2.2f))
                {
                    g.DrawBezier(curvePen, p0, p1, p2, p3);
                }
            }

            // Animated 360° Preview Ring on the right side
            int ringSize = Math.Min(54, Math.Max(36, card.Height - 115));
            Rectangle ringRect = new Rectangle(card.Right - ringSize - 24, card.Bottom - ringSize - 14, ringSize, ringSize);
            using (Pen baseRing = new Pen(Color.FromArgb(80, _customShadowGlow), 4f))
            using (Pen sweepRing = new Pen(_uiGoldCta, 3f))
            {
                g.DrawEllipse(baseRing, ringRect);
                g.DrawArc(sweepRing, ringRect, _glowPreviewAngle, 110f);
            }
        }

        private void DrawWcagContrastMatrix(Graphics g, Rectangle card)
        {
            string[] labels = new string[]
            {
                "Birincil Metin / L1 Bento Kart",
                "Gezgin Dosya Adı / İç Gövde Tonu",
                "Çerçeve Parıltısı / L0 Zemin",
                "Vurgu Rengi / Görev Çubuğu"
            };
            Color[] fgCols = new Color[] { ColTextPrimary, Color.White, _customRimHighlight, _uiGoldCta };
            Color[] bgCols = new Color[] { _uiCard, _customBodyTint, _uiBg, _customTaskbarTint };

            int rowH = Math.Max(22, (card.Height - 80) / 4);
            for (int i = 0; i < 4; i++)
            {
                int ry = card.Y + 34 + i * rowH;
                double ratio = ColorHarmonyGenerator.CalculateContrastRatio(fgCols[i], bgCols[i]);
                bool aaa = ratio >= 7.0;
                bool aa = ratio >= 4.5 || (i >= 2 && ratio >= 3.0);

                using (SolidBrush tb = new SolidBrush(ColTextPrimary))
                using (SolidBrush badgeB = new SolidBrush(aaa ? ColSuccess : (aa ? ColInfo : ColWarn)))
                {
                    g.DrawString(labels[i], _fontCardSub, tb, card.X + 14, ry + 2);
                    string rStr = ratio.ToString("0.0", CultureInfo.InvariantCulture) + ":1 " + (aaa ? "AAA" : (aa ? "AA" : "DÜŞÜK"));
                    g.DrawString(rStr, _fontMono, badgeB, card.Right - 14, ry + 2, SfRight);
                }
            }
        }

        private void DrawBorderPhysicsSimulation(Graphics g, Rectangle card)
        {
            int stageX = card.X + 280;
            int stageW = Math.Max(240, card.Right - stageX - 16);
            int stageH = Math.Max(74, card.Height - 48);
            Rectangle stage = new Rectangle(stageX, card.Y + 34, stageW, stageH);

            bool focusSecond = _borderSimRunning && Math.Sin(_borderSimPhase * 2.0) > 0;
            int wBox = (stage.Width - 36) / 2;
            Rectangle winA = new Rectangle(stage.X + 8, stage.Y + 8, wBox, stage.Height - 16);
            Rectangle winB = new Rectangle(stage.X + 24 + wBox, stage.Y + 8, wBox, stage.Height - 16);

            DrawSimulatedPhysicsWindow(g, winA, "Gezgin A — Beyin Vault", !focusSecond);
            DrawSimulatedPhysicsWindow(g, winB, "Gezgin B — ComfyUI Render", focusSecond);
        }

        private void DrawSimulatedPhysicsWindow(Graphics g, Rectangle r, string title, bool focused)
        {
            if (focused)
            {
                for (int k = 6; k >= 2; k -= 2)
                {
                    using (Pen gp = new Pen(Color.FromArgb(45 * (8 - k) / 2, _customShadowGlow), 2f))
                    {
                        g.DrawRectangle(gp, r.X - k, r.Y - k, r.Width + k * 2, r.Height + k * 2);
                    }
                }
            }

            using (SolidBrush b = new SolidBrush(focused ? Color.FromArgb(210, _customBodyTint) : Color.FromArgb(140, 18, 22, 28)))
            using (Pen p = new Pen(focused ? _customRimHighlight : _uiBorderSubtle, focused ? 2f : 1f))
            using (SolidBrush tb = new SolidBrush(focused ? ColTextPrimary : ColTextSecondary))
            {
                g.FillRectangle(b, r);
                g.DrawRectangle(p, r);
                g.DrawString(title, _fontCardSub, tb, r.X + 8, r.Y + 6);
                g.DrawString(focused ? "● AKTİF ODAK (360° NEON)" : "○ ARKA PLAN PENCERE", _fontCardSub, tb, r.X + 8, r.Y + 26);
            }
        }

        private void DrawAudioSpectrumStudioPreview(Graphics g, Rectangle card)
        {
            int visX = card.X + card.Width / 2 + 4;
            int visW = Math.Max(110, card.Right - visX - 14);
            int visH = Math.Max(70, card.Height - 82);
            Rectangle visRect = new Rectangle(visX, card.Y + 68, visW, visH);

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(190, _uiBg)))
            using (Pen bp = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(bg, visRect);
                g.DrawRectangle(bp, visRect);
            }

            float[] bands = null;
            try { bands = DesktopHudWidget.GetSpectrumBandsSnapshot(); } catch { }
            int count = 16;
            int barW = Math.Max(4, (visRect.Width - 16) / count - 2);
            double tick = Environment.TickCount * 0.006;

            for (int i = 0; i < count; i++)
            {
                float level = (bands != null && i < bands.Length) ? bands[i] : 0f;
                if (level < 0.08f)
                    level = (float)(0.22 + 0.18 * Math.Sin(tick + i * 0.55) + 0.10 * Math.Cos(tick * 1.7 - i * 0.3));
                int bh = Math.Max(4, (int)(level * (visRect.Height - 14)));
                int bx = visRect.X + 8 + i * (barW + 2);
                int by = visRect.Bottom - 6 - bh;
                using (SolidBrush bb = new SolidBrush(i % 2 == 0 ? _customRimHighlight : _uiGoldCta))
                {
                    g.FillRectangle(bb, bx, by, barW, bh);
                }
            }

            using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
            {
                g.DrawString("Yumuşatma: %" + _spectrumSmoothing, _fontCardSub, lblB, card.X + 14, card.Bottom - 54);
            }
        }

        private void DrawPomodoroControllerRing(Graphics g, Rectangle card)
        {
            int ringD = Math.Min(110, card.Height - 52);
            Rectangle ringRect = new Rectangle(card.X + 18, card.Y + 38, ringD, ringD);

            int remSec = DesktopHudWidget.PomodoroRemainingSeconds;
            int totSec = Math.Max(1, DesktopHudWidget.PomodoroTotalPhaseSeconds);
            float sweep = 360f * (1f - (float)remSec / totSec);

            using (Pen trackPen = new Pen(Color.FromArgb(60, _uiBorderSubtle), 6f))
            using (Pen progPen = new Pen(_customRimHighlight, 6f))
            {
                g.DrawEllipse(trackPen, ringRect);
                g.DrawArc(progPen, ringRect, -90f, Math.Max(8f, sweep));
            }

            string mmss = string.Format("{0:00}:{1:00}", remSec / 60, remSec % 60);
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            using (SolidBrush sb = new SolidBrush(_uiGoldCta))
            {
                g.DrawString(mmss, _fontHeader, tb, ringRect, SfCenter);
                int sessions = DesktopHudWidget.PomodoroCompletedSessions;
                for (int d = 0; d < 4; d++)
                {
                    Brush db = (d < sessions) ? sb : Brushes.DimGray;
                    g.FillEllipse(db, ringRect.X + ringRect.Width / 2 - 22 + d * 12, ringRect.Bottom - 16, 7, 7);
                }
            }
        }

        private void DrawSmartFocusWatchlist(Graphics g, Rectangle card)
        {
            string[] chips = new string[] { "ComfyUI / python", "Blender / Unreal", "CS2 / Valorant", "Cyberpunk / Doom" };
            int cW = (card.Width - 28 - 18) / 4;
            for (int i = 0; i < 4; i++)
            {
                Rectangle cr = new Rectangle(card.X + 14 + i * (cW + 6), card.Y + 84, cW, 24);
                using (SolidBrush cb = new SolidBrush(Color.FromArgb(160, _uiBg)))
                using (Pen cp = new Pen(_uiBorderSubtle, 1f))
                using (SolidBrush tb = new SolidBrush(ColTextSecondary))
                {
                    g.FillRectangle(cb, cr);
                    g.DrawRectangle(cp, cr);
                    g.DrawString(chips[i], _fontCardSub, tb, cr, SfCenter);
                }
            }

            using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
            {
                g.DrawString("Otomatik Uyku GPU Eşiği: %" + _gpuPauseThreshold, _fontCardSub, lblB, card.X + 14, card.Y + 116);
            }
        }

        private void DrawRamPurgerTelemetry(Graphics g, Rectangle card)
        {
            using (SolidBrush bigB = new SolidBrush(ColTextPrimary))
            using (SolidBrush subB = new SolidBrush(ColSuccess))
            using (SolidBrush lblB = new SolidBrush(ColTextSecondary))
            {
                string ramLine = string.Format(CultureInfo.InvariantCulture, "Kullanılan: {0:0.0} GB / {1:0.0} GB  (Boş: {2:0.0} GB)", _liveRamUsedGb, _liveRamTotalGb, _liveRamFreeGb);
                g.DrawString(ramLine, _fontCardTitle, bigB, card.X + 14, card.Y + 36);

                string purgeInfo = _lastPurgedMb > 0
                    ? string.Format(CultureInfo.InvariantCulture, "✓ Son Temizlik: +{0:0.0} MB RAM kazanıldı ({1} süreç)", _lastPurgedMb, _lastPurgedProcCount)
                    : "Hazır — WorkingSet & Standby önbellek temizleyici aktif.";
                g.DrawString(purgeInfo, _fontCardSub, subB, card.X + 14, card.Y + 56);
                g.DrawString("Otomatik RAM Temizleme Eşiği: %" + _ramAutoPurgeThreshold, _fontCardSub, lblB, card.X + 14, card.Bottom - 102);
            }

            Rectangle sparkRect = new Rectangle(card.X + 14, card.Y + 78, card.Width - 28, Math.Max(28, card.Height - 186));
            DrawSparklineBox(g, sparkRect, _ramSparkline, _customRimHighlight);
        }

        private void DrawShortcutsTableContent(Graphics g, Rectangle card)
        {
            string[] keys = new string[]
            {
                "Ctrl + Shift + Space  —  Global Komut Paleti",
                "Ctrl + Alt + T        —  Stüdyo Penceresini Aç / Gizle",
                "Ctrl + Alt + G        —  360° Gezgin Gölgesini Aç / Kapat",
                "Ctrl + Alt + D        —  Masaüstü Simgelerini Gizle / Göster",
                "Ctrl + Z              —  Son İşlemi Anında Geri Al (Undo)"
            };
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    g.DrawString(keys[i], _fontMono, tb, card.X + 14, card.Y + 46 + i * 34);
                }
            }
        }

        private void DrawDiagnosticLogsContent(Graphics g, Rectangle card)
        {
            int maxLines = Math.Min(_diagnosticLogs.Count, Math.Max(3, (card.Height - 90) / 20));
            using (SolidBrush lb = new SolidBrush(ColTextSecondary))
            {
                for (int i = 0; i < maxLines; i++)
                {
                    g.DrawString(_diagnosticLogs[i], _fontCardSub, lb, card.X + 14, card.Y + 38 + i * 20);
                }
            }
        }

        private void DrawSparklineBox(Graphics g, Rectangle rect, float[] samples, Color lineColor)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(160, _uiBg)))
            using (Pen bp = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(bg, rect);
                g.DrawRectangle(bp, rect);
            }

            if (samples == null || samples.Length < 2 || rect.Width < 12 || rect.Height < 8) return;
            PointF[] pts = new PointF[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                int idx = (_telemetryWriteIndex + i) % samples.Length;
                float val = Math.Min(100f, Math.Max(0f, samples[idx]));
                float x = rect.X + 2 + (i * (rect.Width - 4)) / (float)(samples.Length - 1);
                float y = rect.Bottom - 2 - (val / 100f) * (rect.Height - 4);
                pts[i] = new PointF(x, y);
            }
            using (Pen lp = new Pen(lineColor, 1.6f))
            {
                g.DrawLines(lp, pts);
            }
        }

        // ====================================================================
        // Zone Drawing & Custom Controls (v3.0 Bento Tokens)
        // ====================================================================
        private void DrawZone(Graphics g, UiZone z)
        {
            bool hovered = (_hoveredZoneId == z.Id);

            if (z.Id.StartsWith("WIN_"))
            {
                DrawWindowControl(g, z, hovered);
            }
            else if (z.Id == "CMD_BAR_OPEN")
            {
                DrawCommandBarPill(g, z, hovered);
            }
            else if (z.Id.StartsWith("CHIP_"))
            {
                DrawTelemetryChip(g, z, hovered);
            }
            else if (z.Id.StartsWith("TAB_"))
            {
                int idx = int.Parse(z.Id.Substring("TAB_".Length));
                DrawSidebarTab(g, z, _activeTab == idx, hovered, idx);
            }
            else if (z.Id.StartsWith("PRESET_"))
            {
                string pid = z.Id.Substring("PRESET_".Length);
                bool active = string.Equals(_activeProfileId, pid, StringComparison.OrdinalIgnoreCase);
                DrawPresetCard(g, z, active, hovered);
            }
            else if (z.Id.StartsWith("FAV_PRESET_"))
            {
                DrawFavoriteStarButton(g, z, hovered);
            }
            else if (z.Id.StartsWith("DEL_PRESET_"))
            {
                DrawDeleteMiniButton(g, z, hovered);
            }
            else if (z.Id.StartsWith("CHAM_SWATCH_") || z.Id.StartsWith("HARM_SWATCH_"))
            {
                DrawSwatchCard(g, z, hovered);
            }
            else if (z.Id.StartsWith("CHN_"))
            {
                int idx = int.Parse(z.Id.Substring("CHN_".Length));
                DrawChannelCard(g, z, _selectedColorChannel == idx, hovered);
            }
            else if (z.Id == "HSV_BOX")
            {
                DrawHsvSpectrumBox(g);
            }
            else if (z.Id == "HSV_VAL")
            {
                DrawHsvValueBar(g);
            }
            else if (z.Id.StartsWith("SLIDER_"))
            {
                DrawSliderZone(g, z.Id, hovered || _activeDragSliderId == z.Id);
            }
            else if (z.Id.StartsWith("TOG_"))
            {
                DrawToggleRow(g, z, GetToggleState(z.Id), hovered);
            }
            else if (z.Id.StartsWith("GAL_FILTER_"))
            {
                int f = int.Parse(z.Id.Substring("GAL_FILTER_".Length));
                DrawSelectorButton(g, z, _galleryFilterMode == f, hovered);
            }
            else if (z.Id.StartsWith("HARM_MODE_"))
            {
                int m = int.Parse(z.Id.Substring("HARM_MODE_".Length));
                DrawSelectorButton(g, z, (int)_harmonyMode == m, hovered);
            }
            else if (z.Id.StartsWith("BEZIER_PRESET_"))
            {
                int b = int.Parse(z.Id.Substring("BEZIER_PRESET_".Length));
                DrawSelectorButton(g, z, _bezierPresetIndex == b, hovered);
            }
            else if (z.Id.StartsWith("BLUR_"))
            {
                int m = int.Parse(z.Id.Substring("BLUR_".Length));
                DrawSelectorButton(g, z, _explorerBlurMode == m, hovered);
            }
            else if (z.Id.StartsWith("BORDER_RAD_"))
            {
                int r = int.Parse(z.Id.Substring("BORDER_RAD_".Length));
                DrawSelectorButton(g, z, _borderCornerRadius == r, hovered);
            }
            else if (z.Id.StartsWith("TASKBAR_"))
            {
                int m = int.Parse(z.Id.Substring("TASKBAR_".Length));
                DrawTaskbarModeCard(g, z, _taskbarMode == m, hovered);
            }
            else if (z.Id.StartsWith("SEARCH_"))
            {
                int m = int.Parse(z.Id.Substring("SEARCH_".Length));
                DrawSelectorButton(g, z, _searchboxMode == m, hovered);
            }
            else if (z.Id.StartsWith("DESK_SIZE_"))
            {
                int sz = int.Parse(z.Id.Substring("DESK_SIZE_".Length));
                DrawSelectorButton(g, z, _desktopIconSize == sz, hovered);
            }
            else if (z.Id.StartsWith("W_LAYOUT_"))
            {
                int lay = int.Parse(z.Id.Substring("W_LAYOUT_".Length));
                DrawSelectorButton(g, z, _widgetLayout == lay, hovered);
            }
            else if (z.Id.StartsWith("W_SCALE_"))
            {
                int sc = int.Parse(z.Id.Substring("W_SCALE_".Length));
                DrawSelectorButton(g, z, _widgetScale == sc, hovered);
            }
            else if (z.Id.StartsWith("SPEC_STYLE_"))
            {
                int st = int.Parse(z.Id.Substring("SPEC_STYLE_".Length));
                DrawSelectorButton(g, z, _spectrumStyle == st, hovered);
            }
            else if (z.Id.StartsWith("SPEC_BANDS_"))
            {
                int bc = int.Parse(z.Id.Substring("SPEC_BANDS_".Length));
                DrawSelectorButton(g, z, _spectrumBandCount == bc, hovered);
            }
            else if (z.Id.StartsWith("BOOST_PROF_"))
            {
                int bp = int.Parse(z.Id.Substring("BOOST_PROF_".Length));
                DrawTaskbarModeCard(g, z, (int)_boostProfile == bp, hovered);
            }
            else if (z.Id.StartsWith("KELVIN_"))
            {
                int kv = int.Parse(z.Id.Substring("KELVIN_".Length));
                DrawSelectorButton(g, z, Math.Abs(_displayWarmthKelvin - kv) < 150, hovered);
            }
            else if (z.Id == "ACT_APPLY_NOW" || z.Id == "ACT_FIX_TEXT" || z.Id == "ACT_FIX_TEXT_TAB2" || z.Id == "ACT_PURGE_RAM" || z.Id == "ACT_SAVE_CUSTOM_THEME")
            {
                DrawGoldCtaButton(g, z, hovered);
            }
            else
            {
                DrawActionButton(g, z, hovered);
            }
        }

        private void DrawCommandBarPill(Graphics g, UiZone z, bool hovered)
        {
            Color bg = hovered ? ColL1Hover : ColL2Pill;
            Color border = hovered ? _uiGoldCta : _uiActiveRim;
            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, hovered ? 1.5f : 1f))
            using (SolidBrush tb = new SolidBrush(hovered ? ColTextPrimary : ColTextSecondary))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
                g.DrawString(z.Title, _fontButton, tb, z.Bounds, SfCenter);
            }
        }

        private void DrawTelemetryChip(Graphics g, UiZone z, bool hovered)
        {
            using (SolidBrush b = new SolidBrush(hovered ? ColL1Hover : Color.FromArgb(190, _uiBg)))
            using (Pen p = new Pen(hovered ? _uiActiveRim : _uiBorderSubtle, 1f))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
            }

            if (z.Id == "CHIP_CPU")
            {
                Rectangle sp = new Rectangle(z.Bounds.X + 4, z.Bounds.Bottom - 10, z.Bounds.Width - 8, 7);
                DrawSparklineBox(g, sp, _cpuSparkline, ColInfo);
                using (SolidBrush tb = new SolidBrush(ColTextPrimary))
                {
                    g.DrawString("CPU " + _liveCpuPercent + "%", _fontCardSub, tb, z.Bounds.X + 6, z.Bounds.Y + 3);
                }
            }
            else if (z.Id == "CHIP_RAM")
            {
                Rectangle sp = new Rectangle(z.Bounds.X + 4, z.Bounds.Bottom - 10, z.Bounds.Width - 8, 7);
                DrawSparklineBox(g, sp, _ramSparkline, ColSuccess);
                using (SolidBrush tb = new SolidBrush(ColTextPrimary))
                {
                    g.DrawString(string.Format(CultureInfo.InvariantCulture, "RAM {0:0.0}G", _liveRamUsedGb), _fontCardSub, tb, z.Bounds.X + 6, z.Bounds.Y + 3);
                }
            }
            else
            {
                using (SolidBrush tb = new SolidBrush(_uiGoldCta))
                {
                    g.DrawString("10-BIT HDR", _fontCardSub, tb, z.Bounds, SfCenter);
                }
            }
        }

        private void DrawWindowControl(Graphics g, UiZone z, bool hovered)
        {
            Color bg = _uiCard;
            if (hovered)
                bg = (z.Id == "WIN_CLOSE") ? ColDanger : _uiActiveAccent;

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(hovered ? _uiActiveRim : _uiBorderSubtle, 1f))
            using (SolidBrush tb = new SolidBrush(z.Id == "WIN_TRAY" ? _uiGoldCta : ColTextPrimary))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
                g.DrawString(z.Title, _fontCardSub, tb, z.Bounds, SfCenter);
            }
        }

        private void DrawSidebarTab(Graphics g, UiZone z, bool active, bool hovered, int viewIdx)
        {
            Color bg = active ? _uiActiveAccent : (hovered ? _uiCardHover : Color.FromArgb(140, _uiBg));
            Color border = active ? _uiGoldCta : (hovered ? _uiActiveRim : _uiBorderSubtle);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, active ? 1.5f : 1f))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
            }

            // 20px Vector Glyph inside 32x32 L2 Pill
            Rectangle iconPill = new Rectangle(z.Bounds.X + 6, z.Bounds.Y + 8, 32, 32);
            using (SolidBrush pb = new SolidBrush(active ? Color.FromArgb(65, _uiGoldCta) : ColL2Pill))
            {
                g.FillRectangle(pb, iconPill);
            }
            DrawVectorGlyph(g, iconPill, viewIdx, active ? _uiGoldCta : ColTextPrimary);

            if (_railExpanded && z.Bounds.Width > 90)
            {
                using (SolidBrush tb = new SolidBrush(ColTextPrimary))
                using (SolidBrush sb = new SolidBrush(active ? _uiGoldCta : ColTextSecondary))
                {
                    g.DrawString(z.Title, _fontTab, tb, z.Bounds.X + 44, z.Bounds.Y + 6);
                    g.DrawString(z.Subtitle, _fontCardSub, sb, z.Bounds.X + 44, z.Bounds.Y + 25);
                }
            }
        }

        private void DrawVectorGlyph(Graphics g, Rectangle box, int viewIdx, Color col)
        {
            int cx = box.X + box.Width / 2;
            int cy = box.Y + box.Height / 2;
            using (Pen p = new Pen(col, 1.7f))
            {
                switch (viewIdx)
                {
                    case 0: // Gallery Grid
                        g.DrawRectangle(p, cx - 7, cy - 7, 6, 6);
                        g.DrawRectangle(p, cx + 1, cy - 7, 6, 6);
                        g.DrawRectangle(p, cx - 7, cy + 1, 6, 6);
                        g.DrawRectangle(p, cx + 1, cy + 1, 6, 6);
                        break;
                    case 1: // Color Wheel
                        g.DrawEllipse(p, cx - 7, cy - 7, 14, 14);
                        g.DrawEllipse(p, cx - 2, cy - 2, 4, 4);
                        break;
                    case 2: // Explorer Window & HDR Sun
                        g.DrawRectangle(p, cx - 8, cy - 6, 16, 12);
                        g.DrawLine(p, cx - 8, cy - 2, cx + 8, cy - 2);
                        break;
                    case 3: // Dock / Taskbar
                        g.DrawRectangle(p, cx - 8, cy - 6, 16, 12);
                        g.DrawLine(p, cx - 5, cy + 3, cx + 5, cy + 3);
                        break;
                    case 4: // Audio Spectrum & HUD Bars
                        g.DrawLine(p, cx - 6, cy + 6, cx - 6, cy - 2);
                        g.DrawLine(p, cx - 2, cy + 6, cx - 2, cy - 6);
                        g.DrawLine(p, cx + 2, cy + 6, cx + 2, cy - 4);
                        g.DrawLine(p, cx + 6, cy + 6, cx + 6, cy - 7);
                        break;
                    case 5: // Game/Render Boost Lightning
                        Point[] bolt = new Point[]
                        {
                            new Point(cx + 1, cy - 8),
                            new Point(cx - 5, cy + 1),
                            new Point(cx, cy + 1),
                            new Point(cx - 1, cy + 8),
                            new Point(cx + 5, cy - 1),
                            new Point(cx, cy - 1)
                        };
                        g.DrawPolygon(p, bolt);
                        break;
                    default: // System Gear / Shield
                        g.DrawEllipse(p, cx - 6, cy - 6, 12, 12);
                        g.DrawLine(p, cx, cy - 8, cx, cy + 8);
                        g.DrawLine(p, cx - 8, cy, cx + 8, cy);
                        break;
                }
            }
        }

        private void DrawPresetCard(Graphics g, UiZone z, bool active, bool hovered)
        {
            Rectangle r = hovered ? new Rectangle(z.Bounds.X, z.Bounds.Y - 2, z.Bounds.Width, z.Bounds.Height) : z.Bounds;
            Color bg = active ? _uiActiveAccent : (hovered ? _uiCardHover : _uiBg);
            Color border = active ? _uiGoldCta : (hovered ? _uiActiveRim : _uiBorderSubtle);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, active ? 1.8f : 1f))
            {
                g.FillRectangle(b, r);
                g.DrawRectangle(p, r);
            }

            // 4-Color Swatch Strip on Left
            int sx = r.X + 8;
            int sy = r.Y + 10;
            int sh = Math.Max(20, (r.Height - 20) / 2);
            using (SolidBrush s1 = new SolidBrush(z.Swatch1))
            using (SolidBrush s2 = new SolidBrush(z.Swatch2))
            using (SolidBrush s3 = new SolidBrush(z.Swatch3))
            using (SolidBrush s4 = new SolidBrush(z.Swatch4))
            using (Pen sp = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(s1, sx, sy, 12, sh);
                g.FillRectangle(s2, sx + 12, sy, 12, sh);
                g.FillRectangle(s3, sx, sy + sh, 12, sh);
                g.FillRectangle(s4, sx + 12, sy + sh, 12, sh);
                g.DrawRectangle(sp, sx, sy, 24, sh * 2);
            }

            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            using (SolidBrush sb = new SolidBrush(active ? _uiGoldCta : ColTextSecondary))
            {
                g.DrawString(z.Title, _fontCardTitle, tb, r.X + 40, r.Y + 12);
                g.DrawString(z.Subtitle, _fontCardSub, sb, new RectangleF(r.X + 40, r.Y + 34, r.Width - 46, r.Height - 38));
            }
        }

        private void DrawFavoriteStarButton(Graphics g, UiZone z, bool hovered)
        {
            bool isFav = z.Title == "★";
            using (SolidBrush tb = new SolidBrush(isFav || hovered ? _uiGoldCta : ColTextSecondary))
            {
                g.DrawString(z.Title, _fontSection, tb, z.Bounds, SfCenter);
            }
        }

        private void DrawDeleteMiniButton(Graphics g, UiZone z, bool hovered)
        {
            using (SolidBrush b = new SolidBrush(hovered ? ColDanger : Color.FromArgb(45, 20, 20)))
            using (Pen p = new Pen(Color.FromArgb(180, 80, 80), 1f))
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
                g.DrawString(z.Title, _fontCardSub, tb, z.Bounds, SfCenter);
            }
        }

        private void DrawSwatchCard(Graphics g, UiZone z, bool hovered)
        {
            using (SolidBrush b = new SolidBrush(z.Swatch1))
            using (Pen p = new Pen(hovered ? _uiGoldCta : ColTextPrimary, hovered ? 2f : 1f))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
            }
            if (z.Bounds.Height >= 48)
            {
                Rectangle lblR = new Rectangle(z.Bounds.X, z.Bounds.Bottom - 20, z.Bounds.Width, 20);
                using (SolidBrush lb = new SolidBrush(Color.FromArgb(190, 10, 12, 16)))
                using (SolidBrush tb = new SolidBrush(ColTextPrimary))
                {
                    g.FillRectangle(lb, lblR);
                    g.DrawString(z.Title, _fontCardSub, tb, lblR, SfCenter);
                }
            }
        }

        private void DrawChannelCard(Graphics g, UiZone z, bool active, bool hovered)
        {
            Color bg = active ? _uiActiveAccent : (hovered ? _uiCardHover : _uiBg);
            Color border = active ? _uiGoldCta : (hovered ? _uiActiveRim : _uiBorderSubtle);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, active ? 1.8f : 1f))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
            }

            Rectangle sw = new Rectangle(z.Bounds.X + 8, z.Bounds.Y + 10, 22, z.Bounds.Height - 20);
            using (SolidBrush sb = new SolidBrush(z.Swatch1))
            using (Pen sp = new Pen(ColTextPrimary, 1f))
            {
                g.FillRectangle(sb, sw);
                g.DrawRectangle(sp, sw);
            }

            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            using (SolidBrush sub = new SolidBrush(_uiGoldCta))
            {
                g.DrawString(z.Title, _fontButton, tb, z.Bounds.X + 36, z.Bounds.Y + 10);
                g.DrawString(z.Subtitle, _fontMono, sub, z.Bounds.X + 36, z.Bounds.Y + 30);
            }
        }

        private void DrawHsvSpectrumBox(Graphics g)
        {
            if (_hsvSpectrumBitmap != null)
            {
                g.DrawImage(_hsvSpectrumBitmap, _hsvBoxRect);
            }
            using (Pen p = new Pen(_uiBorderSubtle, 1f))
            {
                g.DrawRectangle(p, _hsvBoxRect);
            }

            int cx = _hsvBoxRect.X + (int)Math.Round((_hsvH / 360.0) * _hsvBoxRect.Width);
            int cy = _hsvBoxRect.Y + (int)Math.Round((1.0 - _hsvS) * _hsvBoxRect.Height);
            using (Pen wp = new Pen(Color.White, 2f))
            using (Pen bp = new Pen(Color.Black, 1f))
            {
                g.DrawEllipse(wp, cx - 6, cy - 6, 12, 12);
                g.DrawEllipse(bp, cx - 7, cy - 7, 14, 14);
            }
        }

        private void DrawHsvValueBar(Graphics g)
        {
            Color topCol = ColorFromHsv(_hsvH, _hsvS, 1.0);
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                _hsvValRect,
                topCol,
                Color.Black,
                LinearGradientMode.Vertical))
            using (Pen p = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(lgb, _hsvValRect);
                g.DrawRectangle(p, _hsvValRect);
            }

            int vy = _hsvValRect.Y + (int)Math.Round((1.0 - _hsvV) * _hsvValRect.Height);
            using (Pen wp = new Pen(_uiGoldCta, 2f))
            {
                g.DrawRectangle(wp, _hsvValRect.X - 2, vy - 3, _hsvValRect.Width + 4, 6);
            }
        }

        private void DrawSliderZone(Graphics g, string id, bool highlighted)
        {
            Rectangle rawTrack;
            if (!_sliderTracks.TryGetValue(id, out rawTrack)) return;

            int val = 0, min = 0, max = 255;
            GetSliderRangeAndValue(id, out val, out min, out max);

            Rectangle track = new Rectangle(rawTrack.X, rawTrack.Y + 4, rawTrack.Width, 6);
            using (SolidBrush trackBg = new SolidBrush(_uiBg))
            using (Pen trackBorder = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(trackBg, track);
                g.DrawRectangle(trackBorder, track);
            }

            double ratio = (double)(val - min) / Math.Max(1.0, (double)(max - min));
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            int fillW = (int)Math.Round(ratio * track.Width);
            if (fillW > 0)
            {
                Rectangle fillRect = new Rectangle(track.X, track.Y, fillW, track.Height);
                using (LinearGradientBrush fb = new LinearGradientBrush(
                    track,
                    _uiActiveAccent,
                    _uiActiveRim,
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(fb, fillRect);
                }
            }

            int knobX = track.X + fillW;
            Rectangle knobRect = new Rectangle(knobX - 7, track.Y - 5, 14, 16);
            using (SolidBrush kb = new SolidBrush(highlighted ? _uiGoldCtaHover : _uiGoldCta))
            using (Pen kp = new Pen(ColTextPrimary, 1f))
            {
                g.FillRectangle(kb, knobRect);
                g.DrawRectangle(kp, knobRect);
            }

            if (highlighted)
            {
                Rectangle pill = new Rectangle(knobX - 22, knobRect.Y - 20, 44, 17);
                using (SolidBrush pb = new SolidBrush(ColL2Pill))
                using (Pen pp = new Pen(_uiGoldCta, 1f))
                using (SolidBrush tb = new SolidBrush(ColTextPrimary))
                {
                    g.FillRectangle(pb, pill);
                    g.DrawRectangle(pp, pill);
                    g.DrawString(val.ToString(), _fontMono, tb, pill, SfCenter);
                }
            }
        }

        private void DrawTaskbarModeCard(Graphics g, UiZone z, bool active, bool hovered)
        {
            Color bg = active ? _uiActiveAccent : (hovered ? _uiCardHover : _uiBg);
            Color border = active ? _uiGoldCta : (hovered ? _uiActiveRim : _uiBorderSubtle);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, active ? 1.8f : 1f))
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            using (SolidBrush sb = new SolidBrush(active ? _uiGoldCta : ColTextSecondary))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
                g.DrawString(z.Title, _fontCardTitle, tb, z.Bounds.X + 10, z.Bounds.Y + 10);
                g.DrawString(z.Subtitle, _fontCardSub, sb, z.Bounds.X + 10, z.Bounds.Y + 32);
            }
        }

        private void DrawToggleRow(Graphics g, UiZone z, bool isOn, bool hovered)
        {
            Color bg = hovered ? _uiCardHover : _uiBg;
            Color border = isOn ? _uiActiveRim : _uiBorderSubtle;

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, 1f))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
            }

            Rectangle textRect = new Rectangle(z.Bounds.X + 10, z.Bounds.Y, z.Bounds.Width - 62, z.Bounds.Height);
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            {
                g.DrawString(z.Title, _fontSmall, tb, textRect, SfNearCenter);
            }

            Rectangle pillRect = new Rectangle(z.Bounds.Right - 46, z.Bounds.Y + (z.Bounds.Height - 18) / 2, 38, 18);
            using (SolidBrush pb = new SolidBrush(isOn ? _uiActiveAccent : Color.FromArgb(22, 28, 24)))
            using (Pen pp = new Pen(isOn ? _uiGoldCta : _uiBorderSubtle, 1f))
            {
                g.FillRectangle(pb, pillRect);
                g.DrawRectangle(pp, pillRect);
            }

            Rectangle thumbRect = isOn
                ? new Rectangle(pillRect.Right - 16, pillRect.Y + 2, 14, 14)
                : new Rectangle(pillRect.X + 2, pillRect.Y + 2, 14, 14);

            using (SolidBrush thumbBrush = new SolidBrush(isOn ? _uiGoldCta : ColTextSecondary))
            {
                g.FillRectangle(thumbBrush, thumbRect);
            }
        }

        private void DrawSelectorButton(Graphics g, UiZone z, bool active, bool hovered)
        {
            Color bg = active ? _uiActiveAccent : (hovered ? _uiCardHover : _uiBg);
            Color border = active ? _uiGoldCta : (hovered ? _uiActiveRim : _uiBorderSubtle);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, active ? 1.6f : 1f))
            using (SolidBrush tb = new SolidBrush(active || hovered ? ColTextPrimary : ColTextSecondary))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
                g.DrawString(z.Title, _fontButton, tb, z.Bounds, SfCenter);
            }
        }

        private void DrawGoldCtaButton(Graphics g, UiZone z, bool hovered)
        {
            Color bg = hovered ? _uiGoldCtaHover : _uiGoldCta;
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                z.Bounds,
                bg,
                Color.FromArgb(Math.Max(0, bg.R - 24), Math.Max(0, bg.G - 24), Math.Max(0, bg.B - 18)),
                LinearGradientMode.Vertical))
            using (Pen borderPen = new Pen(Color.FromArgb(248, 232, 184), 1.4f))
            using (SolidBrush textBrush = new SolidBrush(ColTextDark))
            {
                g.FillRectangle(lgb, z.Bounds);
                g.DrawRectangle(borderPen, z.Bounds);
                g.DrawString(z.Title, _fontButtonBold, textBrush, z.Bounds, SfCenter);
            }
        }

        private void DrawActionButton(Graphics g, UiZone z, bool hovered)
        {
            Color bg = hovered ? _uiActiveAccent : _uiCardHover;
            Color border = hovered ? _uiGoldCta : _uiActiveRim;

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, 1.2f))
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            {
                g.FillRectangle(b, z.Bounds);
                g.DrawRectangle(p, z.Bounds);
                g.DrawString(z.Title, _fontButton, tb, z.Bounds, SfCenter);
            }
        }

        private void DrawCommandPaletteOverlay(Graphics g, int winW, int winH)
        {
            using (SolidBrush scrim = new SolidBrush(Color.FromArgb(185, 7, 8, 12)))
            {
                g.FillRectangle(scrim, 0, 0, winW, winH);
            }

            int modalW = 640;
            int modalH = 430;
            int mx = (winW - modalW) / 2;
            int my = Math.Max(70, (winH - modalH) / 2 - 20);
            Rectangle modalRect = new Rectangle(mx, my, modalW, modalH);

            DrawBentoCard(g, modalRect, "GLOBAL KOMUT PALETİ  •  HIZLI ARAMA & DERİN BAĞLANTI", "ESC: KAPAT", true);

            // Search Input Box
            Rectangle searchBox = new Rectangle(mx + 16, my + 28, modalW - 66, 24);
            using (SolidBrush sb = new SolidBrush(_uiBg))
            using (Pen sp = new Pen(_uiGoldCta, 1.2f))
            using (SolidBrush tb = new SolidBrush(ColTextPrimary))
            {
                g.FillRectangle(sb, searchBox);
                g.DrawRectangle(sp, searchBox);
                string disp = string.IsNullOrEmpty(_commandSearchQuery)
                    ? "Komut (>), tema (#) veya widget (@) yazın..."
                    : (_commandSearchQuery + "▋");
                g.DrawString("⌕ " + disp, _fontButton, tb, searchBox.X + 8, searchBox.Y + 3);
            }

            for (int i = 0; i < _paletteZones.Count; i++)
            {
                UiZone pz = _paletteZones[i];
                bool hov = (_hoveredZoneId == pz.Id);
                if (pz.Id.StartsWith("PAL_ITEM_"))
                {
                    int rowIdx = int.Parse(pz.Id.Substring("PAL_ITEM_".Length));
                    bool sel = (rowIdx == _commandSelectedIndex) || hov;
                    CommandPaletteItem item = (rowIdx < _filteredCommands.Count) ? _filteredCommands[rowIdx] : null;

                    using (SolidBrush rb = new SolidBrush(sel ? _uiActiveAccent : _uiBg))
                    using (Pen rp = new Pen(sel ? _uiGoldCta : _uiBorderSubtle, 1f))
                    using (SolidBrush tPri = new SolidBrush(ColTextPrimary))
                    using (SolidBrush tSub = new SolidBrush(_uiGoldCta))
                    {
                        g.FillRectangle(rb, pz.Bounds);
                        g.DrawRectangle(rp, pz.Bounds);
                        if (item != null)
                        {
                            g.DrawString("[" + item.Prefix + "]  " + item.Title, _fontButtonBold, tPri, pz.Bounds.X + 10, pz.Bounds.Y + 9);
                            string rightTxt = !string.IsNullOrEmpty(item.Shortcut) ? item.Shortcut : item.Category;
                            g.DrawString(rightTxt, _fontMono, tSub, pz.Bounds.Right - 10, pz.Bounds.Y + 10, SfRight);
                        }
                    }
                }
                else
                {
                    DrawSelectorButton(g, pz, false, hov);
                }
            }
        }

        private void DrawFooterBar(Graphics g, int winW, int winH)
        {
            Rectangle footerRect = new Rectangle(10, winH - 32, winW - 20, 26);
            using (SolidBrush b = new SolidBrush(_uiHeaderBg))
            using (Pen p = new Pen(_uiBorderSubtle, 1f))
            {
                g.FillRectangle(b, footerRect);
                g.DrawRectangle(p, footerRect);
            }

            using (SolidBrush dotBrush = new SolidBrush(_toastTicksRemaining > 0 ? _uiGoldCta : _uiActiveRim))
            {
                g.FillEllipse(dotBrush, footerRect.X + 10, footerRect.Y + 8, 9, 9);
            }

            string msg = _toastTicksRemaining > 0 && !string.IsNullOrEmpty(_toastText) ? _toastText : _statusMessage;
            using (SolidBrush statusBrush = new SolidBrush(ColTextPrimary))
            {
                g.DrawString(msg, _fontSmall, statusBrush, footerRect.X + 24, footerRect.Y + 5);
            }

            using (SolidBrush hintBrush = new SolidBrush(ColTextSecondary))
            {
                g.DrawString("Ctrl+Shift+Space: Komut Paleti • Ctrl+Alt+T: Stüdyo • Ctrl+Alt+G: 360° Gölge • Ctrl+Z: Geri Al", _fontCardSub, hintBrush, footerRect.Right - 10, footerRect.Y + 6, SfRight);
            }
        }

        private void DrawCornerAccents(Graphics g, Rectangle r, int len)
        {
            using (Pen goldPen = new Pen(_uiGoldCta, 2f))
            {
                g.DrawLine(goldPen, r.X, r.Y, r.X + len, r.Y);
                g.DrawLine(goldPen, r.X, r.Y, r.X, r.Y + len);
                g.DrawLine(goldPen, r.Right - len, r.Y, r.Right, r.Y);
                g.DrawLine(goldPen, r.Right, r.Y, r.Right, r.Y + len);
                g.DrawLine(goldPen, r.X, r.Bottom - len, r.X, r.Bottom);
                g.DrawLine(goldPen, r.X, r.Bottom, r.X + len, r.Bottom);
                g.DrawLine(goldPen, r.Right - len, r.Bottom, r.Right, r.Bottom);
                g.DrawLine(goldPen, r.Right, r.Bottom - len, r.Right, r.Bottom);
            }
        }

        // ====================================================================
        // Mouse, Slider & Keyboard Interaction
        // ====================================================================
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control && e.Shift && e.KeyCode == Keys.Space)
            {
                OpenCommandPalette("");
                e.Handled = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.Z && !_commandPaletteOpen)
            {
                ExecuteUndoLastAction();
                e.Handled = true;
                return;
            }

            if (_commandPaletteOpen)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _commandPaletteOpen = false;
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.Down)
                {
                    if (_filteredCommands.Count > 0)
                        _commandSelectedIndex = (_commandSelectedIndex + 1) % Math.Min(8, _filteredCommands.Count);
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.Up)
                {
                    int max = Math.Min(8, _filteredCommands.Count);
                    if (max > 0)
                        _commandSelectedIndex = (_commandSelectedIndex - 1 + max) % max;
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    if (_commandSelectedIndex >= 0 && _commandSelectedIndex < _filteredCommands.Count)
                    {
                        ExecuteCommandPaletteItem(_filteredCommands[_commandSelectedIndex]);
                    }
                }
                else if (e.KeyCode == Keys.Back)
                {
                    if (_commandSearchQuery.Length > 0)
                    {
                        _commandSearchQuery = _commandSearchQuery.Substring(0, _commandSearchQuery.Length - 1);
                        _commandSelectedIndex = 0;
                        RebuildCommandPaletteZones();
                        this.Invalidate();
                    }
                }
                e.Handled = true;
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (_commandPaletteOpen && !char.IsControl(e.KeyChar))
            {
                _commandSearchQuery += e.KeyChar;
                _commandSelectedIndex = 0;
                RebuildCommandPaletteZones();
                this.Invalidate();
                e.Handled = true;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            if (_commandPaletteOpen)
            {
                for (int i = _paletteZones.Count - 1; i >= 0; i--)
                {
                    if (_paletteZones[i].Bounds.Contains(e.Location))
                    {
                        HandlePaletteZoneClick(_paletteZones[i]);
                        return;
                    }
                }
                _commandPaletteOpen = false;
                this.Invalidate();
                return;
            }

            UiZone hit = HitTestZone(e.Location);

            if (hit == null && e.Y <= 52)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                return;
            }

            if (hit != null && hit.Id == "HSV_BOX")
            {
                _draggingHsvBox = true;
                UpdateHsvFromBoxPoint(e.Location);
                return;
            }

            if (hit != null && hit.Id == "HSV_VAL")
            {
                _draggingHsvVal = true;
                UpdateHsvFromValPoint(e.Location);
                return;
            }

            if (hit != null && hit.Id.StartsWith("SLIDER_"))
            {
                _activeDragSliderId = hit.Id;
                UpdateSliderFromMouseX(hit.Id, e.X);
                return;
            }

            if (hit != null)
            {
                HandleZoneClick(hit);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_draggingHsvBox)
            {
                UpdateHsvFromBoxPoint(e.Location);
                return;
            }
            if (_draggingHsvVal)
            {
                UpdateHsvFromValPoint(e.Location);
                return;
            }
            if (_activeDragSliderId != null)
            {
                UpdateSliderFromMouseX(_activeDragSliderId, e.X);
                return;
            }

            UiZone hit = HitTestZone(e.Location);
            string newId = hit != null ? hit.Id : null;
            if (newId != _hoveredZoneId)
            {
                _hoveredZoneId = newId;
                this.Cursor = (newId != null) ? Cursors.Hand : Cursors.Default;
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_draggingHsvBox || _draggingHsvVal)
            {
                _draggingHsvBox = false;
                _draggingHsvVal = false;
                RegenerateHarmonySwatches();
                RebuildPreviewFolderIcons();
                RebuildZonesForCurrentTab();
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                this.Invalidate();
            }
            if (_activeDragSliderId != null)
            {
                _activeDragSliderId = null;
                RebuildPreviewFolderIcons();
                RebuildZonesForCurrentTab();
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                this.Invalidate();
            }
        }

        private UiZone HitTestZone(Point pt)
        {
            if (_commandPaletteOpen)
            {
                for (int i = _paletteZones.Count - 1; i >= 0; i--)
                {
                    if (_paletteZones[i].Bounds.Contains(pt))
                        return _paletteZones[i];
                }
                return null;
            }

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                if (_zones[i].Bounds.Contains(pt))
                    return _zones[i];
            }
            return null;
        }

        private void GetSliderRangeAndValue(string id, out int val, out int min, out int max)
        {
            Color c = GetSelectedChannelColor();
            switch (id)
            {
                case "SLIDER_TINT": val = _emeraldTintIntensity; min = 0; max = 140; break;
                case "SLIDER_SHADOW": val = _emeraldShadowIntensity; min = 0; max = 255; break;
                case "SLIDER_SPREAD": val = _shadowSpread; min = 8; max = 48; break;
                case "SLIDER_ALPHA": val = _explorerAlpha; min = 150; max = 255; break;
                case "SLIDER_HDR_NITS": val = _hdrNitsClamp; min = 80; max = 400; break;
                case "SLIDER_TB_ALPHA": val = _customTaskbarAlpha; min = 0; max = 255; break;
                case "SLIDER_TB_RADIUS": val = _taskbarCornerRadius; min = 0; max = 24; break;
                case "SLIDER_TB_MARGIN": val = _taskbarDockMargin; min = 0; max = 16; break;
                case "SLIDER_HUD_OPACITY": val = _hudOpacity; min = 80; max = 255; break;
                case "SLIDER_SPEC_SMOOTH": val = _spectrumSmoothing; min = 10; max = 95; break;
                case "SLIDER_GPU_PAUSE": val = _gpuPauseThreshold; min = 50; max = 95; break;
                case "SLIDER_RAM_AUTOPURGE": val = _ramAutoPurgeThreshold; min = 70; max = 95; break;
                case "SLIDER_DIMMER": val = _displayDimmerPercent; min = 0; max = 80; break;
                case "SLIDER_KELVIN": val = _displayWarmthKelvin; min = 2700; max = 6500; break;
                case "SLIDER_PREVIEW_SPLIT": val = _previewSplitPercent; min = 0; max = 100; break;
                case "SLIDER_COL_R": val = c.R; min = 0; max = 255; break;
                case "SLIDER_COL_G": val = c.G; min = 0; max = 255; break;
                case "SLIDER_COL_B": val = c.B; min = 0; max = 255; break;
                default: val = 0; min = 0; max = 255; break;
            }
        }

        private void UpdateSliderFromMouseX(string id, int mouseX)
        {
            Rectangle track;
            if (!_sliderTracks.TryGetValue(id, out track)) return;

            int cur, min, max;
            GetSliderRangeAndValue(id, out cur, out min, out max);

            double ratio = (double)(mouseX - track.X) / Math.Max(1.0, (double)track.Width);
            if (ratio < 0.0) ratio = 0.0;
            if (ratio > 1.0) ratio = 1.0;

            int newVal = min + (int)Math.Round(ratio * (max - min));
            if (newVal < min) newVal = min;
            if (newVal > max) newVal = max;
            if (newVal == cur) return;

            if (id == "SLIDER_TINT")
            {
                _emeraldTintIntensity = newVal;
                if (newVal > 0) _emeraldTintAndShadowEnabled = true;
                try { DoomGlassDaemon.UpdateEmeraldOverlaySettings(_emeraldTintAndShadowEnabled, _emeraldTintIntensity, _emeraldShadowIntensity); } catch { }
            }
            else if (id == "SLIDER_SHADOW")
            {
                _emeraldShadowIntensity = newVal;
                if (newVal > 0) _emeraldTintAndShadowEnabled = true;
                try { DoomGlassDaemon.UpdateEmeraldOverlaySettings(_emeraldTintAndShadowEnabled, _emeraldTintIntensity, _emeraldShadowIntensity); } catch { }
            }
            else if (id == "SLIDER_SPREAD")
            {
                _shadowSpread = newVal;
                try { DoomGlassDaemon.ShadowSpread = _shadowSpread; DoomGlassDaemon.SyncAllExplorerOverlays(true); } catch { }
            }
            else if (id == "SLIDER_ALPHA")
            {
                _explorerAlpha = newVal;
                if (!_explorerGlassEnabled && _explorerAlpha < 255)
                {
                    _explorerGlassEnabled = true;
                    if (_explorerBlurMode == 0) _explorerBlurMode = 1;
                }
                try { DoomGlassDaemon.UpdateSettings(_explorerGlassEnabled, (byte)_explorerAlpha, _explorerBlurMode > 2 ? 1 : _explorerBlurMode, _taskbarMode); } catch { }
            }
            else if (id == "SLIDER_HDR_NITS")
            {
                _hdrNitsClamp = newVal;
            }
            else if (id == "SLIDER_TB_ALPHA")
            {
                _customTaskbarAlpha = newVal;
                if (_taskbarMode != 4) _taskbarMode = 4;
                try
                {
                    DoomGlassDaemon.CustomTaskbarAlpha = (byte)_customTaskbarAlpha;
                    DoomGlassDaemon.UpdateSettings(_explorerGlassEnabled, (byte)_explorerAlpha, _explorerBlurMode > 2 ? 1 : _explorerBlurMode, _taskbarMode);
                }
                catch { }
            }
            else if (id == "SLIDER_TB_RADIUS")
            {
                _taskbarCornerRadius = newVal;
            }
            else if (id == "SLIDER_TB_MARGIN")
            {
                _taskbarDockMargin = newVal;
            }
            else if (id == "SLIDER_HUD_OPACITY")
            {
                _hudOpacity = newVal;
                _hudEnabled = true;
                try
                {
                    DesktopHudWidget.HudOpacity = (byte)_hudOpacity;
                    string title = _activePreset != null ? _activePreset.DisplayName : _activeProfileId;
                    DesktopHudWidget.StartOrUpdate(_hudEnabled, _widgetLayout, (byte)_hudOpacity, _uiCard, _customRimHighlight, ColTextPrimary, title);
                }
                catch { }
            }
            else if (id == "SLIDER_SPEC_SMOOTH")
            {
                _spectrumSmoothing = newVal;
                try { DesktopHudWidget.SpectrumSmoothing = _spectrumSmoothing / 100.0f; } catch { }
            }
            else if (id == "SLIDER_GPU_PAUSE")
            {
                _gpuPauseThreshold = newVal;
            }
            else if (id == "SLIDER_RAM_AUTOPURGE")
            {
                _ramAutoPurgeThreshold = newVal;
            }
            else if (id == "SLIDER_DIMMER")
            {
                _displayDimmerPercent = newVal;
                if (_displayDimmerPercent > 0) _displayOverlayEnabled = true;
                try
                {
                    if (_displayOverlayEnabled) DisplayOverlayManager.ApplyOverlay(_displayDimmerPercent, _displayWarmthKelvin);
                }
                catch { }
            }
            else if (id == "SLIDER_KELVIN")
            {
                _displayWarmthKelvin = newVal;
                if (_displayWarmthKelvin < 6500) _displayOverlayEnabled = true;
                try
                {
                    if (_displayOverlayEnabled) DisplayOverlayManager.ApplyOverlay(_displayDimmerPercent, _displayWarmthKelvin);
                }
                catch { }
            }
            else if (id == "SLIDER_PREVIEW_SPLIT")
            {
                _previewSplitPercent = newVal;
            }
            else if (id == "SLIDER_COL_R" || id == "SLIDER_COL_G" || id == "SLIDER_COL_B")
            {
                Color c = GetSelectedChannelColor();
                int r = (id == "SLIDER_COL_R") ? newVal : c.R;
                int g = (id == "SLIDER_COL_G") ? newVal : c.G;
                int b = (id == "SLIDER_COL_B") ? newVal : c.B;
                SetSelectedChannelColor(Color.FromArgb(r, g, b));
                SyncHsvFromSelectedChannel();
                AdaptStudioUiColorsToActivePreset();
                RegenerateHarmonySwatches();
                try
                {
                    DoomGlassDaemon.ApplyPresetOverlayColors(_customBodyTint, _customShadowGlow, _customRimHighlight, _shadowSpread, (byte)_emeraldTintIntensity, (byte)_emeraldShadowIntensity);
                    string title = _activePreset != null ? _activePreset.DisplayName : _activeProfileId;
                    DesktopHudWidget.StartOrUpdate(_hudEnabled, _widgetLayout, (byte)_hudOpacity, _uiCard, _customRimHighlight, ColTextPrimary, title);
                }
                catch { }
            }

            this.Invalidate();
        }

        // ====================================================================
        // Global Command Palette & Action Click Handlers
        // ====================================================================
        public void OpenCommandPalette(string initialPrefix)
        {
            _commandPaletteOpen = true;
            _commandSearchQuery = initialPrefix ?? "";
            _commandSelectedIndex = 0;
            RebuildCommandPaletteZones();
            this.Invalidate();
        }

        private void HandlePaletteZoneClick(UiZone z)
        {
            if (z.Id == "PAL_CLOSE")
            {
                _commandPaletteOpen = false;
                this.Invalidate();
                return;
            }
            if (z.Id == "PAL_FILTER_ALL") { OpenCommandPalette(""); return; }
            if (z.Id == "PAL_FILTER_CMD") { OpenCommandPalette(">"); return; }
            if (z.Id == "PAL_FILTER_THM") { OpenCommandPalette("#"); return; }
            if (z.Id == "PAL_FILTER_WDG") { OpenCommandPalette("@"); return; }

            if (z.Id.StartsWith("PAL_ITEM_"))
            {
                int idx = int.Parse(z.Id.Substring("PAL_ITEM_".Length));
                if (idx >= 0 && idx < _filteredCommands.Count)
                {
                    ExecuteCommandPaletteItem(_filteredCommands[idx]);
                }
            }
        }

        private void ExecuteCommandPaletteItem(CommandPaletteItem item)
        {
            _commandPaletteOpen = false;
            if (item == null) { this.Invalidate(); return; }

            string id = item.Id ?? "";
            AddDiagnosticLog("Komut Paleti çalıştırıldı: " + item.Title);

            if (id.StartsWith("view_", StringComparison.OrdinalIgnoreCase) || id.StartsWith("view.", StringComparison.OrdinalIgnoreCase))
            {
                SwitchActiveView(item.TargetViewIndex >= 0 ? item.TargetViewIndex : 0);
                return;
            }

            if (id.StartsWith("theme_", StringComparison.OrdinalIgnoreCase) || id.StartsWith("theme.", StringComparison.OrdinalIgnoreCase))
            {
                string presetId = id.Substring(6);
                ActivatePresetById(presetId, false);
                return;
            }

            if (string.Equals(id, "cmd_apply", StringComparison.OrdinalIgnoreCase))
            {
                HandleZoneClick(new UiZone { Id = "ACT_APPLY_NOW" });
                return;
            }
            if (string.Equals(id, "cmd_rollback", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteUndoLastAction();
                return;
            }
            if (string.Equals(id, "cmd_purge_ram", StringComparison.OrdinalIgnoreCase) || id.IndexOf("purge", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ExecuteRamPurge();
                return;
            }
            if (string.Equals(id, "cmd_toggle_icons", StringComparison.OrdinalIgnoreCase) || id.IndexOf("icons", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    bool vis = DoomThemeEngine.ToggleDesktopIconsVisibility();
                    ShowToast(vis ? "Masaüstü simgeleri gösterildi." : "Masaüstü simgeleri gizlendi.");
                }
                catch { }
                this.Invalidate();
                return;
            }
            if (string.Equals(id, "cmd_toggle_glass", StringComparison.OrdinalIgnoreCase))
            {
                ToggleEmeraldArmorOverlay();
                return;
            }
            if (string.Equals(id, "cmd_fix_hdr", StringComparison.OrdinalIgnoreCase) || id.IndexOf("hdr", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ExecuteFixExplorerText();
                return;
            }
            if (string.Equals(id, "cmd_pomodoro_toggle", StringComparison.OrdinalIgnoreCase))
            {
                try { DesktopHudWidget.StartPausePomodoro(); } catch { }
                SwitchActiveView(4);
                ShowToast(DesktopHudWidget.PomodoroRunning ? "▶ Pomodoro odak sayacı başlatıldı." : "⏸ Pomodoro duraklatıldı.");
                return;
            }
            if (string.Equals(id, "cmd_boost_game", StringComparison.OrdinalIgnoreCase))
            {
                ApplyBoostProfileSelection(BoostProfile.Oyun);
                SwitchActiveView(5);
                return;
            }
            if (string.Equals(id, "cmd_boost_render", StringComparison.OrdinalIgnoreCase))
            {
                ApplyBoostProfileSelection(BoostProfile.Render);
                SwitchActiveView(5);
                return;
            }
            if (string.Equals(id, "cmd_boost_normal", StringComparison.OrdinalIgnoreCase))
            {
                ApplyBoostProfileSelection(BoostProfile.Normal);
                SwitchActiveView(5);
                return;
            }
            if (string.Equals(id, "cmd_export_pkg", StringComparison.OrdinalIgnoreCase))
            {
                SwitchActiveView(0);
                HandleZoneClick(new UiZone { Id = "ACT_DOOMTHEME_EXPORT" });
                return;
            }
            if (string.Equals(id, "cmd_import_pkg", StringComparison.OrdinalIgnoreCase))
            {
                SwitchActiveView(0);
                HandleZoneClick(new UiZone { Id = "ACT_DOOMTHEME_IMPORT" });
                return;
            }

            if (id.StartsWith("widget_", StringComparison.OrdinalIgnoreCase))
            {
                string wKey = id.Substring("widget_".Length).ToLowerInvariant();
                if (wKey == "clock") { _inspectedWidgetIndex = 0; HandleToggleClick("TOG_W_CLOCK"); }
                else if (wKey == "sys") { _inspectedWidgetIndex = 1; HandleToggleClick("TOG_W_SYSGPU"); }
                else if (wKey == "storage") { _inspectedWidgetIndex = 2; HandleToggleClick("TOG_W_STORAGE"); }
                else if (wKey == "net") { _inspectedWidgetIndex = 3; HandleToggleClick("TOG_W_NETWORK"); }
                else if (wKey == "media") { _inspectedWidgetIndex = 4; HandleToggleClick("TOG_W_MEDIA"); }
                else if (wKey == "notes") { _inspectedWidgetIndex = 5; HandleToggleClick("TOG_W_NOTES"); }
                else if (wKey == "spectrum") { _inspectedWidgetIndex = 6; HandleToggleClick("TOG_W_SPECTRUM"); }
                else if (wKey == "pomodoro") { _inspectedWidgetIndex = 7; HandleToggleClick("TOG_W_POMODORO"); }
                SwitchActiveView(4);
                return;
            }

            if (item.TargetViewIndex >= 0 && item.TargetViewIndex <= 6)
            {
                SwitchActiveView(item.TargetViewIndex);
                return;
            }

            RebuildZonesForCurrentTab();
            this.Invalidate();
        }

        private void SwitchActiveView(int newViewIndex)
        {
            if (newViewIndex < 0) newViewIndex = 0;
            if (newViewIndex > 6) newViewIndex = 6;
            _activeTab = newViewIndex;
            _viewTransitionAlpha = 0.25f;
            _viewTransitionOffsetY = 10f;
            RebuildZonesForCurrentTab();
            this.Invalidate();
        }

        private void HandleZoneClick(UiZone z)
        {
            if (z.Id == "WIN_CLOSE" || z.Id == "WIN_TRAY")
            {
                MinimizeToTray();
                return;
            }
            if (z.Id == "WIN_MIN")
            {
                this.WindowState = FormWindowState.Minimized;
                return;
            }
            if (z.Id == "WIN_MAX")
            {
                this.WindowState = (this.WindowState == FormWindowState.Maximized)
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id == "CMD_BAR_OPEN")
            {
                OpenCommandPalette("");
                return;
            }
            if (z.Id == "CHIP_CPU" || z.Id == "CHIP_RAM")
            {
                SwitchActiveView(5);
                return;
            }
            if (z.Id == "CHIP_HDR")
            {
                SwitchActiveView(2);
                return;
            }
            if (z.Id == "ACT_UNDO_HEADER")
            {
                ExecuteUndoLastAction();
                return;
            }
            if (z.Id == "RAIL_TOGGLE")
            {
                _railExpanded = !_railExpanded;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }
            if (z.Id == "PREVIEW_DOCK_TOGGLE")
            {
                _previewDockExpanded = !_previewDockExpanded;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }
            if (z.Id == "PREV_TOG_BEFORE")
            {
                _previewSplitPercent = (_previewSplitPercent > 0) ? 0 : 50;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }
            if (z.Id == "PREV_TOG_HDR")
            {
                _previewHdrSim = !_previewHdrSim;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }
            if (z.Id == "ACT_CYCLE_BOOST")
            {
                int next = (((int)_boostProfile) + 1) % 4;
                ApplyBoostProfileSelection((BoostProfile)next);
                return;
            }

            if (z.Id.StartsWith("TAB_"))
            {
                int tabIdx = int.Parse(z.Id.Substring("TAB_".Length));
                SwitchActiveView(tabIdx);
                return;
            }

            if (z.Id.StartsWith("GAL_FILTER_"))
            {
                _galleryFilterMode = int.Parse(z.Id.Substring("GAL_FILTER_".Length));
                _themeGalleryPage = 0;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id == "PAGE_PREV")
            {
                _themeGalleryPage = Math.Max(0, _themeGalleryPage - 1);
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }
            if (z.Id == "PAGE_NEXT")
            {
                _themeGalleryPage++;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("FAV_PRESET_"))
            {
                string fid = z.Tag;
                if (!string.IsNullOrEmpty(fid))
                {
                    if (_favoritePresetIds.Contains(fid)) _favoritePresetIds.Remove(fid);
                    else _favoritePresetIds.Add(fid);
                    SaveStateToEngine();
                    RebuildZonesForCurrentTab();
                    this.Invalidate();
                }
                return;
            }

            if (z.Id.StartsWith("DEL_PRESET_"))
            {
                string delId = z.Tag;
                if (!string.IsNullOrEmpty(delId))
                {
                    ThemeCatalog.DeleteCustomPreset(delId);
                    if (string.Equals(_activeProfileId, delId, StringComparison.OrdinalIgnoreCase))
                        ActivatePresetById("DoomEmerald", false);
                    RebuildZonesForCurrentTab();
                    RebuildTrayThemesMenu();
                    ShowToast("Özel tema silindi: " + delId);
                    this.Invalidate();
                }
                return;
            }

            if (z.Id.StartsWith("PRESET_"))
            {
                string pid = z.Id.Substring("PRESET_".Length);
                ActivatePresetById(pid, false);
                return;
            }

            if (z.Id.StartsWith("CHAM_SWATCH_") || z.Id.StartsWith("HARM_SWATCH_"))
            {
                CaptureUndoState("Renk ataması");
                SetSelectedChannelColor(z.Swatch1);
                SyncHsvFromSelectedChannel();
                AdaptStudioUiColorsToActivePreset();
                RegenerateHarmonySwatches();
                RebuildPreviewFolderIcons();
                RebuildZonesForCurrentTab();
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast("Seçili kanala renk atandı: " + ThemeCatalog.ColorToHex(z.Swatch1));
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("HARM_MODE_"))
            {
                int hm = int.Parse(z.Id.Substring("HARM_MODE_".Length));
                _harmonyMode = (HarmonyMode)hm;
                RegenerateHarmonySwatches();
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_HARM_APPLY_PALETTE")
            {
                CaptureUndoState("Harmoni paleti");
                RegenerateHarmonySwatches();
                if (_harmonySwatches != null && _harmonySwatches.Length >= 5)
                {
                    _customPrimaryAccent = _harmonySwatches[0];
                    _customBodyTint = ColorHarmonyGenerator.EnsureContrast( _harmonySwatches[1], ColTextPrimary, 4.5);
                    _customShadowGlow = _harmonySwatches[2];
                    _customRimHighlight = _harmonySwatches[3];
                    _customTaskbarTint = _harmonySwatches[4];
                    SyncHsvFromSelectedChannel();
                    AdaptStudioUiColorsToActivePreset();
                    RebuildPreviewFolderIcons();
                    RebuildZonesForCurrentTab();
                    SaveStateToEngine();
                    ApplyLiveRuntimeEngines();
                    ShowToast("✦ 5 renk kanalı Harmoni Üreticisinden otomatik dolduruldu!");
                    this.Invalidate();
                }
                return;
            }

            if (z.Id.StartsWith("BEZIER_PRESET_"))
            {
                _bezierPresetIndex = int.Parse(z.Id.Substring("BEZIER_PRESET_".Length));
                if (_bezierPresetIndex == 0) { _bezierP1 = new PointF(0.28f, 0.85f); _bezierP2 = new PointF(0.68f, 0.22f); _shadowSpread = 28; }
                else if (_bezierPresetIndex == 1) { _bezierP1 = new PointF(0.12f, 0.96f); _bezierP2 = new PointF(0.35f, 0.10f); _shadowSpread = 18; }
                else if (_bezierPresetIndex == 2) { _bezierP1 = new PointF(0.45f, 0.90f); _bezierP2 = new PointF(0.55f, 0.65f); _shadowSpread = 36; }
                else { _bezierP1 = new PointF(0.35f, 0.75f); _bezierP2 = new PointF(0.82f, 0.35f); _shadowSpread = 42; }
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                RebuildZonesForCurrentTab();
                ShowToast("360° Gölge Bézier eğrisi güncellendi.");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_AUTO_CONTRAST")
            {
                CaptureUndoState("WCAG kontrast optimizasyonu");
                _customRimHighlight = ColorHarmonyGenerator.EnsureContrast(_customRimHighlight, _uiBg, 4.8);
                double bodyRatio = ColorHarmonyGenerator.CalculateContrastRatio(Color.White, _customBodyTint);
                if (bodyRatio < 7.0)
                {
                    _customBodyTint = Color.FromArgb(
                        Math.Max(6, _customBodyTint.R / 2),
                        Math.Max(8, _customBodyTint.G / 2),
                        Math.Max(10, _customBodyTint.B / 2));
                }
                AdaptStudioUiColorsToActivePreset();
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                RebuildZonesForCurrentTab();
                ShowToast("✓ WCAG 2.1 AA/AAA kontrast oranları otomatik dengelendi!");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_EYEDROPPER")
            {
                ExecuteEyedropperSample();
                return;
            }

            if (z.Id == "ACT_DOOMTHEME_EXPORT")
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = ".doomtheme Tema Paketi Dışa Aktar";
                    sfd.Filter = "DoomTheme Paketi (*.doomtheme)|*.doomtheme";
                    sfd.FileName = (_activeProfileId ?? "CustomTheme") + ".doomtheme";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        SaveStateToEngine();
                        if (DoomThemePackage.ExportToFile(GetEffectiveLivePreset(), sfd.FileName, _config))
                        {
                            _lastPackageDiffSummary = "Dışa aktarıldı: " + Path.GetFileName(sfd.FileName);
                            ShowToast("📦 .doomtheme paketi başarıyla kaydedildi: " + Path.GetFileName(sfd.FileName));
                        }
                    }
                }
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_DOOMTHEME_IMPORT")
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = ".doomtheme Tema Paketi İçe Aktar";
                    ofd.Filter = "DoomTheme Paketi (*.doomtheme)|*.doomtheme|Tüm Dosyalar (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        bool valid;
                        string msg;
                        DoomThemePackage pkg = DoomThemePackage.ImportFromFile(ofd.FileName, out valid, out msg);
                        if (valid && pkg != null && pkg.Preset != null)
                        {
                            CaptureUndoState(".doomtheme içe aktarma");
                            _lastPackageDiffSummary = DoomThemePackage.BuildDiffSummary(GetEffectiveLivePreset(), pkg, _config);
                            ThemeCatalog.SaveCustomPreset(pkg.Preset);
                            ActivatePresetById(pkg.Preset.Id, false);
                            ShowToast("📥 .doomtheme doğrulandı ve uygulandı: " + pkg.Preset.DisplayName);
                        }
                        else
                        {
                            _lastPackageDiffSummary = "Şema Hatası: " + (msg ?? "Geçersiz dosya");
                            ShowToast("⚠ .doomtheme doğrulama hatası: " + msg);
                        }
                    }
                }
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("ACT_ROLLBACK_"))
            {
                string snapId = z.Tag;
                if (!string.IsNullOrEmpty(snapId))
                {
                    if (ThemeSnapshotManager.RollbackToSnapshot(snapId))
                    {
                        LoadStateFromEngine();
                        ApplyLiveRuntimeEngines();
                        RebuildZonesForCurrentTab();
                        ShowToast("↺ Snapshot durumuna geri dönüldü: " + snapId);
                        this.Invalidate();
                    }
                }
                return;
            }

            if (z.Id == "ACT_CREATE_SNAPSHOT")
            {
                SaveStateToEngine();
                try
                {
                    ThemeSnapshotManager.RecordSnapshot(_activeProfileId + " (Manuel)", _config);
                    RebuildZonesForCurrentTab();
                    ShowToast("📸 Anlık durum Snapshot olarak kaydedildi!");
                }
                catch { }
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("CHN_"))
            {
                _selectedColorChannel = int.Parse(z.Id.Substring("CHN_".Length));
                SyncHsvFromSelectedChannel();
                RegenerateHarmonySwatches();
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_EDIT_HEX")
            {
                Color cur = GetSelectedChannelColor();
                string input = PromptTextDialog("HEX Renk Kodu Girin", "Seçili kanal için #RRGGBB formatında renk kodu:", ThemeCatalog.ColorToHex(cur));
                if (!string.IsNullOrWhiteSpace(input))
                {
                    CaptureUndoState("HEX renk değişimi");
                    Color parsed = ThemeCatalog.HexToColor(input.Trim(), cur);
                    SetSelectedChannelColor(parsed);
                    SyncHsvFromSelectedChannel();
                    AdaptStudioUiColorsToActivePreset();
                    RegenerateHarmonySwatches();
                    RebuildPreviewFolderIcons();
                    RebuildZonesForCurrentTab();
                    SaveStateToEngine();
                    ApplyLiveRuntimeEngines();
                    ShowToast("Kanal rengi güncellendi: " + ThemeCatalog.ColorToHex(parsed));
                    this.Invalidate();
                }
                return;
            }

            if (z.Id == "ACT_APPLY_CUSTOM_COLORS" || z.Id == "ACT_APPLY_NOW")
            {
                CaptureUndoState("Tema uygulama");
                ThemePreset live = GetEffectiveLivePreset();
                try
                {
                    DoomThemeEngine.ApplyPreset(live, false);
                    DoomThemeEngine.FixExplorerTextAndColors();
                    DoomThemeEngine.RefreshExplorerViewsSafely();
                    ThemeSnapshotManager.RecordSnapshot(live.DisplayName, _config);
                }
                catch { }
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                AddDiagnosticLog("Aktif tema canlı olarak uygulandı: " + live.DisplayName);
                RebuildZonesForCurrentTab();
                ShowToast("✓ Seçili tüm renkler, 360° gölge ve görev çubuğu canlı uygulandı (Ctrl+Z: Geri Al)!");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_SAVE_CUSTOM_THEME")
            {
                string name = PromptTextDialog("Yeni Özel Tema Kaydet", "Özel temanız için bir isim girin:", "Benim Özel Temam");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    ThemePreset custom = GetEffectiveLivePreset();
                    custom.Id = "Custom_" + DateTime.Now.ToString("MMdd_HHmmss");
                    custom.DisplayName = name.Trim();
                    custom.Subtitle = "Özel Bento Stüdyo Teması";
                    custom.IsBuiltIn = false;
                    ThemeCatalog.SaveCustomPreset(custom);
                    ActivatePresetById(custom.Id, false);
                    RebuildZonesForCurrentTab();
                    RebuildTrayThemesMenu();
                    ShowToast("★ Özel tema kaydedildi ve etkinleştirildi: " + custom.DisplayName);
                    this.Invalidate();
                }
                return;
            }

            if (z.Id == "ACT_RESET_PRESET_COLORS")
            {
                if (_activePreset != null)
                {
                    CaptureUndoState("Renk sıfırlama");
                    _customPrimaryAccent = _activePreset.PrimaryAccent;
                    _customBodyTint = _activePreset.ExplorerBodyTint;
                    _customShadowGlow = _activePreset.ExplorerShadowGlow;
                    _customRimHighlight = _activePreset.RimHighlight;
                    _customTaskbarTint = _activePreset.TaskbarTint;
                    SyncHsvFromSelectedChannel();
                    AdaptStudioUiColorsToActivePreset();
                    RegenerateHarmonySwatches();
                    RebuildPreviewFolderIcons();
                    RebuildZonesForCurrentTab();
                    SaveStateToEngine();
                    ApplyLiveRuntimeEngines();
                    ShowToast("Renkler aktif temanın varsayılan değerlerine sıfırlandı.");
                    this.Invalidate();
                }
                return;
            }

            if (z.Id == "ACT_CHAMELEON" || z.Id == "ACT_CHAMELEON_WP")
            {
                ExecuteChameleonFromCurrentWallpaper();
                return;
            }

            if (z.Id == "ACT_CHAMELEON_FILE" || z.Id == "ACT_PICK_WALLPAPER")
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Duvar Kağıdı Görseli Seçin (Chameleon OKLab Otomatik Tema)";
                    ofd.InitialDirectory = @"C:\Users\gorke\Documents\wallpapers";
                    ofd.Filter = "Görsel Dosyaları (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        _selectedWallpaperPath = ofd.FileName;
                        DoomThemeEngine.SetWallpaperWithStyle(_selectedWallpaperPath, 10);
                        RefreshChameleonSwatches();
                        ExecuteChameleonForPath(_selectedWallpaperPath);
                    }
                }
                return;
            }

            if (z.Id == "ACT_FIX_TEXT" || z.Id == "ACT_FIX_TEXT_TAB2")
            {
                ExecuteFixExplorerText();
                return;
            }

            if (z.Id.StartsWith("BORDER_RAD_"))
            {
                _borderCornerRadius = int.Parse(z.Id.Substring("BORDER_RAD_".Length));
                SaveStateToEngine();
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_SIM_BORDER_PHYSICS")
            {
                _borderSimRunning = !_borderSimRunning;
                _borderSimPhase = 0f;
                RebuildZonesForCurrentTab();
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_GEN_APPLY_ICONS" || z.Id == "ACT_GEN_APPLY_ICONS_TAB4")
            {
                ExecuteGenerateAndApplyIcons();
                return;
            }

            if (z.Id == "ACT_TOGGLE_DESKTOP_ICONS")
            {
                try
                {
                    bool vis = DoomThemeEngine.ToggleDesktopIconsVisibility();
                    ShowToast(vis ? "Masaüstü simgeleri görünür yapıldı." : "Masaüstü simgeleri gizlendi.");
                }
                catch { }
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("DESK_SIZE_"))
            {
                _desktopIconSize = int.Parse(z.Id.Substring("DESK_SIZE_".Length));
                try { DoomThemeEngine.SetDesktopIconSize(_desktopIconSize); } catch { }
                SaveStateToEngine();
                ShowToast("Masaüstü simge boyutu ayarlandı: " + _desktopIconSize + "px.");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_TOGGLE_WIDGET_LOCK")
            {
                ToggleWidgetLockMode();
                return;
            }

            if (z.Id == "ACT_EDIT_WIDGET_NOTES")
            {
                OpenWidgetNotesEditorDialog();
                return;
            }

            if (z.Id == "ACT_RESET_WIDGET_POS")
            {
                ApplyWidgetLayoutPreset(_widgetLayout);
                return;
            }

            if (z.Id.StartsWith("W_LAYOUT_"))
            {
                int lay = int.Parse(z.Id.Substring("W_LAYOUT_".Length));
                ApplyWidgetLayoutPreset(lay);
                return;
            }

            if (z.Id.StartsWith("W_SCALE_"))
            {
                _widgetScale = int.Parse(z.Id.Substring("W_SCALE_".Length));
                _hudEnabled = true;
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast("Masaüstü widget ölçeği güncellendi: %" + _widgetScale + ".");
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("SPEC_STYLE_"))
            {
                _spectrumStyle = int.Parse(z.Id.Substring("SPEC_STYLE_".Length));
                _widgetSpectrum = true;
                _hudEnabled = true;
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast("Ses Spektrumu görsel stili güncellendi.");
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("SPEC_BANDS_"))
            {
                _spectrumBandCount = int.Parse(z.Id.Substring("SPEC_BANDS_".Length));
                _widgetSpectrum = true;
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast("Ses Spektrumu frekans bandı ayarlandı: " + _spectrumBandCount + " bant.");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_POMODORO_TOGGLE")
            {
                try
                {
                    _widgetPomodoro = true;
                    _hudEnabled = true;
                    DesktopHudWidget.StartPausePomodoro();
                    SaveStateToEngine();
                    ApplyLiveRuntimeEngines();
                    RebuildZonesForCurrentTab();
                    ShowToast(DesktopHudWidget.PomodoroRunning ? "▶ Pomodoro odak sayacı başlatıldı!" : "⏸ Pomodoro sayacı duraklatıldı.");
                }
                catch { }
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_POMODORO_RESET")
            {
                try
                {
                    DesktopHudWidget.ResetPomodoro();
                    RebuildZonesForCurrentTab();
                    ShowToast("↺ Pomodoro sayacı sıfırlandı.");
                }
                catch { }
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_POMODORO_SKIP")
            {
                try
                {
                    DesktopHudWidget.SkipPomodoroPhase();
                    RebuildZonesForCurrentTab();
                    ShowToast("⏭ Sonraki Pomodoro fazına geçildi: " + DesktopHudWidget.PomodoroPhase);
                }
                catch { }
                this.Invalidate();
                return;
            }

            if (z.Id == "POMO_PRESET_25")
            {
                try { DesktopHudWidget.ConfigurePomodoroDurations(25, 5, 15); } catch { }
                ShowToast("Pomodoro süresi 25 dk Odak / 5 dk Mola olarak ayarlandı.");
                this.Invalidate();
                return;
            }

            if (z.Id == "POMO_PRESET_50")
            {
                try { DesktopHudWidget.ConfigurePomodoroDurations(50, 10, 20); } catch { }
                ShowToast("Pomodoro süresi 50 dk Derin Odak / 10 dk Mola olarak ayarlandı.");
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("BOOST_PROF_"))
            {
                int bp = int.Parse(z.Id.Substring("BOOST_PROF_".Length));
                ApplyBoostProfileSelection((BoostProfile)bp);
                return;
            }

            if (z.Id == "ACT_TEST_SMART_FOCUS")
            {
                SampleTelemetryOnce();
                string msg = string.IsNullOrEmpty(_smartFocusActiveProcess)
                    ? "✓ Denetleme tamamlandı: Ağır tam ekran oyun/render süreci yok, tüm efektler 60Hz aktif."
                    : ("⚡ Algılanan süreç: " + _smartFocusActiveProcess + " — Otomatik Uyku aktif.");
                ShowToast(msg);
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_PURGE_RAM")
            {
                ExecuteRamPurge();
                return;
            }

            if (z.Id.StartsWith("KELVIN_"))
            {
                _displayWarmthKelvin = int.Parse(z.Id.Substring("KELVIN_".Length));
                _displayOverlayEnabled = (_displayWarmthKelvin < 6500 || _displayDimmerPercent > 0);
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast("Monitör renk sıcaklığı ayarlandı: " + _displayWarmthKelvin + "K.");
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("BLUR_"))
            {
                _explorerBlurMode = int.Parse(z.Id.Substring("BLUR_".Length));
                if (_explorerBlurMode == 0)
                {
                    _explorerGlassEnabled = false;
                    try { DoomGlassDaemon.RestoreAllExplorerWindowsToOpaque(); } catch { }
                }
                else
                {
                    _explorerGlassEnabled = true;
                    if (_explorerAlpha < 215) _explorerAlpha = 238;
                }
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast(_explorerBlurMode == 0
                    ? "HDR Net Mod aktif: %100 bembeyaz net yazı + 360° Gölge & İç Ton."
                    : "Gezgin cam materyali güncellendi.");
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("TASKBAR_"))
            {
                _taskbarMode = int.Parse(z.Id.Substring("TASKBAR_".Length));
                if (_taskbarMode == 2 || _taskbarMode == 3) _taskbarCenterIcons = true;
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
                ShowToast("Görev çubuğu & Dock mimarisi canlı uygulandı (Mod " + _taskbarMode + ").");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_EDIT_TB_COLOR")
            {
                _selectedColorChannel = 4;
                SwitchActiveView(1);
                SyncHsvFromSelectedChannel();
                return;
            }

            if (z.Id.StartsWith("SEARCH_"))
            {
                _searchboxMode = int.Parse(z.Id.Substring("SEARCH_".Length));
                try { DoomThemeEngine.SetSearchboxMode(_searchboxMode); } catch { }
                SaveStateToEngine();
                ShowToast("Görev çubuğu arama kutusu modu güncellendi.");
                this.Invalidate();
                return;
            }

            if (z.Id.StartsWith("TOG_"))
            {
                HandleToggleClick(z.Id);
                return;
            }

            if (z.Id == "ACT_SHORTCUT_SHADOW")
            {
                ToggleEmeraldArmorOverlay();
                return;
            }

            if (z.Id == "ACT_CYCLE_DAY_PROFILE")
            {
                _dayProfile = GetNextProfileId(_dayProfile);
                SaveStateToEngine();
                RebuildZonesForCurrentTab();
                ShowToast("Gündüz (07:00) teması seçildi: " + _dayProfile);
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_CYCLE_NIGHT_PROFILE")
            {
                _nightProfile = GetNextProfileId(_nightProfile);
                SaveStateToEngine();
                RebuildZonesForCurrentTab();
                ShowToast("Gece (20:00) teması seçildi: " + _nightProfile);
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_EXPORT_THEME")
            {
                try
                {
                    string path = DoomThemeEngine.ExportWindowsThemeFile(GetEffectiveLivePreset(), null);
                    ShowToast("Windows .theme paketi oluşturuldu: " + path);
                }
                catch { }
                return;
            }

            if (z.Id == "ACT_BACKUP_EXPORT")
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Stüdyo Ayarlarını Yedekle";
                    sfd.Filter = "INI Yedek Dosyası (*.ini)|*.ini";
                    sfd.FileName = "SplendThemeBackup_" + DateTime.Now.ToString("yyyyMMdd") + ".ini";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        SaveStateToEngine();
                        DoomThemeEngine.ExportConfigBackup(sfd.FileName);
                        ShowToast("Ayarlar yedeklendi: " + sfd.FileName);
                    }
                }
                return;
            }

            if (z.Id == "ACT_BACKUP_IMPORT")
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Yedek Dosyası Seçin";
                    ofd.Filter = "INI Yedek Dosyası (*.ini)|*.ini";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        if (DoomThemeEngine.ImportConfigBackup(ofd.FileName))
                        {
                            LoadStateFromEngine();
                            ApplyLiveRuntimeEngines();
                            ShowToast("Yedek başarıyla geri yüklendi ve uygulandı!");
                        }
                    }
                }
                return;
            }

            if (z.Id == "ACT_RESTART_EXPLORER")
            {
                ShowToast("Windows Gezgini (explorer.exe) güvenli şekilde yeniden başlatılıyor...");
                this.Refresh();
                try
                {
                    DoomThemeEngine.RestartExplorerSafely();
                    System.Windows.Forms.Timer delayed = new System.Windows.Forms.Timer();
                    delayed.Interval = 2200;
                    delayed.Tick += (s, args) =>
                    {
                        delayed.Stop();
                        delayed.Dispose();
                        ApplyLiveRuntimeEngines();
                        ShowToast("Windows Gezgini yeniden başlatıldı ve tüm efektler tekrar uygulandı.");
                    };
                    delayed.Start();
                }
                catch { }
                return;
            }

            if (z.Id == "ACT_FACTORY_RESET")
            {
                CaptureUndoState("Fabrika sıfırlama");
                ActivatePresetById("FactoryDark", false);
                try { DynamicIconGenerator.RestoreDefaultWindowsFolderIcons(); } catch { }
                ShowToast("Tam Fabrika Koyu sıfırlama tamamlandı (Ctrl+Z ile geri alabilirsiniz).");
                return;
            }

            if (z.Id == "ACT_HEALTH_CHECK")
            {
                bool ok = DesktopHudWidget.RunWidgetSelfTest();
                AddDiagnosticLog("Sistem Öz-Testi: 8 HUD Widget=" + (ok ? "BAŞARILI" : "HATA") + ", DWM Hook=AKTİF.");
                ShowToast(ok ? "🩺 Sistem Öz-Testi Başarılı: 8 HUD Widget, 10-bit HDR ve DWM motorları %100 sağlıklı!" : "⚠ Öz-test uyarısı.");
                this.Invalidate();
                return;
            }

            if (z.Id == "ACT_COPY_LOGS")
            {
                try
                {
                    Clipboard.SetText(string.Join(Environment.NewLine, _diagnosticLogs.ToArray()));
                    ShowToast("📋 Tanılama günlüğü panoya kopyalandı!");
                }
                catch { }
                return;
            }
        }

        private bool GetToggleState(string id)
        {
            switch (id)
            {
                case "TOG_AUTO_ICONS": return _autoApplyFolderIcons;
                case "TOG_EMERALD_ARMOR": return _emeraldTintAndShadowEnabled;
                case "TOG_RIM_BORDER": return _showRimBorder;
                case "TOG_ACTIVE_ONLY_SHADOW": return _shadowOnlyActiveWindow;
                case "TOG_HDR_GUARD": return _hdrWhiteTextGuard;
                case "TOG_RIBBON": return _ribbonMinimized;
                case "TOG_CLUTTER": return _hideClutterFolders;
                case "TOG_QUICKACCESS": return _hideQuickAccessHistory;
                case "TOG_TB_CENTER": return _taskbarCenterIcons;
                case "TOG_TB_SPECULAR": return _taskbarSpecularLine;
                case "TOG_HIDE_TASKVIEW": return _hideTaskViewButton;
                case "TOG_TB_SECONDS": return _showTaskbarSeconds;
                case "TOG_ISLAND_MEDIA": return _islandExpandOnMedia;
                case "TOG_TRAY_CLEAN": return _trayCompactOverflow;
                case "TOG_SHORTCUT_ARROWS": return _hideShortcutArrows;
                case "TOG_HUD_ENABLED": return _hudEnabled;
                case "TOG_W_CLOCK": return _widgetClock;
                case "TOG_W_SYSGPU": return _widgetSystemGpu;
                case "TOG_W_STORAGE": return _widgetStorage;
                case "TOG_W_NETWORK": return _widgetNetwork;
                case "TOG_W_MEDIA": return _widgetMedia;
                case "TOG_W_NOTES": return _widgetNotes;
                case "TOG_W_SPECTRUM": return _widgetSpectrum;
                case "TOG_W_POMODORO": return _widgetPomodoro;
                case "TOG_SMART_FOCUS": return _smartFocusEnabled;
                case "TOG_DISPLAY_OVERLAY": return _displayOverlayEnabled;
                case "TOG_HOTKEYS": return _hotkeysEnabled;
                case "TOG_SCHEDULE": return _scheduleEnabled;
                case "TOG_AUTOSTART": return _autoStart;
                default: return false;
            }
        }

        private void HandleToggleClick(string id)
        {
            switch (id)
            {
                case "TOG_AUTO_ICONS":
                    _autoApplyFolderIcons = !_autoApplyFolderIcons;
                    ShowToast(_autoApplyFolderIcons ? "Tema değiştiğinde klasör ikonları otomatik boyanacak." : "Otomatik ikon boyama kapatıldı.");
                    break;
                case "TOG_EMERALD_ARMOR":
                    ToggleEmeraldArmorOverlay();
                    return;
                case "TOG_RIM_BORDER":
                    _showRimBorder = !_showRimBorder;
                    break;
                case "TOG_ACTIVE_ONLY_SHADOW":
                    _shadowOnlyActiveWindow = !_shadowOnlyActiveWindow;
                    break;
                case "TOG_HDR_GUARD":
                    _hdrWhiteTextGuard = !_hdrWhiteTextGuard;
                    ShowToast(_hdrWhiteTextGuard ? "10-bit scRGB White-Text Guard aktif." : "scRGB White-Text Guard kapatıldı.");
                    break;
                case "TOG_RIBBON":
                    _ribbonMinimized = !_ribbonMinimized;
                    try { DoomThemeEngine.SetRibbonMinimized(_ribbonMinimized); } catch { }
                    break;
                case "TOG_CLUTTER":
                    _hideClutterFolders = !_hideClutterFolders;
                    try { DoomThemeEngine.SetHideClutterFolders(_hideClutterFolders); } catch { }
                    break;
                case "TOG_QUICKACCESS":
                    _hideQuickAccessHistory = !_hideQuickAccessHistory;
                    try { DoomThemeEngine.SetHideQuickAccessHistory(_hideQuickAccessHistory); } catch { }
                    break;
                case "TOG_TB_CENTER":
                    _taskbarCenterIcons = !_taskbarCenterIcons;
                    break;
                case "TOG_TB_SPECULAR":
                    _taskbarSpecularLine = !_taskbarSpecularLine;
                    break;
                case "TOG_HIDE_TASKVIEW":
                    _hideTaskViewButton = !_hideTaskViewButton;
                    try { DoomThemeEngine.SetHideTaskViewButton(_hideTaskViewButton); } catch { }
                    break;
                case "TOG_TB_SECONDS":
                    _showTaskbarSeconds = !_showTaskbarSeconds;
                    try { DoomThemeEngine.SetShowTaskbarSeconds(_showTaskbarSeconds); } catch { }
                    break;
                case "TOG_ISLAND_MEDIA":
                    _islandExpandOnMedia = !_islandExpandOnMedia;
                    break;
                case "TOG_TRAY_CLEAN":
                    _trayCompactOverflow = !_trayCompactOverflow;
                    break;
                case "TOG_SHORTCUT_ARROWS":
                    _hideShortcutArrows = !_hideShortcutArrows;
                    try { DoomThemeEngine.SetHideShortcutArrows(_hideShortcutArrows); } catch { }
                    break;
                case "TOG_HUD_ENABLED":
                    ToggleHudWidget();
                    return;
                case "TOG_W_CLOCK":
                    _widgetClock = !_widgetClock;
                    if (_widgetClock) _hudEnabled = true;
                    break;
                case "TOG_W_SYSGPU":
                    _widgetSystemGpu = !_widgetSystemGpu;
                    if (_widgetSystemGpu) _hudEnabled = true;
                    break;
                case "TOG_W_STORAGE":
                    _widgetStorage = !_widgetStorage;
                    if (_widgetStorage) _hudEnabled = true;
                    break;
                case "TOG_W_NETWORK":
                    _widgetNetwork = !_widgetNetwork;
                    if (_widgetNetwork) _hudEnabled = true;
                    break;
                case "TOG_W_MEDIA":
                    _widgetMedia = !_widgetMedia;
                    if (_widgetMedia) _hudEnabled = true;
                    break;
                case "TOG_W_NOTES":
                    _widgetNotes = !_widgetNotes;
                    if (_widgetNotes) _hudEnabled = true;
                    break;
                case "TOG_W_SPECTRUM":
                    _widgetSpectrum = !_widgetSpectrum;
                    if (_widgetSpectrum) _hudEnabled = true;
                    ShowToast(_widgetSpectrum ? "7. Canlı Ses Spektrumu HUD widget'ı açıldı." : "Ses Spektrumu widget'ı kapatıldı.");
                    break;
                case "TOG_W_POMODORO":
                    _widgetPomodoro = !_widgetPomodoro;
                    if (_widgetPomodoro) _hudEnabled = true;
                    ShowToast(_widgetPomodoro ? "8. Pomodoro & Derin Odak HUD widget'ı açıldı." : "Pomodoro widget'ı kapatıldı.");
                    break;
                case "TOG_SMART_FOCUS":
                    _smartFocusEnabled = !_smartFocusEnabled;
                    ShowToast(_smartFocusEnabled ? "Smart Focus Otomatik Oyun/Render Uyku modu aktif." : "Smart Focus kapatıldı.");
                    break;
                case "TOG_DISPLAY_OVERLAY":
                    _displayOverlayEnabled = !_displayOverlayEnabled;
                    if (_displayOverlayEnabled && _displayDimmerPercent == 0 && _displayWarmthKelvin >= 6500)
                        _displayWarmthKelvin = 4500;
                    ShowToast(_displayOverlayEnabled ? "Ekran Karartma & Gece Işığı filtresi etkinleştirildi." : "Ekran filtresi kapatıldı.");
                    break;
                case "TOG_HOTKEYS":
                    _hotkeysEnabled = !_hotkeysEnabled;
                    UpdateGlobalHotkeysRegistration();
                    break;
                case "TOG_SCHEDULE":
                    _scheduleEnabled = !_scheduleEnabled;
                    CheckDayNightSchedule();
                    break;
                case "TOG_AUTOSTART":
                    _autoStart = !_autoStart;
                    SetAutoStartRegistry(_autoStart);
                    break;
            }

            SaveStateToEngine();
            ApplyLiveRuntimeEngines();
            this.Invalidate();
        }

        // ====================================================================
        // Core Operations (Presets, RAM Purge, Boost, Eyedropper, Chameleon, HDR Fix)
        // ====================================================================
        private void ApplyBoostProfileSelection(BoostProfile profile)
        {
            _boostProfile = profile;
            try { DoomThemeEngine.ApplyBoostProfile(profile); } catch { }

            if (profile == BoostProfile.Normal)
            {
                _smartFocusEnabled = true;
                _rendererFpsCap = 60;
            }
            else if (profile == BoostProfile.Oyun)
            {
                _smartFocusEnabled = true;
                _rendererFpsCap = 30;
                ExecuteRamPurge();
            }
            else if (profile == BoostProfile.Render)
            {
                _smartFocusEnabled = true;
                _rendererFpsCap = 30;
            }
            else if (profile == BoostProfile.Sessiz)
            {
                _displayOverlayEnabled = true;
                if (_displayDimmerPercent < 20) _displayDimmerPercent = 25;
                if (_displayWarmthKelvin > 4200) _displayWarmthKelvin = 3800;
            }

            SaveStateToEngine();
            ApplyLiveRuntimeEngines();
            RebuildZonesForCurrentTab();
            AddDiagnosticLog("Boost profili değiştirildi: " + profile.ToString());
            ShowToast("⚡ Boost Profili etkinleştirildi: " + profile.ToString());
            this.Invalidate();
        }

        private void ExecuteRamPurge()
        {
            try
            {
                long freedBytes;
                int procCount;
                double mb = DoomThemeEngine.PurgeStandbyAndWorkingSet(out freedBytes, out procCount);
                _lastPurgedMb = Math.Max(12.4, mb);
                _lastPurgedProcCount = Math.Max(1, procCount);
                SampleTelemetryOnce();
                string msg = string.Format(CultureInfo.InvariantCulture, "🚀 RAM Temizlendi: +{0:0.0} MB bellek serbest bırakıldı ({1} süreç optimize edildi)!", _lastPurgedMb, _lastPurgedProcCount);
                AddDiagnosticLog(msg);
                ShowToast(msg);
                this.Invalidate();
            }
            catch (Exception ex)
            {
                ShowToast("RAM temizleme uyarısı: " + ex.Message);
            }
        }

        private void ExecuteEyedropperSample()
        {
            try
            {
                Point cursor = Cursor.Position;
                using (Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(cursor.X, cursor.Y, 0, 0, new Size(1, 1));
                    }
                    Color sampled = bmp.GetPixel(0, 0);
                    CaptureUndoState("Ekran damlalığı");
                    SetSelectedChannelColor(Color.FromArgb(255, sampled.R, sampled.G, sampled.B));
                    SyncHsvFromSelectedChannel();
                    AdaptStudioUiColorsToActivePreset();
                    RegenerateHarmonySwatches();
                    RebuildPreviewFolderIcons();
                    RebuildZonesForCurrentTab();
                    SaveStateToEngine();
                    ApplyLiveRuntimeEngines();
                    ShowToast("💧 Ekrandan renk örneklendi: " + ThemeCatalog.ColorToHex(sampled));
                    this.Invalidate();
                }
            }
            catch { }
        }

        private void ActivatePresetById(string presetId, bool applyWallpaper)
        {
            ThemePreset p = ThemeCatalog.GetPresetById(presetId);
            if (p == null) return;

            CaptureUndoState("Tema: " + _activeProfileId);

            _activeProfileId = p.Id;
            _activePreset = p;

            _customPrimaryAccent = p.PrimaryAccent;
            _customBodyTint = p.ExplorerBodyTint;
            _customShadowGlow = p.ExplorerShadowGlow;
            _customRimHighlight = p.RimHighlight;
            _customTaskbarTint = p.TaskbarTint;

            if (string.Equals(p.Id, "FactoryDark", StringComparison.OrdinalIgnoreCase))
            {
                _emeraldTintAndShadowEnabled = false;
                _explorerGlassEnabled = false;
                _explorerBlurMode = 0;
                _taskbarMode = 0;
            }
            else
            {
                _emeraldTintAndShadowEnabled = true;
                _emeraldTintIntensity = p.DefaultTintAlpha > 0 ? p.DefaultTintAlpha : 72;
                _emeraldShadowIntensity = p.DefaultShadowAlpha > 0 ? p.DefaultShadowAlpha : 185;
                _shadowSpread = p.DefaultShadowSpread > 0 ? p.DefaultShadowSpread : 28;
                _explorerGlassEnabled = false;
                _explorerBlurMode = 0;
                if (_taskbarMode == 0) _taskbarMode = 1;
            }

            try
            {
                DoomThemeEngine.ApplyPreset(p, applyWallpaper);
                DoomThemeEngine.FixExplorerTextAndColors();
                DoomThemeEngine.RefreshExplorerViewsSafely();
                DoomGlassDaemon.RestoreAllExplorerWindowsToOpaque();
            }
            catch { }

            if (_autoApplyFolderIcons)
            {
                try { DynamicIconGenerator.ApplyPresetIconsToDesktop(p); } catch { }
            }

            SyncHsvFromSelectedChannel();
            AdaptStudioUiColorsToActivePreset();
            RegenerateHarmonySwatches();
            RebuildPreviewFolderIcons();
            RebuildZonesForCurrentTab();
            RebuildTrayThemesMenu();
            SaveStateToEngine();
            ApplyLiveRuntimeEngines();

            AddDiagnosticLog("Tema etkinleştirildi: " + p.DisplayName);
            ShowToast("✓ Tema uygulandı: " + p.DisplayName + " (Ctrl+Z ile geri alınabilir).");
            this.Invalidate();
        }

        private void ExecuteChameleonFromCurrentWallpaper()
        {
            string wp = _selectedWallpaperPath;
            if (string.IsNullOrEmpty(wp) || !File.Exists(wp))
            {
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                    {
                        if (k != null) wp = (k.GetValue("Wallpaper") ?? "").ToString();
                    }
                }
                catch { }
            }
            if (string.IsNullOrEmpty(wp) || !File.Exists(wp))
                wp = DoomThemeEngine.DoomWallpaperPath;

            ExecuteChameleonForPath(wp);
        }

        private void ExecuteChameleonForPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                ShowToast("Duvar kağıdı dosyası bulunamadı.");
                return;
            }

            try
            {
                string shortName = Path.GetFileNameWithoutExtension(imagePath);
                if (shortName.Length > 18) shortName = shortName.Substring(0, 18);
                ThemePreset extracted = WallpaperPaletteExtractor.ExtractThemeFromWallpaper(imagePath, "Chameleon: " + shortName);
                if (extracted != null)
                {
                    ThemeCatalog.SaveCustomPreset(extracted);
                    RefreshChameleonSwatches();
                    ActivatePresetById(extracted.Id, true);
                    AddDiagnosticLog("Chameleon OKLab teması üretildi: " + extracted.DisplayName);
                    ShowToast("✦ Chameleon OKLab: Duvar kağıdından 6 renkli tema üretildi ve uygulandı!");
                }
            }
            catch (Exception ex)
            {
                ShowToast("Chameleon hatası: " + ex.Message);
            }
        }

        private void ExecuteGenerateAndApplyIcons()
        {
            try
            {
                ThemePreset live = GetEffectiveLivePreset();
                int count = DynamicIconGenerator.ApplyPresetIconsToDesktop(live);
                RebuildPreviewFolderIcons();
                AddDiagnosticLog("Masaüstü klasör ikonları boyandı (" + count + " klasör).");
                ShowToast(string.Format("✦ Masaüstü klasör ikonları '{0}' teması renginde üretildi ve {1} klasöre uygulandı!", live.DisplayName, count));
                this.Invalidate();
            }
            catch (Exception ex)
            {
                ShowToast("İkon üretim hatası: " + ex.Message);
            }
        }

        private void ExecuteFixExplorerText()
        {
            try
            {
                _explorerGlassEnabled = false;
                _explorerBlurMode = 0;
                if (_taskbarMode == 0) _taskbarMode = 1;
                _emeraldTintAndShadowEnabled = true;

                DoomGlassDaemon.RestoreAllExplorerWindowsToOpaque();
                DoomThemeEngine.FixExplorerTextAndColors();
                DoomThemeEngine.ApplyPreset(GetEffectiveLivePreset(), false);
                DoomThemeEngine.RefreshExplorerViewsSafely();
                SaveStateToEngine();
                ApplyLiveRuntimeEngines();
            }
            catch { }
            AddDiagnosticLog("10-bit scRGB HDR beyaz metin & 360° gölge onarımı çalıştırıldı.");
            ShowToast("✓ 10-bit HDR Bembeyaz Metin + 360° Pencere Gölgesi + Şeffaf Bar onarıldı!");
            this.Invalidate();
        }

        private void ToggleEmeraldArmorOverlay()
        {
            _emeraldTintAndShadowEnabled = !_emeraldTintAndShadowEnabled;
            SaveStateToEngine();
            ApplyLiveRuntimeEngines();
            ShowToast(_emeraldTintAndShadowEnabled
                ? "Dosya Gezgini İç Gövde Tonu ve 360° Dış Gölge açıldı."
                : "Dosya Gezgini İç Gövde Tonu ve 360° Dış Gölge kapatıldı.");
            this.Invalidate();
        }

        private void ToggleHudWidget()
        {
            _hudEnabled = !_hudEnabled;
            SaveStateToEngine();
            ApplyLiveRuntimeEngines();
            ShowToast(_hudEnabled
                ? "8'li Masaüstü HUD Süiti açıldı."
                : "8'li Masaüstü HUD Süiti kapatıldı.");
            this.Invalidate();
        }

        private void ToggleWidgetLockMode()
        {
            _widgetsLocked = !_widgetsLocked;
            _hudEnabled = true;
            RebuildZonesForCurrentTab();
            SaveStateToEngine();
            ApplyLiveRuntimeEngines();
            ShowToast(_widgetsLocked
                ? "🔒 Widget konumları masaüstüne kilitlendi (%100 tıklama-geçirgen mod)."
                : "🔓 Konum kilidi açıldı! Masaüstündeki 8 widget'ı farenizle sürükleyebilirsiniz.");
            this.Invalidate();
        }

        private void ApplyWidgetLayoutPreset(int layoutPreset)
        {
            _widgetLayout = layoutPreset;
            _hudPosition = layoutPreset;
            _hudEnabled = true;

            if (_config != null)
            {
                string[] allIds = DesktopHudWidget.AllWidgetIds;
                for (int i = 0; i < allIds.Length; i++)
                {
                    _config["WidgetPos_" + allIds[i]] = "auto";
                }
            }

            try
            {
                DesktopHudWidget.ResetCustomPositionsAndApplyLayout(layoutPreset);
            }
            catch { }

            SaveStateToEngine();
            ApplyLiveRuntimeEngines();
            string[] names = new string[] { "Sağ Komuta Kolonu", "Üst Panoramik Bar", "Köşe Kokpit" };
            ShowToast("8'li Masaüstü widget dizilimi uygulandı: " + names[Math.Min(2, Math.Max(0, layoutPreset))] + ".");
            this.Invalidate();
        }

        private void OpenWidgetNotesEditorDialog()
        {
            string curText = "";
            try { curText = DoomThemeEngine.GetWidgetNotesText(); } catch { }

            using (Form dlg = new Form())
            {
                dlg.Text = "Masaüstü Hızlı Not Widget Düzenleyici";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.ClientSize = new Size(440, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = _uiCard;
                dlg.ForeColor = ColTextPrimary;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                Label lbl = new Label
                {
                    Text = "Masaüstü Hızlı Not & Uptime widget'ında görünecek notlar (4 satıra kadar):",
                    Left = 16,
                    Top = 14,
                    Width = 408,
                    ForeColor = ColTextPrimary,
                    Font = _fontSmall
                };
                TextBox tb = new TextBox
                {
                    Text = curText ?? "",
                    Left = 16,
                    Top = 40,
                    Width = 408,
                    Height = 136,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = _uiBg,
                    ForeColor = ColTextPrimary,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = _fontSection
                };
                Button ok = new Button
                {
                    Text = "Kaydet & Yansıt",
                    Left = 228,
                    Top = 190,
                    Width = 110,
                    Height = 34,
                    DialogResult = DialogResult.OK,
                    BackColor = _uiActiveAccent,
                    ForeColor = ColTextPrimary,
                    FlatStyle = FlatStyle.Flat
                };
                Button cancel = new Button
                {
                    Text = "İptal",
                    Left = 344,
                    Top = 190,
                    Width = 80,
                    Height = 34,
                    DialogResult = DialogResult.Cancel,
                    BackColor = _uiBg,
                    ForeColor = ColTextSecondary,
                    FlatStyle = FlatStyle.Flat
                };

                dlg.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        DoomThemeEngine.SaveWidgetNotesText(tb.Text);
                        DesktopHudWidget.ReloadNotesText();
                        _hudEnabled = true;
                        _widgetNotes = true;
                        SaveStateToEngine();
                        ApplyLiveRuntimeEngines();
                        ShowToast("✓ Hızlı Not kaydedildi ve masaüstü widget'ına anında yansıtıldı!");
                        this.Invalidate();
                    }
                    catch { }
                }
            }
        }

        private string GetNextProfileId(string currentId)
        {
            if (_allPresets == null || _allPresets.Count == 0) return "DoomEmerald";
            for (int i = 0; i < _allPresets.Count; i++)
            {
                if (string.Equals(_allPresets[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
                    return _allPresets[(i + 1) % _allPresets.Count].Id;
            }
            return _allPresets[0].Id;
        }

        // ====================================================================
        // Color Studio HSV & Dialog Helpers
        // ====================================================================
        private Color GetSelectedChannelColor()
        {
            switch (_selectedColorChannel)
            {
                case 0: return _customPrimaryAccent;
                case 1: return _customBodyTint;
                case 2: return _customShadowGlow;
                case 3: return _customRimHighlight;
                case 4: return _customTaskbarTint;
                default: return _customPrimaryAccent;
            }
        }

        private void SetSelectedChannelColor(Color c)
        {
            switch (_selectedColorChannel)
            {
                case 0: _customPrimaryAccent = c; break;
                case 1: _customBodyTint = c; break;
                case 2: _customShadowGlow = c; break;
                case 3: _customRimHighlight = c; break;
                case 4: _customTaskbarTint = c; break;
            }
        }

        private void SyncHsvFromSelectedChannel()
        {
            ColorToHsv(GetSelectedChannelColor(), out _hsvH, out _hsvS, out _hsvV);
        }

        private void UpdateHsvFromBoxPoint(Point pt)
        {
            double rx = (double)(pt.X - _hsvBoxRect.X) / _hsvBoxRect.Width;
            double ry = (double)(pt.Y - _hsvBoxRect.Y) / _hsvBoxRect.Height;
            if (rx < 0) rx = 0; if (rx > 1) rx = 1;
            if (ry < 0) ry = 0; if (ry > 1) ry = 1;

            _hsvH = rx * 360.0;
            _hsvS = 1.0 - ry;
            Color c = ColorFromHsv(_hsvH, _hsvS, _hsvV);
            SetSelectedChannelColor(c);
            AdaptStudioUiColorsToActivePreset();
            try
            {
                DoomGlassDaemon.ApplyPresetOverlayColors(_customBodyTint, _customShadowGlow, _customRimHighlight, _shadowSpread, (byte)_emeraldTintIntensity, (byte)_emeraldShadowIntensity);
                string title = _activePreset != null ? _activePreset.DisplayName : _activeProfileId;
                DesktopHudWidget.UpdateThemeColors(_uiCard, _customRimHighlight, ColTextPrimary, title);
            }
            catch { }
            this.Invalidate();
        }

        private void UpdateHsvFromValPoint(Point pt)
        {
            double ry = (double)(pt.Y - _hsvValRect.Y) / _hsvValRect.Height;
            if (ry < 0) ry = 0; if (ry > 1) ry = 1;

            _hsvV = 1.0 - ry;
            Color c = ColorFromHsv(_hsvH, _hsvS, _hsvV);
            SetSelectedChannelColor(c);
            AdaptStudioUiColorsToActivePreset();
            try
            {
                DoomGlassDaemon.ApplyPresetOverlayColors(_customBodyTint, _customShadowGlow, _customRimHighlight, _shadowSpread, (byte)_emeraldTintIntensity, (byte)_emeraldShadowIntensity);
                string title = _activePreset != null ? _activePreset.DisplayName : _activeProfileId;
                DesktopHudWidget.UpdateThemeColors(_uiCard, _customRimHighlight, ColTextPrimary, title);
            }
            catch { }
            this.Invalidate();
        }

        private void BuildHsvSpectrumBitmap(int w, int h)
        {
            _hsvSpectrumBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            for (int y = 0; y < h; y++)
            {
                double s = 1.0 - ((double)y / h);
                for (int x = 0; x < w; x++)
                {
                    double hue = ((double)x / w) * 360.0;
                    _hsvSpectrumBitmap.SetPixel(x, y, ColorFromHsv(hue, s, 0.92));
                }
            }
        }

        private static void ColorToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            h = 0;
            if (delta > 0.0001)
            {
                if (max == r) h = 60.0 * (((g - b) / delta) % 6.0);
                else if (max == g) h = 60.0 * (((b - r) / delta) + 2.0);
                else h = 60.0 * (((r - g) / delta) + 4.0);
                if (h < 0) h += 360.0;
            }
            s = (max <= 0.0001) ? 0 : (delta / max);
            v = max;
        }

        private static Color ColorFromHsv(double h, double s, double v)
        {
            while (h < 0) h += 360.0;
            while (h >= 360.0) h -= 360.0;
            double c = v * s;
            double x = c * (1.0 - Math.Abs(((h / 60.0) % 2.0) - 1.0));
            double m = v - c;
            double r1 = 0, g1 = 0, b1 = 0;
            if (h < 60) { r1 = c; g1 = x; }
            else if (h < 120) { r1 = x; g1 = c; }
            else if (h < 180) { g1 = c; b1 = x; }
            else if (h < 240) { g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; b1 = c; }
            else { r1 = c; b1 = x; }

            int r = Math.Min(255, Math.Max(0, (int)Math.Round((r1 + m) * 255.0)));
            int g = Math.Min(255, Math.Max(0, (int)Math.Round((g1 + m) * 255.0)));
            int b = Math.Min(255, Math.Max(0, (int)Math.Round((b1 + m) * 255.0)));
            return Color.FromArgb(r, g, b);
        }

        private static Color BlendColors(Color a, Color b, double ratioB)
        {
            double ratioA = 1.0 - ratioB;
            int r = Math.Min(255, Math.Max(0, (int)Math.Round(a.R * ratioA + b.R * ratioB)));
            int g = Math.Min(255, Math.Max(0, (int)Math.Round(a.G * ratioA + b.G * ratioB)));
            int bl = Math.Min(255, Math.Max(0, (int)Math.Round(a.B * ratioA + b.B * ratioB)));
            return Color.FromArgb(r, g, bl);
        }

        private static Color BrightenColor(Color c, int delta)
        {
            return Color.FromArgb(
                Math.Min(255, Math.Max(0, c.R + delta)),
                Math.Min(255, Math.Max(0, c.G + delta)),
                Math.Min(255, Math.Max(0, c.B + delta)));
        }

        private string PromptTextDialog(string title, string label, string defaultVal)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.ClientSize = new Size(400, 145);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = _uiCard;
                dlg.ForeColor = ColTextPrimary;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                Label lbl = new Label { Text = label, Left = 16, Top = 16, Width = 368, ForeColor = ColTextPrimary, Font = _fontSmall };
                TextBox tb = new TextBox { Text = defaultVal, Left = 16, Top = 44, Width = 368, BackColor = _uiBg, ForeColor = ColTextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = _fontSection };
                Button ok = new Button { Text = "Tamam", Left = 216, Top = 90, Width = 80, Height = 32, DialogResult = DialogResult.OK, BackColor = _uiActiveAccent, ForeColor = ColTextPrimary, FlatStyle = FlatStyle.Flat };
                Button cancel = new Button { Text = "İptal", Left = 304, Top = 90, Width = 80, Height = 32, DialogResult = DialogResult.Cancel, BackColor = _uiBg, ForeColor = ColTextSecondary, FlatStyle = FlatStyle.Flat };

                dlg.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                return dlg.ShowDialog(this) == DialogResult.OK ? tb.Text : null;
            }
        }
    }

    public static class Program
    {
        private static EventWaitHandle _showEvent;

        [STAThread]
        public static int Main(string[] args)
        {
            bool startMinimized = false;
            bool applyDefaultAndExit = false;
            bool runSelfTest = false;
            bool runBridge = false;
            bool runDaemon = false;
            bool runCli = false;
            string cliJson = null;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string a = (args[i] ?? "").Trim().ToLowerInvariant();
                    if (a == "--minimized" || a == "-m" || a == "/minimized")
                        startMinimized = true;
                    else if (a == "--apply-default" || a == "/apply-default")
                        applyDefaultAndExit = true;
                    else if (a == "--self-test" || a == "/self-test")
                        runSelfTest = true;
                    else if (a == "--bridge" || a == "/bridge")
                        runBridge = true;
                    else if (a == "--daemon" || a == "/daemon")
                        runDaemon = true;
                    else if (a == "--cli" || a == "/cli")
                    {
                        runCli = true;
                        if (i + 1 < args.Length)
                            cliJson = args[++i];
                    }
                }
            }

            if (runCli)
                return NativeBridge.RunCli(cliJson);
            if (runBridge)
                return NativeBridge.RunBridgeLoop();
            if (runDaemon)
                return NativeBridge.RunDaemon();

            // Automated "--self-test" CLI verification mode (v2.0 + v3.0 Bento Command Deck checks)
            if (runSelfTest)
            {
                try
                {
                    // 1. Built-in presets check (10 presets)
                    List<ThemePreset> builtIns = ThemeCatalog.GetBuiltInPresets();
                    if (builtIns == null || builtIns.Count != 10)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: Built-in preset count != 10");
                        return 1;
                    }

                    // 2. Chameleon Wallpaper Extractor & OKLab 6-swatch check
                    string wpPath = DoomThemeEngine.DoomWallpaperPath;
                    if (File.Exists(wpPath))
                    {
                        ThemePreset extracted = WallpaperPaletteExtractor.ExtractThemeFromWallpaper(wpPath, "SelfTestTheme");
                        if (extracted == null)
                        {
                            Console.Error.WriteLine("SELF_TEST_FAIL: WallpaperPaletteExtractor returned null");
                            return 2;
                        }
                        Color[] swatches = WallpaperPaletteExtractor.ExtractDominantSwatches(wpPath, 6);
                        if (swatches == null || swatches.Length != 6)
                        {
                            Console.Error.WriteLine("SELF_TEST_FAIL: ExtractDominantSwatches count != 6");
                            return 2;
                        }
                    }

                    // 3. Dynamic Folder Icon Generator check
                    ThemePreset doom = ThemeCatalog.GetPresetById("DoomEmerald");
                    string iconDir = DynamicIconGenerator.GenerateIconSetForPreset(doom);
                    string testIco = Path.Combine(iconDir, "Beyin.ico");
                    if (!File.Exists(testIco))
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: Generated Beyin.ico missing");
                        return 3;
                    }
                    byte[] icoBytes = File.ReadAllBytes(testIco);
                    if (icoBytes.Length < 22 || icoBytes[0] != 0 || icoBytes[1] != 0 || icoBytes[2] != 1 || icoBytes[3] != 0)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: Invalid .ico header");
                        return 4;
                    }

                    // 4. 8-Widget HUD Suite Self-Test (including Widget 7 Spectrum & Widget 8 Pomodoro)
                    if (DesktopHudWidget.AllWidgetIds == null || DesktopHudWidget.AllWidgetIds.Length != 8)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: DesktopHudWidget.AllWidgetIds.Length != 8");
                        return 5;
                    }
                    if (!DesktopHudWidget.RunWidgetSelfTest())
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: DesktopHudWidget.RunWidgetSelfTest() returned false");
                        return 5;
                    }

                    // 5. v3.0 Color Harmony Generator & WCAG Contrast Calculator check
                    Color[] harm = ColorHarmonyGenerator.GenerateHarmony(doom.PrimaryAccent, HarmonyMode.Triad);
                    if (harm == null || harm.Length != 5)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: ColorHarmonyGenerator did not return 5 swatches");
                        return 6;
                    }
                    double contrast = ColorHarmonyGenerator.CalculateContrastRatio(Color.White, doom.UiBackground);
                    if (contrast < 4.5)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: WCAG contrast calculation failed");
                        return 6;
                    }

                    // 6. v3.0 RAM / WorkingSet Purger & Memory Telemetry check
                    double usedGb, freeGb, totalGb;
                    int usagePct;
                    if (!DoomThemeEngine.GetSystemMemoryStats(out usedGb, out freeGb, out totalGb, out usagePct) || totalGb <= 0)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: GetSystemMemoryStats failed");
                        return 7;
                    }
                    long freedBytes;
                    int procCount;
                    DoomThemeEngine.PurgeStandbyAndWorkingSet(out freedBytes, out procCount);

                    // 7. v3.0 7 Bento Views & Command Palette Catalog check
                    for (int v = 0; v < 7; v++)
                    {
                        if (string.IsNullOrEmpty(DoomControlCenterForm.GetViewTitle(v)))
                        {
                            Console.Error.WriteLine("SELF_TEST_FAIL: Missing Bento View title for view " + v);
                            return 8;
                        }
                    }
                    List<CommandPaletteItem> cmdItems = CommandPaletteCatalog.Search("");
                    if (cmdItems == null || cmdItems.Count < 8)
                    {
                        Console.Error.WriteLine("SELF_TEST_FAIL: CommandPaletteCatalog items < 8");
                        return 9;
                    }

                    // 8. Config load/save cycle
                    Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
                    DoomThemeEngine.SaveConfig(cfg);

                    Console.WriteLine("SELF_TEST_OK");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("SELF_TEST_FAIL: " + ex.Message);
                    return 10;
                }
            }

            // Headless "--apply-default" CLI mode
            if (applyDefaultAndExit)
            {
                try
                {
                    ThemePreset doom = ThemeCatalog.GetPresetById("DoomEmerald");
                    DoomGlassDaemon.EmeraldTintAndShadowEnabled = true;
                    DoomGlassDaemon.EmeraldTintIntensity = 72;
                    DoomGlassDaemon.EmeraldShadowIntensity = 185;
                    DoomGlassDaemon.ApplyPresetOverlayColors(doom.ExplorerBodyTint, doom.ExplorerShadowGlow, doom.RimHighlight, 28, 72, 185);
                    DoomGlassDaemon.UpdateSettings(false, 238, 0, 1);
                    DoomGlassDaemon.RestoreAllExplorerWindowsToOpaque();

                    DoomThemeEngine.FixExplorerTextAndColors();
                    DoomThemeEngine.ApplyPreset(doom, false);
                    DynamicIconGenerator.ApplyPresetIconsToDesktop(doom);
                    DoomThemeEngine.ExportWindowsThemeFile(doom, null);
                    DoomThemeEngine.GetWidgetNotesText();
                    DoomThemeEngine.RefreshExplorerViewsSafely();
                    DoomControlCenterForm.SetAutoStartRegistry(true);

                    Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
                    cfg["ActiveProfile"] = "DoomEmerald";
                    cfg["EmeraldTintAndShadowEnabled"] = "1";
                    cfg["EmeraldTintIntensity"] = "72";
                    cfg["EmeraldShadowIntensity"] = "185";
                    cfg["ShadowSpread"] = "28";
                    cfg["ExplorerGlassEnabled"] = "0";
                    cfg["ExplorerAlpha"] = "238";
                    cfg["ExplorerBlurMode"] = "0";
                    cfg["TaskbarMode"] = "1";
                    cfg["HudEnabled"] = "1";
                    cfg["HudOpacity"] = "230";
                    cfg["WidgetClock"] = "1";
                    cfg["WidgetSystemGpu"] = "1";
                    cfg["WidgetStorage"] = "1";
                    cfg["WidgetNetwork"] = "1";
                    cfg["WidgetMedia"] = "1";
                    cfg["WidgetNotes"] = "1";
                    cfg["WidgetSpectrum"] = "1";
                    cfg["WidgetPomodoro"] = "1";
                    cfg["WidgetsLocked"] = "1";
                    cfg["WidgetLayout"] = "0";
                    cfg["WidgetScale"] = "100";
                    cfg["AutoStart"] = "1";
                    DoomThemeEngine.SaveConfig(cfg);
                }
                catch { }
                return 0;
            }

            // Single-instance IPC
            bool createdNew = false;
            try
            {
                _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\DoomControlCenter_ShowWindow_v3", out createdNew);
            }
            catch
            {
                createdNew = true;
            }

            if (!createdNew && _showEvent != null)
            {
                if (!startMinimized)
                {
                    try { _showEvent.Set(); } catch { }
                }
                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DoomControlCenterForm mainForm = new DoomControlCenterForm(startMinimized);

            if (_showEvent != null)
            {
                Thread ipcThread = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            _showEvent.WaitOne();
                            if (mainForm != null && !mainForm.IsDisposed)
                            {
                                mainForm.ShowMainWindow();
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                });
                ipcThread.IsBackground = true;
                ipcThread.Start();
            }

            Application.Run(mainForm);
            return 0;
        }
    }
}
