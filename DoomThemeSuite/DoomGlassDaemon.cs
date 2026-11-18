using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DoomThemeSuite
{
    public static class DoomGlassDaemon
    {
        // ====================================================================
        // Win32 & DWM P/Invoke Declarations
        // ====================================================================

        private delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst,
            ref POINT pptDst,
            ref SIZE psize,
            IntPtr hdcSrc,
            ref POINT pptSrc,
            uint crKey,
            ref BLENDFUNCTION pblend,
            uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int Width;
            public int Height;
            public SIZE(int w, int h) { Width = w; Height = h; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DWM_BLURBEHIND
        {
            public uint dwFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fEnable;
            public IntPtr hRgnBlur;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fTransitionOnMaximized;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        private struct ACCENT_POLICY
        {
            public int AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINCOMPATTRDATA
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINCOMPATTRDATA data);

        // ====================================================================
        // Win32 / DWM Constants
        // ====================================================================

        private const int GWL_EXSTYLE = -20;
        private const int GWLP_HWNDPARENT = -8;

        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const uint LWA_ALPHA = 0x02;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const uint ULW_ALPHA = 0x00000002;

        private const uint GW_HWNDPREV = 3;
        private const int SW_HIDE = 0;
        private const int SW_SHOWNA = 8;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOREDRAW = 0x0008;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_HIDEWINDOW = 0x0080;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOSENDCHANGING = 0x0400;
        private const uint SWP_ASYNCWINDOWPOS = 0x4000;

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const int DWMWA_CLOAKED = 14;
        private const uint DWM_BB_ENABLE = 0x00000001;

        // RedrawWindow flags: RDW_INVALIDATE (0x0001) | RDW_ERASE (0x0004) | RDW_ALLCHILDREN (0x0080) | RDW_FRAME (0x0400) = 0x0485
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_ALLCHILDREN = 0x0080;
        private const uint RDW_FRAME = 0x0400;
        private const uint RDW_HDR_REFRESH = RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_FRAME; // 0x0485

        // WinEvent Constants
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        private const int WCA_ACCENT_POLICY = 19;

        public const int ACCENT_DISABLED = 0;
        public const int ACCENT_ENABLE_GRADIENT = 1;
        public const int ACCENT_ENABLE_TRANSPARENTGRADIENT = 2;
        public const int ACCENT_ENABLE_BLURBEHIND = 3;
        public const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

        /// <summary>
        /// Default spread in pixels around all 4 sides of CabinetWClass for the outer drop shadow & glow.
        /// </summary>
        public const int SHADOW_SPREAD = 28;

        // ====================================================================
        // Public Color Constants (AABBGGRR)
        // ====================================================================

        /// <summary>
        /// Deep dark obsidian-emerald tint (0xCC0D1912 in AABBGGRR: Alpha=0xCC, R=0x12, G=0x19, B=0x0D).
        /// Used only in Mode 2 (SDR Only Blur).
        /// </summary>
        public const uint EMERALD_GLASS_GRADIENT_COLOR = 0xCC0D1912;

        /// <summary>
        /// Clear transparent / invisible taskbar tint (0x00000000 in AABBGGRR: A=0, R=0, G=0, B=0).
        /// Used with ACCENT_ENABLE_TRANSPARENTGRADIENT (2) and AccentFlags = 2 so the taskbar background is 100% invisible.
        /// </summary>
        public const uint TASKBAR_CLEAR_COLOR = 0x00000000;

        /// <summary>
        /// Subtle obsidian-emerald taskbar blur tint (0x550D1912 in AABBGGRR).
        /// </summary>
        public const uint TASKBAR_BLUR_COLOR = 0x550D1912;

        /// <summary>
        /// Doctor Doom Emerald Bar taskbar tint (0xB8182C1F in AABBGGRR: Alpha=0xB8, R=0x1F, G=0x2C, B=0x18).
        /// </summary>
        public const uint TASKBAR_DOOM_EMERALD_COLOR = 0xB8182C1F;

        // ====================================================================
        // Daemon State & Properties (v2.0 Parametric Multi-Color Engine)
        // ====================================================================

        private static bool _explorerGlassEnabled = false;          // HDR-Safe Default: false (100% solid crisp white text in HDR)
        private static byte _explorerAlpha = 238;                   // HDR-Safe Default uniform alpha when enabled: 238
        private static int _explorerBlurMode = 0;                   // 0 = HDR-Safe Crisp Dark (Default), 1 = HDR-Safe Uniform Alpha, 2 = SDR Only Blur
        private static int _taskbarMode = 1;                        // 1 = TranslucentTB-Style Clear/Invisible Taskbar Default! (0..4)
        private static int _lastAppliedTaskbarMode = -1;
        private static uint _lastAppliedTaskbarTint = 0xFFFFFFFF;

        private static bool _emeraldTintAndShadowEnabled = true;    // HDR-Safe Companion Overlay & Drop Shadow enabled by default!
        private static byte _emeraldTintIntensity = 72;             // Interior body tint alpha (default 72, range 0..140)
        private static byte _emeraldShadowIntensity = 185;          // Outer drop shadow alpha (default 185, range 0..255)

        // v2.0 Multi-Color Parametric Overlay State
        private static Color _overlayBodyTintColor = Color.FromArgb(14, 54, 32);
        private static Color _overlayShadowGlowColor = Color.FromArgb(38, 115, 71);
        private static Color _overlayRimBorderColor = Color.FromArgb(79, 158, 110);
        private static int _shadowSpread = 28;                      // 8..48 (default 28)
        private static bool _showRimBorder = true;
        private static bool _shadowOnlyActiveWindow = false;

        // v2.0 Taskbar Colors & Icon Centering State
        private static Color _presetTaskbarTintColor = Color.FromArgb(31, 44, 24);
        private static Color _customTaskbarTintColor = Color.FromArgb(31, 44, 24);
        private static byte _customTaskbarAlpha = 180;
        private static bool _taskbarCenterIcons = false;
        private static int _lastCenteredTaskListX = -1;

        private static uint _customTaskbarTint = 0;
        private static bool _isRunning = false;

        private static WinEventDelegate _winEventDelegate;
        private static IntPtr _hookSystemEvents = IntPtr.Zero;
        private static IntPtr _hookObjectEvents = IntPtr.Zero;
        private static Timer _overlaySyncTimer;

        private static readonly Dictionary<IntPtr, EmeraldShadowOverlay> _overlays = new Dictionary<IntPtr, EmeraldShadowOverlay>();
        private static bool _isSyncingOverlays = false;

        public static bool ExplorerGlassEnabled
        {
            get { return _explorerGlassEnabled; }
            set { _explorerGlassEnabled = value; }
        }

        public static byte ExplorerAlpha
        {
            get { return _explorerAlpha; }
            set { _explorerAlpha = ClampAlpha(value); }
        }

        public static int ExplorerBlurMode
        {
            get { return _explorerBlurMode; }
            set { _explorerBlurMode = ClampBlurMode(value); }
        }

        public static int TaskbarMode
        {
            get { return _taskbarMode; }
            set
            {
                int clamped = ClampTaskbarMode(value);
                if (_taskbarMode != clamped)
                {
                    _taskbarMode = clamped;
                    ApplyTaskbarStyle(_taskbarMode, _customTaskbarTint);
                }
            }
        }

        /// <summary>
        /// Enables or disables the HDR-Safe Interior Body Tint + Outer 360° Window Drop Shadow.
        /// Default is true.
        /// </summary>
        public static bool EmeraldTintAndShadowEnabled
        {
            get { return _emeraldTintAndShadowEnabled; }
            set
            {
                if (_emeraldTintAndShadowEnabled != value)
                {
                    _emeraldTintAndShadowEnabled = value;
                    EnsureOverlayTimerState();
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Intensity of the interior body tint over Explorer's #202020 background (default 72, range 0..140).
        /// </summary>
        public static byte EmeraldTintIntensity
        {
            get { return _emeraldTintIntensity; }
            set
            {
                byte clamped = ClampTintIntensity(value);
                if (_emeraldTintIntensity != clamped)
                {
                    _emeraldTintIntensity = clamped;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Intensity of the outer drop shadow & rim glow around Explorer windows (default 185, range 0..255).
        /// </summary>
        public static byte EmeraldShadowIntensity
        {
            get { return _emeraldShadowIntensity; }
            set
            {
                byte clamped = ClampByte(value, 0, 255);
                if (_emeraldShadowIntensity != clamped)
                {
                    _emeraldShadowIntensity = clamped;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Parametric RGB color for File Explorer's interior body tint (default R=14, G=54, B=32).
        /// </summary>
        public static Color OverlayBodyTintColor
        {
            get { return _overlayBodyTintColor; }
            set
            {
                if (_overlayBodyTintColor != value)
                {
                    _overlayBodyTintColor = value;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Parametric RGB color for File Explorer's 360° outer drop shadow & glow (default R=38, G=115, B=71).
        /// </summary>
        public static Color OverlayShadowGlowColor
        {
            get { return _overlayShadowGlowColor; }
            set
            {
                if (_overlayShadowGlowColor != value)
                {
                    _overlayShadowGlowColor = value;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Parametric RGB color for File Explorer's 2px crisp rim border (default R=79, G=158, B=110).
        /// </summary>
        public static Color OverlayRimBorderColor
        {
            get { return _overlayRimBorderColor; }
            set
            {
                if (_overlayRimBorderColor != value)
                {
                    _overlayRimBorderColor = value;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Spread width in pixels for the 360° outer drop shadow around Explorer windows (clamped 8..48, default 28).
        /// </summary>
        public static int ShadowSpread
        {
            get { return _shadowSpread; }
            set
            {
                int clamped = ClampSpread(value);
                if (_shadowSpread != clamped)
                {
                    _shadowSpread = clamped;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Whether to draw the crisp 2px rim highlight border around Explorer windows (default true).
        /// </summary>
        public static bool ShowRimBorder
        {
            get { return _showRimBorder; }
            set
            {
                if (_showRimBorder != value)
                {
                    _showRimBorder = value;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// When true, unfocused Explorer windows hide their outer drop shadow/rim and keep only subtle interior tint (default false).
        /// </summary>
        public static bool ShadowOnlyActiveWindow
        {
            get { return _shadowOnlyActiveWindow; }
            set
            {
                if (_shadowOnlyActiveWindow != value)
                {
                    _shadowOnlyActiveWindow = value;
                    SyncAllExplorerOverlays(true);
                }
            }
        }

        /// <summary>
        /// Taskbar tint RGB color used when TaskbarMode == 3 (Active Preset Theme Bar).
        /// </summary>
        public static Color PresetTaskbarTintColor
        {
            get { return _presetTaskbarTintColor; }
            set
            {
                if (_presetTaskbarTintColor != value)
                {
                    _presetTaskbarTintColor = value;
                    if (_taskbarMode == 3)
                    {
                        ApplyTaskbarStyleInternal(3, 0, true);
                    }
                }
            }
        }

        /// <summary>
        /// Taskbar tint RGB color used when TaskbarMode == 4 (Custom RGB/Alpha Bar).
        /// </summary>
        public static Color CustomTaskbarTintColor
        {
            get { return _customTaskbarTintColor; }
            set
            {
                if (_customTaskbarTintColor != value)
                {
                    _customTaskbarTintColor = value;
                    if (_taskbarMode == 4)
                    {
                        ApplyTaskbarStyleInternal(4, 0, true);
                    }
                }
            }
        }

        /// <summary>
        /// Taskbar alpha opacity (0..255, default 180) used when TaskbarMode == 3 or TaskbarMode == 4.
        /// </summary>
        public static byte CustomTaskbarAlpha
        {
            get { return _customTaskbarAlpha; }
            set
            {
                if (_customTaskbarAlpha != value)
                {
                    _customTaskbarAlpha = value;
                    if (_taskbarMode == 3 || _taskbarMode == 4)
                    {
                        ApplyTaskbarStyleInternal(_taskbarMode, 0, true);
                    }
                }
            }
        }

        /// <summary>
        /// Windows 11 style taskbar icon centering on Windows 10 (Shell_TrayWnd -> ReBarWindow32 -> MSTaskSwWClass -> MSTaskListWClass).
        /// When set back to false, immediately restores MSTaskListWClass X position to 0.
        /// </summary>
        public static bool TaskbarCenterIcons
        {
            get { return _taskbarCenterIcons; }
            set
            {
                if (_taskbarCenterIcons != value)
                {
                    _taskbarCenterIcons = value;
                    ApplyTaskbarIconCentering(true);
                }
            }
        }

        public static bool IsRunning
        {
            get { return _isRunning; }
        }

        private static byte ClampAlpha(int alpha)
        {
            if (alpha < 150) return 150;
            if (alpha > 255) return 255;
            return (byte)alpha;
        }

        private static byte ClampTintIntensity(int tint)
        {
            if (tint < 0) return 0;
            if (tint > 140) return 140;
            return (byte)tint;
        }

        private static byte ClampByte(int val, int min, int max)
        {
            if (val < min) return (byte)min;
            if (val > max) return (byte)max;
            return (byte)val;
        }

        private static int ClampSpread(int spread)
        {
            if (spread < 8) return 8;
            if (spread > 48) return 48;
            return spread;
        }

        private static int ClampBlurMode(int mode)
        {
            if (mode < 0) return 0;
            if (mode > 2) return 2;
            return mode;
        }

        private static int ClampTaskbarMode(int mode)
        {
            if (mode < 0) return 0;
            if (mode > 4) return 4;
            return mode;
        }

        private static uint ColorToTaskbarAabbggrr(Color c, byte alpha)
        {
            return ((uint)alpha << 24) | ((uint)c.B << 16) | ((uint)c.G << 8) | (uint)c.R;
        }

        // ====================================================================
        // Core Lifecycle & Settings API
        // ====================================================================

        /// <summary>
        /// Updates parametric overlay colors, spread, and intensities in one atomic call and refreshes all open Explorer overlays.
        /// </summary>
        public static void ApplyPresetOverlayColors(
            Color bodyTint,
            Color shadowGlow,
            Color rimBorder,
            int spread,
            byte tintAlpha,
            byte shadowAlpha)
        {
            _overlayBodyTintColor = bodyTint;
            _overlayShadowGlowColor = shadowGlow;
            _overlayRimBorderColor = rimBorder;
            _shadowSpread = ClampSpread(spread);
            _emeraldTintIntensity = ClampTintIntensity(tintAlpha);
            _emeraldShadowIntensity = ClampByte(shadowAlpha, 0, 255);

            EnsureOverlayTimerState();
            SyncAllExplorerOverlays(true);
        }

        /// <summary>
        /// Updates daemon settings in memory and immediately refreshes open Explorer windows, overlays,
        /// and applies Taskbar style only if TaskbarMode changed (zero polling flicker!).
        /// </summary>
        public static void UpdateSettings(bool explorerGlassEnabled, byte explorerAlpha, int explorerBlurMode, int taskbarMode)
        {
            _explorerGlassEnabled = explorerGlassEnabled;
            _explorerAlpha = ClampAlpha(explorerAlpha);
            _explorerBlurMode = ClampBlurMode(explorerBlurMode);

            int newTaskbarMode = ClampTaskbarMode(taskbarMode);
            bool taskbarChanged = (newTaskbarMode != _taskbarMode) || (_lastAppliedTaskbarMode != newTaskbarMode);
            _taskbarMode = newTaskbarMode;
            _customTaskbarTint = 0;

            EnsureOverlayTimerState();
            RefreshAllOpenWindows(taskbarChanged);
        }

        /// <summary>
        /// Overload accepting int for explorerAlpha for convenience when binding to TrackBar/slider values.
        /// </summary>
        public static void UpdateSettings(bool explorerGlassEnabled, int explorerAlpha, int explorerBlurMode, int taskbarMode)
        {
            UpdateSettings(explorerGlassEnabled, ClampAlpha(explorerAlpha), explorerBlurMode, taskbarMode);
        }

        /// <summary>
        /// Full overload including HDR-Safe Tint & Shadow parameters.
        /// </summary>
        public static void UpdateSettings(
            bool explorerGlassEnabled,
            int explorerAlpha,
            int explorerBlurMode,
            int taskbarMode,
            bool emeraldTintAndShadowEnabled,
            int emeraldTintIntensity,
            int emeraldShadowIntensity)
        {
            _explorerGlassEnabled = explorerGlassEnabled;
            _explorerAlpha = ClampAlpha(explorerAlpha);
            _explorerBlurMode = ClampBlurMode(explorerBlurMode);

            int newTaskbarMode = ClampTaskbarMode(taskbarMode);
            bool taskbarChanged = (newTaskbarMode != _taskbarMode) || (_lastAppliedTaskbarMode != newTaskbarMode);
            _taskbarMode = newTaskbarMode;

            _emeraldTintAndShadowEnabled = emeraldTintAndShadowEnabled;
            _emeraldTintIntensity = ClampTintIntensity(emeraldTintIntensity);
            _emeraldShadowIntensity = ClampByte(emeraldShadowIntensity, 0, 255);
            _customTaskbarTint = 0;

            EnsureOverlayTimerState();
            RefreshAllOpenWindows(taskbarChanged);
        }

        /// <summary>
        /// Updates only the HDR-Safe Body Tint and Outer Drop Shadow settings and refreshes all Explorer overlays live.
        /// </summary>
        public static void UpdateEmeraldOverlaySettings(bool enabled, int tintIntensity, int shadowIntensity)
        {
            _emeraldTintAndShadowEnabled = enabled;
            _emeraldTintIntensity = ClampTintIntensity(tintIntensity);
            _emeraldShadowIntensity = ClampByte(shadowIntensity, 0, 255);

            EnsureOverlayTimerState();
            SyncAllExplorerOverlays(true);
        }

        /// <summary>
        /// Starts the WinEvent hooks for File Explorer and the 16ms Overlay tracking timer.
        /// Automatically terminates any legacy ExplorerGlass.exe and conflicting TranslucentTB.exe processes.
        /// Does NOT run any periodic polling timer on Shell_TrayWnd (preventing Taskbar flickering).
        /// </summary>
        public static void StartDaemon()
        {
            StopLegacyExplorerGlassProcesses();

            if (_winEventDelegate == null)
            {
                _winEventDelegate = new WinEventDelegate(WinEventProc);
            }

            if (_hookSystemEvents == IntPtr.Zero)
            {
                _hookSystemEvents = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_MINIMIZEEND,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            }

            if (_hookObjectEvents == IntPtr.Zero)
            {
                _hookObjectEvents = SetWinEventHook(
                    EVENT_OBJECT_DESTROY,
                    EVENT_OBJECT_LOCATIONCHANGE,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            }

            if (_overlaySyncTimer == null)
            {
                _overlaySyncTimer = new Timer();
                _overlaySyncTimer.Interval = 16; // ~60 FPS real-time tracking of Explorer window bounds/z-order
                _overlaySyncTimer.Tick += OnOverlaySyncTimerTick;
            }

            _isRunning = true;
            EnsureOverlayTimerState();
            RefreshAllOpenWindows(true);
            ApplyTaskbarIconCentering(true);
        }

        /// <summary>
        /// Stops the WinEvent hooks, destroys all overlays, stops timers, and restores Explorer windows and Taskbar to normal state.
        /// </summary>
        public static void StopDaemon()
        {
            StopDaemon(true);
        }

        /// <summary>
        /// Stops the WinEvent hooks, destroys all overlays, stops timers, and optionally restores all open Explorer windows and Taskbar.
        /// </summary>
        public static void StopDaemon(bool restoreWindowsToOpaque)
        {
            _isRunning = false;

            if (_overlaySyncTimer != null)
            {
                _overlaySyncTimer.Stop();
                _overlaySyncTimer.Tick -= OnOverlaySyncTimerTick;
                _overlaySyncTimer.Dispose();
                _overlaySyncTimer = null;
            }

            if (_hookSystemEvents != IntPtr.Zero)
            {
                try { UnhookWinEvent(_hookSystemEvents); } catch { }
                _hookSystemEvents = IntPtr.Zero;
            }

            if (_hookObjectEvents != IntPtr.Zero)
            {
                try { UnhookWinEvent(_hookObjectEvents); } catch { }
                _hookObjectEvents = IntPtr.Zero;
            }

            DestroyAllEmeraldOverlays();

            if (restoreWindowsToOpaque)
            {
                RestoreAllExplorerWindowsToOpaque();
                ApplyTaskbarStyleInternal(0, 0, true);
                RestoreTaskbarIconAlignment();
            }
        }

        /// <summary>
        /// Immediately re-applies the configured HDR-safe style and parametric Tint + Shadow overlays
        /// to all currently open File Explorer windows, and updates the Taskbar.
        /// </summary>
        public static void RefreshAllOpenWindows()
        {
            RefreshAllOpenWindows(false);
        }

        private static void RefreshAllOpenWindows(bool forceTaskbarApply)
        {
            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (IsExplorerWindow(hWnd))
                    {
                        ApplyExplorerWindowStyle(hWnd, _explorerGlassEnabled, _explorerAlpha, _explorerBlurMode);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }

            SyncAllExplorerOverlays(true);
            ApplyTaskbarStyleInternal(_taskbarMode, _customTaskbarTint, forceTaskbarApply);
            if (_taskbarCenterIcons)
            {
                ApplyTaskbarIconCentering(false);
            }
        }

        /// <summary>
        /// Immediately restores all currently open File Explorer windows to HDR-Safe Crisp Dark opaque state (Mode 0):
        /// Clears WCA_ACCENT_POLICY (ACCENT_DISABLED = 0), disables DwmEnableBlurBehindWindow(false),
        /// removes WS_EX_LAYERED, and forces RedrawWindow (0x0485) so GDI text in HDR is 100% solid white.
        /// Also syncs the HDR-Safe Tint & Shadow overlays if enabled.
        /// </summary>
        public static void RestoreAllExplorerWindowsToOpaque()
        {
            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (IsExplorerWindow(hWnd))
                    {
                        ApplyExplorerWindowStyle(hWnd, false, 255, 0);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }

            SyncAllExplorerOverlays(true);
        }

        // ====================================================================
        // File Explorer Window Styling (CabinetWClass / ExploreWClass ONLY)
        // ====================================================================

        /// <summary>
        /// Checks strictly whether hWnd is a top-level File Explorer window (CabinetWClass or ExploreWClass).
        /// Never touches non-Explorer windows (never touches Antigravity.exe or other applications).
        /// </summary>
        public static bool IsExplorerWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return false;

            StringBuilder sb = new StringBuilder(64);
            if (GetClassName(hWnd, sb, sb.Capacity) <= 0)
                return false;

            string cls = sb.ToString();
            return string.Equals(cls, "CabinetWClass", StringComparison.Ordinal) ||
                   string.Equals(cls, "ExploreWClass", StringComparison.Ordinal);
        }

        /// <summary>
        /// Styles a single File Explorer window and syncs its overlay if and only if it is CabinetWClass or ExploreWClass.
        /// </summary>
        public static void StyleIfExplorer(IntPtr hWnd)
        {
            if (IsExplorerWindow(hWnd))
            {
                ApplyExplorerWindowStyle(hWnd, _explorerGlassEnabled, _explorerAlpha, _explorerBlurMode);
                SyncSingleExplorerOverlay(hWnd, false);
            }
        }

        /// <summary>
        /// Applies HDR-Safe styling to an Explorer window (CabinetWClass / ExploreWClass):
        /// - Always clears per-pixel composition first: sets ACCENT_DISABLED = 0 via SetWindowCompositionAttribute
        ///   AND calls DwmEnableBlurBehindWindow(hWnd, fEnable = false).
        /// - Mode 0 (HDR-Safe Crisp Dark — Default) or when !glassEnabled:
        ///   Completely removes WS_EX_LAYERED, sets ACCENT_DISABLED = 0, DwmEnableBlurBehindWindow(false),
        ///   and calls RedrawWindow(..., 0x0485) so GDI text in HDR is 100% opaque crisp #FFFFFF white with zero alpha punch-through!
        /// - Mode 1 (HDR-Safe Uniform Alpha):
        ///   Explicitly sets ACCENT_DISABLED = 0 and DwmEnableBlurBehindWindow(false) first (so DWM does NOT do per-pixel scRGB alpha),
        ///   then applies uniform WS_EX_LAYERED + SetLayeredWindowAttributes(hWnd, 0, alpha, LWA_ALPHA) (default alpha = 238), plus RedrawWindow.
        /// - Mode 2 (SDR Only Blur):
        ///   Available only if manually selected for SDR displays; applies WS_EX_LAYERED + ACCENT_ENABLE_BLURBEHIND with current OverlayBodyTintColor.
        /// </summary>
        public static void ApplyExplorerWindowStyle(IntPtr hWnd, bool glassEnabled, byte alpha, int blurMode)
        {
            if (!IsExplorerWindow(hWnd))
                return;

            try
            {
                byte clampedAlpha = ClampAlpha(alpha);
                int clampedMode = ClampBlurMode(blurMode);

                // STEP 1: Always explicitly clear DWM per-pixel alpha composition on Explorer windows first!
                SetAccentPolicy(hWnd, ACCENT_DISABLED, 0, 0);
                DisableDwmBlurBehind(hWnd);

                if (!glassEnabled || clampedMode == 0)
                {
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_LAYERED) != 0)
                    {
                        SetLayeredWindowAttributes(hWnd, 0, 255, LWA_ALPHA);
                        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);
                    }
                    RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RDW_HDR_REFRESH);
                    return;
                }

                if (clampedMode == 1)
                {
                    int currentEx = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((currentEx & WS_EX_LAYERED) == 0)
                    {
                        SetWindowLong(hWnd, GWL_EXSTYLE, currentEx | WS_EX_LAYERED);
                    }
                    SetLayeredWindowAttributes(hWnd, 0, clampedAlpha, LWA_ALPHA);
                    RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RDW_HDR_REFRESH);
                    return;
                }

                // Mode 2 (SDR Only Blur):
                {
                    int currentEx = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((currentEx & WS_EX_LAYERED) == 0)
                    {
                        SetWindowLong(hWnd, GWL_EXSTYLE, currentEx | WS_EX_LAYERED);
                    }
                    SetLayeredWindowAttributes(hWnd, 0, clampedAlpha, LWA_ALPHA);
                    uint sdrGrad = ColorToTaskbarAabbggrr(_overlayBodyTintColor, 0xCC);
                    SetAccentPolicy(hWnd, ACCENT_ENABLE_BLURBEHIND, 2, sdrGrad);
                    RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RDW_HDR_REFRESH);
                }
            }
            catch
            {
            }
        }

        // ====================================================================
        // HDR-Safe Multi-Color Companion Overlay & Shadow Engine
        // ====================================================================

        /// <summary>
        /// Synchronizes all open File Explorer windows with their click-through Parametric Tint & 360° Shadow overlays.
        /// </summary>
        public static void SyncAllExplorerOverlays(bool forceRedraw)
        {
            if (_isSyncingOverlays)
                return;

            _isSyncingOverlays = true;
            try
            {
                if (!_emeraldTintAndShadowEnabled || (_emeraldTintIntensity == 0 && _emeraldShadowIntensity == 0))
                {
                    DestroyAllEmeraldOverlays();
                    return;
                }

                HashSet<IntPtr> activeExplorerHwnds = new HashSet<IntPtr>();

                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (IsExplorerWindow(hWnd))
                    {
                        activeExplorerHwnds.Add(hWnd);
                        SyncSingleExplorerOverlay(hWnd, forceRedraw);
                    }
                    return true;
                }, IntPtr.Zero);

                // Clean up overlays for closed Explorer windows
                if (_overlays.Count > 0)
                {
                    List<IntPtr> toRemove = null;
                    foreach (KeyValuePair<IntPtr, EmeraldShadowOverlay> kvp in _overlays)
                    {
                        if (!activeExplorerHwnds.Contains(kvp.Key) || !IsWindow(kvp.Key))
                        {
                            if (toRemove == null) toRemove = new List<IntPtr>();
                            toRemove.Add(kvp.Key);
                        }
                    }

                    if (toRemove != null)
                    {
                        for (int i = 0; i < toRemove.Count; i++)
                        {
                            IntPtr key = toRemove[i];
                            EmeraldShadowOverlay ov;
                            if (_overlays.TryGetValue(key, out ov))
                            {
                                _overlays.Remove(key);
                                try { ov.Dispose(); } catch { }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _isSyncingOverlays = false;
            }
        }

        private static void SyncSingleExplorerOverlay(IntPtr targetHwnd, bool forceRedraw)
        {
            if (targetHwnd == IntPtr.Zero)
                return;

            if (!_emeraldTintAndShadowEnabled || (_emeraldTintIntensity == 0 && _emeraldShadowIntensity == 0))
            {
                RemoveExplorerOverlay(targetHwnd);
                return;
            }

            if (!IsWindow(targetHwnd))
            {
                RemoveExplorerOverlay(targetHwnd);
                return;
            }

            try
            {
                EmeraldShadowOverlay overlay;
                if (!_overlays.TryGetValue(targetHwnd, out overlay) || overlay == null || overlay.IsDisposed)
                {
                    overlay = new EmeraldShadowOverlay(targetHwnd);
                    _overlays[targetHwnd] = overlay;
                }

                overlay.SyncWithTarget(
                    _emeraldTintIntensity,
                    _emeraldShadowIntensity,
                    _shadowSpread,
                    _overlayBodyTintColor,
                    _overlayShadowGlowColor,
                    _overlayRimBorderColor,
                    _showRimBorder,
                    _shadowOnlyActiveWindow,
                    forceRedraw);
            }
            catch
            {
            }
        }

        private static void RemoveExplorerOverlay(IntPtr targetHwnd)
        {
            EmeraldShadowOverlay ov;
            if (_overlays.TryGetValue(targetHwnd, out ov))
            {
                _overlays.Remove(targetHwnd);
                try { ov.Dispose(); } catch { }
            }
        }

        public static void DestroyAllEmeraldOverlays()
        {
            try
            {
                foreach (KeyValuePair<IntPtr, EmeraldShadowOverlay> kvp in _overlays)
                {
                    try { kvp.Value.Dispose(); } catch { }
                }
                _overlays.Clear();
            }
            catch
            {
            }
        }

        private static bool GetExtendedFrameBounds(IntPtr hWnd, out RECT rect)
        {
            try
            {
                int hr = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
                if (hr == 0 && rect.Width > 0 && rect.Height > 0)
                {
                    return true;
                }
            }
            catch
            {
            }
            return GetWindowRect(hWnd, out rect);
        }

        private static bool IsWindowCloaked(IntPtr hWnd)
        {
            try
            {
                int cloaked = 0;
                int hr = DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
                return (hr == 0 && cloaked != 0);
            }
            catch
            {
                return false;
            }
        }

        private static void SetOwnerWindow(IntPtr childHwnd, IntPtr ownerHwnd)
        {
            try
            {
                if (IntPtr.Size == 8)
                    SetWindowLongPtr64(childHwnd, GWLP_HWNDPARENT, ownerHwnd);
                else
                    SetWindowLong32(childHwnd, GWLP_HWNDPARENT, ownerHwnd.ToInt32());
            }
            catch
            {
            }
        }

        private static bool IsTaskbarOrTopmostShellWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            StringBuilder sb = new StringBuilder(64);
            if (GetClassName(hWnd, sb, sb.Capacity) <= 0)
                return false;

            string cls = sb.ToString();
            return string.Equals(cls, "Shell_TrayWnd", StringComparison.Ordinal) ||
                   string.Equals(cls, "SecondaryTrayWnd", StringComparison.Ordinal);
        }

        /// <summary>
        /// Click-through, non-activating per-pixel alpha layered window (WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE)
        /// positioned around a CabinetWClass / ExploreWClass window and strictly clamped to the monitor's WorkingArea
        /// so it NEVER overlaps or flickers the Windows Taskbar (Shell_TrayWnd)!
        /// Supports parametric multi-color presets (BodyTint, ShadowGlow, RimBorder, ShadowSpread, ShowRimBorder, ShadowOnlyActiveWindow).
        /// </summary>
        private sealed class EmeraldShadowOverlay : NativeWindow, IDisposable
        {
            private readonly IntPtr _targetHwnd;
            private bool _isDisposed;
            private bool _isVisible;

            private int _lastOutX;
            private int _lastOutY;
            private int _lastOutW;
            private int _lastOutH;
            private int _lastInnerRelX;
            private int _lastInnerRelY;
            private int _lastInnerW;
            private int _lastInnerH;
            private int _lastSpread = -1;
            private byte _lastTintAlpha;
            private byte _lastShadowAlpha;
            private int _lastBodyRgb;
            private int _lastShadowRgb;
            private int _lastRimRgb;
            private bool _lastShowRim = true;
            private bool _lastShadowOnlyActive = false;
            private bool _lastFocused = true;

            public bool IsDisposed { get { return _isDisposed; } }

            public EmeraldShadowOverlay(IntPtr targetHwnd)
            {
                _targetHwnd = targetHwnd;
                CreateOverlayHandle();
            }

            private void CreateOverlayHandle()
            {
                CreateParams cp = new CreateParams();
                cp.Caption = "DoomEmeraldShadowOverlay";
                cp.Style = WS_POPUP;
                // Strictly NO WS_EX_TOPMOST! Uses WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
                cp.ExStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                cp.X = -32000;
                cp.Y = -32000;
                cp.Width = 1;
                cp.Height = 1;

                this.CreateHandle(cp);

                if (this.Handle != IntPtr.Zero && _targetHwnd != IntPtr.Zero)
                {
                    SetOwnerWindow(this.Handle, _targetHwnd);
                }
            }

            public void SyncWithTarget(
                byte tintIntensity,
                byte shadowIntensity,
                int configuredSpread,
                Color bodyTintColor,
                Color shadowGlowColor,
                Color rimBorderColor,
                bool showRimBorder,
                bool shadowOnlyActive,
                bool forceRedraw)
            {
                if (_isDisposed || this.Handle == IntPtr.Zero)
                    return;

                if (!IsWindow(_targetHwnd) || !IsWindowVisible(_targetHwnd) || IsIconic(_targetHwnd) || IsWindowCloaked(_targetHwnd))
                {
                    HideOverlay();
                    return;
                }

                RECT frameRect;
                if (!GetExtendedFrameBounds(_targetHwnd, out frameRect))
                {
                    HideOverlay();
                    return;
                }

                int targetW = frameRect.Width;
                int targetH = frameRect.Height;
                if (targetW < 80 || targetH < 60)
                {
                    HideOverlay();
                    return;
                }

                IntPtr fg = GetForegroundWindow();
                bool isFocused = (fg == _targetHwnd);

                bool zoomed = IsZoomed(_targetHwnd);
                int spread = zoomed ? 0 : ClampSpread(configuredSpread);
                if (shadowOnlyActive && !isFocused)
                {
                    spread = 0;
                }

                // Desired un-clamped overlay bounds
                int rawLeft = frameRect.Left - spread;
                int rawTop = frameRect.Top - spread;
                int rawRight = frameRect.Right + spread;
                int rawBottom = frameRect.Bottom + spread;

                // Clamp strictly to Screen.FromHandle(_targetHwnd).WorkingArea so the overlay NEVER extends into or covers the Taskbar!
                Rectangle workArea;
                try
                {
                    workArea = Screen.FromHandle(_targetHwnd).WorkingArea;
                }
                catch
                {
                    workArea = Screen.PrimaryScreen.WorkingArea;
                }

                int clampedLeft = Math.Max(rawLeft, workArea.Left);
                int clampedTop = Math.Max(rawTop, workArea.Top);
                int clampedRight = Math.Min(rawRight, workArea.Right);
                int clampedBottom = Math.Min(rawBottom, workArea.Bottom);

                int outW = clampedRight - clampedLeft;
                int outH = clampedBottom - clampedTop;

                if (outW < 40 || outH < 40)
                {
                    HideOverlay();
                    return;
                }

                // Calculate where the Explorer window's actual frame sits inside our clamped overlay bitmap
                int innerRelX = frameRect.Left - clampedLeft;
                int innerRelY = frameRect.Top - clampedTop;
                int innerW = targetW;
                int innerH = targetH;

                int bodyRgb = bodyTintColor.ToArgb() & 0x00FFFFFF;
                int shadowRgb = shadowGlowColor.ToArgb() & 0x00FFFFFF;
                int rimRgb = rimBorderColor.ToArgb() & 0x00FFFFFF;

                bool sizeOrStyleChanged =
                    forceRedraw ||
                    (outW != _lastOutW) ||
                    (outH != _lastOutH) ||
                    (innerRelX != _lastInnerRelX) ||
                    (innerRelY != _lastInnerRelY) ||
                    (innerW != _lastInnerW) ||
                    (innerH != _lastInnerH) ||
                    (spread != _lastSpread) ||
                    (tintIntensity != _lastTintAlpha) ||
                    (shadowIntensity != _lastShadowAlpha) ||
                    (bodyRgb != _lastBodyRgb) ||
                    (shadowRgb != _lastShadowRgb) ||
                    (rimRgb != _lastRimRgb) ||
                    (showRimBorder != _lastShowRim) ||
                    (shadowOnlyActive != _lastShadowOnlyActive) ||
                    (isFocused != _lastFocused);

                bool posChanged = (clampedLeft != _lastOutX) || (clampedTop != _lastOutY);

                _lastOutX = clampedLeft;
                _lastOutY = clampedTop;
                _lastOutW = outW;
                _lastOutH = outH;
                _lastInnerRelX = innerRelX;
                _lastInnerRelY = innerRelY;
                _lastInnerW = innerW;
                _lastInnerH = innerH;
                _lastSpread = spread;
                _lastTintAlpha = tintIntensity;
                _lastShadowAlpha = shadowIntensity;
                _lastBodyRgb = bodyRgb;
                _lastShadowRgb = shadowRgb;
                _lastRimRgb = rimRgb;
                _lastShowRim = showRimBorder;
                _lastShadowOnlyActive = shadowOnlyActive;
                _lastFocused = isFocused;

                if (sizeOrStyleChanged)
                {
                    RenderAndPushLayeredBitmap(
                        clampedLeft,
                        clampedTop,
                        outW,
                        outH,
                        innerRelX,
                        innerRelY,
                        innerW,
                        innerH,
                        spread,
                        tintIntensity,
                        shadowIntensity,
                        bodyTintColor,
                        shadowGlowColor,
                        rimBorderColor,
                        showRimBorder,
                        shadowOnlyActive,
                        isFocused);
                }
                else if (posChanged)
                {
                    SetWindowPos(
                        this.Handle,
                        IntPtr.Zero,
                        clampedLeft,
                        clampedTop,
                        outW,
                        outH,
                        SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
                }

                if (!_isVisible)
                {
                    ShowWindow(this.Handle, SW_SHOWNA);
                    _isVisible = true;
                }

                // Keep Z-Order locked immediately above _targetHwnd without ever touching Shell_TrayWnd or topmost Z-order
                SyncZOrderSafely();
            }

            private void SyncZOrderSafely()
            {
                if (this.Handle == IntPtr.Zero || _targetHwnd == IntPtr.Zero)
                    return;

                IntPtr prevAboveTarget = GetWindow(_targetHwnd, GW_HWNDPREV);
                if (prevAboveTarget == this.Handle)
                    return;

                // Never insert above Shell_TrayWnd / SecondaryTrayWnd
                if (prevAboveTarget != IntPtr.Zero && !IsTaskbarOrTopmostShellWindow(prevAboveTarget))
                {
                    SetWindowPos(
                        this.Handle,
                        prevAboveTarget,
                        0,
                        0,
                        0,
                        0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
                }
            }

            private void RenderAndPushLayeredBitmap(
                int screenX,
                int screenY,
                int width,
                int height,
                int innerRelX,
                int innerRelY,
                int innerW,
                int innerH,
                int spread,
                byte tintAlpha,
                byte shadowMaxAlpha,
                Color bodyTintColor,
                Color shadowGlowColor,
                Color rimBorderColor,
                bool showRimBorder,
                bool shadowOnlyActive,
                bool isFocused)
            {
                if (width <= 0 || height <= 0 || this.Handle == IntPtr.Zero)
                    return;

                using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        Rectangle innerRect = new Rectangle(innerRelX, innerRelY, innerW, innerH);
                        bool drawShadowAndRim = !(shadowOnlyActive && !isFocused);

                        // 1. Draw Outer 360° Parametric Drop Shadow & Glow around all 4 sides
                        if (drawShadowAndRim && spread > 0 && shadowMaxAlpha > 0)
                        {
                            // Exclude the inner Explorer rect so outer shadow rings only paint in the margin outside Explorer
                            g.SetClip(innerRect, CombineMode.Exclude);

                            int outerR = Math.Max(0, (int)(shadowGlowColor.R * 0.75));
                            int outerG = Math.Max(0, (int)(shadowGlowColor.G * 0.75));
                            int outerB = Math.Max(0, (int)(shadowGlowColor.B * 0.75));

                            double focusScale = isFocused ? 1.0 : 0.72;
                            for (int d = spread; d >= 1; d--)
                            {
                                double t = (double)(spread - d + 1) / (double)spread; // 0..1 from outer edge to inner border
                                double weight = t * t * (0.16 + 0.22 * t);
                                int ringAlpha = (int)Math.Round(shadowMaxAlpha * focusScale * weight);
                                if (ringAlpha < 1) ringAlpha = 1;
                                if (ringAlpha > 215) ringAlpha = 215;

                                int rCol = (int)Math.Round(outerR + (shadowGlowColor.R - outerR) * t);
                                int gCol = (int)Math.Round(outerG + (shadowGlowColor.G - outerG) * t);
                                int bCol = (int)Math.Round(outerB + (shadowGlowColor.B - outerB) * t);

                                Rectangle ringRect = new Rectangle(
                                    innerRect.X - d,
                                    innerRect.Y - d,
                                    innerRect.Width + (d * 2) - 1,
                                    innerRect.Height + (d * 2) - 1);

                                int cornerRadius = Math.Min(14, Math.Max(2, d / 2 + 2));
                                using (GraphicsPath path = CreateRoundedRectPath(ringRect, cornerRadius))
                                using (Pen ringPen = new Pen(Color.FromArgb(ringAlpha, rCol, gCol, bCol), 2.2f))
                                {
                                    ringPen.LineJoin = LineJoin.Round;
                                    g.DrawPath(ringPen, path);
                                }
                            }

                            g.ResetClip();
                        }

                        // 2. Fill Inside Explorer Rect with Parametric Body Tint
                        // Turns Explorer's #202020 grey background into the active theme's color
                        // while white #FFFFFF GDI text shines right through with 100% HDR clarity!
                        Rectangle visibleInner = Rectangle.Intersect(new Rectangle(0, 0, width, height), innerRect);
                        if (tintAlpha > 0 && visibleInner.Width > 0 && visibleInner.Height > 0)
                        {
                            byte effectiveTintAlpha = (!isFocused && shadowOnlyActive)
                                ? (byte)Math.Max(0, (int)(tintAlpha * 0.65))
                                : tintAlpha;

                            if (effectiveTintAlpha > 0)
                            {
                                Color topTint = Color.FromArgb(
                                    effectiveTintAlpha,
                                    bodyTintColor.R,
                                    bodyTintColor.G,
                                    bodyTintColor.B);

                                Color botTint = Color.FromArgb(
                                    effectiveTintAlpha,
                                    Math.Max(0, (int)(bodyTintColor.R * 0.76)),
                                    Math.Max(0, (int)(bodyTintColor.G * 0.76)),
                                    Math.Max(0, (int)(bodyTintColor.B * 0.76)));

                                using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                                    visibleInner,
                                    topTint,
                                    botTint,
                                    LinearGradientMode.Vertical))
                                {
                                    g.FillRectangle(bodyBrush, visibleInner);
                                }
                            }
                        }

                        // 3. Crisp 2px Parametric Rim Border right on the Explorer window edge
                        if (showRimBorder && drawShadowAndRim && shadowMaxAlpha > 0 && innerRect.Width > 2 && innerRect.Height > 2)
                        {
                            int rimAlpha = isFocused
                                ? Math.Min(245, (int)shadowMaxAlpha + 45)
                                : Math.Min(190, (int)(shadowMaxAlpha * 0.78));

                            Color rimOuter = isFocused
                                ? Color.FromArgb(rimAlpha, rimBorderColor.R, rimBorderColor.G, rimBorderColor.B)
                                : Color.FromArgb(
                                    rimAlpha,
                                    Math.Max(0, (int)(rimBorderColor.R * 0.68)),
                                    Math.Max(0, (int)(rimBorderColor.G * 0.68)),
                                    Math.Max(0, (int)(rimBorderColor.B * 0.68)));

                            Color rimInner = Color.FromArgb(
                                Math.Max(40, rimAlpha / 2),
                                Math.Min(255, rimBorderColor.R + 28),
                                Math.Min(255, rimBorderColor.G + 28),
                                Math.Min(255, rimBorderColor.B + 28));

                            using (Pen outerRimPen = new Pen(rimOuter, 2f))
                            {
                                g.DrawRectangle(outerRimPen, innerRect.X, innerRect.Y, innerRect.Width - 1, innerRect.Height - 1);
                            }
                            using (Pen innerRimPen = new Pen(rimInner, 1f))
                            {
                                g.DrawRectangle(innerRimPen, innerRect.X + 1, innerRect.Y + 1, innerRect.Width - 3, innerRect.Height - 3);
                            }
                        }
                    }

                    // Push 32bppPArgb bitmap to Win32 Layered Window via UpdateLayeredWindow
                    IntPtr screenDc = GetDC(IntPtr.Zero);
                    if (screenDc != IntPtr.Zero)
                    {
                        IntPtr memDc = CreateCompatibleDC(screenDc);
                        if (memDc != IntPtr.Zero)
                        {
                            IntPtr hBitmap = IntPtr.Zero;
                            IntPtr oldBitmap = IntPtr.Zero;
                            try
                            {
                                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                                oldBitmap = SelectObject(memDc, hBitmap);

                                POINT dstPt = new POINT(screenX, screenY);
                                SIZE dstSize = new SIZE(width, height);
                                POINT srcPt = new POINT(0, 0);
                                BLENDFUNCTION blend = new BLENDFUNCTION
                                {
                                    BlendOp = AC_SRC_OVER,
                                    BlendFlags = 0,
                                    SourceConstantAlpha = 255,
                                    AlphaFormat = AC_SRC_ALPHA
                                };

                                UpdateLayeredWindow(
                                    this.Handle,
                                    screenDc,
                                    ref dstPt,
                                    ref dstSize,
                                    memDc,
                                    ref srcPt,
                                    0,
                                    ref blend,
                                    ULW_ALPHA);
                            }
                            finally
                            {
                                if (oldBitmap != IntPtr.Zero)
                                    SelectObject(memDc, oldBitmap);
                                if (hBitmap != IntPtr.Zero)
                                    DeleteObject(hBitmap);
                                DeleteDC(memDc);
                            }
                        }
                        ReleaseDC(IntPtr.Zero, screenDc);
                    }
                }
            }

            private static GraphicsPath CreateRoundedRectPath(Rectangle r, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                int d = radius * 2;
                if (d <= 0 || d > r.Width || d > r.Height)
                {
                    path.AddRectangle(r);
                    return path;
                }

                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            public void HideOverlay()
            {
                if (_isVisible && this.Handle != IntPtr.Zero)
                {
                    ShowWindow(this.Handle, SW_HIDE);
                    _isVisible = false;
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;
                _isDisposed = true;
                try
                {
                    HideOverlay();
                    if (this.Handle != IntPtr.Zero)
                    {
                        this.DestroyHandle();
                    }
                }
                catch
                {
                }
            }
        }

        // ====================================================================
        // Taskbar Transparency Styler (Modes 0..4) & Icon Centering
        // ====================================================================

        /// <summary>
        /// Applies Taskbar transparency using default or configured tint colors for modes 0..4:
        /// mode 0 = Default Native Windows Taskbar (ACCENT_DISABLED = 0)
        /// mode 1 = Clear / 100% Invisible (ACCENT_ENABLE_TRANSPARENTGRADIENT = 2, AccentFlags = 2, GradientColor = 0x00000000) — Default!
        /// mode 2 = Subtle Blur Glass (ACCENT_ENABLE_BLURBEHIND = 3, GradientColor = 0x550D1912)
        /// mode 3 = Active Preset Theme Bar (ACCENT_ENABLE_BLURBEHIND = 3, AccentFlags = 2, uses PresetTaskbarTintColor + CustomTaskbarAlpha)
        /// mode 4 = Custom RGB/Alpha Bar (ACCENT_ENABLE_BLURBEHIND = 3, AccentFlags = 2, uses CustomTaskbarTintColor + CustomTaskbarAlpha)
        /// </summary>
        public static void ApplyTaskbarStyle(int mode)
        {
            ApplyTaskbarStyle(mode, 0);
        }

        /// <summary>
        /// Explicitly applies the requested Taskbar mode (0..4) once when called by the user or settings update.
        /// </summary>
        public static void ApplyTaskbarStyle(int mode, uint tintColorAABBGGRR)
        {
            ApplyTaskbarStyleInternal(mode, tintColorAABBGGRR, true);
        }

        private static void ApplyTaskbarStyleInternal(int mode, uint tintColorAABBGGRR, bool forceApply)
        {
            int clampedMode = ClampTaskbarMode(mode);
            _taskbarMode = clampedMode;
            _customTaskbarTint = tintColorAABBGGRR;

            uint computedColor = ComputeTaskbarGradientColor(clampedMode, tintColorAABBGGRR);

            // If TaskbarMode == 0 and we already restored or initialized mode 0, do not touch Shell_TrayWnd again!
            if (!forceApply && _lastAppliedTaskbarMode == clampedMode && _lastAppliedTaskbarTint == computedColor)
            {
                return;
            }
            if (clampedMode == 0 && _lastAppliedTaskbarMode == 0)
            {
                return;
            }

            _lastAppliedTaskbarMode = clampedMode;
            _lastAppliedTaskbarTint = computedColor;

            int accentState;
            int accentFlags;

            switch (clampedMode)
            {
                case 1:
                    // Clear / 100% Invisible Taskbar
                    accentState = ACCENT_ENABLE_TRANSPARENTGRADIENT;
                    accentFlags = 2;
                    break;

                case 2:
                    // Subtle Blur Glass
                    accentState = ACCENT_ENABLE_BLURBEHIND;
                    accentFlags = 2;
                    break;

                case 3:
                    // Active Preset Theme Bar
                    accentState = ACCENT_ENABLE_BLURBEHIND;
                    accentFlags = 2;
                    break;

                case 4:
                    // Custom RGB/Alpha Bar
                    accentState = ACCENT_ENABLE_BLURBEHIND;
                    accentFlags = 2;
                    break;

                case 0:
                default:
                    // Default Windows Taskbar (restores native Windows composition once)
                    accentState = ACCENT_DISABLED;
                    accentFlags = 0;
                    break;
            }

            try
            {
                // Primary taskbar
                IntPtr primaryTray = FindWindow("Shell_TrayWnd", null);
                if (primaryTray != IntPtr.Zero)
                {
                    SetAccentPolicy(primaryTray, accentState, accentFlags, computedColor);
                }

                // Multi-monitor secondary taskbars (and any additional Shell_TrayWnd / SecondaryTrayWnd)
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (hWnd == IntPtr.Zero) return true;
                    StringBuilder sb = new StringBuilder(64);
                    if (GetClassName(hWnd, sb, sb.Capacity) > 0)
                    {
                        string cls = sb.ToString();
                        if (string.Equals(cls, "Shell_TrayWnd", StringComparison.Ordinal) ||
                            string.Equals(cls, "SecondaryTrayWnd", StringComparison.Ordinal))
                        {
                            SetAccentPolicy(hWnd, accentState, accentFlags, computedColor);
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
        }

        private static uint ComputeTaskbarGradientColor(int mode, uint overrideAabbggrr)
        {
            if (overrideAabbggrr != 0)
                return overrideAabbggrr;

            switch (mode)
            {
                case 1:
                    return TASKBAR_CLEAR_COLOR; // 0x00000000
                case 2:
                    return TASKBAR_BLUR_COLOR;  // 0x550D1912
                case 3:
                    return ColorToTaskbarAabbggrr(_presetTaskbarTintColor, _customTaskbarAlpha);
                case 4:
                    return ColorToTaskbarAabbggrr(_customTaskbarTintColor, _customTaskbarAlpha);
                case 0:
                default:
                    return 0x00000000;
            }
        }

        /// <summary>
        /// Centers or restores the pinned/running app icons inside Windows 10's taskbar:
        /// Shell_TrayWnd -> ReBarWindow32 -> MSTaskSwWClass -> MSTaskListWClass.
        /// When TaskbarCenterIcons is false, immediately restores MSTaskListWClass X position to 0.
        /// </summary>
        public static void ApplyTaskbarIconCentering(bool forceUpdate)
        {
            try
            {
                if (!_taskbarCenterIcons)
                {
                    RestoreTaskbarIconAlignment();
                    return;
                }

                IntPtr trayHwnd = FindWindow("Shell_TrayWnd", null);
                if (trayHwnd == IntPtr.Zero)
                    return;

                IntPtr rebarHwnd = FindWindowEx(trayHwnd, IntPtr.Zero, "ReBarWindow32", null);
                if (rebarHwnd == IntPtr.Zero)
                    return;

                IntPtr taskSwHwnd = FindWindowEx(rebarHwnd, IntPtr.Zero, "MSTaskSwWClass", null);
                if (taskSwHwnd == IntPtr.Zero)
                    taskSwHwnd = rebarHwnd;

                IntPtr taskListHwnd = FindWindowEx(taskSwHwnd, IntPtr.Zero, "MSTaskListWClass", null);
                if (taskListHwnd == IntPtr.Zero)
                    return;

                RECT trayRect;
                RECT swRect;
                if (!GetWindowRect(trayHwnd, out trayRect) || !GetWindowRect(taskSwHwnd, out swRect))
                    return;

                // Only center horizontal taskbars
                if (trayRect.Width <= trayRect.Height)
                    return;

                // Count visible top-level taskbar windows to estimate icon strip width cleanly without cross-process memory allocation
                int visibleAppCount = CountTaskbarEligibleWindows();
                int estimatedIconsWidth = Math.Max(160, (visibleAppCount + 5) * 44);

                int screenCenter = trayRect.Left + (trayRect.Width / 2);
                int desiredScreenLeft = screenCenter - (estimatedIconsWidth / 2);

                // Relative X offset inside MSTaskSwWClass / ReBarWindow32
                int targetRelX = desiredScreenLeft - swRect.Left;
                int maxRelX = Math.Max(0, (swRect.Width - estimatedIconsWidth) - 16);
                if (targetRelX < 0) targetRelX = 0;
                if (targetRelX > maxRelX) targetRelX = maxRelX;

                if (forceUpdate || Math.Abs(targetRelX - _lastCenteredTaskListX) > 6)
                {
                    _lastCenteredTaskListX = targetRelX;
                    SetWindowPos(
                        taskListHwnd,
                        IntPtr.Zero,
                        targetRelX,
                        0,
                        0,
                        0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);
                }
            }
            catch
            {
            }
        }

        public static void RestoreTaskbarIconAlignment()
        {
            try
            {
                IntPtr trayHwnd = FindWindow("Shell_TrayWnd", null);
                if (trayHwnd == IntPtr.Zero)
                    return;

                IntPtr rebarHwnd = FindWindowEx(trayHwnd, IntPtr.Zero, "ReBarWindow32", null);
                if (rebarHwnd == IntPtr.Zero)
                    return;

                IntPtr taskSwHwnd = FindWindowEx(rebarHwnd, IntPtr.Zero, "MSTaskSwWClass", null);
                if (taskSwHwnd == IntPtr.Zero)
                    taskSwHwnd = rebarHwnd;

                IntPtr taskListHwnd = FindWindowEx(taskSwHwnd, IntPtr.Zero, "MSTaskListWClass", null);
                if (taskListHwnd != IntPtr.Zero)
                {
                    _lastCenteredTaskListX = 0;
                    SetWindowPos(
                        taskListHwnd,
                        IntPtr.Zero,
                        0,
                        0,
                        0,
                        0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);
                }
            }
            catch
            {
            }
        }

        private static int CountTaskbarEligibleWindows()
        {
            int count = 0;
            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                        return true;

                    if (IsWindowCloaked(hWnd))
                        return true;

                    count++;
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
            return count;
        }

        // ====================================================================
        // Internal Helpers, WinEvent Callback & Overlay Timer
        // ====================================================================

        private static void DisableDwmBlurBehind(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return;

            try
            {
                DWM_BLURBEHIND bb = new DWM_BLURBEHIND();
                bb.dwFlags = DWM_BB_ENABLE;
                bb.fEnable = false;
                bb.hRgnBlur = IntPtr.Zero;
                bb.fTransitionOnMaximized = false;
                DwmEnableBlurBehindWindow(hWnd, ref bb);
            }
            catch
            {
            }
        }

        private static void SetAccentPolicy(IntPtr hWnd, int accentState, int accentFlags, uint gradientColor)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return;

            ACCENT_POLICY accent = new ACCENT_POLICY();
            accent.AccentState = accentState;
            accent.AccentFlags = accentFlags;
            accent.GradientColor = gradientColor;
            accent.AnimationId = 0;

            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, ptr, false);
                WINCOMPATTRDATA data = new WINCOMPATTRDATA();
                data.Attribute = WCA_ACCENT_POLICY;
                data.SizeOfData = size;
                data.Data = ptr;
                SetWindowCompositionAttribute(hWnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static bool IsTaskbarResetWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            StringBuilder sb = new StringBuilder(64);
            if (GetClassName(hWnd, sb, sb.Capacity) <= 0)
                return false;

            string cls = sb.ToString();
            return string.Equals(cls, "Shell_TrayWnd", StringComparison.Ordinal) ||
                   string.Equals(cls, "SecondaryTrayWnd", StringComparison.Ordinal) ||
                   string.Equals(cls, "Windows.UI.Core.CoreWindow", StringComparison.Ordinal);
        }

        private static void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            // idObject == 0 (OBJID_WINDOW) ensures we only inspect window-level events
            if (idObject != 0 || hwnd == IntPtr.Zero)
                return;

            if (eventType == EVENT_OBJECT_DESTROY)
            {
                if (_overlays.ContainsKey(hwnd))
                {
                    RemoveExplorerOverlay(hwnd);
                }
                return;
            }

            if (eventType == EVENT_OBJECT_HIDE || eventType == EVENT_SYSTEM_MINIMIZESTART)
            {
                EmeraldShadowOverlay ov;
                if (_overlays.TryGetValue(hwnd, out ov) && ov != null)
                {
                    ov.HideOverlay();
                }
                if (_taskbarMode > 0 && IsTaskbarResetWindow(hwnd))
                {
                    ApplyTaskbarStyleInternal(_taskbarMode, _customTaskbarTint, true);
                }
                return;
            }

            if (eventType == EVENT_OBJECT_LOCATIONCHANGE)
            {
                EmeraldShadowOverlay ov;
                if (_overlays.TryGetValue(hwnd, out ov) && ov != null)
                {
                    ov.SyncWithTarget(
                        _emeraldTintIntensity,
                        _emeraldShadowIntensity,
                        _shadowSpread,
                        _overlayBodyTintColor,
                        _overlayShadowGlowColor,
                        _overlayRimBorderColor,
                        _showRimBorder,
                        _shadowOnlyActiveWindow,
                        false);
                }
                else if (IsExplorerWindow(hwnd))
                {
                    SyncSingleExplorerOverlay(hwnd, false);
                }
                return;
            }

            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                if (IsExplorerWindow(hwnd))
                {
                    ApplyExplorerWindowStyle(hwnd, _explorerGlassEnabled, _explorerAlpha, _explorerBlurMode);
                }
                // Re-assert TranslucentTB-style clear/invisible or themed taskbar whenever foreground window changes
                if (_taskbarMode > 0)
                {
                    ApplyTaskbarStyleInternal(_taskbarMode, _customTaskbarTint, true);
                }
                if (_taskbarCenterIcons)
                {
                    ApplyTaskbarIconCentering(false);
                }
                // Refresh overlays so active/inactive rim and z-order update immediately
                SyncAllExplorerOverlays(false);
                return;
            }

            if (eventType == EVENT_OBJECT_SHOW || eventType == EVENT_SYSTEM_MINIMIZEEND)
            {
                if (IsExplorerWindow(hwnd))
                {
                    ApplyExplorerWindowStyle(hwnd, _explorerGlassEnabled, _explorerAlpha, _explorerBlurMode);
                    SyncSingleExplorerOverlay(hwnd, false);
                }
                else if (_taskbarMode > 0 && IsTaskbarResetWindow(hwnd))
                {
                    ApplyTaskbarStyleInternal(_taskbarMode, _customTaskbarTint, true);
                }
                if (_taskbarCenterIcons)
                {
                    ApplyTaskbarIconCentering(false);
                }
            }
        }

        private static void OnOverlaySyncTimerTick(object sender, EventArgs e)
        {
            if (_emeraldTintAndShadowEnabled)
            {
                SyncAllExplorerOverlays(false);
            }
        }

        private static void EnsureOverlayTimerState()
        {
            if (_overlaySyncTimer != null)
            {
                if (_isRunning && _emeraldTintAndShadowEnabled)
                {
                    if (!_overlaySyncTimer.Enabled)
                        _overlaySyncTimer.Start();
                }
                else
                {
                    if (_overlaySyncTimer.Enabled)
                        _overlaySyncTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Stops any old standalone ExplorerGlass.exe or conflicting TranslucentTB.exe background process
        /// so they do not fight over Shell_TrayWnd or Explorer windows.
        /// Never touches Explorer or any other process.
        /// </summary>
        public static void StopLegacyExplorerGlassProcesses()
        {
            try
            {
                int currentPid = Process.GetCurrentProcess().Id;
                string[] targetNames = new string[] { "ExplorerGlass", "TranslucentTB" };
                for (int t = 0; t < targetNames.Length; t++)
                {
                    Process[] procs = Process.GetProcessesByName(targetNames[t]);
                    for (int i = 0; i < procs.Length; i++)
                    {
                        if (procs[i].Id != currentPid)
                        {
                            try { procs[i].Kill(); } catch { }
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
