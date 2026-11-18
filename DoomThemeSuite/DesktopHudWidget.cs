using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DoomThemeSuite
{
    public static class DesktopHudWidget
    {
        // ====================================================================
        // Canonical Widget Identifiers & File Paths (8 Layered 32-bit HUDs)
        // ====================================================================

        public const string IdClockDate = "ClockDate";
        public const string IdSystemGpu = "SystemGpu";
        public const string IdStorageDrives = "StorageDrives";
        public const string IdNetworkTraffic = "NetworkTraffic";
        public const string IdMediaAudio = "MediaAudio";
        public const string IdNotesUptime = "NotesUptime";
        public const string IdSpectrum = "Spectrum";
        public const string IdAudioSpectrum = "Spectrum"; // Alias for cross-module convenience
        public const string IdPomodoro = "Pomodoro";
        public const string IdPomodoroTimer = "Pomodoro"; // Alias for cross-module convenience

        public static readonly string[] AllWidgetIds = new string[]
        {
            IdClockDate,
            IdSystemGpu,
            IdStorageDrives,
            IdNetworkTraffic,
            IdMediaAudio,
            IdNotesUptime,
            IdSpectrum,
            IdPomodoro
        };

        public static readonly string DefaultNotesPath =
            @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\widget_notes.txt";

        // ====================================================================
        // Win32 & CoreAudio Interop Declarations
        // ====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int Width;
            public int Height;
            public SIZE(int width, int height)
            {
                Width = width;
                Height = height;
            }
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
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        // CoreAudio WASAPI Peak Meter COM Interop (Zero-Lockup Hardware/Mixer Metering)
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject
        {
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);

            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);

            [PreserveSig]
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IntPtr ppDevice);

            [PreserveSig]
            int RegisterEndpointNotificationCallback(IntPtr pClient);

            [PreserveSig]
            int UnregisterEndpointNotificationCallback(IntPtr pClient);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

            [PreserveSig]
            int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);

            [PreserveSig]
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

            [PreserveSig]
            int GetState(out int pdwState);
        }

        [ComImport]
        [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            [PreserveSig]
            int GetPeakValue(out float pfPeak);

            [PreserveSig]
            int GetMeteringChannelCount(out int pnChannelCount);

            [PreserveSig]
            int GetChannelsPeakValues(int u32ChannelCount, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] afPeakValues);

            [PreserveSig]
            int QueryHardwareSupport(out int pdwHardwareSupportMask);
        }

        // WinMM WaveIn Interop Declarations (Optional Loopback / Preferred Audio Capture)
        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [DllImport("winmm.dll")]
        private static extern uint waveInGetNumDevs();

        [DllImport("winmm.dll")]
        private static extern int waveInOpen(
            out IntPtr phwi,
            uint uDeviceID,
            ref WAVEFORMATEX pwfx,
            IntPtr dwCallback,
            IntPtr dwInstance,
            uint fdwOpen);

        [DllImport("winmm.dll")]
        private static extern int waveInPrepareHeader(IntPtr hwi, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInUnprepareHeader(IntPtr hwi, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInAddBuffer(IntPtr hwi, ref WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInStart(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInReset(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInClose(IntPtr hwi);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

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

        [DllImport("user32.dll", SetLastError = true)]
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
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out FILETIME lpIdleTime,
            out FILETIME lpKernelTime,
            out FILETIME lpUserTime);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        private const int GWL_EXSTYLE = -20;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_SETCURSOR = 0x0020;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSELEAVE = 0x02A3;
        private const int WM_CAPTURECHANGED = 0x0215;

        private const int HTTRANSPARENT = -1;
        private const int HTCLIENT = 1;
        private const int MA_NOACTIVATE = 3;

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const uint ULW_ALPHA = 0x00000002;

        private const int SW_HIDE = 0;
        private const int SW_SHOWNA = 8;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOSENDCHANGING = 0x0400;

        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        private const byte VK_MEDIA_PREV_TRACK = 0xB1;
        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        // ====================================================================
        // Base Logical Geometry (100% Scale) & Formatting Tables
        // ====================================================================

        private const int BaseOuterMargin = 8;
        private const int BaseCardWidth = 296;
        private const int BaseCardHeight = 148;
        private const int BaseTotalWidth = BaseCardWidth + (BaseOuterMargin * 2);   // 312
        private const int BaseTotalHeight = BaseCardHeight + (BaseOuterMargin * 2); // 164

        private const int SparklineHistoryLen = 30;
        private const int MaxSpectrumBands = 64;

        private static readonly string[] TurkishMonths = new string[]
        {
            "",
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };

        private static readonly string[] TurkishDays = new string[]
        {
            "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi"
        };

        // Logical hit-rects for MediaAudio transport buttons (inside BaseTotalWidth x BaseTotalHeight space)
        private static readonly RectangleF[] MediaButtonLogicalRects = new RectangleF[]
        {
            new RectangleF(BaseOuterMargin + 16f, BaseOuterMargin + 105f, 60f, 30f),  // 0: Prev
            new RectangleF(BaseOuterMargin + 84f, BaseOuterMargin + 105f, 64f, 30f),  // 1: Play/Pause
            new RectangleF(BaseOuterMargin + 156f, BaseOuterMargin + 105f, 60f, 30f), // 2: Next
            new RectangleF(BaseOuterMargin + 224f, BaseOuterMargin + 105f, 56f, 30f)  // 3: Mute
        };

        // Logical hit-rects for Spectrum widget controls (0: Style cycle, 1: Prev, 2: Play/Pause, 3: Next)
        private static readonly RectangleF[] SpectrumButtonLogicalRects = new RectangleF[]
        {
            new RectangleF(BaseOuterMargin + 190f, BaseOuterMargin + 8f, 94f, 20f),   // 0: Cycle Style Pill
            new RectangleF(BaseOuterMargin + 200f, BaseOuterMargin + 114f, 24f, 22f), // 1: Mini Prev
            new RectangleF(BaseOuterMargin + 228f, BaseOuterMargin + 114f, 26f, 22f), // 2: Mini Play/Pause
            new RectangleF(BaseOuterMargin + 258f, BaseOuterMargin + 114f, 24f, 22f)  // 3: Mini Next
        };

        // Logical hit-rects for Pomodoro widget controls (0: Start/Pause, 1: Reset, 2: Skip Phase)
        private static readonly RectangleF[] PomodoroButtonLogicalRects = new RectangleF[]
        {
            new RectangleF(BaseOuterMargin + 118f, BaseOuterMargin + 108f, 68f, 26f), // 0: Start/Pause
            new RectangleF(BaseOuterMargin + 192f, BaseOuterMargin + 108f, 44f, 26f), // 1: Reset
            new RectangleF(BaseOuterMargin + 242f, BaseOuterMargin + 108f, 40f, 26f)  // 2: Skip
        };

        // ====================================================================
        // Suite Configuration & Runtime Backing State
        // ====================================================================

        private static bool _enabled = true;
        private static bool _isStarted = false;
        private static bool _isSmartPaused = false;

        private static bool _showClock = true;
        private static bool _showSystemGpu = true;
        private static bool _showStorage = true;
        private static bool _showNetwork = true;
        private static bool _showMedia = true;
        private static bool _showNotes = true;
        private static bool _showSpectrum = true;
        private static bool _showPomodoro = true;

        private static bool _widgetsLocked = true;
        private static int _layoutPreset = 0;         // 0=Sağ Komuta Kolonu, 1=Üst Panoramik Bar, 2=Köşe Kokpit
        private static int _widgetScalePercent = 100; // 85, 100, 115
        private static byte _hudOpacity = 230;        // 80..255

        private static Color _cardBg = Color.FromArgb(14, 24, 18);
        private static Color _accentRim = Color.FromArgb(108, 168, 129);
        private static Color _primaryText = Color.FromArgb(242, 247, 244);
        private static string _themeTitle = "Doctor Doom Emerald";

        public static Action<string, int, int> OnWidgetMoved;

        private static readonly Dictionary<string, Point> _customPositions =
            new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, WidgetInstance> _widgets =
            new Dictionary<string, WidgetInstance>(StringComparer.OrdinalIgnoreCase);

        private static System.Windows.Forms.Timer _refreshTimer = null;
        private static System.Windows.Forms.Timer _spectrumTimer = null;
        private static int _tickCounter = 0;
        private static int _spectrumFrameCounter = 0;

        // ====================================================================
        // Telemetry State (CPU, RAM, Async GPU, Storage, Network, Media, Notes)
        // ====================================================================

        // CPU
        private static bool _hasCpuBaseline = false;
        private static ulong _prevIdleTime = 0UL;
        private static ulong _prevKernelTime = 0UL;
        private static ulong _prevUserTime = 0UL;
        private static int _lastCpuSampleTick = 0;
        private static int _cachedCpuPercent = 0;
        private static readonly float[] _cpuHistory = new float[SparklineHistoryLen];

        // RAM
        private static int _cachedRamPercent = 0;
        private static double _cachedRamUsedGb = 0.0;
        private static double _cachedRamTotalGb = 0.0;
        private static readonly float[] _ramHistory = new float[SparklineHistoryLen];

        // Async NVIDIA GPU via nvidia-smi (never blocks UI thread)
        private static readonly object _gpuSync = new object();
        private static int _gpuPollInFlight = 0;
        private static int _gpuFailStreak = 0;
        private static int _lastGpuPollTick = 0;
        private static string _resolvedNvidiaSmiPath = null;
        private static bool _gpuAvailable = false;
        private static string _gpuName = "GPU";
        private static int _gpuUtilPercent = 0;
        private static double _gpuVramUsedGb = 0.0;
        private static double _gpuVramTotalGb = 0.0;
        private static int _gpuTempC = 0;

        // Storage Drives
        private struct DriveSnapshot
        {
            public string Label;
            public double UsedGb;
            public double FreeGb;
            public double TotalGb;
            public int UsedPercent;
        }

        private static List<DriveSnapshot> _cachedDrives = new List<DriveSnapshot>();
        private static int _lastDrivePollTick = 0;

        // Network Traffic
        private static bool _hasNetBaseline = false;
        private static long _prevNetBytesRecv = 0L;
        private static long _prevNetBytesSent = 0L;
        private static int _lastNetSampleTick = 0;
        private static double _cachedDownBytesSec = 0.0;
        private static double _cachedUpBytesSec = 0.0;
        private static readonly double[] _netDownHistory = new double[SparklineHistoryLen];
        private static readonly double[] _netUpHistory = new double[SparklineHistoryLen];
        private static string _cachedLocalIpv4 = "Bağlı";
        private static int _lastIpCheckTick = 0;

        // Media & Audio
        private static string _cachedMediaTitle = "Ses & Medya Kontrolü";
        private static string _cachedMediaSource = "Medya Tuşları Hazır";
        private static bool _hasActiveMediaTrack = false;
        private static int _lastMediaScanTick = 0;
        private static int _hoverMediaBtn = -1;
        private static int _flashMediaBtn = -1;
        private static int _flashMediaBtnTick = 0;
        private static int _marqueeCharOffset = 0;

        // Notes & Uptime
        private static string[] _cachedNotesLines = new string[]
        {
            "• SPLEND // Theme Studio v3.0 aktif",
            "• Widget kilidini açıp sürükleyebilirsiniz",
            "• Ctrl+Shift+Space ile Komut Paletini açın"
        };
        private static DateTime _lastNotesWriteUtc = DateTime.MinValue;
        private static bool _notesInitialized = false;

        // ====================================================================
        // Widget 7: Live Audio Spectrum Visualizer State
        // ====================================================================

        private static int _spectrumStyle = 0;            // 0 = Bars, 1 = Mirror, 2 = Wave, 3 = RadialRing
        private static int _spectrumBandCount = 32;       // 16, 32, 64
        private static float _spectrumSmoothing = 0.68f;  // 0.10f .. 0.95f
        private static bool _spectrumPeakHold = true;

        private static readonly float[] _spectrumBands = new float[MaxSpectrumBands];
        private static readonly float[] _spectrumPeaks = new float[MaxSpectrumBands];
        private static readonly float[] _spectrumPeakVelocity = new float[MaxSpectrumBands];
        private static readonly float[] _fftReal = new float[256];
        private static readonly float[] _fftImag = new float[256];

        private static float _liveAudioMasterPeak = 0.0f;
        private static float _liveAudioLeftPeak = 0.0f;
        private static float _liveAudioRightPeak = 0.0f;
        private static bool _coreAudioInitialized = false;
        private static bool _coreAudioFailed = false;
        private static IAudioMeterInformation _audioMeterInfo = null;
        private static int _lastCoreAudioRetryTick = 0;

        // Optional WinMM WaveIn PCM capture state
        private static IntPtr _waveInHandle = IntPtr.Zero;
        private static GCHandle _waveBufferGcHandle;
        private static byte[] _waveBufferBytes = null;
        private static WAVEHDR _waveHeader;
        private static bool _waveInAttempted = false;
        private static bool _waveInActive = false;

        private static int _hoverSpectrumBtn = -1;
        private static int _flashSpectrumBtn = -1;
        private static int _flashSpectrumBtnTick = 0;

        // ====================================================================
        // Widget 8: Pomodoro & Deep Focus Timer State
        // ====================================================================

        private static bool _pomodoroRunning = false;
        private static int _pomodoroPhaseIndex = 0;        // 0 = ODAK (25m), 1 = MOLA (5m), 2 = UZUN MOLA (15m)
        private static string _pomodoroPhase = "ODAK";
        private static int _pomodoroFocusMinutes = 25;
        private static int _pomodoroShortBreakMinutes = 5;
        private static int _pomodoroLongBreakMinutes = 15;
        private static int _pomodoroRemainingSeconds = 25 * 60;
        private static int _pomodoroCompletedSessions = 0; // 0..4
        private static int _lastPomodoroWallTick = 0;

        private static int _hoverPomodoroBtn = -1;
        private static int _flashPomodoroBtn = -1;
        private static int _flashPomodoroBtnTick = 0;

        // Cached Font Family availability
        private static string _resolvedMonoFontFamily = null;

        // ====================================================================
        // Public Static Properties
        // ====================================================================

        public static bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (_enabled)
                {
                    EnsureStartedAndRender(false);
                }
                else
                {
                    StopInternal();
                }
            }
        }

        public static bool IsSmartPaused
        {
            get { return _isSmartPaused; }
            set
            {
                if (_isSmartPaused == value) return;
                _isSmartPaused = value;
                UpdateTimerFrequenciesForSmartPause();
                if (!_isSmartPaused && _enabled && _isStarted)
                {
                    SyncAndRenderAllWidgets(false);
                }
            }
        }

        public static bool ShowClock
        {
            get { return _showClock; }
            set
            {
                if (_showClock == value) return;
                _showClock = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowSystemGpu
        {
            get { return _showSystemGpu; }
            set
            {
                if (_showSystemGpu == value) return;
                _showSystemGpu = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowStorage
        {
            get { return _showStorage; }
            set
            {
                if (_showStorage == value) return;
                _showStorage = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowNetwork
        {
            get { return _showNetwork; }
            set
            {
                if (_showNetwork == value) return;
                _showNetwork = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowMedia
        {
            get { return _showMedia; }
            set
            {
                if (_showMedia == value) return;
                _showMedia = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowNotes
        {
            get { return _showNotes; }
            set
            {
                if (_showNotes == value) return;
                _showNotes = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowSpectrum
        {
            get { return _showSpectrum; }
            set
            {
                if (_showSpectrum == value) return;
                _showSpectrum = value;
                UpdateTimerFrequenciesForSmartPause();
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowAudioSpectrum
        {
            get { return _showSpectrum; }
            set { ShowSpectrum = value; }
        }

        public static bool ShowPomodoro
        {
            get { return _showPomodoro; }
            set
            {
                if (_showPomodoro == value) return;
                _showPomodoro = value;
                OnConfigPropertyChanged();
            }
        }

        public static bool ShowPomodoroTimer
        {
            get { return _showPomodoro; }
            set { ShowPomodoro = value; }
        }

        public static bool WidgetsLocked
        {
            get { return _widgetsLocked; }
            set
            {
                if (_widgetsLocked == value) return;
                _widgetsLocked = value;
                _hoverMediaBtn = -1;
                _hoverSpectrumBtn = -1;
                _hoverPomodoroBtn = -1;
                UpdateAllWindowLockStyles();
                OnConfigPropertyChanged();
            }
        }

        public static int LayoutPreset
        {
            get { return _layoutPreset; }
            set
            {
                int clamped = ClampLayout(value);
                if (_layoutPreset == clamped) return;
                _layoutPreset = clamped;
                OnConfigPropertyChanged();
            }
        }

        /// <summary>
        /// Backward-compatible alias for LayoutPreset.
        /// </summary>
        public static int PositionPreset
        {
            get { return _layoutPreset; }
            set { LayoutPreset = value; }
        }

        public static int WidgetScalePercent
        {
            get { return _widgetScalePercent; }
            set
            {
                int normalized = NormalizeScalePercent(value);
                if (_widgetScalePercent == normalized) return;
                _widgetScalePercent = normalized;
                OnConfigPropertyChanged();
            }
        }

        public static byte HudOpacity
        {
            get { return _hudOpacity; }
            set
            {
                byte clamped = ClampOpacity(value);
                if (_hudOpacity == clamped) return;
                _hudOpacity = clamped;
                OnConfigPropertyChanged();
            }
        }

        public static Color CardBackground
        {
            get { return _cardBg; }
            set { _cardBg = value; OnConfigPropertyChanged(); }
        }

        public static Color AccentRim
        {
            get { return _accentRim; }
            set { _accentRim = value; OnConfigPropertyChanged(); }
        }

        public static Color PrimaryText
        {
            get { return _primaryText; }
            set { _primaryText = value; OnConfigPropertyChanged(); }
        }

        public static string ThemeTitle
        {
            get { return _themeTitle; }
            set
            {
                _themeTitle = string.IsNullOrEmpty(value) ? "Custom Studio" : value.Trim();
                OnConfigPropertyChanged();
            }
        }

        // ====================================================================
        // Spectrum Public Properties & Methods
        // ====================================================================

        /// <summary>
        /// Visual style for the Spectrum HUD widget: 0 = Bars, 1 = Mirror, 2 = Wave, 3 = RadialRing.
        /// </summary>
        public static int SpectrumStyle
        {
            get { return _spectrumStyle; }
            set
            {
                int clamped = ClampInt(value, 0, 3);
                if (_spectrumStyle == clamped) return;
                _spectrumStyle = clamped;
                RenderSpecificWidgetIfActive(IdSpectrum);
            }
        }

        /// <summary>
        /// Number of frequency bands (16, 32, or 64).
        /// </summary>
        public static int SpectrumBandCount
        {
            get { return _spectrumBandCount; }
            set
            {
                int norm = NormalizeBandCount(value);
                if (_spectrumBandCount == norm) return;
                _spectrumBandCount = norm;
                RenderSpecificWidgetIfActive(IdSpectrum);
            }
        }

        /// <summary>
        /// Temporal smoothing factor (0.10f to 0.95f).
        /// </summary>
        public static float SpectrumSmoothing
        {
            get { return _spectrumSmoothing; }
            set
            {
                float clamped = value;
                if (clamped < 0.10f) clamped = 0.10f;
                if (clamped > 0.95f) clamped = 0.95f;
                _spectrumSmoothing = clamped;
            }
        }

        public static bool SpectrumPeakHold
        {
            get { return _spectrumPeakHold; }
            set
            {
                if (_spectrumPeakHold == value) return;
                _spectrumPeakHold = value;
                RenderSpecificWidgetIfActive(IdSpectrum);
            }
        }

        public static float CurrentAudioPeakLevel
        {
            get { return _liveAudioMasterPeak; }
        }

        public static float[] GetSpectrumBandsSnapshot()
        {
            int count = _spectrumBandCount;
            if (count < 16) count = 16;
            if (count > MaxSpectrumBands) count = MaxSpectrumBands;
            float[] copy = new float[count];
            Array.Copy(_spectrumBands, 0, copy, 0, count);
            return copy;
        }

        public static float[] GetSpectrumPeaksSnapshot()
        {
            int count = _spectrumBandCount;
            if (count < 16) count = 16;
            if (count > MaxSpectrumBands) count = MaxSpectrumBands;
            float[] copy = new float[count];
            Array.Copy(_spectrumPeaks, 0, copy, 0, count);
            return copy;
        }

        // ====================================================================
        // Pomodoro Public Properties & Methods
        // ====================================================================

        public static bool PomodoroRunning
        {
            get { return _pomodoroRunning; }
            set
            {
                if (_pomodoroRunning == value) return;
                _pomodoroRunning = value;
                _lastPomodoroWallTick = Environment.TickCount;
                RenderSpecificWidgetIfActive(IdPomodoro);
            }
        }

        public static string PomodoroPhase
        {
            get { return _pomodoroPhase; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                string upper = value.Trim().ToUpperInvariant();
                if (upper.Contains("UZUN"))
                {
                    _pomodoroPhaseIndex = 2;
                    _pomodoroPhase = "UZUN MOLA";
                }
                else if (upper.Contains("MOLA") || upper.Contains("BREAK"))
                {
                    _pomodoroPhaseIndex = 1;
                    _pomodoroPhase = "MOLA";
                }
                else
                {
                    _pomodoroPhaseIndex = 0;
                    _pomodoroPhase = "ODAK";
                }
                RenderSpecificWidgetIfActive(IdPomodoro);
            }
        }

        public static int PomodoroPhaseIndex
        {
            get { return _pomodoroPhaseIndex; }
            set
            {
                int idx = ClampInt(value, 0, 2);
                _pomodoroPhaseIndex = idx;
                _pomodoroPhase = (idx == 0) ? "ODAK" : (idx == 1 ? "MOLA" : "UZUN MOLA");
                _pomodoroRemainingSeconds = GetPhaseDurationSeconds(idx);
                _lastPomodoroWallTick = Environment.TickCount;
                RenderSpecificWidgetIfActive(IdPomodoro);
            }
        }

        public static int PomodoroRemainingSeconds
        {
            get { return _pomodoroRemainingSeconds; }
            set
            {
                _pomodoroRemainingSeconds = ClampInt(value, 0, 180 * 60);
                _lastPomodoroWallTick = Environment.TickCount;
                RenderSpecificWidgetIfActive(IdPomodoro);
            }
        }

        public static int PomodoroTotalPhaseSeconds
        {
            get { return GetPhaseDurationSeconds(_pomodoroPhaseIndex); }
        }

        public static int PomodoroCompletedSessions
        {
            get { return _pomodoroCompletedSessions; }
            set
            {
                _pomodoroCompletedSessions = ClampInt(value, 0, 4);
                RenderSpecificWidgetIfActive(IdPomodoro);
            }
        }

        public static int PomodoroFocusMinutes
        {
            get { return _pomodoroFocusMinutes; }
            set
            {
                _pomodoroFocusMinutes = ClampInt(value, 1, 120);
                if (!_pomodoroRunning && _pomodoroPhaseIndex == 0)
                {
                    _pomodoroRemainingSeconds = _pomodoroFocusMinutes * 60;
                    RenderSpecificWidgetIfActive(IdPomodoro);
                }
            }
        }

        public static int PomodoroShortBreakMinutes
        {
            get { return _pomodoroShortBreakMinutes; }
            set
            {
                _pomodoroShortBreakMinutes = ClampInt(value, 1, 60);
                if (!_pomodoroRunning && _pomodoroPhaseIndex == 1)
                {
                    _pomodoroRemainingSeconds = _pomodoroShortBreakMinutes * 60;
                    RenderSpecificWidgetIfActive(IdPomodoro);
                }
            }
        }

        public static int PomodoroLongBreakMinutes
        {
            get { return _pomodoroLongBreakMinutes; }
            set
            {
                _pomodoroLongBreakMinutes = ClampInt(value, 1, 90);
                if (!_pomodoroRunning && _pomodoroPhaseIndex == 2)
                {
                    _pomodoroRemainingSeconds = _pomodoroLongBreakMinutes * 60;
                    RenderSpecificWidgetIfActive(IdPomodoro);
                }
            }
        }

        public static void StartPausePomodoro()
        {
            _pomodoroRunning = !_pomodoroRunning;
            _lastPomodoroWallTick = Environment.TickCount;
            if (_pomodoroRemainingSeconds <= 0)
            {
                _pomodoroRemainingSeconds = GetPhaseDurationSeconds(_pomodoroPhaseIndex);
            }
            RenderSpecificWidgetIfActive(IdPomodoro);
        }

        public static void ResetPomodoro()
        {
            _pomodoroRunning = false;
            _pomodoroPhaseIndex = 0;
            _pomodoroPhase = "ODAK";
            _pomodoroRemainingSeconds = _pomodoroFocusMinutes * 60;
            _lastPomodoroWallTick = Environment.TickCount;
            RenderSpecificWidgetIfActive(IdPomodoro);
        }

        public static void SkipPomodoroPhase()
        {
            AdvancePomodoroPhaseInternal(false);
            RenderSpecificWidgetIfActive(IdPomodoro);
        }

        public static void ConfigurePomodoroDurations(int focusMinutes, int shortBreakMinutes, int longBreakMinutes)
        {
            _pomodoroFocusMinutes = ClampInt(focusMinutes, 1, 120);
            _pomodoroShortBreakMinutes = ClampInt(shortBreakMinutes, 1, 60);
            _pomodoroLongBreakMinutes = ClampInt(longBreakMinutes, 1, 90);
            if (!_pomodoroRunning)
            {
                _pomodoroRemainingSeconds = GetPhaseDurationSeconds(_pomodoroPhaseIndex);
            }
            RenderSpecificWidgetIfActive(IdPomodoro);
        }

        // ====================================================================
        // Live Telemetry Snapshot Getters (For Header Chips & Studio Views)
        // ====================================================================

        public static int LatestCpuPercent { get { return _cachedCpuPercent; } }
        public static int LatestRamPercent { get { return _cachedRamPercent; } }
        public static double LatestRamUsedGb { get { return _cachedRamUsedGb; } }
        public static double LatestRamTotalGb { get { return _cachedRamTotalGb; } }
        public static int LatestGpuUtilPercent { get { return _gpuUtilPercent; } }
        public static int LatestGpuTempC { get { return _gpuTempC; } }
        public static bool IsGpuTelemetryAvailable { get { return _gpuAvailable; } }
        public static string ActiveMediaTitle { get { return _cachedMediaTitle; } }
        public static string ActiveMediaSource { get { return _cachedMediaSource; } }

        public static float[] GetCpuHistorySnapshot()
        {
            float[] copy = new float[SparklineHistoryLen];
            Array.Copy(_cpuHistory, 0, copy, 0, SparklineHistoryLen);
            return copy;
        }

        // ====================================================================
        // Public Static Methods
        // ====================================================================

        public static void StartOrUpdate(
            bool enabled,
            int positionPreset,
            byte opacity,
            Color cardBg,
            Color accentRim,
            Color primaryText,
            string themeTitle)
        {
            _layoutPreset = ClampLayout(positionPreset);
            _hudOpacity = ClampOpacity(opacity);
            _cardBg = cardBg;
            _accentRim = accentRim;
            _primaryText = primaryText;
            _themeTitle = string.IsNullOrEmpty(themeTitle) ? "Custom Studio" : themeTitle.Trim();
            _enabled = enabled;

            if (!_enabled)
            {
                StopInternal();
                return;
            }

            EnsureStartedAndRender(false);
        }

        public static void StartOrUpdate(
            bool enabled,
            int layoutPreset,
            int scalePercent,
            byte opacity,
            bool locked,
            Color cardBg,
            Color accentRim,
            Color primaryText,
            string themeTitle)
        {
            _layoutPreset = ClampLayout(layoutPreset);
            _widgetScalePercent = NormalizeScalePercent(scalePercent);
            _hudOpacity = ClampOpacity(opacity);
            _widgetsLocked = locked;
            _cardBg = cardBg;
            _accentRim = accentRim;
            _primaryText = primaryText;
            _themeTitle = string.IsNullOrEmpty(themeTitle) ? "Custom Studio" : themeTitle.Trim();
            _enabled = enabled;

            if (!_enabled)
            {
                StopInternal();
                return;
            }

            UpdateAllWindowLockStyles();
            EnsureStartedAndRender(false);
        }

        public static void UpdateThemeColors(Color cardBg, Color accentRim, Color primaryText, string themeTitle)
        {
            _cardBg = cardBg;
            _accentRim = accentRim;
            _primaryText = primaryText;
            if (!string.IsNullOrEmpty(themeTitle))
            {
                _themeTitle = themeTitle.Trim();
            }

            if (_enabled && _isStarted)
            {
                SyncAndRenderAllWidgets(false);
            }
        }

        public static void ApplyWidgetLayout(int layoutPreset)
        {
            _layoutPreset = ClampLayout(layoutPreset);
            _customPositions.Clear();
            if (_enabled && _isStarted)
            {
                SyncAndRenderAllWidgets(false);
            }
        }

        public static void ResetCustomPositionsAndApplyLayout(int layoutPreset)
        {
            ApplyWidgetLayout(layoutPreset);
        }

        public static void SetCustomWidgetPosition(string widgetId, int x, int y)
        {
            string canonical = NormalizeWidgetId(widgetId);
            if (string.IsNullOrEmpty(canonical))
            {
                return;
            }

            _customPositions[canonical] = new Point(x, y);
            if (_enabled && _isStarted)
            {
                SyncAndRenderAllWidgets(false);
            }
        }

        public static void ClearCustomWidgetPosition(string widgetId)
        {
            string canonical = NormalizeWidgetId(widgetId);
            if (string.IsNullOrEmpty(canonical))
            {
                return;
            }

            if (_customPositions.Remove(canonical) && _enabled && _isStarted)
            {
                SyncAndRenderAllWidgets(false);
            }
        }

        public static Dictionary<string, Point> GetCustomWidgetPositions()
        {
            Dictionary<string, Point> copy = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Point> kv in _customPositions)
            {
                copy[kv.Key] = kv.Value;
            }
            return copy;
        }

        public static bool IsWidgetVisible(string widgetId)
        {
            string id = NormalizeWidgetId(widgetId);
            if (string.Equals(id, IdClockDate, StringComparison.OrdinalIgnoreCase)) return _showClock;
            if (string.Equals(id, IdSystemGpu, StringComparison.OrdinalIgnoreCase)) return _showSystemGpu;
            if (string.Equals(id, IdStorageDrives, StringComparison.OrdinalIgnoreCase)) return _showStorage;
            if (string.Equals(id, IdNetworkTraffic, StringComparison.OrdinalIgnoreCase)) return _showNetwork;
            if (string.Equals(id, IdMediaAudio, StringComparison.OrdinalIgnoreCase)) return _showMedia;
            if (string.Equals(id, IdNotesUptime, StringComparison.OrdinalIgnoreCase)) return _showNotes;
            if (string.Equals(id, IdSpectrum, StringComparison.OrdinalIgnoreCase)) return _showSpectrum;
            if (string.Equals(id, IdPomodoro, StringComparison.OrdinalIgnoreCase)) return _showPomodoro;
            return false;
        }

        public static void SetWidgetVisible(string widgetId, bool visible)
        {
            string id = NormalizeWidgetId(widgetId);
            if (string.Equals(id, IdClockDate, StringComparison.OrdinalIgnoreCase)) ShowClock = visible;
            else if (string.Equals(id, IdSystemGpu, StringComparison.OrdinalIgnoreCase)) ShowSystemGpu = visible;
            else if (string.Equals(id, IdStorageDrives, StringComparison.OrdinalIgnoreCase)) ShowStorage = visible;
            else if (string.Equals(id, IdNetworkTraffic, StringComparison.OrdinalIgnoreCase)) ShowNetwork = visible;
            else if (string.Equals(id, IdMediaAudio, StringComparison.OrdinalIgnoreCase)) ShowMedia = visible;
            else if (string.Equals(id, IdNotesUptime, StringComparison.OrdinalIgnoreCase)) ShowNotes = visible;
            else if (string.Equals(id, IdSpectrum, StringComparison.OrdinalIgnoreCase)) ShowSpectrum = visible;
            else if (string.Equals(id, IdPomodoro, StringComparison.OrdinalIgnoreCase)) ShowPomodoro = visible;
        }

        public static void ReloadNotesText()
        {
            LoadNotesFromDisk(true);
            if (_enabled && _isStarted && _showNotes)
            {
                RenderSpecificWidgetIfActive(IdNotesUptime);
            }
        }

        public static Bitmap RenderSingleWidgetPreview(string widgetId, float scale)
        {
            string canonical = NormalizeWidgetId(widgetId);
            if (string.IsNullOrEmpty(canonical)) canonical = IdClockDate;
            return RenderWidgetBitmapOffscreen(canonical, scale, true);
        }

        public static Bitmap RenderWidgetThumbnailForPreview(Color cardBg, Color accentRim)
        {
            Bitmap thumb = new Bitmap(260, 120, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(thumb))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                Color savedBg = _cardBg;
                Color savedRim = _accentRim;
                try
                {
                    _cardBg = cardBg;
                    _accentRim = accentRim;
                    using (Bitmap clockBmp = RenderWidgetBitmapOffscreen(IdClockDate, 1.0f, true))
                    using (Bitmap specBmp = RenderWidgetBitmapOffscreen(IdSpectrum, 1.0f, true))
                    {
                        g.DrawImage(clockBmp, new Rectangle(2, 16, 124, 65));
                        g.DrawImage(specBmp, new Rectangle(132, 16, 124, 65));
                    }
                }
                finally
                {
                    _cardBg = savedBg;
                    _accentRim = savedRim;
                }
            }
            return thumb;
        }

        /// <summary>
        /// Renders all 8 widgets (including Spectrum in all 4 styles and Pomodoro) offscreen at current scale
        /// and verifies non-empty pixel output with 0 handle leaks.
        /// </summary>
        public static bool RunWidgetSelfTest()
        {
            try
            {
                EnsureNotesFileExists();
                LoadNotesFromDisk(true);
                SampleFastSynchronousTelemetry(true);
                StepAudioSpectrumPhysics(true);

                if (AllWidgetIds.Length < 8)
                {
                    return false;
                }

                for (int i = 0; i < AllWidgetIds.Length; i++)
                {
                    string id = AllWidgetIds[i];
                    using (Bitmap bmp = RenderWidgetBitmapOffscreen(id, 1.0f, false))
                    {
                        if (bmp == null || bmp.Width < 100 || bmp.Height < 80)
                        {
                            return false;
                        }

                        if (!HasNonEmptyPixels(bmp, 400))
                        {
                            return false;
                        }
                    }
                }

                // Also verify all 4 Spectrum visual styles render cleanly
                int savedStyle = _spectrumStyle;
                try
                {
                    for (int style = 0; style <= 3; style++)
                    {
                        _spectrumStyle = style;
                        using (Bitmap specBmp = RenderWidgetBitmapOffscreen(IdSpectrum, 1.0f, true))
                        {
                            if (specBmp == null || !HasNonEmptyPixels(specBmp, 400))
                            {
                                return false;
                            }
                        }
                    }
                }
                finally
                {
                    _spectrumStyle = savedStyle;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void RefreshNow()
        {
            if (_enabled && _isStarted)
            {
                SyncAndRenderAllWidgets(false);
            }
        }

        public static void Stop()
        {
            _enabled = false;
            StopInternal();
        }

        // ====================================================================
        // Lifecycle, Smart Pause & Suite Refresh Loops
        // ====================================================================

        private static string NormalizeWidgetId(string rawId)
        {
            if (string.IsNullOrEmpty(rawId)) return string.Empty;
            string s = rawId.Trim();
            if (string.Equals(s, IdClockDate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "Clock", StringComparison.OrdinalIgnoreCase))
                return IdClockDate;
            if (string.Equals(s, IdSystemGpu, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "System", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "CpuRam", StringComparison.OrdinalIgnoreCase))
                return IdSystemGpu;
            if (string.Equals(s, IdStorageDrives, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "Storage", StringComparison.OrdinalIgnoreCase))
                return IdStorageDrives;
            if (string.Equals(s, IdNetworkTraffic, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "Network", StringComparison.OrdinalIgnoreCase))
                return IdNetworkTraffic;
            if (string.Equals(s, IdMediaAudio, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "Media", StringComparison.OrdinalIgnoreCase))
                return IdMediaAudio;
            if (string.Equals(s, IdNotesUptime, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "Notes", StringComparison.OrdinalIgnoreCase))
                return IdNotesUptime;
            if (string.Equals(s, IdSpectrum, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "AudioSpectrum", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "SpectrumVisualizer", StringComparison.OrdinalIgnoreCase))
                return IdSpectrum;
            if (string.Equals(s, IdPomodoro, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "PomodoroTimer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "PomodoroFocus", StringComparison.OrdinalIgnoreCase))
                return IdPomodoro;
            return s;
        }

        private static void OnConfigPropertyChanged()
        {
            if (_enabled && _isStarted)
            {
                SyncAndRenderAllWidgets(false);
            }
        }

        private static void UpdateTimerFrequenciesForSmartPause()
        {
            try
            {
                if (_refreshTimer != null)
                {
                    _refreshTimer.Interval = _isSmartPaused ? 4000 : 1000;
                }

                if (_spectrumTimer != null)
                {
                    bool shouldRunFastSpectrum = _enabled && _isStarted && _showSpectrum && !_isSmartPaused;
                    if (shouldRunFastSpectrum && !_spectrumTimer.Enabled)
                    {
                        _spectrumTimer.Start();
                    }
                    else if (!shouldRunFastSpectrum && _spectrumTimer.Enabled)
                    {
                        _spectrumTimer.Stop();
                    }
                }
            }
            catch
            {
            }
        }

        private static void EnsureStartedAndRender(bool allowDeltaSample)
        {
            try
            {
                _isStarted = true;
                EnsureNotesFileExists();
                if (!_notesInitialized)
                {
                    LoadNotesFromDisk(true);
                }

                if (_refreshTimer == null)
                {
                    _refreshTimer = new System.Windows.Forms.Timer();
                    _refreshTimer.Interval = _isSmartPaused ? 4000 : 1000;
                    _refreshTimer.Tick += OnSuiteTimerTick;
                }

                if (_spectrumTimer == null)
                {
                    _spectrumTimer = new System.Windows.Forms.Timer();
                    _spectrumTimer.Interval = 55; // ~18 FPS smooth spectrum & peak-hold physics
                    _spectrumTimer.Tick += OnSpectrumAnimationTick;
                }

                StepAudioSpectrumPhysics(false);
                SyncAndRenderAllWidgets(allowDeltaSample);

                if (!_refreshTimer.Enabled)
                {
                    _refreshTimer.Start();
                }

                UpdateTimerFrequenciesForSmartPause();
            }
            catch
            {
            }
        }

        private static void StopInternal()
        {
            _isStarted = false;

            try
            {
                if (_spectrumTimer != null)
                {
                    _spectrumTimer.Stop();
                    _spectrumTimer.Tick -= OnSpectrumAnimationTick;
                    _spectrumTimer.Dispose();
                    _spectrumTimer = null;
                }
            }
            catch
            {
            }

            try
            {
                if (_refreshTimer != null)
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Tick -= OnSuiteTimerTick;
                    _refreshTimer.Dispose();
                    _refreshTimer = null;
                }
            }
            catch
            {
            }

            try
            {
                foreach (KeyValuePair<string, WidgetInstance> kv in _widgets)
                {
                    if (kv.Value != null)
                    {
                        kv.Value.DisposeWindow();
                    }
                }
                _widgets.Clear();
            }
            catch
            {
            }

            ShutdownWaveInCapture();
            _hasCpuBaseline = false;
            _hasNetBaseline = false;
            _hoverMediaBtn = -1;
            _hoverSpectrumBtn = -1;
            _hoverPomodoroBtn = -1;
        }

        private static void OnSuiteTimerTick(object sender, EventArgs e)
        {
            if (!_enabled)
            {
                StopInternal();
                return;
            }

            _tickCounter++;
            if (_hasActiveMediaTrack && _cachedMediaTitle != null && _cachedMediaTitle.Length > 24)
            {
                _marqueeCharOffset = (_marqueeCharOffset + 1) % (_cachedMediaTitle.Length + 6);
            }
            else
            {
                _marqueeCharOffset = 0;
            }

            TickPomodoroCountdown();
            SyncAndRenderAllWidgets(true);
        }

        private static void OnSpectrumAnimationTick(object sender, EventArgs e)
        {
            if (!_enabled || !_isStarted || !_showSpectrum || _isSmartPaused)
            {
                return;
            }

            _spectrumFrameCounter++;
            StepAudioSpectrumPhysics(false);
            RenderSpecificWidgetIfActive(IdSpectrum);
        }

        private static void SyncAndRenderAllWidgets(bool allowTelemetryTick)
        {
            if (!_enabled || !_isStarted)
            {
                return;
            }

            List<string> activeIds = GetActiveWidgetIds();
            if (activeIds.Count == 0)
            {
                HideAllExistingWindows();
                return;
            }

            SampleFastSynchronousTelemetry(allowTelemetryTick);
            if (_showSystemGpu && !_isSmartPaused)
            {
                MaybePollNvidiaGpuAsync();
            }
            if (_showNotes && (_tickCounter % 3 == 0))
            {
                LoadNotesFromDisk(false);
            }
            if (_showSpectrum && (_isSmartPaused || _spectrumTimer == null || !_spectrumTimer.Enabled))
            {
                StepAudioSpectrumPhysics(false);
            }

            float scale = _widgetScalePercent / 100.0f;
            int winW = Math.Max(160, (int)Math.Round(BaseTotalWidth * scale));
            int winH = Math.Max(90, (int)Math.Round(BaseTotalHeight * scale));

            Dictionary<string, Point> targetCoords = ComputeWidgetPositions(activeIds, winW, winH);

            // Hide windows for disabled widgets
            for (int i = 0; i < AllWidgetIds.Length; i++)
            {
                string id = AllWidgetIds[i];
                if (!IsWidgetVisible(id))
                {
                    WidgetInstance existing;
                    if (_widgets.TryGetValue(id, out existing) && existing != null)
                    {
                        existing.DisposeWindow();
                        _widgets.Remove(id);
                    }
                }
            }

            // Create / update / present each active widget
            for (int i = 0; i < activeIds.Count; i++)
            {
                string id = activeIds[i];
                WidgetInstance inst;
                if (!_widgets.TryGetValue(id, out inst) || inst == null || inst.Handle == IntPtr.Zero)
                {
                    inst = new WidgetInstance(id);
                    inst.CreateNativeHudWindow(winW, winH, _widgetsLocked);
                    _widgets[id] = inst;
                }
                else
                {
                    inst.SyncLockStyle(_widgetsLocked);
                }

                Point pt;
                if (!targetCoords.TryGetValue(id, out pt))
                {
                    pt = new Point(100, 100);
                }

                if (!inst.IsDragging)
                {
                    inst.CurrentX = pt.X;
                    inst.CurrentY = pt.Y;
                }

                using (Bitmap bmp = RenderWidgetBitmapOffscreen(id, scale, _widgetsLocked))
                {
                    inst.PresentBitmap(bmp, inst.CurrentX, inst.CurrentY, _hudOpacity);
                }
            }
        }

        private static void RenderSpecificWidgetIfActive(string widgetId)
        {
            string canonical = NormalizeWidgetId(widgetId);
            if (!_enabled || !_isStarted || !IsWidgetVisible(canonical))
            {
                return;
            }

            WidgetInstance inst;
            if (_widgets.TryGetValue(canonical, out inst) && inst != null && inst.Handle != IntPtr.Zero)
            {
                float scale = _widgetScalePercent / 100.0f;
                using (Bitmap bmp = RenderWidgetBitmapOffscreen(canonical, scale, _widgetsLocked))
                {
                    inst.PresentBitmap(bmp, inst.CurrentX, inst.CurrentY, _hudOpacity);
                }
            }
        }

        private static void HideAllExistingWindows()
        {
            foreach (KeyValuePair<string, WidgetInstance> kv in _widgets)
            {
                if (kv.Value != null)
                {
                    kv.Value.DisposeWindow();
                }
            }
            _widgets.Clear();
        }

        private static void UpdateAllWindowLockStyles()
        {
            foreach (KeyValuePair<string, WidgetInstance> kv in _widgets)
            {
                if (kv.Value != null && kv.Value.Handle != IntPtr.Zero)
                {
                    kv.Value.SyncLockStyle(_widgetsLocked);
                }
            }
        }

        private static List<string> GetActiveWidgetIds()
        {
            List<string> list = new List<string>(8);
            if (_showClock) list.Add(IdClockDate);
            if (_showSystemGpu) list.Add(IdSystemGpu);
            if (_showStorage) list.Add(IdStorageDrives);
            if (_showNetwork) list.Add(IdNetworkTraffic);
            if (_showMedia) list.Add(IdMediaAudio);
            if (_showNotes) list.Add(IdNotesUptime);
            if (_showSpectrum) list.Add(IdSpectrum);
            if (_showPomodoro) list.Add(IdPomodoro);
            return list;
        }

        // ====================================================================
        // Layout Engine (3 Responsive Presets + Custom Drag Overrides for 8 Widgets)
        // ====================================================================

        private static Dictionary<string, Point> ComputeWidgetPositions(List<string> activeIds, int winW, int winH)
        {
            Dictionary<string, Point> result = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);

            Rectangle wa = Screen.PrimaryScreen != null
                ? Screen.PrimaryScreen.WorkingArea
                : new Rectangle(0, 0, 1920, 1040);

            int edgePad = 14;
            int gap = Math.Max(6, (int)Math.Round(8.0 * (_widgetScalePercent / 100.0)));

            if (_layoutPreset == 1)
            {
                // 1 = Üst Panoramik Bar (Ultrawide Horizontal Top Strip)
                int count = activeIds.Count;
                int maxCols = Math.Max(1, (wa.Width - (edgePad * 2) + gap) / (winW + gap));
                if (maxCols > count) maxCols = count;

                for (int i = 0; i < count; i++)
                {
                    int row = i / maxCols;
                    int col = i % maxCols;
                    int itemsInThisRow = (row == count / maxCols) ? (count % maxCols) : maxCols;
                    if (itemsInThisRow == 0) itemsInThisRow = maxCols;

                    int rowTotalW = (itemsInThisRow * winW) + ((itemsInThisRow - 1) * gap);
                    int startX = wa.Left + (wa.Width - rowTotalW) / 2;

                    int x = startX + col * (winW + gap);
                    int y = wa.Top + edgePad + row * (winH + gap);
                    result[activeIds[i]] = new Point(x, y);
                }
            }
            else if (_layoutPreset == 2)
            {
                // 2 = Köşe Kokpit Düzeni (Top-Center + Right 2-column Cockpit, clear of left desktop icons)
                int rightColX = wa.Right - winW - edgePad;
                int secondRightColX = rightColX - winW - gap;
                int centerTopX = wa.Left + (wa.Width - winW) / 2;

                result[IdClockDate] = new Point(centerTopX - (winW / 2) - (gap / 2), wa.Top + edgePad);
                result[IdPomodoro] = new Point(centerTopX + (winW / 2) + (gap / 2), wa.Top + edgePad);
                result[IdSystemGpu] = new Point(rightColX, wa.Top + edgePad);
                result[IdNetworkTraffic] = new Point(rightColX, wa.Top + edgePad + winH + gap);
                result[IdStorageDrives] = new Point(secondRightColX, wa.Bottom - (winH * 2) - gap - edgePad);
                result[IdSpectrum] = new Point(rightColX, wa.Bottom - (winH * 2) - gap - edgePad);
                result[IdMediaAudio] = new Point(rightColX, wa.Bottom - winH - edgePad);
                result[IdNotesUptime] = new Point(secondRightColX, wa.Bottom - winH - edgePad);
            }
            else
            {
                // 0 = Sağ Komuta Kolonu (Default: Clean 2-column right-docked command grid along WorkingArea.Right)
                int colRightX = wa.Right - winW - edgePad;
                int colLeftX = colRightX - winW - gap;

                int totalRows = (activeIds.Count + 1) / 2;
                int rowStep = winH + gap;
                if (totalRows > 1)
                {
                    int availH = wa.Height - (edgePad * 2) - winH;
                    int maxStep = availH / (totalRows - 1);
                    if (maxStep < rowStep && maxStep >= 80)
                    {
                        rowStep = maxStep;
                    }
                }

                for (int i = 0; i < activeIds.Count; i++)
                {
                    string id = activeIds[i];
                    int row = i / 2;
                    int col = i % 2;

                    int x;
                    if (activeIds.Count == 1)
                    {
                        x = colRightX;
                    }
                    else
                    {
                        x = (col == 0) ? colLeftX : colRightX;
                    }

                    int y = wa.Top + edgePad + (row * rowStep);
                    result[id] = new Point(x, y);
                }
            }

            // Apply per-widget custom dragged X,Y overrides (clamped to VirtualScreen)
            Rectangle vs = SystemInformation.VirtualScreen;
            for (int i = 0; i < activeIds.Count; i++)
            {
                string id = activeIds[i];
                Point customPt;
                if (_customPositions.TryGetValue(id, out customPt))
                {
                    int cx = ClampInt(customPt.X, vs.Left - winW + 80, vs.Right - 80);
                    int cy = ClampInt(customPt.Y, vs.Top, vs.Bottom - 60);
                    result[id] = new Point(cx, cy);
                }
            }

            return result;
        }

        // ====================================================================
        // Audio Spectrum Capture (CoreAudio WASAPI Peak + WaveIn FFT + Harmonic Synth)
        // ====================================================================

        private static void TrySampleCoreAudioPeak(out float masterPeak, out float leftPeak, out float rightPeak, out bool gotHardwareSample)
        {
            masterPeak = 0.0f;
            leftPeak = 0.0f;
            rightPeak = 0.0f;
            gotHardwareSample = false;

            int nowTick = Environment.TickCount;
            if (_coreAudioFailed && unchecked((uint)(nowTick - _lastCoreAudioRetryTick)) < 10000u)
            {
                return;
            }

            try
            {
                if (!_coreAudioInitialized || _audioMeterInfo == null)
                {
                    _lastCoreAudioRetryTick = nowTick;
                    IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
                    IMMDevice device = null;
                    // eRender = 0, eMultimedia = 1 (fallback eConsole = 0)
                    int hr = enumerator.GetDefaultAudioEndpoint(0, 1, out device);
                    if (hr != 0 || device == null)
                    {
                        hr = enumerator.GetDefaultAudioEndpoint(0, 0, out device);
                    }

                    if (hr == 0 && device != null)
                    {
                        Guid iidMeter = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");
                        object meterObj;
                        // CLSCTX_ALL = 23
                        int actHr = device.Activate(ref iidMeter, 23, IntPtr.Zero, out meterObj);
                        if (actHr == 0 && meterObj != null)
                        {
                            _audioMeterInfo = meterObj as IAudioMeterInformation;
                            _coreAudioInitialized = (_audioMeterInfo != null);
                            _coreAudioFailed = !_coreAudioInitialized;
                        }
                        else
                        {
                            _coreAudioFailed = true;
                        }
                    }
                    else
                    {
                        _coreAudioFailed = true;
                    }
                }

                if (_audioMeterInfo != null)
                {
                    float pk;
                    if (_audioMeterInfo.GetPeakValue(out pk) == 0)
                    {
                        if (pk >= 0.0f && pk <= 1.0f)
                        {
                            masterPeak = pk;
                            leftPeak = pk;
                            rightPeak = pk;
                            gotHardwareSample = true;
                        }
                    }

                    int chCount;
                    if (_audioMeterInfo.GetMeteringChannelCount(out chCount) == 0 && chCount >= 2 && chCount <= 8)
                    {
                        float[] chPeaks = new float[chCount];
                        if (_audioMeterInfo.GetChannelsPeakValues(chCount, chPeaks) == 0)
                        {
                            leftPeak = ClampFloat(chPeaks[0], 0.0f, 1.0f);
                            rightPeak = ClampFloat(chPeaks[1], 0.0f, 1.0f);
                        }
                    }
                }
            }
            catch
            {
                _audioMeterInfo = null;
                _coreAudioInitialized = false;
                _coreAudioFailed = true;
            }
        }

        private static bool TrySampleWaveInFft(float[] targetBands, int bandCount)
        {
            try
            {
                if (!_waveInAttempted)
                {
                    _waveInAttempted = true;
                    uint numDevs = waveInGetNumDevs();
                    if (numDevs > 0)
                    {
                        WAVEFORMATEX fmt = new WAVEFORMATEX();
                        fmt.wFormatTag = 1; // WAVE_FORMAT_PCM
                        fmt.nChannels = 1;
                        fmt.nSamplesPerSec = 22050;
                        fmt.wBitsPerSample = 16;
                        fmt.nBlockAlign = 2;
                        fmt.nAvgBytesPerSec = 44100;
                        fmt.cbSize = 0;

                        IntPtr hwi;
                        // WAVE_MAPPER = 0xFFFFFFFF
                        int res = waveInOpen(out hwi, 0xFFFFFFFFu, ref fmt, IntPtr.Zero, IntPtr.Zero, 0u);
                        if (res == 0 && hwi != IntPtr.Zero)
                        {
                            _waveInHandle = hwi;
                            _waveBufferBytes = new byte[512]; // 256 16-bit samples
                            _waveBufferGcHandle = GCHandle.Alloc(_waveBufferBytes, GCHandleType.Pinned);
                            _waveHeader = new WAVEHDR();
                            _waveHeader.lpData = _waveBufferGcHandle.AddrOfPinnedObject();
                            _waveHeader.dwBufferLength = (uint)_waveBufferBytes.Length;
                            _waveHeader.dwFlags = 0;

                            if (waveInPrepareHeader(_waveInHandle, ref _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR))) == 0)
                            {
                                waveInAddBuffer(_waveInHandle, ref _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
                                waveInStart(_waveInHandle);
                                _waveInActive = true;
                            }
                        }
                    }
                }

                if (!_waveInActive || _waveInHandle == IntPtr.Zero || _waveBufferBytes == null)
                {
                    return false;
                }

                // WHDR_DONE = 0x00000001
                if ((_waveHeader.dwFlags & 0x01u) == 0u)
                {
                    return false;
                }

                float signalRms = 0.0f;
                for (int i = 0; i < 256; i++)
                {
                    short sample = BitConverter.ToInt16(_waveBufferBytes, i * 2);
                    float norm = sample / 32768.0f;
                    signalRms += norm * norm;
                    // Hann window
                    float w = 0.5f * (1.0f - (float)Math.Cos((2.0 * Math.PI * i) / 255.0));
                    _fftReal[i] = norm * w;
                    _fftImag[i] = 0.0f;
                }

                // Re-queue buffer for next frame
                waveInAddBuffer(_waveInHandle, ref _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));

                signalRms = (float)Math.Sqrt(signalRms / 256.0f);
                if (signalRms < 0.008f)
                {
                    return false;
                }

                // In-place Radix-2 FFT (N = 256)
                ComputeRadix2Fft(_fftReal, _fftImag, 256);

                int bins = 128;
                for (int b = 0; b < bandCount; b++)
                {
                    int startBin = 1 + (b * (bins - 2)) / bandCount;
                    int endBin = Math.Max(startBin + 1, 1 + ((b + 1) * (bins - 2)) / bandCount);
                    float sum = 0.0f;
                    for (int k = startBin; k < endBin && k < bins; k++)
                    {
                        float mag = (float)Math.Sqrt(_fftReal[k] * _fftReal[k] + _fftImag[k] * _fftImag[k]);
                        sum += mag;
                    }
                    float avg = sum / Math.Max(1, endBin - startBin);
                    float scaled = (float)Math.Log10(1.0 + avg * 18.0);
                    targetBands[b] = ClampFloat(scaled, 0.04f, 1.0f);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ShutdownWaveInCapture()
        {
            try
            {
                _waveInActive = false;
                if (_waveInHandle != IntPtr.Zero)
                {
                    try { waveInReset(_waveInHandle); } catch { }
                    try { waveInUnprepareHeader(_waveInHandle, ref _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR))); } catch { }
                    try { waveInClose(_waveInHandle); } catch { }
                    _waveInHandle = IntPtr.Zero;
                }
                if (_waveBufferGcHandle.IsAllocated)
                {
                    _waveBufferGcHandle.Free();
                }
            }
            catch
            {
            }
        }

        private static void ComputeRadix2Fft(float[] real, float[] imag, int n)
        {
            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (i < j)
                {
                    float tr = real[j]; real[j] = real[i]; real[i] = tr;
                    float ti = imag[j]; imag[j] = imag[i]; imag[i] = ti;
                }
                int m = n >> 1;
                while (m >= 1 && j >= m)
                {
                    j -= m;
                    m >>= 1;
                }
                j += m;
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                int half = len >> 1;
                double ang = -2.0 * Math.PI / len;
                float wLenR = (float)Math.Cos(ang);
                float wLenI = (float)Math.Sin(ang);

                for (int i = 0; i < n; i += len)
                {
                    float wR = 1.0f;
                    float wI = 0.0f;
                    for (int k = 0; k < half; k++)
                    {
                        int u = i + k;
                        int v = i + k + half;
                        float tR = wR * real[v] - wI * imag[v];
                        float tI = wR * imag[v] + wI * real[v];
                        real[v] = real[u] - tR;
                        imag[v] = imag[u] - tI;
                        real[u] += tR;
                        imag[u] += tI;

                        float nextWR = wR * wLenR - wI * wLenI;
                        wI = wR * wLenI + wI * wLenR;
                        wR = nextWR;
                    }
                }
            }
        }

        private static void StepAudioSpectrumPhysics(bool forceDemoEnvelope)
        {
            float hwPeak;
            float hwLeft;
            float hwRight;
            bool gotHw;
            TrySampleCoreAudioPeak(out hwPeak, out hwLeft, out hwRight, out gotHw);

            float[] rawTarget = new float[MaxSpectrumBands];
            bool gotWaveFft = !forceDemoEnvelope && TrySampleWaveInFft(rawTarget, MaxSpectrumBands);

            double t = (_spectrumFrameCounter * 0.14) + (_tickCounter * 0.45);
            float baseEnergy;

            if (forceDemoEnvelope)
            {
                baseEnergy = 0.68f;
                _liveAudioMasterPeak = 0.72f;
                _liveAudioLeftPeak = 0.74f;
                _liveAudioRightPeak = 0.69f;
            }
            else if (gotHw && hwPeak > 0.01f)
            {
                // Boost perceived loudness curve (cube-root compression) so quiet music still dances vividly
                baseEnergy = ClampFloat((float)Math.Pow(hwPeak, 0.58) * 1.15f, 0.08f, 1.0f);
                _liveAudioMasterPeak = baseEnergy;
                _liveAudioLeftPeak = ClampFloat((float)Math.Pow(hwLeft, 0.58) * 1.12f, 0.06f, 1.0f);
                _liveAudioRightPeak = ClampFloat((float)Math.Pow(hwRight, 0.58) * 1.12f, 0.06f, 1.0f);
            }
            else if (_hasActiveMediaTrack)
            {
                baseEnergy = 0.56f + 0.22f * (float)Math.Abs(Math.Sin(t * 0.7));
                _liveAudioMasterPeak = baseEnergy;
                _liveAudioLeftPeak = ClampFloat(baseEnergy * (0.92f + 0.08f * (float)Math.Sin(t * 1.3)), 0.1f, 1.0f);
                _liveAudioRightPeak = ClampFloat(baseEnergy * (0.92f + 0.08f * (float)Math.Cos(t * 1.1)), 0.1f, 1.0f);
            }
            else
            {
                // Subtle cyber-studio idle breathing floor so spectrum bars always look alive
                baseEnergy = 0.22f + 0.10f * (float)Math.Sin(t * 0.5);
                _liveAudioMasterPeak = baseEnergy;
                _liveAudioLeftPeak = baseEnergy * 0.95f;
                _liveAudioRightPeak = baseEnergy * 0.90f;
            }

            if (!gotWaveFft)
            {
                // Multi-band harmonic synthesizer shaped by real stereo balance & musical frequency contours
                float stereoSkew = (_liveAudioRightPeak - _liveAudioLeftPeak) * 0.35f;
                for (int i = 0; i < MaxSpectrumBands; i++)
                {
                    double x = (double)i / (double)(MaxSpectrumBands - 1); // 0.0 (Sub-bass) -> 1.0 (Brilliance)

                    // Classic Fletcher-Munson / studio EQ curve: strong bass punch, dip at 35%, presence peak at 68%
                    double bassBell = Math.Exp(-Math.Pow((x - 0.12) / 0.16, 2.0)) * 0.95;
                    double midBell = Math.Exp(-Math.Pow((x - 0.46) / 0.22, 2.0)) * 0.72;
                    double presenceBell = Math.Exp(-Math.Pow((x - 0.76) / 0.18, 2.0)) * 0.80;
                    double envelope = 0.18 + Math.Max(bassBell, Math.Max(midBell, presenceBell));

                    double wave1 = Math.Sin(t * 1.85 - i * 0.29);
                    double wave2 = Math.Cos(t * 2.90 + i * 0.47);
                    double wave3 = Math.Sin(t * 4.10 - i * 0.71);
                    double harmonic = 0.48 + 0.26 * Math.Abs(wave1) + 0.16 * Math.Abs(wave2) + 0.10 * Math.Abs(wave3);

                    float panBias = 1.0f + stereoSkew * (float)(x - 0.5);
                    float val = (float)(baseEnergy * envelope * harmonic * panBias);
                    rawTarget[i] = ClampFloat(val, 0.05f, 0.98f);
                }
            }

            // Temporal smoothing + Peak-hold dots with gravity decay
            float smooth = ClampFloat(_spectrumSmoothing, 0.10f, 0.95f);
            for (int i = 0; i < MaxSpectrumBands; i++)
            {
                float prev = _spectrumBands[i];
                float target = rawTarget[i];

                // Fast attack, smooth release
                float factor = (target > prev) ? (smooth * 0.55f) : smooth;
                float current = (prev * factor) + (target * (1.0f - factor));
                current = ClampFloat(current, 0.04f, 1.0f);
                _spectrumBands[i] = current;

                if (current >= _spectrumPeaks[i])
                {
                    _spectrumPeaks[i] = current;
                    _spectrumPeakVelocity[i] = 0.004f;
                }
                else
                {
                    _spectrumPeakVelocity[i] += 0.0032f; // Gravity acceleration
                    _spectrumPeaks[i] = Math.Max(current, _spectrumPeaks[i] - _spectrumPeakVelocity[i]);
                }
            }
        }

        // ====================================================================
        // Pomodoro Timer Engine
        // ====================================================================

        private static int GetPhaseDurationSeconds(int phaseIdx)
        {
            if (phaseIdx == 1) return Math.Max(60, _pomodoroShortBreakMinutes * 60);
            if (phaseIdx == 2) return Math.Max(60, _pomodoroLongBreakMinutes * 60);
            return Math.Max(60, _pomodoroFocusMinutes * 60);
        }

        private static void TickPomodoroCountdown()
        {
            if (!_pomodoroRunning)
            {
                _lastPomodoroWallTick = Environment.TickCount;
                return;
            }

            int nowTick = Environment.TickCount;
            if (_lastPomodoroWallTick == 0)
            {
                _lastPomodoroWallTick = nowTick;
                return;
            }

            uint elapsedMs = unchecked((uint)(nowTick - _lastPomodoroWallTick));
            int elapsedSec = (int)Math.Max(1u, elapsedMs / 950u);
            if (elapsedSec > 10) elapsedSec = 1;
            _lastPomodoroWallTick = nowTick;

            _pomodoroRemainingSeconds -= elapsedSec;
            if (_pomodoroRemainingSeconds <= 0)
            {
                AdvancePomodoroPhaseInternal(true);
            }
        }

        private static void AdvancePomodoroPhaseInternal(bool keepRunning)
        {
            if (_pomodoroPhaseIndex == 0)
            {
                // Completed a Focus session
                _pomodoroCompletedSessions++;
                if (_pomodoroCompletedSessions >= 4)
                {
                    _pomodoroPhaseIndex = 2; // Long Break after 4 sessions
                    _pomodoroPhase = "UZUN MOLA";
                    _pomodoroCompletedSessions = 4;
                }
                else
                {
                    _pomodoroPhaseIndex = 1; // Short Break
                    _pomodoroPhase = "MOLA";
                }
            }
            else
            {
                // Completed a Break -> back to Focus
                if (_pomodoroPhaseIndex == 2)
                {
                    _pomodoroCompletedSessions = 0;
                }
                _pomodoroPhaseIndex = 0;
                _pomodoroPhase = "ODAK";
            }

            _pomodoroRemainingSeconds = GetPhaseDurationSeconds(_pomodoroPhaseIndex);
            _pomodoroRunning = keepRunning;
            _lastPomodoroWallTick = Environment.TickCount;
        }

        // ====================================================================
        // Telemetry Collectors (Zero-WMI, Fast & Safe)
        // ====================================================================

        private static void SampleFastSynchronousTelemetry(bool isTimerTick)
        {
            if (_showSystemGpu)
            {
                SampleCpuAndSparkline(isTimerTick);
                SampleMemoryStatus(isTimerTick);
            }

            if (_showStorage)
            {
                SampleStorageDrivesIfDue();
            }

            if (_showNetwork)
            {
                SampleNetworkTraffic(isTimerTick);
            }

            if (_showMedia || _showSpectrum)
            {
                SampleActiveMediaWindowIfDue();
            }
        }

        private static void SampleCpuAndSparkline(bool pushHistoryPoint)
        {
            try
            {
                int nowTick = Environment.TickCount;
                uint elapsedMs = unchecked((uint)(nowTick - _lastCpuSampleTick));

                if (_hasCpuBaseline && !pushHistoryPoint && elapsedMs < 400u)
                {
                    return;
                }

                FILETIME idleFt;
                FILETIME kernelFt;
                FILETIME userFt;
                if (!GetSystemTimes(out idleFt, out kernelFt, out userFt))
                {
                    return;
                }

                ulong idleNow = FileTimeToUInt64(idleFt);
                ulong kernelNow = FileTimeToUInt64(kernelFt);
                ulong userNow = FileTimeToUInt64(userFt);

                if (!_hasCpuBaseline)
                {
                    _prevIdleTime = idleNow;
                    _prevKernelTime = kernelNow;
                    _prevUserTime = userNow;
                    _lastCpuSampleTick = nowTick;
                    _hasCpuBaseline = true;
                    return;
                }

                if (elapsedMs >= 200u)
                {
                    ulong idleDelta = idleNow >= _prevIdleTime ? (idleNow - _prevIdleTime) : 0UL;
                    ulong kernelDelta = kernelNow >= _prevKernelTime ? (kernelNow - _prevKernelTime) : 0UL;
                    ulong userDelta = userNow >= _prevUserTime ? (userNow - _prevUserTime) : 0UL;

                    _prevIdleTime = idleNow;
                    _prevKernelTime = kernelNow;
                    _prevUserTime = userNow;
                    _lastCpuSampleTick = nowTick;

                    ulong totalSys = kernelDelta + userDelta;
                    if (totalSys > 0UL && totalSys >= idleDelta)
                    {
                        ulong busy = totalSys - idleDelta;
                        int pct = (int)Math.Round((busy * 100.0) / (double)totalSys);
                        _cachedCpuPercent = ClampInt(pct, 0, 100);
                    }
                }

                if (pushHistoryPoint)
                {
                    for (int i = 0; i < SparklineHistoryLen - 1; i++)
                    {
                        _cpuHistory[i] = _cpuHistory[i + 1];
                    }
                    _cpuHistory[SparklineHistoryLen - 1] = _cachedCpuPercent;
                }
            }
            catch
            {
            }
        }

        private static void SampleMemoryStatus(bool pushHistoryPoint)
        {
            try
            {
                MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref mem))
                {
                    _cachedRamPercent = ClampInt((int)mem.dwMemoryLoad, 0, 100);
                    const double BytesPerGb = 1024.0 * 1024.0 * 1024.0;
                    _cachedRamTotalGb = mem.ullTotalPhys / BytesPerGb;
                    double availGb = mem.ullAvailPhys / BytesPerGb;
                    _cachedRamUsedGb = Math.Max(0.0, _cachedRamTotalGb - availGb);

                    if (pushHistoryPoint)
                    {
                        for (int i = 0; i < SparklineHistoryLen - 1; i++)
                        {
                            _ramHistory[i] = _ramHistory[i + 1];
                        }
                        _ramHistory[SparklineHistoryLen - 1] = _cachedRamPercent;
                    }
                }
            }
            catch
            {
            }
        }

        private static void MaybePollNvidiaGpuAsync()
        {
            if (_gpuFailStreak >= 3)
            {
                return;
            }

            int nowTick = Environment.TickCount;
            if (_lastGpuPollTick != 0 && unchecked((uint)(nowTick - _lastGpuPollTick)) < 3000u)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _gpuPollInFlight, 1, 0) != 0)
            {
                return;
            }

            _lastGpuPollTick = nowTick;
            ThreadPool.QueueUserWorkItem(PollNvidiaSmiWorker);
        }

        private static void PollNvidiaSmiWorker(object state)
        {
            try
            {
                string smiPath = ResolveNvidiaSmiExecutable();
                if (string.IsNullOrEmpty(smiPath))
                {
                    _gpuFailStreak = 10;
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = smiPath;
                psi.Arguments = "--query-gpu=name,utilization.gpu,memory.used,memory.total,temperature.gpu --format=csv,noheader,nounits";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                using (Process proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        _gpuFailStreak++;
                        return;
                    }

                    string output = proc.StandardOutput.ReadToEnd();
                    if (!proc.WaitForExit(1800))
                    {
                        try { proc.Kill(); } catch { }
                        _gpuFailStreak++;
                        return;
                    }

                    if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
                    {
                        _gpuFailStreak++;
                        return;
                    }

                    string firstLine = output.Trim();
                    int nlIdx = firstLine.IndexOf('\n');
                    if (nlIdx >= 0)
                    {
                        firstLine = firstLine.Substring(0, nlIdx).Trim();
                    }

                    string[] parts = firstLine.Split(',');
                    if (parts.Length >= 5)
                    {
                        string rawName = parts[0].Trim();
                        rawName = rawName.Replace("NVIDIA GeForce ", "").Replace("NVIDIA ", "").Trim();
                        if (rawName.Length > 15)
                        {
                            rawName = rawName.Replace("Ti SUPER", "TiS").Replace("SUPER", "Super");
                        }

                        int util;
                        double memUsedMb;
                        double memTotalMb;
                        int tempC;

                        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out util))
                            util = 0;
                        if (!double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out memUsedMb))
                            memUsedMb = 0.0;
                        if (!double.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out memTotalMb))
                            memTotalMb = 0.0;
                        if (!int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out tempC))
                            tempC = 0;

                        lock (_gpuSync)
                        {
                            _gpuAvailable = true;
                            _gpuName = string.IsNullOrEmpty(rawName) ? "RTX GPU" : rawName;
                            _gpuUtilPercent = ClampInt(util, 0, 100);
                            _gpuVramUsedGb = memUsedMb / 1024.0;
                            _gpuVramTotalGb = memTotalMb / 1024.0;
                            _gpuTempC = ClampInt(tempC, 0, 120);
                        }
                        _gpuFailStreak = 0;
                    }
                }
            }
            catch
            {
                _gpuFailStreak++;
            }
            finally
            {
                Interlocked.Exchange(ref _gpuPollInFlight, 0);
            }
        }

        private static string ResolveNvidiaSmiExecutable()
        {
            if (_resolvedNvidiaSmiPath != null)
            {
                return _resolvedNvidiaSmiPath;
            }

            string sys32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvidia-smi.exe");
            if (File.Exists(sys32))
            {
                _resolvedNvidiaSmiPath = sys32;
                return _resolvedNvidiaSmiPath;
            }

            string nvsmi = @"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe";
            if (File.Exists(nvsmi))
            {
                _resolvedNvidiaSmiPath = nvsmi;
                return _resolvedNvidiaSmiPath;
            }

            _resolvedNvidiaSmiPath = "";
            return _resolvedNvidiaSmiPath;
        }

        private static void SampleStorageDrivesIfDue()
        {
            int nowTick = Environment.TickCount;
            if (_cachedDrives.Count > 0 && unchecked((uint)(nowTick - _lastDrivePollTick)) < 8000u)
            {
                return;
            }

            _lastDrivePollTick = nowTick;
            List<DriveSnapshot> snapshots = new List<DriveSnapshot>();

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                const double BytesPerGb = 1024.0 * 1024.0 * 1024.0;

                for (int i = 0; i < drives.Length && snapshots.Count < 3; i++)
                {
                    DriveInfo d = drives[i];
                    if (d.DriveType != DriveType.Fixed || !d.IsReady)
                    {
                        continue;
                    }

                    long totalBytes = d.TotalSize;
                    if (totalBytes <= 0L)
                    {
                        continue;
                    }

                    long freeBytes = d.AvailableFreeSpace;
                    long usedBytes = Math.Max(0L, totalBytes - freeBytes);

                    DriveSnapshot snap = new DriveSnapshot();
                    snap.Label = d.Name.TrimEnd('\\');
                    snap.TotalGb = totalBytes / BytesPerGb;
                    snap.FreeGb = freeBytes / BytesPerGb;
                    snap.UsedGb = usedBytes / BytesPerGb;
                    snap.UsedPercent = ClampInt((int)Math.Round((usedBytes * 100.0) / (double)totalBytes), 0, 100);
                    snapshots.Add(snap);
                }
            }
            catch
            {
            }

            _cachedDrives = snapshots;
        }

        private static void SampleNetworkTraffic(bool pushHistoryPoint)
        {
            try
            {
                int nowTick = Environment.TickCount;
                uint elapsedMs = unchecked((uint)(nowTick - _lastNetSampleTick));

                if (_hasNetBaseline && !pushHistoryPoint && elapsedMs < 400u)
                {
                    return;
                }

                long totalRecv = 0L;
                long totalSent = 0L;
                bool refreshIp = (_lastIpCheckTick == 0) || (unchecked((uint)(nowTick - _lastIpCheckTick)) >= 15000u);
                string detectedIp = null;

                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < nics.Length; i++)
                {
                    NetworkInterface nic = nics[i];
                    if (nic.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    try
                    {
                        IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();
                        totalRecv += stats.BytesReceived;
                        totalSent += stats.BytesSent;
                    }
                    catch
                    {
                    }

                    if (refreshIp && detectedIp == null)
                    {
                        try
                        {
                            IPInterfaceProperties ipProps = nic.GetIPProperties();
                            UnicastIPAddressInformationCollection unicasts = ipProps.UnicastAddresses;
                            for (int u = 0; u < unicasts.Count; u++)
                            {
                                if (unicasts[u].Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    string ipStr = unicasts[u].Address.ToString();
                                    if (!ipStr.StartsWith("169.254.", StringComparison.Ordinal))
                                    {
                                        detectedIp = ipStr;
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                if (refreshIp)
                {
                    _lastIpCheckTick = nowTick;
                    _cachedLocalIpv4 = string.IsNullOrEmpty(detectedIp) ? "Çevrimiçi" : detectedIp;
                }

                if (!_hasNetBaseline)
                {
                    _prevNetBytesRecv = totalRecv;
                    _prevNetBytesSent = totalSent;
                    _lastNetSampleTick = nowTick;
                    _hasNetBaseline = true;
                    return;
                }

                if (elapsedMs >= 250u)
                {
                    double seconds = elapsedMs / 1000.0;
                    long recvDelta = totalRecv >= _prevNetBytesRecv ? (totalRecv - _prevNetBytesRecv) : 0L;
                    long sentDelta = totalSent >= _prevNetBytesSent ? (totalSent - _prevNetBytesSent) : 0L;

                    _prevNetBytesRecv = totalRecv;
                    _prevNetBytesSent = totalSent;
                    _lastNetSampleTick = nowTick;

                    _cachedDownBytesSec = recvDelta / seconds;
                    _cachedUpBytesSec = sentDelta / seconds;
                }

                if (pushHistoryPoint)
                {
                    for (int i = 0; i < SparklineHistoryLen - 1; i++)
                    {
                        _netDownHistory[i] = _netDownHistory[i + 1];
                        _netUpHistory[i] = _netUpHistory[i + 1];
                    }
                    _netDownHistory[SparklineHistoryLen - 1] = _cachedDownBytesSec;
                    _netUpHistory[SparklineHistoryLen - 1] = _cachedUpBytesSec;
                }
            }
            catch
            {
            }
        }

        private static void SampleActiveMediaWindowIfDue()
        {
            int nowTick = Environment.TickCount;
            if (_lastMediaScanTick != 0 && unchecked((uint)(nowTick - _lastMediaScanTick)) < 2000u)
            {
                return;
            }
            _lastMediaScanTick = nowTick;

            string foundTrack = null;
            string foundApp = null;

            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    int len = GetWindowTextLength(hWnd);
                    if (len <= 2 || len > 260)
                        return true;

                    StringBuilder sb = new StringBuilder(len + 2);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString().Trim();
                    if (title.Length == 0)
                        return true;

                    if (title.EndsWith(" - VLC media player", StringComparison.OrdinalIgnoreCase))
                    {
                        foundTrack = title.Substring(0, title.Length - " - VLC media player".Length).Trim();
                        foundApp = "VLC Player";
                        return false;
                    }
                    if (title.EndsWith(" - YouTube Music", StringComparison.OrdinalIgnoreCase))
                    {
                        foundTrack = title.Substring(0, title.Length - " - YouTube Music".Length).Trim();
                        foundApp = "YouTube Music";
                        return false;
                    }
                    if (title.IndexOf(" - YouTube", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        int idx = title.IndexOf(" - YouTube", StringComparison.OrdinalIgnoreCase);
                        foundTrack = title.Substring(0, idx).Trim();
                        foundApp = "YouTube";
                        return false;
                    }

                    if (title.Contains(" - "))
                    {
                        uint pid;
                        GetWindowThreadProcessId(hWnd, out pid);
                        if (pid != 0)
                        {
                            try
                            {
                                using (Process p = Process.GetProcessById((int)pid))
                                {
                                    string procName = p.ProcessName;
                                    if (string.Equals(procName, "Spotify", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundTrack = title;
                                        foundApp = "Spotify";
                                        return false;
                                    }
                                    if (string.Equals(procName, "AppleMusic", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(procName, "iTunes", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundTrack = title;
                                        foundApp = "Apple Music";
                                        return false;
                                    }
                                    if (string.Equals(procName, "AIMP", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(procName, "foobar2000", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(procName, "MusicBee", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(procName, "PotPlayerMini64", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundTrack = title;
                                        foundApp = procName;
                                        return false;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(foundTrack))
            {
                _cachedMediaTitle = foundTrack;
                _cachedMediaSource = foundApp ?? "Medya Çalıyor";
                _hasActiveMediaTrack = true;
            }
            else
            {
                _cachedMediaTitle = "Ses & Medya Kontrolü";
                _cachedMediaSource = _widgetsLocked ? "WASAPI Ses Çıkışı" : "Medya tuşları aktif";
                _hasActiveMediaTrack = false;
            }
        }

        private static void EnsureNotesFileExists()
        {
            try
            {
                string dir = Path.GetDirectoryName(DefaultNotesPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(DefaultNotesPath))
                {
                    string defaultNote =
                        "• SPLEND // Theme Studio v3.0 aktif\r\n" +
                        "• Widget kilidini açıp sürükleyebilirsiniz\r\n" +
                        "• Ctrl+Shift+Space: Komut Paleti | Ctrl+Alt+T: Stüdyo\r\n";
                    File.WriteAllText(DefaultNotesPath, defaultNote, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static void LoadNotesFromDisk(bool force)
        {
            try
            {
                EnsureNotesFileExists();
                if (!File.Exists(DefaultNotesPath))
                {
                    return;
                }

                DateTime writeUtc = File.GetLastWriteTimeUtc(DefaultNotesPath);
                if (!force && _notesInitialized && writeUtc == _lastNotesWriteUtc)
                {
                    return;
                }

                _lastNotesWriteUtc = writeUtc;
                _notesInitialized = true;

                string[] rawLines = File.ReadAllLines(DefaultNotesPath, Encoding.UTF8);
                List<string> cleaned = new List<string>(4);
                for (int i = 0; i < rawLines.Length && cleaned.Count < 4; i++)
                {
                    string line = rawLines[i].Trim();
                    if (line.Length > 0)
                    {
                        cleaned.Add(line);
                    }
                }

                if (cleaned.Count == 0)
                {
                    cleaned.Add("• Hızlı not eklemek için Stüdyoyu açın");
                }

                _cachedNotesLines = cleaned.ToArray();
            }
            catch
            {
            }
        }

        // ====================================================================
        // Offscreen Per-Widget GDI+ Editorial Cyber-Studio v3.0 Renderer
        // ====================================================================

        private static Bitmap RenderWidgetBitmapOffscreen(string widgetId, float scale, bool isLocked)
        {
            if (scale < 0.75f) scale = 0.75f;
            if (scale > 1.35f) scale = 1.35f;

            int physW = Math.Max(160, (int)Math.Round(BaseTotalWidth * scale));
            int physH = Math.Max(90, (int)Math.Round(BaseTotalHeight * scale));

            Bitmap bmp = new Bitmap(physW, physH, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                if (Math.Abs(scale - 1.0f) > 0.001f)
                {
                    g.ScaleTransform(scale, scale);
                }

                Rectangle cardRect = new Rectangle(BaseOuterMargin, BaseOuterMargin, BaseCardWidth, BaseCardHeight);
                Color accent = EnsureReadableAccent(_accentRim);
                Color brightText = Color.FromArgb(255, _primaryText.R, _primaryText.G, _primaryText.B);
                Color mutedText = Color.FromArgb(186, MixColors(brightText, accent, 0.22f));
                Color warmAccent = ShiftHueAccent2(accent);

                DrawArmorCardChrome(g, cardRect, accent, warmAccent, isLocked);

                string canonical = NormalizeWidgetId(widgetId);
                if (string.Equals(canonical, IdClockDate, StringComparison.OrdinalIgnoreCase))
                {
                    DrawClockDateContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else if (string.Equals(canonical, IdSystemGpu, StringComparison.OrdinalIgnoreCase))
                {
                    DrawSystemGpuContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else if (string.Equals(canonical, IdStorageDrives, StringComparison.OrdinalIgnoreCase))
                {
                    DrawStorageDrivesContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else if (string.Equals(canonical, IdNetworkTraffic, StringComparison.OrdinalIgnoreCase))
                {
                    DrawNetworkTrafficContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else if (string.Equals(canonical, IdMediaAudio, StringComparison.OrdinalIgnoreCase))
                {
                    DrawMediaAudioContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else if (string.Equals(canonical, IdSpectrum, StringComparison.OrdinalIgnoreCase))
                {
                    DrawSpectrumContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else if (string.Equals(canonical, IdPomodoro, StringComparison.OrdinalIgnoreCase))
                {
                    DrawPomodoroContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
                else
                {
                    DrawNotesUptimeContent(g, cardRect, accent, warmAccent, brightText, mutedText, isLocked);
                }
            }

            return bmp;
        }

        private static void DrawArmorCardChrome(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            bool isLocked)
        {
            // 1. Soft Outer Neon Bloom outside cardRect
            g.SetClip(cardRect, CombineMode.Exclude);
            for (int d = BaseOuterMargin - 1; d >= 1; d--)
            {
                float t = (float)(BaseOuterMargin - d) / (float)BaseOuterMargin;
                int alpha = (int)Math.Round(38f * t * t);
                if (alpha < 2) alpha = 2;

                Rectangle ringRect = new Rectangle(
                    cardRect.X - d,
                    cardRect.Y - d,
                    cardRect.Width + (d * 2),
                    cardRect.Height + (d * 2));

                using (GraphicsPath ringPath = CreateRoundedRect(ringRect, 16 + d / 2))
                using (Pen ringPen = new Pen(Color.FromArgb(alpha, accent.R / 2, accent.G / 2, accent.B / 2), 1.8f))
                {
                    ringPen.LineJoin = LineJoin.Round;
                    g.DrawPath(ringPen, ringPath);
                }
            }
            g.ResetClip();

            // 2. L1 Bento Dark Glass Body Fill + Dual Radial Ambient Illumination + Top Accent Bar
            Color topFill = Color.FromArgb(242, ClampDarkCard(_cardBg, 1.18f));
            Color botFill = Color.FromArgb(250, ClampDarkCard(_cardBg, 0.65f));

            using (GraphicsPath cardPath = CreateRoundedRect(cardRect, 16))
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    cardRect,
                    topFill,
                    botFill,
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(bgBrush, cardPath);
                }

                g.SetClip(cardPath);

                // Radial glow 1 (top-right primary accent)
                using (GraphicsPath radialPath = new GraphicsPath())
                {
                    radialPath.AddEllipse(cardRect.Right - 195, cardRect.Top - 75, 270, 185);
                    using (PathGradientBrush pgb = new PathGradientBrush(radialPath))
                    {
                        ColorBlend cb = new ColorBlend(3);
                        cb.Positions = new float[] { 0.0f, 0.45f, 1.0f };
                        cb.Colors = new Color[]
                        {
                            Color.FromArgb(0, accent.R, accent.G, accent.B),
                            Color.FromArgb(16, accent.R, accent.G, accent.B),
                            Color.FromArgb(46, accent.R, accent.G, accent.B)
                        };
                        pgb.InterpolationColors = cb;
                        g.FillPath(pgb, radialPath);
                    }
                }

                // Radial glow 2 (bottom-left secondary warmAccent)
                using (GraphicsPath radialPath2 = new GraphicsPath())
                {
                    radialPath2.AddEllipse(cardRect.Left - 85, cardRect.Bottom - 95, 210, 155);
                    using (PathGradientBrush pgb2 = new PathGradientBrush(radialPath2))
                    {
                        ColorBlend cb2 = new ColorBlend(3);
                        cb2.Positions = new float[] { 0.0f, 0.5f, 1.0f };
                        cb2.Colors = new Color[]
                        {
                            Color.FromArgb(0, warmAccent.R, warmAccent.G, warmAccent.B),
                            Color.FromArgb(10, warmAccent.R, warmAccent.G, warmAccent.B),
                            Color.FromArgb(28, warmAccent.R, warmAccent.G, warmAccent.B)
                        };
                        pgb2.InterpolationColors = cb2;
                        g.FillPath(pgb2, radialPath2);
                    }
                }

                // Subtle Top Specular & Accent Strip
                Rectangle topBar = new Rectangle(cardRect.X + 20, cardRect.Y, cardRect.Width - 40, 2);
                using (LinearGradientBrush barBrush = new LinearGradientBrush(
                    topBar,
                    Color.FromArgb(10, accent.R, accent.G, accent.B),
                    Color.FromArgb(225, accent.R, accent.G, accent.B),
                    LinearGradientMode.Horizontal))
                {
                    ColorBlend topBlend = new ColorBlend(3);
                    topBlend.Positions = new float[] { 0.0f, 0.5f, 1.0f };
                    topBlend.Colors = new Color[]
                    {
                        Color.FromArgb(0, accent.R, accent.G, accent.B),
                        Color.FromArgb(225, accent.R, accent.G, accent.B),
                        Color.FromArgb(0, accent.R, accent.G, accent.B)
                    };
                    barBrush.InterpolationColors = topBlend;
                    g.FillRectangle(barBrush, topBar);
                }

                g.ResetClip();

                // 3. L3 Specular Inner 1px Top-to-Bottom Gradient Rim
                Rectangle innerSpecRect = new Rectangle(cardRect.X + 1, cardRect.Y + 1, cardRect.Width - 2, cardRect.Height - 2);
                using (GraphicsPath specPath = CreateRoundedRect(innerSpecRect, 15))
                using (LinearGradientBrush specBrush = new LinearGradientBrush(
                    innerSpecRect,
                    Color.FromArgb(44, 255, 255, 255),
                    Color.FromArgb(8, 255, 255, 255),
                    LinearGradientMode.Vertical))
                using (Pen specPen = new Pen(specBrush, 1.0f))
                {
                    g.DrawPath(specPen, specPath);
                }

                // 4. Outer Rim Border (Solid theme rim when locked; Dashed interactive edit border when unlocked!)
                if (isLocked)
                {
                    using (LinearGradientBrush rimGrad = new LinearGradientBrush(
                        cardRect,
                        Color.FromArgb(195, accent.R, accent.G, accent.B),
                        Color.FromArgb(88, accent.R, accent.G, accent.B),
                        LinearGradientMode.Vertical))
                    using (Pen rimPen = new Pen(rimGrad, 1.3f))
                    {
                        rimPen.LineJoin = LineJoin.Round;
                        g.DrawPath(rimPen, cardPath);
                    }
                }
                else
                {
                    using (Pen editPen = new Pen(Color.FromArgb(235, warmAccent.R, warmAccent.G, warmAccent.B), 1.8f))
                    {
                        editPen.DashStyle = DashStyle.Dash;
                        editPen.DashPattern = new float[] { 4.0f, 2.5f };
                        editPen.LineJoin = LineJoin.Round;
                        g.DrawPath(editPen, cardPath);
                    }
                }
            }

            // Unlocked drag indicator pill in top-right corner
            if (!isLocked)
            {
                Rectangle dragPill = new Rectangle(cardRect.Right - 82, cardRect.Y + 8, 72, 18);
                using (GraphicsPath dp = CreateRoundedRect(dragPill, 9))
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(68, warmAccent.R, warmAccent.G, warmAccent.B)))
                using (Pen border = new Pen(Color.FromArgb(195, warmAccent.R, warmAccent.G, warmAccent.B), 1.0f))
                using (Font f = new Font("Segoe UI Semibold", 6.8f, FontStyle.Bold, GraphicsUnit.Point))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(250, 255, 244, 214)))
                {
                    g.FillPath(fill, dp);
                    g.DrawPath(border, dp);
                    g.DrawString("✥ SÜRÜKLE", f, tb, dragPill.X + 7f, dragPill.Y + 2f);
                }
            }
        }

        private static void DrawWidgetHeader(
            Graphics g,
            Rectangle cardRect,
            string sectionTitle,
            string rightBadgeText,
            Color accent,
            Color brightText,
            bool isLocked)
        {
            using (Font hdrFont = new Font("Segoe UI Semibold", 7.4f, FontStyle.Bold, GraphicsUnit.Point))
            using (SolidBrush hdrBrush = new SolidBrush(Color.FromArgb(220, accent.R, accent.G, accent.B)))
            using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(65, accent.R, accent.G, accent.B)))
            using (SolidBrush dotBrush = new SolidBrush(accent))
            {
                g.FillEllipse(glowBrush, cardRect.X + 12, cardRect.Y + 11, 9, 9);
                g.FillEllipse(dotBrush, cardRect.X + 14, cardRect.Y + 13, 5, 5);
                g.DrawString(sectionTitle, hdrFont, hdrBrush, cardRect.X + 24f, cardRect.Y + 9f);
            }

            if (isLocked && !string.IsNullOrEmpty(rightBadgeText))
            {
                string pillLabel = TruncateLabel(rightBadgeText, 21);
                using (Font badgeFont = new Font("Segoe UI Semibold", 7.0f, FontStyle.Bold, GraphicsUnit.Point))
                {
                    SizeF sz = g.MeasureString(pillLabel, badgeFont);
                    int pw = Math.Min(158, Math.Max(48, (int)Math.Ceiling(sz.Width) + 14));
                    Rectangle pillRect = new Rectangle(cardRect.Right - pw - 12, cardRect.Y + 8, pw, 19);

                    using (GraphicsPath pp = CreateRoundedRect(pillRect, 9))
                    using (SolidBrush pf = new SolidBrush(Color.FromArgb(36, accent.R, accent.G, accent.B)))
                    using (Pen pb = new Pen(Color.FromArgb(130, accent.R, accent.G, accent.B), 1.0f))
                    using (SolidBrush ptb = new SolidBrush(MixColors(brightText, accent, 0.25f)))
                    {
                        g.FillPath(pf, pp);
                        g.DrawPath(pb, pp);
                        g.DrawString(pillLabel, badgeFont, ptb, pillRect.X + 7f, pillRect.Y + 2.5f);
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // Widget 1: ClockDate ("Saat, Tarih & Gün İlerlemesi")
        // --------------------------------------------------------------------

        private static void DrawClockDateContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            DateTime now = DateTime.Now;
            DrawWidgetHeader(g, cardRect, "SAAT & TAKVİM", _themeTitle, accent, brightText, isLocked);

            string hm = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            string ss = ":" + now.ToString("ss", CultureInfo.InvariantCulture);

            float hmRight = cardRect.X + 14f;
            using (Font clockFont = CreateMonoFont(25.5f, FontStyle.Bold))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(45, accent.R, accent.G, accent.B)))
            using (SolidBrush clockBrush = new SolidBrush(brightText))
            {
                g.DrawString(hm, clockFont, shadowBrush, cardRect.X + 12f, cardRect.Y + 28f);
                g.DrawString(hm, clockFont, clockBrush, cardRect.X + 11f, cardRect.Y + 27f);
                SizeF sz = g.MeasureString(hm, clockFont);
                hmRight = cardRect.X + 6f + sz.Width;
            }

            using (Font secFont = CreateMonoFont(10.5f, FontStyle.Bold))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(215, accent.R, accent.G, accent.B)))
            {
                g.DrawString(ss, secFont, secBrush, hmRight - 4f, cardRect.Y + 46f);
            }

            // Week / Day index mini chip on the right
            int dayOfYear = now.DayOfYear;
            int weekNum = ((dayOfYear - 1) / 7) + 1;
            string wkChip = string.Format(CultureInfo.InvariantCulture, "H{0:00} · GÜN {1}", weekNum, dayOfYear);
            using (Font wkFont = CreateMonoFont(7.2f, FontStyle.Bold))
            using (SolidBrush wkBrush = new SolidBrush(Color.FromArgb(190, warmAccent.R, warmAccent.G, warmAccent.B)))
            {
                SizeF wsz = g.MeasureString(wkChip, wkFont);
                g.DrawString(wkChip, wkFont, wkBrush, cardRect.Right - wsz.Width - 14f, cardRect.Y + 50f);
            }

            string dateLine = FormatTurkishDateLine(now);
            using (Font dateFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush dateBrush = new SolidBrush(mutedText))
            {
                g.DrawString(dateLine, dateFont, dateBrush, cardRect.X + 15f, cardRect.Y + 79f);
            }

            // Day Progress Bar (% of 24h elapsed)
            double dayPct = (now.TimeOfDay.TotalSeconds / 86400.0) * 100.0;
            int dayPctInt = ClampInt((int)Math.Round(dayPct), 0, 100);
            string dayLeft = "GÜN İLERLEMESİ";
            string dayRight = string.Format(CultureInfo.InvariantCulture, "%{0:0.0}", dayPct);

            using (Font subFont = new Font("Segoe UI Semibold", 7.3f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font pctFont = CreateMonoFont(7.6f, FontStyle.Bold))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(195, mutedText.R, mutedText.G, mutedText.B)))
            using (SolidBrush valBrush = new SolidBrush(accent))
            {
                g.DrawString(dayLeft, subFont, subBrush, cardRect.X + 15f, cardRect.Y + 106f);
                SizeF rsz = g.MeasureString(dayRight, pctFont);
                g.DrawString(dayRight, pctFont, valBrush, cardRect.Right - rsz.Width - 13f, cardRect.Y + 105f);
            }

            DrawProgressBar(g, cardRect.X + 15, cardRect.Y + 126, cardRect.Width - 30, 7, dayPctInt, accent);
        }

        // --------------------------------------------------------------------
        // Widget 2: SystemGpu ("CPU, RAM & NVIDIA GPU/VRAM")
        // --------------------------------------------------------------------

        private static void DrawSystemGpuContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            bool hasGpu;
            string gpuTitle;
            int gpuUtil;
            double vramUsed;
            double vramTotal;
            int gpuTemp;

            lock (_gpuSync)
            {
                hasGpu = _gpuAvailable;
                gpuTitle = _gpuName;
                gpuUtil = _gpuUtilPercent;
                vramUsed = _gpuVramUsedGb;
                vramTotal = _gpuVramTotalGb;
                gpuTemp = _gpuTempC;
            }

            string badge = hasGpu
                ? string.Format(CultureInfo.InvariantCulture, "{0}°C · GPU", gpuTemp)
                : "CANLI TELEMETRİ";

            DrawWidgetHeader(g, cardRect, "SİSTEM & DONANIM", badge, accent, brightText, isLocked);

            // Row 1: CPU % + 30-Point Live Sparkline Graph
            using (Font cpuLblFont = new Font("Segoe UI Semibold", 7.6f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font cpuValFont = CreateMonoFont(14.5f, FontStyle.Bold))
            using (SolidBrush mutedBrush = new SolidBrush(mutedText))
            using (SolidBrush brightBrush = new SolidBrush(brightText))
            {
                g.DrawString("CPU YÜKÜ", cpuLblFont, mutedBrush, cardRect.X + 14f, cardRect.Y + 32f);
                string cpuVal = string.Format(CultureInfo.InvariantCulture, "{0}%", _cachedCpuPercent);
                g.DrawString(cpuVal, cpuValFont, brightBrush, cardRect.X + 13f, cardRect.Y + 46f);
            }

            Rectangle sparkRect = new Rectangle(cardRect.X + 104, cardRect.Y + 33, cardRect.Width - 118, 38);
            DrawSingleSparklineBox(g, sparkRect, _cpuHistory, 100.0f, accent);

            // Row 2: RAM % & GB
            string ramLeft = string.Format(CultureInfo.InvariantCulture, "RAM  {0}%", _cachedRamPercent);
            string ramRight = _cachedRamTotalGb > 0.1
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.0} / {1:0.0} GB", _cachedRamUsedGb, _cachedRamTotalGb)
                : "";

            using (Font rowFont = new Font("Segoe UI Semibold", 7.6f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font monoFont = CreateMonoFont(7.5f, FontStyle.Bold))
            using (SolidBrush brightBrush = new SolidBrush(brightText))
            using (SolidBrush mutedBrush = new SolidBrush(mutedText))
            {
                g.DrawString(ramLeft, rowFont, brightBrush, cardRect.X + 14f, cardRect.Y + 78f);
                if (ramRight.Length > 0)
                {
                    SizeF sz = g.MeasureString(ramRight, monoFont);
                    g.DrawString(ramRight, monoFont, mutedBrush, cardRect.Right - sz.Width - 13f, cardRect.Y + 78f);
                }
            }
            DrawProgressBar(g, cardRect.X + 15, cardRect.Y + 95, cardRect.Width - 30, 6, _cachedRamPercent, accent);

            // Row 3: NVIDIA GPU % & VRAM Used/Total GB
            string gpuLeft;
            string gpuRight;
            int gpuBarPct;

            if (hasGpu)
            {
                gpuLeft = string.Format(CultureInfo.InvariantCulture, "{0}  {1}%", TruncateLabel(gpuTitle, 14), gpuUtil);
                gpuRight = vramTotal > 0.1
                    ? string.Format(CultureInfo.InvariantCulture, "VRAM {0:0.0}/{1:0.0} GB", vramUsed, vramTotal)
                    : string.Format(CultureInfo.InvariantCulture, "{0}°C", gpuTemp);
                gpuBarPct = gpuUtil;
            }
            else
            {
                gpuLeft = "DWM GRAFİK MOTORU";
                gpuRight = "10-bit Donanım Aktif";
                gpuBarPct = Math.Max(8, _cachedCpuPercent / 2);
            }

            Color gpuBarColor = (hasGpu && gpuTemp >= 78) ? Color.FromArgb(255, 181, 71) : warmAccent;
            using (Font rowFont = new Font("Segoe UI Semibold", 7.6f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font monoFont = CreateMonoFont(7.4f, FontStyle.Bold))
            using (SolidBrush brightBrush = new SolidBrush(brightText))
            using (SolidBrush vramBrush = new SolidBrush(MixColors(brightText, gpuBarColor, 0.35f)))
            {
                g.DrawString(gpuLeft, rowFont, brightBrush, cardRect.X + 14f, cardRect.Y + 108f);
                SizeF sz = g.MeasureString(gpuRight, monoFont);
                g.DrawString(gpuRight, monoFont, vramBrush, cardRect.Right - sz.Width - 13f, cardRect.Y + 108f);
            }
            DrawProgressBar(g, cardRect.X + 15, cardRect.Y + 125, cardRect.Width - 30, 6, gpuBarPct, gpuBarColor);
        }

        // --------------------------------------------------------------------
        // Widget 3: StorageDrives ("Disk Durumu C: & D:")
        // --------------------------------------------------------------------

        private static void DrawStorageDrivesContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            List<DriveSnapshot> drives = _cachedDrives;
            string badge = string.Format(CultureInfo.InvariantCulture, "{0} DİSK", drives.Count);
            DrawWidgetHeader(g, cardRect, "DEPOLAMA BİRİMLERİ", badge, accent, brightText, isLocked);

            if (drives.Count == 0)
            {
                using (Font f = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point))
                using (SolidBrush b = new SolidBrush(mutedText))
                {
                    g.DrawString("Sabit disk bilgisi okunuyor...", f, b, cardRect.X + 16f, cardRect.Y + 62f);
                }
                return;
            }

            int maxShown = Math.Min(3, drives.Count);
            int rowStep = (maxShown <= 2) ? 48 : 34;
            int startY = (maxShown <= 2) ? (cardRect.Y + 38) : (cardRect.Y + 34);

            Color warningAmber = Color.FromArgb(255, 181, 71);

            using (Font driveFont = CreateMonoFont(8.6f, FontStyle.Bold))
            using (Font infoFont = new Font("Segoe UI", 7.8f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush brightBrush = new SolidBrush(brightText))
            using (SolidBrush mutedBrush = new SolidBrush(mutedText))
            {
                for (int i = 0; i < maxShown; i++)
                {
                    DriveSnapshot d = drives[i];
                    int y = startY + (i * rowStep);

                    bool isHighUsage = d.UsedPercent > 85;
                    Color barColor = isHighUsage ? warningAmber : (i == 0 ? accent : MixColors(accent, warmAccent, 0.35f));

                    g.DrawString(d.Label, driveFont, brightBrush, cardRect.X + 15f, y);

                    string capInfo = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:0.0} GB boş / {1:0} GB",
                        d.FreeGb,
                        d.TotalGb);
                    g.DrawString(capInfo, infoFont, mutedBrush, cardRect.X + 46f, y + 1f);

                    string pctText = string.Format(CultureInfo.InvariantCulture, "{0}%", d.UsedPercent);
                    using (SolidBrush pctBrush = new SolidBrush(barColor))
                    {
                        SizeF psz = g.MeasureString(pctText, driveFont);
                        g.DrawString(pctText, driveFont, pctBrush, cardRect.Right - psz.Width - 13f, y);
                    }

                    int barH = (maxShown <= 2) ? 8 : 6;
                    int barOffsetY = (maxShown <= 2) ? 21 : 18;
                    DrawProgressBar(g, cardRect.X + 15, y + barOffsetY, cardRect.Width - 30, barH, d.UsedPercent, barColor);
                }
            }
        }

        // --------------------------------------------------------------------
        // Widget 4: NetworkTraffic ("Canlı Ağ Trafiği Down / Up")
        // --------------------------------------------------------------------

        private static void DrawNetworkTrafficContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            DrawWidgetHeader(g, cardRect, "CANLI AĞ TRAFİĞİ", _cachedLocalIpv4, accent, brightText, isLocked);

            string downText = "↓ " + FormatTransferRate(_cachedDownBytesSec);
            string upText = "↑ " + FormatTransferRate(_cachedUpBytesSec);

            using (Font rateFont = CreateMonoFont(9.8f, FontStyle.Bold))
            using (Font tagFont = new Font("Segoe UI Semibold", 7.1f, FontStyle.Bold, GraphicsUnit.Point))
            using (SolidBrush downBrush = new SolidBrush(accent))
            using (SolidBrush upBrush = new SolidBrush(warmAccent))
            using (SolidBrush mutedBrush = new SolidBrush(mutedText))
            {
                g.DrawString(downText, rateFont, downBrush, cardRect.X + 14f, cardRect.Y + 33f);
                g.DrawString("İNDİRME HIZI", tagFont, mutedBrush, cardRect.X + 16f, cardRect.Y + 53f);

                g.DrawString(upText, rateFont, upBrush, cardRect.X + 156f, cardRect.Y + 33f);
                g.DrawString("GÖNDERME HIZI", tagFont, mutedBrush, cardRect.X + 158f, cardRect.Y + 53f);
            }

            Rectangle graphRect = new Rectangle(cardRect.X + 15, cardRect.Y + 74, cardRect.Width - 30, 58);
            DrawDualNetworkSparklineBox(g, graphRect, _netDownHistory, _netUpHistory, accent, warmAccent);
        }

        // --------------------------------------------------------------------
        // Widget 5: MediaAudio ("Medya Çalar & Hızlı Kontrol")
        // --------------------------------------------------------------------

        private static void DrawMediaAudioContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            DrawWidgetHeader(g, cardRect, "MEDYA & SES", _cachedMediaSource, accent, brightText, isLocked);

            // Marquee-style track title + source status
            string displayTrack = GetMarqueeWindowText(_cachedMediaTitle, 25);
            string subLine = _hasActiveMediaTrack
                ? ("● " + _cachedMediaSource + " üzerinden oynatılıyor")
                : (isLocked ? "Kontrol için widget kilidini açın" : "Medya kontrol tuşları hazır");

            using (Font titleFont = new Font("Segoe UI Semibold", 9.8f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font subFont = new Font("Segoe UI", 7.7f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush brightBrush = new SolidBrush(brightText))
            using (SolidBrush mutedBrush = new SolidBrush(mutedText))
            {
                g.DrawString(displayTrack, titleFont, brightBrush, cardRect.X + 15f, cardRect.Y + 36f);
                g.DrawString(subLine, subFont, mutedBrush, cardRect.X + 15f, cardRect.Y + 58f);
            }

            // Animated-style Equalizer Bars (top-right of body, linked to live audio peak!)
            int eqBaseX = cardRect.Right - 80;
            int eqBotY = cardRect.Y + 76;
            for (int b = 0; b < 9; b++)
            {
                int bandIdx = (b * 3) % MaxSpectrumBands;
                double liveVal = _spectrumBands[bandIdx];
                double phase = (_tickCounter * 0.85) + (b * 0.72);
                double wave = (liveVal > 0.08)
                    ? Math.Min(1.0, 0.20 + liveVal * 0.85)
                    : (_hasActiveMediaTrack
                        ? (0.35 + 0.65 * Math.Abs(Math.Sin(phase)))
                        : (0.22 + 0.28 * Math.Abs(Math.Cos(b * 0.65))));

                int barH = Math.Max(5, (int)Math.Round(30.0 * wave));
                Rectangle eqBar = new Rectangle(eqBaseX + (b * 7), eqBotY - barH, 4, barH);
                Color barCol = (b % 3 == 0) ? warmAccent : accent;
                using (SolidBrush eqBrush = new SolidBrush(Color.FromArgb(210, barCol.R, barCol.G, barCol.B)))
                {
                    g.FillRectangle(eqBrush, eqBar);
                }
            }

            // 4 Media Transport Buttons along Bottom Row
            int nowTick = Environment.TickCount;
            bool isFlashValid = (_flashMediaBtn >= 0) && (unchecked((uint)(nowTick - _flashMediaBtnTick)) < 600u);

            for (int i = 0; i < 4; i++)
            {
                RectangleF rf = MediaButtonLogicalRects[i];
                Rectangle btnRect = new Rectangle(
                    (int)Math.Round(rf.X),
                    (int)Math.Round(rf.Y),
                    (int)Math.Round(rf.Width),
                    (int)Math.Round(rf.Height));

                bool isHovered = (!isLocked && _hoverMediaBtn == i);
                bool isFlashed = (isFlashValid && _flashMediaBtn == i);

                int fillAlpha = isFlashed ? 120 : (isHovered ? 75 : (isLocked ? 28 : 44));
                int borderAlpha = isFlashed ? 245 : (isHovered ? 220 : (isLocked ? 85 : 150));
                Color btnTone = (i == 1) ? accent : MixColors(accent, brightText, 0.25f);

                using (GraphicsPath bp = CreateRoundedRect(btnRect, 8))
                using (SolidBrush bf = new SolidBrush(Color.FromArgb(fillAlpha, accent.R, accent.G, accent.B)))
                using (Pen bpen = new Pen(Color.FromArgb(borderAlpha, btnTone.R, btnTone.G, btnTone.B), 1.1f))
                {
                    g.FillPath(bf, bp);
                    g.DrawPath(bpen, bp);
                }

                Color glyphCol = isLocked
                    ? Color.FromArgb(185, brightText.R, brightText.G, brightText.B)
                    : brightText;
                DrawMediaTransportVectorIcon(g, btnRect, i, glyphCol);
            }
        }

        private static void DrawMediaTransportVectorIcon(Graphics g, Rectangle r, int btnIndex, Color col)
        {
            float cx = r.X + r.Width * 0.5f;
            float cy = r.Y + r.Height * 0.5f;

            using (SolidBrush b = new SolidBrush(col))
            using (Pen p = new Pen(col, 1.8f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;

                if (btnIndex == 0)
                {
                    // Prev: |<
                    g.FillRectangle(b, cx - 7f, cy - 5.5f, 2.2f, 11f);
                    PointF[] tri = new PointF[]
                    {
                        new PointF(cx - 3.5f, cy),
                        new PointF(cx + 5.5f, cy - 5.5f),
                        new PointF(cx + 5.5f, cy + 5.5f)
                    };
                    g.FillPolygon(b, tri);
                }
                else if (btnIndex == 1)
                {
                    // Play / Pause: > ||
                    PointF[] tri = new PointF[]
                    {
                        new PointF(cx - 7.5f, cy - 6f),
                        new PointF(cx - 7.5f, cy + 6f),
                        new PointF(cx + 1.5f, cy)
                    };
                    g.FillPolygon(b, tri);
                    g.FillRectangle(b, cx + 3.5f, cy - 5.5f, 2.3f, 11f);
                    g.FillRectangle(b, cx + 7.5f, cy - 5.5f, 2.3f, 11f);
                }
                else if (btnIndex == 2)
                {
                    // Next: >|
                    PointF[] tri = new PointF[]
                    {
                        new PointF(cx - 5.5f, cy - 5.5f),
                        new PointF(cx - 5.5f, cy + 5.5f),
                        new PointF(cx + 3.5f, cy)
                    };
                    g.FillPolygon(b, tri);
                    g.FillRectangle(b, cx + 4.8f, cy - 5.5f, 2.2f, 11f);
                }
                else
                {
                    // Mute: Speaker + X
                    PointF[] spk = new PointF[]
                    {
                        new PointF(cx - 8f, cy - 3f),
                        new PointF(cx - 4.5f, cy - 3f),
                        new PointF(cx - 0.5f, cy - 6.5f),
                        new PointF(cx - 0.5f, cy + 6.5f),
                        new PointF(cx - 4.5f, cy + 3f),
                        new PointF(cx - 8f, cy + 3f)
                    };
                    g.FillPolygon(b, spk);
                    g.DrawLine(p, cx + 2.5f, cy - 3.5f, cx + 7.5f, cy + 3.5f);
                    g.DrawLine(p, cx + 7.5f, cy - 3.5f, cx + 2.5f, cy + 3.5f);
                }
            }
        }

        // --------------------------------------------------------------------
        // Widget 6: NotesUptime ("Sistem Uptime & Hızlı Not")
        // --------------------------------------------------------------------

        private static void DrawNotesUptimeContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            string uptimeBadge = FormatUptimeBadge();
            DrawWidgetHeader(g, cardRect, "UPTIME & HIZLI NOT", uptimeBadge, accent, brightText, isLocked);

            // Subtle left accent bar next to quick notes
            Rectangle quoteBar = new Rectangle(cardRect.X + 15, cardRect.Y + 36, 3, 96);
            using (LinearGradientBrush qb = new LinearGradientBrush(
                quoteBar,
                Color.FromArgb(225, accent.R, accent.G, accent.B),
                Color.FromArgb(75, warmAccent.R, warmAccent.G, warmAccent.B),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(qb, quoteBar);
            }

            string[] lines = _cachedNotesLines;
            using (Font noteFont = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(240, brightText.R, brightText.G, brightText.B)))
            using (SolidBrush secBrush = new SolidBrush(mutedText))
            using (StringFormat sf = new StringFormat())
            {
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;

                int maxLines = Math.Min(4, lines != null ? lines.Length : 0);
                for (int i = 0; i < maxLines; i++)
                {
                    SolidBrush lineBrush = (i == 0) ? textBrush : secBrush;
                    RectangleF lineRect = new RectangleF(
                        cardRect.X + 24f,
                        cardRect.Y + 35f + (i * 24f),
                        cardRect.Width - 34f,
                        21f);
                    g.DrawString(lines[i], noteFont, lineBrush, lineRect, sf);
                }
            }
        }

        // --------------------------------------------------------------------
        // Widget 7: Spectrum ("Canlı Ses Spektrumu & Now Playing Bar")
        // --------------------------------------------------------------------

        private static void DrawSpectrumContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            string[] styleNames = new string[] { "BARS", "MIRROR", "WAVE", "RING" };
            int st = ClampInt(_spectrumStyle, 0, 3);
            string badge = string.Format(CultureInfo.InvariantCulture, "{0} · {1}B", styleNames[st], _spectrumBandCount);

            DrawWidgetHeader(g, cardRect, "SES SPEKTRUMU", badge, accent, brightText, isLocked);

            // Main Spectrum Visualizer Stage Box
            Rectangle stageBox = new Rectangle(cardRect.X + 14, cardRect.Y + 31, cardRect.Width - 28, 76);
            using (GraphicsPath stagePath = CreateRoundedRect(stageBox, 9))
            using (SolidBrush stageBg = new SolidBrush(Color.FromArgb(44, 4, 7, 10)))
            using (Pen stageBorder = new Pen(Color.FromArgb(62, accent.R, accent.G, accent.B), 1.0f))
            {
                g.FillPath(stageBg, stagePath);

                // Subtle studio dB grid lines inside stage
                g.SetClip(stagePath);
                using (Pen gridPen = new Pen(Color.FromArgb(20, 255, 255, 255), 1.0f))
                {
                    gridPen.DashStyle = DashStyle.Dot;
                    g.DrawLine(gridPen, stageBox.X + 4, stageBox.Y + 25, stageBox.Right - 4, stageBox.Y + 25);
                    g.DrawLine(gridPen, stageBox.X + 4, stageBox.Y + 51, stageBox.Right - 4, stageBox.Y + 51);
                }

                int bandCount = NormalizeBandCount(_spectrumBandCount);
                if (st == 0)
                {
                    DrawSpectrumBarsMode(g, stageBox, bandCount, accent, warmAccent, brightText);
                }
                else if (st == 1)
                {
                    DrawSpectrumMirrorMode(g, stageBox, bandCount, accent, warmAccent, brightText);
                }
                else if (st == 2)
                {
                    DrawSpectrumWaveMode(g, stageBox, bandCount, accent, warmAccent, brightText);
                }
                else
                {
                    DrawSpectrumRadialRingMode(g, stageBox, bandCount, accent, warmAccent, brightText, mutedText);
                }

                g.ResetClip();
                g.DrawPath(stageBorder, stagePath);
            }

            // Bottom Now Playing & dB Telemetry Bar
            float dbVal = -42.0f + (_liveAudioMasterPeak * 42.0f);
            string dbText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} dB", dbVal);
            string nowPlaying = _hasActiveMediaTrack
                ? GetMarqueeWindowText(_cachedMediaTitle, 20)
                : "WASAPI Loopback · Canlı";

            using (Font npFont = new Font("Segoe UI Semibold", 7.8f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font dbFont = CreateMonoFont(7.4f, FontStyle.Bold))
            using (SolidBrush npBrush = new SolidBrush(brightText))
            using (SolidBrush dbBrush = new SolidBrush(accent))
            using (SolidBrush dotBrush = new SolidBrush(_hasActiveMediaTrack ? Color.FromArgb(46, 230, 166) : accent))
            {
                g.FillEllipse(dotBrush, cardRect.X + 16, cardRect.Y + 120, 6, 6);
                g.DrawString(nowPlaying, npFont, npBrush, cardRect.X + 25f, cardRect.Y + 115f);
                g.DrawString(dbText, dbFont, dbBrush, cardRect.X + 146f, cardRect.Y + 116f);
            }

            // Mini Transport Buttons at bottom-right (Prev / Play-Pause / Next)
            for (int b = 1; b <= 3; b++)
            {
                RectangleF rf = SpectrumButtonLogicalRects[b];
                Rectangle br = new Rectangle((int)rf.X, (int)rf.Y, (int)rf.Width, (int)rf.Height);
                bool hovered = (!isLocked && _hoverSpectrumBtn == b);
                using (GraphicsPath bp = CreateRoundedRect(br, 6))
                using (SolidBrush bf = new SolidBrush(Color.FromArgb(hovered ? 85 : 34, accent.R, accent.G, accent.B)))
                using (Pen bpen = new Pen(Color.FromArgb(hovered ? 215 : 95, accent.R, accent.G, accent.B), 1.0f))
                {
                    g.FillPath(bf, bp);
                    g.DrawPath(bpen, bp);
                }
                DrawMediaTransportVectorIcon(g, br, b - 1, brightText);
            }
        }

        private static void DrawSpectrumBarsMode(
            Graphics g,
            Rectangle box,
            int bandCount,
            Color accent,
            Color warmAccent,
            Color brightText)
        {
            float padX = 6f;
            float availW = box.Width - (padX * 2f);
            float stepW = availW / bandCount;
            float barW = Math.Max(1.6f, stepW - (bandCount >= 64 ? 1.0f : (bandCount >= 32 ? 1.8f : 3.0f)));
            float botY = box.Bottom - 5f;
            float maxH = box.Height - 12f;

            using (LinearGradientBrush barGrad = new LinearGradientBrush(
                box,
                warmAccent,
                accent,
                LinearGradientMode.Vertical))
            using (SolidBrush peakBrush = new SolidBrush(MixColors(brightText, warmAccent, 0.35f)))
            {
                for (int i = 0; i < bandCount; i++)
                {
                    int srcIdx = (i * MaxSpectrumBands) / bandCount;
                    float val = _spectrumBands[srcIdx];
                    float pk = _spectrumPeaks[srcIdx];

                    float h = Math.Max(3f, val * maxH);
                    float x = box.X + padX + (i * stepW);
                    float y = botY - h;

                    g.FillRectangle(barGrad, x, y, barW, h);

                    if (_spectrumPeakHold)
                    {
                        float py = botY - Math.Max(4f, pk * maxH) - 2.5f;
                        if (py < box.Y + 3f) py = box.Y + 3f;
                        g.FillRectangle(peakBrush, x, py, barW, 2.0f);
                    }
                }
            }
        }

        private static void DrawSpectrumMirrorMode(
            Graphics g,
            Rectangle box,
            int bandCount,
            Color accent,
            Color warmAccent,
            Color brightText)
        {
            float padX = 6f;
            float availW = box.Width - (padX * 2f);
            float stepW = availW / bandCount;
            float barW = Math.Max(1.6f, stepW - (bandCount >= 64 ? 1.0f : 1.8f));
            float midY = box.Y + (box.Height * 0.5f);
            float halfH = (box.Height * 0.5f) - 6f;

            using (Pen centerLine = new Pen(Color.FromArgb(70, accent.R, accent.G, accent.B), 1.0f))
            using (SolidBrush upperBrush = new SolidBrush(Color.FromArgb(225, accent.R, accent.G, accent.B)))
            using (SolidBrush lowerBrush = new SolidBrush(Color.FromArgb(145, warmAccent.R, warmAccent.G, warmAccent.B)))
            using (SolidBrush peakBrush = new SolidBrush(Color.FromArgb(220, brightText.R, brightText.G, brightText.B)))
            {
                g.DrawLine(centerLine, box.X + 4, midY, box.Right - 4, midY);

                for (int i = 0; i < bandCount; i++)
                {
                    int srcIdx = (i * MaxSpectrumBands) / bandCount;
                    float val = _spectrumBands[srcIdx];
                    float pk = _spectrumPeaks[srcIdx];

                    float h = Math.Max(2f, val * halfH);
                    float x = box.X + padX + (i * stepW);

                    g.FillRectangle(upperBrush, x, midY - h, barW, h);
                    g.FillRectangle(lowerBrush, x, midY + 1f, barW, h * 0.82f);

                    if (_spectrumPeakHold)
                    {
                        float pOff = Math.Max(3f, pk * halfH) + 2f;
                        g.FillRectangle(peakBrush, x, midY - pOff, barW, 1.8f);
                    }
                }
            }
        }

        private static void DrawSpectrumWaveMode(
            Graphics g,
            Rectangle box,
            int bandCount,
            Color accent,
            Color warmAccent,
            Color brightText)
        {
            int n = Math.Min(32, Math.Max(16, bandCount));
            PointF[] wavePts = new PointF[n];
            PointF[] waveArea = new PointF[n + 2];
            PointF[] harmPts = new PointF[n];

            float innerX = box.X + 6f;
            float innerW = box.Width - 12f;
            float botY = box.Bottom - 5f;
            float maxH = box.Height - 14f;

            for (int i = 0; i < n; i++)
            {
                int srcIdx = (i * MaxSpectrumBands) / n;
                float val = _spectrumBands[srcIdx];
                float pk = _spectrumPeaks[srcIdx];

                float px = innerX + (innerW * i) / (float)(n - 1);
                float py = botY - (val * maxH);
                float hy = botY - (pk * maxH * 0.85f);

                wavePts[i] = new PointF(px, py);
                waveArea[i + 1] = wavePts[i];
                harmPts[i] = new PointF(px, hy);
            }

            waveArea[0] = new PointF(innerX, botY);
            waveArea[n + 1] = new PointF(innerX + innerW, botY);

            using (LinearGradientBrush areaFill = new LinearGradientBrush(
                box,
                Color.FromArgb(115, accent.R, accent.G, accent.B),
                Color.FromArgb(8, warmAccent.R, warmAccent.G, warmAccent.B),
                LinearGradientMode.Vertical))
            using (Pen mainPen = new Pen(Color.FromArgb(245, accent.R, accent.G, accent.B), 2.0f))
            using (Pen harmPen = new Pen(Color.FromArgb(170, warmAccent.R, warmAccent.G, warmAccent.B), 1.3f))
            {
                mainPen.LineJoin = LineJoin.Round;
                harmPen.LineJoin = LineJoin.Round;
                g.FillPolygon(areaFill, waveArea);
                if (_spectrumPeakHold)
                {
                    g.DrawCurve(harmPen, harmPts, 0.35f);
                }
                g.DrawCurve(mainPen, wavePts, 0.35f);
            }
        }

        private static void DrawSpectrumRadialRingMode(
            Graphics g,
            Rectangle box,
            int bandCount,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText)
        {
            float cx = box.X + box.Width * 0.5f;
            float cy = box.Y + box.Height * 0.5f;
            float innerR = 15.0f + (_liveAudioMasterPeak * 3.0f);
            float maxSpoke = 18.5f;

            // Left & Right Stereo VU Meter Bars flanking the Radial Ring
            int vuH = box.Height - 16;
            int vuL = (int)Math.Round(_liveAudioLeftPeak * vuH);
            int vuR = (int)Math.Round(_liveAudioRightPeak * vuH);
            using (SolidBrush trackB = new SolidBrush(Color.FromArgb(35, 255, 255, 255)))
            using (SolidBrush fillL = new SolidBrush(accent))
            using (SolidBrush fillR = new SolidBrush(warmAccent))
            using (Font chFont = CreateMonoFont(6.8f, FontStyle.Bold))
            using (SolidBrush mutB = new SolidBrush(mutedText))
            {
                g.FillRectangle(trackB, box.X + 10, box.Y + 8, 6, vuH);
                g.FillRectangle(fillL, box.X + 10, box.Bottom - 8 - vuL, 6, vuL);
                g.DrawString("L", chFont, mutB, box.X + 20f, box.Y + 8f);

                g.FillRectangle(trackB, box.Right - 16, box.Y + 8, 6, vuH);
                g.FillRectangle(fillR, box.Right - 16, box.Bottom - 8 - vuR, 6, vuR);
                g.DrawString("R", chFont, mutB, box.Right - 30f, box.Y + 8f);
            }

            // Central Core Circle
            using (SolidBrush coreFill = new SolidBrush(Color.FromArgb(65, accent.R, accent.G, accent.B)))
            using (Pen coreRim = new Pen(Color.FromArgb(200, accent.R, accent.G, accent.B), 1.4f))
            {
                g.FillEllipse(coreFill, cx - innerR, cy - innerR, innerR * 2f, innerR * 2f);
                g.DrawEllipse(coreRim, cx - innerR, cy - innerR, innerR * 2f, innerR * 2f);
            }

            int spokes = Math.Min(36, Math.Max(20, bandCount));
            using (Pen spokePen = new Pen(accent, 2.0f))
            using (Pen warmPen = new Pen(warmAccent, 2.0f))
            {
                spokePen.StartCap = LineCap.Round;
                spokePen.EndCap = LineCap.Round;
                warmPen.StartCap = LineCap.Round;
                warmPen.EndCap = LineCap.Round;

                for (int i = 0; i < spokes; i++)
                {
                    int srcIdx = (i * MaxSpectrumBands) / spokes;
                    float val = _spectrumBands[srcIdx];
                    double angle = (2.0 * Math.PI * i) / spokes - (Math.PI * 0.5);
                    float cos = (float)Math.Cos(angle);
                    float sin = (float)Math.Sin(angle);

                    float r1 = innerR + 2.0f;
                    float r2 = r1 + Math.Max(2.5f, val * maxSpoke);

                    Pen p = (i % 3 == 0) ? warmPen : spokePen;
                    g.DrawLine(p, cx + cos * r1, cy + sin * r1, cx + cos * r2, cy + sin * r2);
                }
            }
        }

        // --------------------------------------------------------------------
        // Widget 8: Pomodoro ("Pomodoro & Derin Odak Sayacı HUD")
        // --------------------------------------------------------------------

        private static void DrawPomodoroContent(
            Graphics g,
            Rectangle cardRect,
            Color accent,
            Color warmAccent,
            Color brightText,
            Color mutedText,
            bool isLocked)
        {
            Color phaseColor = (_pomodoroPhaseIndex == 0)
                ? accent
                : (_pomodoroPhaseIndex == 1 ? Color.FromArgb(46, 230, 166) : warmAccent);

            string badgeText = string.Format(
                CultureInfo.InvariantCulture,
                "{0} · {1}/{2}",
                _pomodoroPhase,
                Math.Min(4, _pomodoroCompletedSessions + (_pomodoroPhaseIndex == 0 ? 1 : 0)),
                4);

            DrawWidgetHeader(g, cardRect, "POMODORO & ODAK", badgeText, phaseColor, brightText, isLocked);

            // Left: Glowing Circular Progress Ring Timer
            float ringX = cardRect.X + 18f;
            float ringY = cardRect.Y + 38f;
            float ringSize = 86f;
            RectangleF ringRect = new RectangleF(ringX, ringY, ringSize, ringSize);

            int totalSec = Math.Max(1, GetPhaseDurationSeconds(_pomodoroPhaseIndex));
            int remSec = ClampInt(_pomodoroRemainingSeconds, 0, totalSec);
            float progressRatio = 1.0f - ((float)remSec / (float)totalSec);
            if (_pomodoroRemainingSeconds == totalSec) progressRatio = 1.0f; // Full ring when fresh

            float sweepAngle = Math.Max(6.0f, progressRatio * 360.0f);

            using (Pen trackPen = new Pen(Color.FromArgb(42, 255, 255, 255), 6.5f))
            using (Pen glowPen = new Pen(Color.FromArgb(65, phaseColor.R, phaseColor.G, phaseColor.B), 10.5f))
            using (Pen arcPen = new Pen(Color.FromArgb(245, phaseColor.R, phaseColor.G, phaseColor.B), 6.5f))
            {
                glowPen.StartCap = LineCap.Round;
                glowPen.EndCap = LineCap.Round;
                arcPen.StartCap = LineCap.Round;
                arcPen.EndCap = LineCap.Round;

                g.DrawEllipse(trackPen, ringRect);
                g.DrawArc(glowPen, ringRect, -90f, sweepAngle);
                g.DrawArc(arcPen, ringRect, -90f, sweepAngle);
            }

            // Center label inside Ring (#1..4 Session Dots & Percentage)
            float rcx = ringX + ringSize * 0.5f;
            float rcy = ringY + ringSize * 0.5f;
            int pctDone = ClampInt((int)Math.Round(((totalSec - remSec) * 100.0) / totalSec), 0, 100);
            string ringCenterTxt = _pomodoroRunning ? (pctDone + "%") : _pomodoroPhase;

            using (Font rf = CreateMonoFont(8.5f, FontStyle.Bold))
            using (SolidBrush rb = new SolidBrush(brightText))
            {
                SizeF rsz = g.MeasureString(ringCenterTxt, rf);
                g.DrawString(ringCenterTxt, rf, rb, rcx - rsz.Width * 0.5f, rcy - 13f);
            }

            // 4 Session Dots inside bottom of ring
            float dotsStartX = rcx - 19f;
            for (int d = 0; d < 4; d++)
            {
                bool completed = d < _pomodoroCompletedSessions;
                bool current = (d == _pomodoroCompletedSessions && _pomodoroPhaseIndex == 0);
                Color dotCol = completed ? phaseColor : (current ? MixColors(phaseColor, Color.White, 0.35f) : Color.FromArgb(65, 255, 255, 255));
                using (SolidBrush db = new SolidBrush(dotCol))
                {
                    g.FillEllipse(db, dotsStartX + (d * 11f), rcy + 7f, 6.5f, 6.5f);
                }
            }

            // Right: Large Tabular Mono Countdown MM:SS + Phase Status + Interactive Buttons
            int mins = remSec / 60;
            int secs = remSec % 60;
            string mmss = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", mins, secs);

            using (Font timeFont = CreateMonoFont(23.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI Semibold", 7.8f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush timeBrush = new SolidBrush(brightText))
            using (SolidBrush subBrush = new SolidBrush(mutedText))
            {
                g.DrawString(mmss, timeFont, timeBrush, cardRect.X + 114f, cardRect.Y + 34f);

                string statusLine = _pomodoroRunning
                    ? (_pomodoroPhaseIndex == 0 ? "● Derin odak oturumu sürüyor" : "● Dinlenme molası sürüyor")
                    : "Duraklatıldı · Başlatmaya hazır";
                g.DrawString(statusLine, subFont, subBrush, cardRect.X + 118f, cardRect.Y + 76f);
            }

            // 3 Interactive Pomodoro Control Buttons (BAŞLAT/DURAKLAT, SIFIRLA, ATLA)
            string[] btnLabels = new string[]
            {
                _pomodoroRunning ? "DURAKLAT" : "BAŞLAT",
                "SIFIRLA",
                "ATLA"
            };

            int nowTick = Environment.TickCount;
            bool isFlashValid = (_flashPomodoroBtn >= 0) && (unchecked((uint)(nowTick - _flashPomodoroBtnTick)) < 500u);

            using (Font btnFont = new Font("Segoe UI Semibold", 7.1f, FontStyle.Bold, GraphicsUnit.Point))
            {
                for (int b = 0; b < 3; b++)
                {
                    RectangleF rf = PomodoroButtonLogicalRects[b];
                    Rectangle br = new Rectangle((int)rf.X, (int)rf.Y, (int)rf.Width, (int)rf.Height);

                    bool isHovered = (!isLocked && _hoverPomodoroBtn == b);
                    bool isFlashed = (isFlashValid && _flashPomodoroBtn == b);
                    int fillAlpha = isFlashed ? 130 : (isHovered ? 85 : (b == 0 ? 58 : 30));
                    int borderAlpha = isFlashed ? 245 : (isHovered ? 225 : (b == 0 ? 185 : 110));

                    using (GraphicsPath bp = CreateRoundedRect(br, 7))
                    using (SolidBrush bf = new SolidBrush(Color.FromArgb(fillAlpha, phaseColor.R, phaseColor.G, phaseColor.B)))
                    using (Pen bpen = new Pen(Color.FromArgb(borderAlpha, phaseColor.R, phaseColor.G, phaseColor.B), 1.1f))
                    using (SolidBrush tb = new SolidBrush(brightText))
                    {
                        g.FillPath(bf, bp);
                        g.DrawPath(bpen, bp);
                        SizeF lsz = g.MeasureString(btnLabels[b], btnFont);
                        g.DrawString(
                            btnLabels[b],
                            btnFont,
                            tb,
                            br.X + (br.Width - lsz.Width) * 0.5f,
                            br.Y + (br.Height - lsz.Height) * 0.5f);
                    }
                }
            }
        }

        // ====================================================================
        // Sparkline & Progress Bar Primitives
        // ====================================================================

        private static void DrawProgressBar(
            Graphics g,
            int x,
            int y,
            int width,
            int height,
            int percent,
            Color accent)
        {
            Rectangle trackRect = new Rectangle(x, y, width, height);
            using (GraphicsPath trackPath = CreateRoundedRect(trackRect, height / 2))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(46, 255, 255, 255)))
            {
                g.FillPath(trackBrush, trackPath);
            }

            if (percent <= 0)
            {
                return;
            }

            int clamped = ClampInt(percent, 0, 100);
            int fillW = (int)Math.Round((width * clamped) / 100.0);
            if (fillW < height) fillW = height;
            if (fillW > width) fillW = width;

            Rectangle fillRect = new Rectangle(x, y, fillW, height);
            Color endGlow = MixColors(accent, Color.White, 0.32f);

            using (GraphicsPath fillPath = CreateRoundedRect(fillRect, height / 2))
            using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                fillRect,
                Color.FromArgb(230, accent.R, accent.G, accent.B),
                Color.FromArgb(250, endGlow.R, endGlow.G, endGlow.B),
                LinearGradientMode.Horizontal))
            {
                g.FillPath(fillBrush, fillPath);
            }
        }

        private static void DrawSingleSparklineBox(
            Graphics g,
            Rectangle box,
            float[] values,
            float maxScale,
            Color accent)
        {
            using (GraphicsPath bgPath = CreateRoundedRect(box, 7))
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(36, 0, 0, 0)))
            using (Pen borderPen = new Pen(Color.FromArgb(58, accent.R, accent.G, accent.B), 1.0f))
            {
                g.FillPath(bgBrush, bgPath);
                g.DrawPath(borderPen, bgPath);
            }

            if (values == null || values.Length < 2 || maxScale <= 0.001f)
            {
                return;
            }

            int n = values.Length;
            PointF[] linePts = new PointF[n];
            PointF[] polyPts = new PointF[n + 2];

            float innerX = box.X + 3f;
            float innerW = box.Width - 6f;
            float innerTop = box.Y + 4f;
            float innerBot = box.Bottom - 3f;
            float innerH = Math.Max(4f, innerBot - innerTop);

            for (int i = 0; i < n; i++)
            {
                float ratio = values[i] / maxScale;
                if (ratio < 0f) ratio = 0f;
                if (ratio > 1f) ratio = 1f;

                float px = innerX + (innerW * i) / (float)(n - 1);
                float py = innerBot - (ratio * innerH);
                linePts[i] = new PointF(px, py);
                polyPts[i + 1] = linePts[i];
            }

            polyPts[0] = new PointF(innerX, innerBot);
            polyPts[n + 1] = new PointF(innerX + innerW, innerBot);

            using (LinearGradientBrush areaBrush = new LinearGradientBrush(
                box,
                Color.FromArgb(95, accent.R, accent.G, accent.B),
                Color.FromArgb(6, accent.R, accent.G, accent.B),
                LinearGradientMode.Vertical))
            using (Pen sparkPen = new Pen(Color.FromArgb(240, accent.R, accent.G, accent.B), 1.6f))
            using (SolidBrush headDot = new SolidBrush(MixColors(accent, Color.White, 0.45f)))
            {
                sparkPen.LineJoin = LineJoin.Round;
                g.FillPolygon(areaBrush, polyPts);
                g.DrawLines(sparkPen, linePts);

                PointF last = linePts[n - 1];
                g.FillEllipse(headDot, last.X - 2.5f, last.Y - 2.5f, 5f, 5f);
            }
        }

        private static void DrawDualNetworkSparklineBox(
            Graphics g,
            Rectangle box,
            double[] downHist,
            double[] upHist,
            Color downCol,
            Color upCol)
        {
            using (GraphicsPath bgPath = CreateRoundedRect(box, 8))
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(38, 0, 0, 0)))
            using (Pen borderPen = new Pen(Color.FromArgb(58, downCol.R, downCol.G, downCol.B), 1.0f))
            {
                g.FillPath(bgBrush, bgPath);
                g.DrawPath(borderPen, bgPath);
            }

            using (Pen gridPen = new Pen(Color.FromArgb(28, 255, 255, 255), 1.0f))
            {
                gridPen.DashStyle = DashStyle.Dot;
                int midY = box.Y + box.Height / 2;
                g.DrawLine(gridPen, box.X + 4, midY, box.Right - 4, midY);
            }

            int n = SparklineHistoryLen;
            double peak = 64.0 * 1024.0;
            for (int i = 0; i < n; i++)
            {
                if (downHist[i] > peak) peak = downHist[i];
                if (upHist[i] > peak) peak = upHist[i];
            }

            PointF[] downPts = new PointF[n];
            PointF[] downArea = new PointF[n + 2];
            PointF[] upPts = new PointF[n];

            float innerX = box.X + 4f;
            float innerW = box.Width - 8f;
            float innerTop = box.Y + 5f;
            float innerBot = box.Bottom - 4f;
            float innerH = Math.Max(6f, innerBot - innerTop);

            for (int i = 0; i < n; i++)
            {
                float rx = innerX + (innerW * i) / (float)(n - 1);
                float rDown = (float)Math.Min(1.0, Math.Max(0.0, downHist[i] / peak));
                float rUp = (float)Math.Min(1.0, Math.Max(0.0, upHist[i] / peak));

                downPts[i] = new PointF(rx, innerBot - rDown * innerH);
                downArea[i + 1] = downPts[i];
                upPts[i] = new PointF(rx, innerBot - rUp * innerH);
            }

            downArea[0] = new PointF(innerX, innerBot);
            downArea[n + 1] = new PointF(innerX + innerW, innerBot);

            using (LinearGradientBrush downFill = new LinearGradientBrush(
                box,
                Color.FromArgb(78, downCol.R, downCol.G, downCol.B),
                Color.FromArgb(5, downCol.R, downCol.G, downCol.B),
                LinearGradientMode.Vertical))
            using (Pen downPen = new Pen(Color.FromArgb(240, downCol.R, downCol.G, downCol.B), 1.7f))
            using (Pen upPen = new Pen(Color.FromArgb(215, upCol.R, upCol.G, upCol.B), 1.4f))
            {
                downPen.LineJoin = LineJoin.Round;
                upPen.LineJoin = LineJoin.Round;
                g.FillPolygon(downFill, downArea);
                g.DrawLines(upPen, upPts);
                g.DrawLines(downPen, downPts);
            }
        }

        // ====================================================================
        // Per-Widget Native Layered Window Instance (Drag + Interactive Click + HWND_BOTTOM)
        // ====================================================================

        private sealed class WidgetInstance : NativeWindow
        {
            public readonly string WidgetId;
            public int CurrentX;
            public int CurrentY;
            public bool IsDragging;

            private bool _isShown;
            private bool _appliedLockedStyle;
            private POINT _dragStartCursor;
            private Point _dragStartOrigin;

            public WidgetInstance(string widgetId)
            {
                WidgetId = widgetId;
                _isShown = false;
                _appliedLockedStyle = true;
            }

            public void CreateNativeHudWindow(int width, int height, bool locked)
            {
                CreateParams cp = new CreateParams();
                cp.Caption = "SplendHudWidget_" + WidgetId;
                cp.X = 0;
                cp.Y = 0;
                cp.Width = width;
                cp.Height = height;
                cp.Style = WS_POPUP;
                cp.ExStyle = WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | (locked ? WS_EX_TRANSPARENT : 0);

                _appliedLockedStyle = locked;
                CreateHandle(cp);
            }

            public void SyncLockStyle(bool locked)
            {
                if (Handle == IntPtr.Zero || _appliedLockedStyle == locked)
                {
                    return;
                }

                int ex = GetWindowLong(Handle, GWL_EXSTYLE);
                if (locked)
                {
                    ex |= WS_EX_TRANSPARENT;
                    if (IsDragging)
                    {
                        IsDragging = false;
                        ReleaseCapture();
                    }
                }
                else
                {
                    ex &= ~WS_EX_TRANSPARENT;
                }

                SetWindowLong(Handle, GWL_EXSTYLE, ex);
                _appliedLockedStyle = locked;
            }

            public void PresentBitmap(Bitmap bmp, int screenX, int screenY, byte masterOpacity)
            {
                if (Handle == IntPtr.Zero || bmp == null)
                {
                    return;
                }

                IntPtr screenDc = GetDC(IntPtr.Zero);
                if (screenDc == IntPtr.Zero)
                {
                    return;
                }

                try
                {
                    IntPtr memDc = CreateCompatibleDC(screenDc);
                    if (memDc == IntPtr.Zero)
                    {
                        return;
                    }

                    IntPtr hBitmap = IntPtr.Zero;
                    IntPtr oldBitmap = IntPtr.Zero;
                    try
                    {
                        hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                        oldBitmap = SelectObject(memDc, hBitmap);

                        POINT dstPt = new POINT(screenX, screenY);
                        SIZE dstSz = new SIZE(bmp.Width, bmp.Height);
                        POINT srcPt = new POINT(0, 0);
                        BLENDFUNCTION blend = new BLENDFUNCTION();
                        blend.BlendOp = AC_SRC_OVER;
                        blend.BlendFlags = 0;
                        blend.SourceConstantAlpha = masterOpacity;
                        blend.AlphaFormat = AC_SRC_ALPHA;

                        UpdateLayeredWindow(
                            Handle,
                            screenDc,
                            ref dstPt,
                            ref dstSz,
                            memDc,
                            ref srcPt,
                            0,
                            ref blend,
                            ULW_ALPHA);

                        if (!_isShown)
                        {
                            ShowWindow(Handle, SW_SHOWNA);
                            _isShown = true;
                        }

                        SetWindowPos(
                            Handle,
                            HWND_BOTTOM,
                            0,
                            0,
                            0,
                            0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
                    }
                    finally
                    {
                        if (oldBitmap != IntPtr.Zero)
                        {
                            SelectObject(memDc, oldBitmap);
                        }
                        if (hBitmap != IntPtr.Zero)
                        {
                            DeleteObject(hBitmap);
                        }
                        DeleteDC(memDc);
                    }
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, screenDc);
                }
            }

            public void DisposeWindow()
            {
                try
                {
                    if (IsDragging)
                    {
                        IsDragging = false;
                        ReleaseCapture();
                    }
                    if (Handle != IntPtr.Zero)
                    {
                        ShowWindow(Handle, SW_HIDE);
                        DestroyHandle();
                    }
                }
                catch
                {
                }
                _isShown = false;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND)
                {
                    m.Result = new IntPtr(1);
                    return;
                }

                if (m.Msg == WM_MOUSEACTIVATE)
                {
                    m.Result = new IntPtr(MA_NOACTIVATE);
                    return;
                }

                if (m.Msg == WM_NCHITTEST)
                {
                    m.Result = new IntPtr(_widgetsLocked ? HTTRANSPARENT : HTCLIENT);
                    return;
                }

                if (!_widgetsLocked)
                {
                    if (m.Msg == WM_SETCURSOR)
                    {
                        bool overBtn =
                            (string.Equals(WidgetId, IdMediaAudio, StringComparison.OrdinalIgnoreCase) && _hoverMediaBtn >= 0) ||
                            (string.Equals(WidgetId, IdSpectrum, StringComparison.OrdinalIgnoreCase) && _hoverSpectrumBtn >= 0) ||
                            (string.Equals(WidgetId, IdPomodoro, StringComparison.OrdinalIgnoreCase) && _hoverPomodoroBtn >= 0);

                        Cursor desired = overBtn ? Cursors.Hand : Cursors.SizeAll;
                        SetCursor(desired.Handle);
                        m.Result = new IntPtr(1);
                        return;
                    }

                    if (m.Msg == WM_LBUTTONDOWN)
                    {
                        int clientX;
                        int clientY;
                        UnpackMouseCoords(m.LParam, out clientX, out clientY);

                        if (string.Equals(WidgetId, IdMediaAudio, StringComparison.OrdinalIgnoreCase))
                        {
                            int hitBtn = HitTestLogicalRects(MediaButtonLogicalRects, clientX, clientY);
                            if (hitBtn >= 0)
                            {
                                TriggerMediaTransportKey(hitBtn);
                                _flashMediaBtn = hitBtn;
                                _flashMediaBtnTick = Environment.TickCount;
                                RenderSpecificWidgetIfActive(IdMediaAudio);
                                m.Result = IntPtr.Zero;
                                return;
                            }
                        }
                        else if (string.Equals(WidgetId, IdSpectrum, StringComparison.OrdinalIgnoreCase))
                        {
                            int hitSpec = HitTestLogicalRects(SpectrumButtonLogicalRects, clientX, clientY);
                            if (hitSpec >= 0)
                            {
                                if (hitSpec == 0)
                                {
                                    SpectrumStyle = (_spectrumStyle + 1) % 4;
                                }
                                else
                                {
                                    TriggerMediaTransportKey(hitSpec - 1);
                                }
                                _flashSpectrumBtn = hitSpec;
                                _flashSpectrumBtnTick = Environment.TickCount;
                                RenderSpecificWidgetIfActive(IdSpectrum);
                                m.Result = IntPtr.Zero;
                                return;
                            }
                        }
                        else if (string.Equals(WidgetId, IdPomodoro, StringComparison.OrdinalIgnoreCase))
                        {
                            int hitPomo = HitTestLogicalRects(PomodoroButtonLogicalRects, clientX, clientY);
                            if (hitPomo >= 0)
                            {
                                _flashPomodoroBtn = hitPomo;
                                _flashPomodoroBtnTick = Environment.TickCount;
                                if (hitPomo == 0) StartPausePomodoro();
                                else if (hitPomo == 1) ResetPomodoro();
                                else if (hitPomo == 2) SkipPomodoroPhase();
                                m.Result = IntPtr.Zero;
                                return;
                            }
                        }

                        POINT cur;
                        if (GetCursorPos(out cur))
                        {
                            IsDragging = true;
                            _dragStartCursor = cur;
                            _dragStartOrigin = new Point(CurrentX, CurrentY);
                            SetCapture(Handle);
                        }
                        m.Result = IntPtr.Zero;
                        return;
                    }

                    if (m.Msg == WM_MOUSEMOVE)
                    {
                        if (IsDragging)
                        {
                            POINT cur;
                            if (GetCursorPos(out cur))
                            {
                                Rectangle vs = SystemInformation.VirtualScreen;
                                int nx = ClampInt(_dragStartOrigin.X + (cur.X - _dragStartCursor.X), vs.Left - 200, vs.Right - 80);
                                int ny = ClampInt(_dragStartOrigin.Y + (cur.Y - _dragStartCursor.Y), vs.Top, vs.Bottom - 60);
                                CurrentX = nx;
                                CurrentY = ny;

                                SetWindowPos(
                                    Handle,
                                    HWND_BOTTOM,
                                    CurrentX,
                                    CurrentY,
                                    0,
                                    0,
                                    SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
                            }
                        }
                        else
                        {
                            int clientX;
                            int clientY;
                            UnpackMouseCoords(m.LParam, out clientX, out clientY);

                            if (string.Equals(WidgetId, IdMediaAudio, StringComparison.OrdinalIgnoreCase))
                            {
                                int hitBtn = HitTestLogicalRects(MediaButtonLogicalRects, clientX, clientY);
                                if (hitBtn != _hoverMediaBtn)
                                {
                                    _hoverMediaBtn = hitBtn;
                                    RenderSpecificWidgetIfActive(IdMediaAudio);
                                }
                            }
                            else if (string.Equals(WidgetId, IdSpectrum, StringComparison.OrdinalIgnoreCase))
                            {
                                int hitSpec = HitTestLogicalRects(SpectrumButtonLogicalRects, clientX, clientY);
                                if (hitSpec != _hoverSpectrumBtn)
                                {
                                    _hoverSpectrumBtn = hitSpec;
                                    RenderSpecificWidgetIfActive(IdSpectrum);
                                }
                            }
                            else if (string.Equals(WidgetId, IdPomodoro, StringComparison.OrdinalIgnoreCase))
                            {
                                int hitPomo = HitTestLogicalRects(PomodoroButtonLogicalRects, clientX, clientY);
                                if (hitPomo != _hoverPomodoroBtn)
                                {
                                    _hoverPomodoroBtn = hitPomo;
                                    RenderSpecificWidgetIfActive(IdPomodoro);
                                }
                            }
                        }
                        m.Result = IntPtr.Zero;
                        return;
                    }

                    if (m.Msg == WM_LBUTTONUP || m.Msg == WM_CAPTURECHANGED)
                    {
                        if (IsDragging)
                        {
                            IsDragging = false;
                            if (m.Msg == WM_LBUTTONUP)
                            {
                                ReleaseCapture();
                            }

                            _customPositions[WidgetId] = new Point(CurrentX, CurrentY);

                            Action<string, int, int> movedHandler = OnWidgetMoved;
                            if (movedHandler != null)
                            {
                                try
                                {
                                    movedHandler(WidgetId, CurrentX, CurrentY);
                                }
                                catch
                                {
                                }
                            }
                        }
                        m.Result = IntPtr.Zero;
                        return;
                    }

                    if (m.Msg == WM_MOUSELEAVE)
                    {
                        if (_hoverMediaBtn != -1)
                        {
                            _hoverMediaBtn = -1;
                            RenderSpecificWidgetIfActive(IdMediaAudio);
                        }
                        if (_hoverSpectrumBtn != -1)
                        {
                            _hoverSpectrumBtn = -1;
                            RenderSpecificWidgetIfActive(IdSpectrum);
                        }
                        if (_hoverPomodoroBtn != -1)
                        {
                            _hoverPomodoroBtn = -1;
                            RenderSpecificWidgetIfActive(IdPomodoro);
                        }
                    }
                }

                base.WndProc(ref m);
            }
        }

        // ====================================================================
        // Media Key Dispatch & Hit-Test Helpers
        // ====================================================================

        private static int HitTestLogicalRects(RectangleF[] rects, int clientX, int clientY)
        {
            float scale = _widgetScalePercent / 100.0f;
            if (scale <= 0.1f) scale = 1.0f;

            float lx = clientX / scale;
            float ly = clientY / scale;

            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].Contains(lx, ly))
                {
                    return i;
                }
            }
            return -1;
        }

        private static void TriggerMediaTransportKey(int btnIndex)
        {
            byte vk;
            if (btnIndex == 0) vk = VK_MEDIA_PREV_TRACK;
            else if (btnIndex == 1) vk = VK_MEDIA_PLAY_PAUSE;
            else if (btnIndex == 2) vk = VK_MEDIA_NEXT_TRACK;
            else vk = VK_VOLUME_MUTE;

            try
            {
                keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch
            {
            }
        }

        private static void UnpackMouseCoords(IntPtr lParam, out int x, out int y)
        {
            long val = lParam.ToInt64();
            x = (short)(val & 0xFFFFL);
            y = (short)((val >> 16) & 0xFFFFL);
        }

        // ====================================================================
        // Typography, Formatting, Self-Test Verification & Color Utilities
        // ====================================================================

        private static Font CreateMonoFont(float sizePt, FontStyle style)
        {
            if (_resolvedMonoFontFamily == null)
            {
                try
                {
                    using (Font test = new Font("Cascadia Mono", 10f, FontStyle.Regular))
                    {
                        if (string.Equals(test.Name, "Cascadia Mono", StringComparison.OrdinalIgnoreCase))
                        {
                            _resolvedMonoFontFamily = "Cascadia Mono";
                        }
                    }
                }
                catch
                {
                }

                if (_resolvedMonoFontFamily == null)
                {
                    _resolvedMonoFontFamily = "Consolas";
                }
            }

            try
            {
                return new Font(_resolvedMonoFontFamily, sizePt, style, GraphicsUnit.Point);
            }
            catch
            {
                return new Font("Segoe UI Semibold", sizePt, style, GraphicsUnit.Point);
            }
        }

        private static string GetMarqueeWindowText(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            if (text.Length <= maxChars)
            {
                return text;
            }
            string padded = text + "   •   " + text;
            int offset = _marqueeCharOffset % (text.Length + 7);
            if (offset + maxChars <= padded.Length)
            {
                return padded.Substring(offset, maxChars);
            }
            return TruncateLabel(text, maxChars);
        }

        private static bool HasNonEmptyPixels(Bitmap bmp, int minNonZeroPixels)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                int byteCount = Math.Abs(data.Stride) * bmp.Height;
                byte[] buffer = new byte[byteCount];
                Marshal.Copy(data.Scan0, buffer, 0, byteCount);

                int nonZero = 0;
                for (int i = 3; i < byteCount; i += 4)
                {
                    if (buffer[i] > 16)
                    {
                        nonZero++;
                        if (nonZero >= minNonZeroPixels)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private static string FormatTurkishDateLine(DateTime dt)
        {
            int m = dt.Month;
            int dow = (int)dt.DayOfWeek;
            string monthName = (m >= 1 && m <= 12) ? TurkishMonths[m] : dt.ToString("MMMM");
            string dayName = (dow >= 0 && dow <= 6) ? TurkishDays[dow] : dt.ToString("dddd");
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} · {3}", dt.Day, monthName, dt.Year, dayName);
        }

        private static string FormatTransferRate(double bytesPerSec)
        {
            if (bytesPerSec < 0.0) bytesPerSec = 0.0;
            double kb = bytesPerSec / 1024.0;
            if (kb < 1000.0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB/s", kb);
            }
            double mb = kb / 1024.0;
            return string.Format(CultureInfo.InvariantCulture, "{0:0.00} MB/s", mb);
        }

        private static string FormatUptimeBadge()
        {
            try
            {
                ulong ms = GetTickCount64();
                TimeSpan ts = TimeSpan.FromMilliseconds(ms);
                if (ts.TotalDays >= 1.0)
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}g {1}sa {2}dk", (int)ts.TotalDays, ts.Hours, ts.Minutes);
                }
                return string.Format(CultureInfo.InvariantCulture, "Açık: {0} sa {1} dk", (int)ts.TotalHours, ts.Minutes);
            }
            catch
            {
                return "Sistem Aktif";
            }
        }

        private static string TruncateLabel(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            if (text.Length <= maxChars)
            {
                return text;
            }
            return text.Substring(0, maxChars - 1).TrimEnd() + "…";
        }

        private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return path;
            }

            int maxR = Math.Min(rect.Width, rect.Height) / 2;
            if (radius > maxR) radius = maxR;
            if (radius <= 1)
            {
                path.AddRectangle(rect);
                return path;
            }

            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180f, 90f);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270f, 90f);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        private static ulong FileTimeToUInt64(FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | (ulong)ft.dwLowDateTime;
        }

        private static int ClampLayout(int preset)
        {
            if (preset < 0 || preset > 2)
            {
                return 0;
            }
            return preset;
        }

        private static int NormalizeScalePercent(int scale)
        {
            if (scale <= 92) return 85;
            if (scale >= 108) return 115;
            return 100;
        }

        private static int NormalizeBandCount(int bands)
        {
            if (bands <= 20) return 16;
            if (bands >= 48) return 64;
            return 32;
        }

        private static byte ClampOpacity(byte opacity)
        {
            if (opacity < 80)
            {
                return 80;
            }
            return opacity;
        }

        private static Color ClampDarkCard(Color c, float scale)
        {
            int r = ClampInt((int)Math.Round(c.R * scale), 0, 72);
            int g = ClampInt((int)Math.Round(c.G * scale), 0, 72);
            int b = ClampInt((int)Math.Round(c.B * scale), 0, 78);
            return Color.FromArgb(255, r, g, b);
        }

        private static Color EnsureReadableAccent(Color c)
        {
            int peak = Math.Max(c.R, Math.Max(c.G, c.B));
            if (peak >= 125)
            {
                return Color.FromArgb(255, c.R, c.G, c.B);
            }
            if (peak == 0)
            {
                return Color.FromArgb(255, 130, 170, 145);
            }
            float lift = (132f - peak) / 255f;
            return MixColors(c, Color.White, lift);
        }

        private static Color ShiftHueAccent2(Color accent)
        {
            // Complementary / split-harmonic secondary accent for v3.0 cyber-studio dual glow & gradients
            Color cyanGold = Color.FromArgb(255,
                ClampInt(accent.B + 70, 90, 245),
                ClampInt((accent.R + accent.G) / 2 + 45, 110, 235),
                ClampInt(accent.G + 55, 100, 245));
            return MixColors(accent, cyanGold, 0.52f);
        }

        private static Color MixColors(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            int r = ClampInt((int)Math.Round(a.R + (b.R - a.R) * t), 0, 255);
            int g = ClampInt((int)Math.Round(a.G + (b.G - a.G) * t), 0, 255);
            int bl = ClampInt((int)Math.Round(a.B + (b.B - a.B) * t), 0, 255);
            return Color.FromArgb(255, r, g, bl);
        }

        private static int ClampInt(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private static float ClampFloat(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
