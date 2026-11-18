using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program {
    delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    struct ACCENT_POLICY {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WINCOMPATTRDATA {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINCOMPATTRDATA data);

    const int GWL_EXSTYLE = -20;
    const int WS_EX_LAYERED = 0x00080000;
    const uint LWA_ALPHA = 0x02;
    const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    const uint EVENT_OBJECT_SHOW = 0x8002;
    const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    static byte TargetAlpha = 192; // Clear see-through (~75% opacity)
    static WinEventDelegate procDelegate;

    static void EnableBlur(IntPtr hWnd) {
        ACCENT_POLICY accent = new ACCENT_POLICY();
        accent.AccentState = 3; // ACCENT_ENABLE_BLURBEHIND
        accent.AccentFlags = 2;
        accent.GradientColor = 0x00000000;

        int size = Marshal.SizeOf(accent);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try {
            Marshal.StructureToPtr(accent, ptr, false);
            WINCOMPATTRDATA data = new WINCOMPATTRDATA();
            data.Attribute = 19; // WCA_ACCENT_POLICY
            data.SizeOfData = size;
            data.Data = ptr;
            SetWindowCompositionAttribute(hWnd, ref data);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
    }

    static void StyleIfExplorer(IntPtr hWnd) {
        if (hWnd == IntPtr.Zero) return;
        StringBuilder sb = new StringBuilder(64);
        if (GetClassName(hWnd, sb, sb.Capacity) > 0) {
            string cls = sb.ToString();
            if (cls == "CabinetWClass" || cls == "ExploreWClass") {
                int ex = GetWindowLong(hWnd, GWL_EXSTYLE);
                if ((ex & WS_EX_LAYERED) == 0) {
                    SetWindowLong(hWnd, GWL_EXSTYLE, ex | WS_EX_LAYERED);
                }
                SetLayeredWindowAttributes(hWnd, 0, TargetAlpha, LWA_ALPHA);
                EnableBlur(hWnd);
            }
        }
    }

    static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
        if (idObject == 0 && hwnd != IntPtr.Zero) {
            StyleIfExplorer(hwnd);
        }
    }

    [STAThread]
    static void Main(string[] args) {
        int myId = Process.GetCurrentProcess().Id;
        foreach (Process p in Process.GetProcessesByName("ExplorerGlass")) {
            if (p.Id != myId) {
                try { p.Kill(); } catch { }
            }
        }

        if (args.Length > 0) {
            byte parsed;
            if (byte.TryParse(args[0], out parsed) && parsed >= 100 && parsed <= 255) {
                TargetAlpha = parsed;
            }
        }

        EnumWindows((hWnd, lParam) => {
            StyleIfExplorer(hWnd);
            return true;
        }, IntPtr.Zero);

        procDelegate = new WinEventDelegate(WinEventProc);
        IntPtr hook1 = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        IntPtr hook2 = SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW, IntPtr.Zero, procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

        Application.Run();

        UnhookWinEvent(hook1);
        UnhookWinEvent(hook2);
    }
}
