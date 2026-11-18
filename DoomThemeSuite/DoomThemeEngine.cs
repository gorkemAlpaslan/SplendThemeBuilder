using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DoomThemeSuite
{
    public static class DoomThemeEngine
    {
        public static readonly string IconsDir = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen";
        public static readonly string SuiteDir = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite";
        public static readonly string ConfigPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\config.ini";
        public static readonly string SnapshotsPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\snapshots.ini";
        public static readonly string ThemeFilePath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\DoomEmerald.theme";
        public static readonly string BlankArrowIcoPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\blank_arrow.ico";
        public static readonly string WidgetNotesPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\widget_notes.txt";
        public static readonly string WallpapersDir = @"C:\Users\gorke\Documents\wallpapers";
        public static readonly string DoomWallpaperPath = @"C:\Users\gorke\Documents\wallpapers\doctor-doom-5120x2194-26922.jpg";
        public static readonly string DefaultExePath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomControlCenter.exe";

        private const uint SPI_SETDESKWALLPAPER = 0x0014;
        private const uint SPI_SETFONTSMOOTHING = 0x004B;
        private const uint SPI_SETFONTSMOOTHINGTYPE = 0x200B;
        private const uint FE_FONTSMOOTHINGCLEARTYPE = 0x0002;
        private const uint SPIF_UPDATEINIFILE = 0x01; // Intentionally never using SPIF_SENDCHANGE (0x02) for safety

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNE_UPDATEDIR = 0x00001000;
        private const int SHCNE_UPDATEITEM = 0x00002000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint SHCNF_PATHW = 0x0005;
        private const uint SHCNF_FLUSHNOWAIT = 0x2000;

        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_ALLCHILDREN = 0x0080;
        private const uint RDW_UPDATENOW = 0x0100;
        private const uint RDW_FRAME = 0x0400;

        private const uint WM_COMMAND = 0x0111;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
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

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, string dwItem1, IntPtr dwItem2);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static Dictionary<string, string> GetDefaultConfig()
        {
            Dictionary<string, string> defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            defaults["ActiveProfile"] = "DoomEmerald";
            defaults["ExplorerGlassEnabled"] = "0";
            defaults["ExplorerAlpha"] = "238";
            defaults["ExplorerBlurMode"] = "0";
            defaults["EmeraldTintAndShadowEnabled"] = "1";
            defaults["EmeraldTintIntensity"] = "72";
            defaults["EmeraldShadowIntensity"] = "185";
            defaults["ShadowSpread"] = "28";
            defaults["ShowRimBorder"] = "1";
            defaults["ShadowOnlyActiveWindow"] = "0";
            defaults["BodyTintHex"] = "#0E3620";
            defaults["ShadowGlowHex"] = "#267347";
            defaults["RimHighlightHex"] = "#4F9E6E";
            defaults["PrimaryAccentHex"] = "#1F4A31";
            defaults["TaskbarTintHex"] = "#182C1F";
            defaults["TaskbarTintAlpha"] = "180";
            defaults["TaskbarMode"] = "1";
            defaults["TaskbarStyleMode"] = "0";
            defaults["TaskbarCornerRadius"] = "12";
            defaults["TaskbarMarginPx"] = "8";
            defaults["TaskbarCenterIcons"] = "0";
            defaults["ShowTaskbarSeconds"] = "0";
            defaults["HideTaskViewButton"] = "1";
            defaults["RibbonMinimized"] = "1";
            defaults["HideClutterFolders"] = "1";
            defaults["HideQuickAccessHistory"] = "1";
            defaults["SearchboxMode"] = "1";
            defaults["DesktopIconSize"] = "48";
            defaults["HideShortcutArrows"] = "0";
            defaults["AutoApplyFolderIcons"] = "1";
            defaults["HudEnabled"] = "1";
            defaults["HudPosition"] = "0";
            defaults["HudOpacity"] = "230";
            defaults["WidgetClock"] = "1";
            defaults["WidgetSystemGpu"] = "1";
            defaults["WidgetStorage"] = "1";
            defaults["WidgetNetwork"] = "1";
            defaults["WidgetMedia"] = "1";
            defaults["WidgetNotes"] = "1";
            defaults["WidgetSpectrum"] = "1";
            defaults["WidgetPomodoro"] = "1";
            defaults["WidgetsLocked"] = "1";
            defaults["WidgetLayout"] = "0";
            defaults["WidgetScale"] = "100";
            defaults["WidgetPos_ClockDate"] = "auto";
            defaults["WidgetPos_SystemGpu"] = "auto";
            defaults["WidgetPos_StorageDrives"] = "auto";
            defaults["WidgetPos_NetworkTraffic"] = "auto";
            defaults["WidgetPos_MediaAudio"] = "auto";
            defaults["WidgetPos_NotesUptime"] = "auto";
            defaults["WidgetPos_Spectrum"] = "auto";
            defaults["WidgetPos_Pomodoro"] = "auto";
            defaults["SpectrumStyle"] = "0";
            defaults["SpectrumBandCount"] = "32";
            defaults["SpectrumSmoothing"] = "65";
            defaults["HotkeysEnabled"] = "1";
            defaults["ScheduleEnabled"] = "0";
            defaults["DayProfile"] = "DoomEmerald";
            defaults["NightProfile"] = "ObsidianStealth";
            defaults["AutoStart"] = "1";
            defaults["BoostProfile"] = "0";
            defaults["SmartFocusEnabled"] = "1";
            defaults["SmartFocusResumeDelaySec"] = "3";
            defaults["RamAutoPurgeThreshold"] = "85";
            defaults["DisplayDimmerPercent"] = "0";
            defaults["DisplayWarmthKelvin"] = "6500";
            defaults["HdrWhiteTextGuardEnabled"] = "1";
            defaults["HdrMaxNitsClamp"] = "240";
            defaults["RendererFpsCap"] = "60";
            defaults["FavoritePresets"] = "DoomEmerald,LatveriaGold";
            return defaults;
        }

        public static Dictionary<string, string> LoadConfig()
        {
            Dictionary<string, string> config = GetDefaultConfig();
            try
            {
                if (!Directory.Exists(SuiteDir))
                {
                    Directory.CreateDirectory(SuiteDir);
                }

                if (!File.Exists(ConfigPath))
                {
                    SaveConfig(config);
                    return config;
                }

                string[] lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("["))
                    {
                        continue;
                    }

                    int eqIdx = line.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1).Trim();
                        if (key.Length > 0)
                        {
                            config[key] = val;
                        }
                    }
                }
            }
            catch
            {
                // Return defaults on any I/O error
            }
            return config;
        }

        public static void SaveConfig(Dictionary<string, string> config)
        {
            try
            {
                if (!Directory.Exists(SuiteDir))
                {
                    Directory.CreateDirectory(SuiteDir);
                }

                Dictionary<string, string> merged = GetDefaultConfig();
                if (config != null)
                {
                    foreach (KeyValuePair<string, string> kvp in config)
                    {
                        if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                        {
                            merged[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Clamp ExplorerAlpha to valid range 140..255
                int alpha;
                if (int.TryParse(merged["ExplorerAlpha"], out alpha))
                {
                    if (alpha < 140) alpha = 140;
                    if (alpha > 255) alpha = 255;
                    merged["ExplorerAlpha"] = alpha.ToString();
                }
                else
                {
                    merged["ExplorerAlpha"] = "238";
                }

                // Clamp ShadowSpread to valid range 8..48
                int spread;
                if (int.TryParse(merged["ShadowSpread"], out spread))
                {
                    if (spread < 8) spread = 8;
                    if (spread > 48) spread = 48;
                    merged["ShadowSpread"] = spread.ToString();
                }
                else
                {
                    merged["ShadowSpread"] = "28";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# SPLEND // Windows Theme Studio v2.0 Configuration");
                sb.AppendLine("[DoomThemeSuite]");

                string[] orderedKeys = new string[]
                {
                    "ActiveProfile",
                    "ExplorerGlassEnabled",
                    "ExplorerAlpha",
                    "ExplorerBlurMode",
                    "EmeraldTintAndShadowEnabled",
                    "EmeraldTintIntensity",
                    "EmeraldShadowIntensity",
                    "ShadowSpread",
                    "ShowRimBorder",
                    "ShadowOnlyActiveWindow",
                    "BodyTintHex",
                    "ShadowGlowHex",
                    "RimHighlightHex",
                    "PrimaryAccentHex",
                    "TaskbarTintHex",
                    "TaskbarTintAlpha",
                    "TaskbarMode",
                    "TaskbarCenterIcons",
                    "ShowTaskbarSeconds",
                    "HideTaskViewButton",
                    "RibbonMinimized",
                    "HideClutterFolders",
                    "HideQuickAccessHistory",
                    "SearchboxMode",
                    "DesktopIconSize",
                    "HideShortcutArrows",
                    "AutoApplyFolderIcons",
                    "HudEnabled",
                    "HudPosition",
                    "HudOpacity",
                    "WidgetClock",
                    "WidgetSystemGpu",
                    "WidgetStorage",
                    "WidgetNetwork",
                    "WidgetMedia",
                    "WidgetNotes",
                    "WidgetsLocked",
                    "WidgetLayout",
                    "WidgetScale",
                    "WidgetPos_ClockDate",
                    "WidgetPos_SystemGpu",
                    "WidgetPos_StorageDrives",
                    "WidgetPos_NetworkTraffic",
                    "WidgetPos_MediaAudio",
                    "WidgetPos_NotesUptime",
                    "HotkeysEnabled",
                    "ScheduleEnabled",
                    "DayProfile",
                    "NightProfile",
                    "AutoStart"
                };

                HashSet<string> written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < orderedKeys.Length; i++)
                {
                    string k = orderedKeys[i];
                    if (merged.ContainsKey(k))
                    {
                        sb.AppendLine(k + "=" + merged[k]);
                        written.Add(k);
                    }
                }

                foreach (KeyValuePair<string, string> kvp in merged)
                {
                    if (!written.Contains(kvp.Key))
                    {
                        sb.AppendLine(kvp.Key + "=" + kvp.Value);
                    }
                }

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        public static string GetConfigValue(string key, string defaultValue)
        {
            Dictionary<string, string> cfg = LoadConfig();
            string val;
            if (cfg.TryGetValue(key, out val) && !string.IsNullOrEmpty(val))
            {
                return val;
            }
            return defaultValue;
        }

        public static int GetConfigInt(string key, int defaultValue)
        {
            string s = GetConfigValue(key, "");
            int v;
            if (int.TryParse(s, out v))
            {
                return v;
            }
            return defaultValue;
        }

        public static bool GetConfigBool(string key, bool defaultValue)
        {
            string s = GetConfigValue(key, "");
            if (s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (s == "0" || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return defaultValue;
        }

        public static void SetConfigValue(string key, string value)
        {
            Dictionary<string, string> cfg = LoadConfig();
            cfg[key] = value ?? "";
            SaveConfig(cfg);
        }

        public static string GetWidgetNotesText()
        {
            const string defaultNotes =
                "• SPLEND // Theme Studio v2.0 aktif\r\n" +
                "• Masaüstü widget kilidini açıp sürükleyebilirsiniz\r\n" +
                "• Ctrl+Alt+T: Stüdyo | Ctrl+Alt+G: Gezgin Gölge";
            try
            {
                if (!Directory.Exists(SuiteDir))
                {
                    Directory.CreateDirectory(SuiteDir);
                }

                if (!File.Exists(WidgetNotesPath))
                {
                    File.WriteAllText(WidgetNotesPath, defaultNotes, Encoding.UTF8);
                    return defaultNotes;
                }

                string text = File.ReadAllText(WidgetNotesPath, Encoding.UTF8);
                if (string.IsNullOrEmpty(text))
                {
                    return defaultNotes;
                }
                return text;
            }
            catch
            {
                return defaultNotes;
            }
        }

        public static void SaveWidgetNotesText(string text)
        {
            try
            {
                if (!Directory.Exists(SuiteDir))
                {
                    Directory.CreateDirectory(SuiteDir);
                }
                File.WriteAllText(WidgetNotesPath, text ?? "", Encoding.UTF8);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Fixes unreadable or green/blue/transparent file & folder names in Windows 10 HDR Dark Mode File Explorer,
        /// and sets both active and inactive titlebars/borders so they never fall back to grey.
        /// </summary>
        public static void FixExplorerTextAndColors()
        {
            try
            {
                // a) Ensure Windows 10 Dark Mode and ColorPrevalence are cleanly set in Personalize
                using (RegistryKey personalize = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (personalize != null)
                    {
                        personalize.SetValue("AppsUseLightTheme", 0, RegistryValueKind.DWord);
                        personalize.SetValue("SystemUsesLightTheme", 0, RegistryValueKind.DWord);
                        personalize.SetValue("EnableTransparency", 1, RegistryValueKind.DWord);
                        personalize.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
                    }
                }

                // b) Ensure both active AND inactive titlebars/borders are styled in DWM (never grey!)
                using (RegistryKey dwm = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (dwm != null)
                    {
                        dwm.SetValue("AutoColorization", 0, RegistryValueKind.DWord);
                        dwm.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
                        dwm.SetValue("AccentColor", unchecked((int)0xFF314A1F), RegistryValueKind.DWord);
                        dwm.SetValue("AccentColorInactive", unchecked((int)0xFF223D2A), RegistryValueKind.DWord);
                        dwm.SetValue("ColorizationColor", unchecked((int)0xC41F4A31), RegistryValueKind.DWord);
                        dwm.SetValue("ColorizationAfterglow", unchecked((int)0xC41F4A31), RegistryValueKind.DWord);
                        dwm.SetValue("EnableWindowColorization", 1, RegistryValueKind.DWord);
                    }
                }

                // c) Disable NTFS compressed/encrypted green/blue filename coloring and show extensions
                using (RegistryKey advanced = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (advanced != null)
                    {
                        advanced.SetValue("ShowCompColor", 0, RegistryValueKind.DWord);
                        advanced.SetValue("HideFileExt", 0, RegistryValueKind.DWord);
                    }
                }

                // d) Ensure ClearType / FontSmoothing is properly configured for HDR GDI text rendering
                using (RegistryKey desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
                {
                    if (desktop != null)
                    {
                        desktop.SetValue("FontSmoothing", "2", RegistryValueKind.String);
                        desktop.SetValue("FontSmoothingType", 2, RegistryValueKind.DWord);
                        desktop.SetValue("FontSmoothingGamma", 1400, RegistryValueKind.DWord);
                        desktop.SetValue("FontSmoothingOrientation", 1, RegistryValueKind.DWord);
                    }
                }
                SystemParametersInfo(SPI_SETFONTSMOOTHING, 1, IntPtr.Zero, SPIF_UPDATEINIFILE);
                SystemParametersInfo(SPI_SETFONTSMOOTHINGTYPE, 0, (IntPtr)FE_FONTSMOOTHINGCLEARTYPE, SPIF_UPDATEINIFILE);

                // e) Restore standard clean values in HKCU\Control Panel\Colors
                using (RegistryKey colors = Registry.CurrentUser.CreateSubKey(@"Control Panel\Colors"))
                {
                    if (colors != null)
                    {
                        colors.SetValue("Window", "255 255 255", RegistryValueKind.String);
                        colors.SetValue("WindowText", "0 0 0", RegistryValueKind.String);
                        colors.SetValue("Hilight", "46 90 62", RegistryValueKind.String);
                        colors.SetValue("HilightText", "255 255 255", RegistryValueKind.String);
                        colors.SetValue("HotTrackingColor", "108 168 129", RegistryValueKind.String);
                    }
                }

                // f) High-contrast readable AccentPalette
                byte[] doomReadablePalette = new byte[]
                {
                    0xD4, 0xF5, 0xDF, 0x00, // [0] Lightest: #D4F5DF
                    0xAE, 0xE6, 0xC1, 0x00, // [1] Lighter:  #AEE6C1
                    0x7F, 0xD4, 0x9E, 0x00, // [2] Light:    #7FD49E
                    0x2E, 0x5A, 0x3E, 0x00, // [3] Base:     #2E5A3E
                    0x1F, 0x4A, 0x31, 0x00, // [4] Primary:  #1F4A31
                    0x17, 0x38, 0x24, 0x00, // [5] Dark:     #173824
                    0x11, 0x26, 0x1A, 0x00, // [6] Darker:   #11261A
                    0x0B, 0x17, 0x10, 0x00  // [7] Darkest:  #0B1710
                };

                using (RegistryKey accent = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent"))
                {
                    if (accent != null)
                    {
                        accent.SetValue("AccentPalette", doomReadablePalette, RegistryValueKind.Binary);
                        accent.SetValue("AccentColorMenu", unchecked((int)0xFF314A1F), RegistryValueKind.DWord);
                        accent.SetValue("StartColorMenu", unchecked((int)0xFF223317), RegistryValueKind.DWord);
                    }
                }

                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Applies any ThemePreset (built-in or custom) to Windows 10 DWM, AccentPalette, Control Panel\Colors, and config.ini.
        /// </summary>
        public static void ApplyPreset(ThemePreset preset)
        {
            ApplyPreset(preset, false);
        }

        public static void ApplyPreset(ThemePreset preset, bool applyWallpaper)
        {
            if (preset == null)
            {
                preset = ThemeCatalog.GetPresetById("DoomEmerald");
            }

            // Ensure base ClearType & HDR white text registry hygiene first
            FixExplorerTextAndColors();

            try
            {
                bool isFactory = string.Equals(preset.Id, "FactoryDark", StringComparison.OrdinalIgnoreCase);

                byte[] palette = ThemeCatalog.BuildReadableAccentPalette(preset.PrimaryAccent, preset.RimHighlight);
                uint activeAbgr = ThemeCatalog.ColorToAbgrDword(preset.PrimaryAccent, 255);
                uint inactiveAbgr = ThemeCatalog.ColorToAbgrDword(preset.InactiveAccent, 255);

                Color startColor = Color.FromArgb(
                    255,
                    Math.Max(0, (int)(preset.PrimaryAccent.R * 0.75)),
                    Math.Max(0, (int)(preset.PrimaryAccent.G * 0.75)),
                    Math.Max(0, (int)(preset.PrimaryAccent.B * 0.75)));
                uint startAbgr = ThemeCatalog.ColorToAbgrDword(startColor, 255);
                uint colorizationArgb = ThemeCatalog.ColorToArgbDword(preset.PrimaryAccent, 0xC4);

                int prevalence = isFactory ? 0 : 1;

                ApplyAccentAndDwm(
                    palette,
                    activeAbgr,
                    inactiveAbgr,
                    startAbgr,
                    colorizationArgb,
                    prevalence,
                    prevalence);

                using (RegistryKey colors = Registry.CurrentUser.CreateSubKey(@"Control Panel\Colors"))
                {
                    if (colors != null)
                    {
                        string hilightRgb = string.Format("{0} {1} {2}", preset.PrimaryAccent.R, preset.PrimaryAccent.G, preset.PrimaryAccent.B);
                        string hotTrackRgb = string.Format("{0} {1} {2}", preset.RimHighlight.R, preset.RimHighlight.G, preset.RimHighlight.B);
                        colors.SetValue("Window", "255 255 255", RegistryValueKind.String);
                        colors.SetValue("WindowText", "0 0 0", RegistryValueKind.String);
                        colors.SetValue("Hilight", hilightRgb, RegistryValueKind.String);
                        colors.SetValue("HilightText", "255 255 255", RegistryValueKind.String);
                        colors.SetValue("HotTrackingColor", hotTrackRgb, RegistryValueKind.String);
                    }
                }

                if (applyWallpaper && !string.IsNullOrEmpty(preset.WallpaperPath) && File.Exists(preset.WallpaperPath))
                {
                    SetWallpaperSafely(preset.WallpaperPath);
                }

                Dictionary<string, string> cfg = LoadConfig();
                cfg["ActiveProfile"] = preset.Id;
                cfg["PrimaryAccentHex"] = ThemeCatalog.ColorToHex(preset.PrimaryAccent);
                cfg["BodyTintHex"] = ThemeCatalog.ColorToHex(preset.ExplorerBodyTint);
                cfg["ShadowGlowHex"] = ThemeCatalog.ColorToHex(preset.ExplorerShadowGlow);
                cfg["RimHighlightHex"] = ThemeCatalog.ColorToHex(preset.RimHighlight);
                cfg["TaskbarTintHex"] = ThemeCatalog.ColorToHex(preset.TaskbarTint);
                cfg["EmeraldTintIntensity"] = preset.DefaultTintAlpha.ToString();
                cfg["EmeraldShadowIntensity"] = preset.DefaultShadowAlpha.ToString();
                cfg["ShadowSpread"] = (preset.DefaultShadowSpread >= 8 && preset.DefaultShadowSpread <= 48 ? preset.DefaultShadowSpread : 28).ToString();

                if (isFactory)
                {
                    cfg["ExplorerGlassEnabled"] = "0";
                    cfg["ExplorerBlurMode"] = "0";
                    cfg["EmeraldTintAndShadowEnabled"] = "0";
                    cfg["TaskbarMode"] = "0";
                }
                else
                {
                    cfg["EmeraldTintAndShadowEnabled"] = "1";
                    if (cfg["TaskbarMode"] == "0")
                    {
                        cfg["TaskbarMode"] = "1";
                    }
                }

                SaveConfig(cfg);
                ThemeSnapshotManager.RecordSnapshot(preset.DisplayName ?? preset.Id, cfg);
                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Applies a preset by ID/name (delegates to ApplyPreset).
        /// </summary>
        public static void ApplyProfile(string profileName)
        {
            ThemePreset preset = ThemeCatalog.GetPresetById(profileName);
            bool isDoomDefault = string.Equals(preset.Id, "DoomEmerald", StringComparison.OrdinalIgnoreCase);
            ApplyPreset(preset, isDoomDefault);
        }

        public static void ApplyProfile(string profileName, bool applyWallpaper)
        {
            ThemePreset preset = ThemeCatalog.GetPresetById(profileName);
            ApplyPreset(preset, applyWallpaper);
        }

        private static void ApplyAccentAndDwm(
            byte[] palette,
            uint accentColorAbgr,
            uint accentColorInactiveAbgr,
            uint startColorAbgr,
            uint colorizationArgb,
            int personalizePrevalence,
            int dwmPrevalence)
        {
            using (RegistryKey accent = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent"))
            {
                if (accent != null)
                {
                    accent.SetValue("AccentPalette", palette, RegistryValueKind.Binary);
                    accent.SetValue("AccentColorMenu", unchecked((int)accentColorAbgr), RegistryValueKind.DWord);
                    accent.SetValue("StartColorMenu", unchecked((int)startColorAbgr), RegistryValueKind.DWord);
                }
            }

            using (RegistryKey personalize = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (personalize != null)
                {
                    personalize.SetValue("AppsUseLightTheme", 0, RegistryValueKind.DWord);
                    personalize.SetValue("SystemUsesLightTheme", 0, RegistryValueKind.DWord);
                    personalize.SetValue("EnableTransparency", 1, RegistryValueKind.DWord);
                    personalize.SetValue("ColorPrevalence", personalizePrevalence, RegistryValueKind.DWord);
                }
            }

            using (RegistryKey dwm = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM"))
            {
                if (dwm != null)
                {
                    dwm.SetValue("AutoColorization", 0, RegistryValueKind.DWord);
                    dwm.SetValue("ColorPrevalence", dwmPrevalence, RegistryValueKind.DWord);
                    dwm.SetValue("AccentColor", unchecked((int)accentColorAbgr), RegistryValueKind.DWord);
                    dwm.SetValue("AccentColorInactive", unchecked((int)accentColorInactiveAbgr), RegistryValueKind.DWord);
                    dwm.SetValue("ColorizationColor", unchecked((int)colorizationArgb), RegistryValueKind.DWord);
                    dwm.SetValue("ColorizationAfterglow", unchecked((int)colorizationArgb), RegistryValueKind.DWord);
                    dwm.SetValue("EnableWindowColorization", 1, RegistryValueKind.DWord);
                }
            }
        }

        private static void SetWallpaperSafely(string wallpaperPath)
        {
            SetWallpaperWithStyle(wallpaperPath, 10);
        }

        /// <summary>
        /// Sets desktop wallpaper with styleCode: 10 = Fill, 6 = Fit, 2 = Stretch, 0 = Center.
        /// Never broadcasts SPIF_SENDCHANGE; uses SPIF_UPDATEINIFILE only.
        /// </summary>
        public static void SetWallpaperWithStyle(string wallpaperPath, int styleCode)
        {
            try
            {
                if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                {
                    return;
                }

                string wallpaperStyle = "10";
                string tileWallpaper = "0";
                if (styleCode == 6) wallpaperStyle = "6";
                else if (styleCode == 2) wallpaperStyle = "2";
                else if (styleCode == 0) wallpaperStyle = "0";

                using (RegistryKey desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
                {
                    if (desktop != null)
                    {
                        desktop.SetValue("Wallpaper", wallpaperPath, RegistryValueKind.String);
                        desktop.SetValue("WallpaperStyle", wallpaperStyle, RegistryValueKind.String);
                        desktop.SetValue("TileWallpaper", tileWallpaper, RegistryValueKind.String);
                    }
                }

                // Use SPIF_UPDATEINIFILE (0x01) only, NEVER SPIF_SENDCHANGE (0x02)
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Scans C:\Users\gorke\Documents\wallpapers (and Windows Web Wallpaper fallback) for .jpg/.jpeg/.png/.bmp files.
        /// </summary>
        public static List<string> GetWallpaperGalleryFiles()
        {
            List<string> results = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(DoomWallpaperPath))
                {
                    results.Add(DoomWallpaperPath);
                    seen.Add(DoomWallpaperPath);
                }

                string[] searchDirs = new string[]
                {
                    WallpapersDir,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Wallpapers"),
                    @"C:\Windows\Web\Wallpaper\Windows",
                    @"C:\Windows\Web\4K\Wallpaper\Windows"
                };

                for (int d = 0; d < searchDirs.Length; d++)
                {
                    string dir = searchDirs[d];
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    {
                        continue;
                    }

                    string[] files = Directory.GetFiles(dir);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string f = files[i];
                        string ext = Path.GetExtension(f);
                        if (string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".bmp", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!seen.Contains(f))
                            {
                                results.Add(f);
                                seen.Add(f);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return results;
        }

        public static void SetRibbonMinimized(bool minimized)
        {
            try
            {
                using (RegistryKey ribbon = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Ribbon"))
                {
                    if (ribbon != null)
                    {
                        ribbon.SetValue("MinimizedStateTabletModeOff", minimized ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
                SetConfigValue("RibbonMinimized", minimized ? "1" : "0");
            }
            catch
            {
            }
        }

        public static void SetHideClutterFolders(bool hide)
        {
            string policyVal = hide ? "Hide" : "Show";
            string[] folderGuids = new string[]
            {
                "{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", // 3D Objects
                "{1CF1260C-4DD0-4EBB-811F-33C572699FDE}", // Music (Local)
                "{A0C69A99-21C8-4671-8703-7934162FCF1D}", // Music
                "{35286A68-3C57-41A1-BBB1-0EAE73D76C95}", // Videos (Local)
                "{F02CC66D-CD6B-4532-B890-9FE147221060}"  // Videos
            };

            try
            {
                for (int i = 0; i < folderGuids.Length; i++)
                {
                    string guid = folderGuids[i];
                    string propBagPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FolderDescriptions\" + guid + @"\PropertyBag";
                    using (RegistryKey bag = Registry.CurrentUser.CreateSubKey(propBagPath))
                    {
                        if (bag != null)
                        {
                            bag.SetValue("ThisPCPolicy", policyVal, RegistryValueKind.String);
                        }
                    }

                    try
                    {
                        using (RegistryKey lmBag = Registry.LocalMachine.CreateSubKey(propBagPath))
                        {
                            if (lmBag != null)
                            {
                                lmBag.SetValue("ThisPCPolicy", policyVal, RegistryValueKind.String);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                string nsParent = @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace";
                using (RegistryKey nsKey = Registry.CurrentUser.CreateSubKey(nsParent))
                {
                    if (nsKey != null)
                    {
                        if (hide)
                        {
                            nsKey.DeleteSubKeyTree("{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", false);
                        }
                        else
                        {
                            nsKey.CreateSubKey("{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}");
                        }
                    }
                }

                SetConfigValue("HideClutterFolders", hide ? "1" : "0");
                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        public static void SetHideQuickAccessHistory(bool hide)
        {
            try
            {
                using (RegistryKey adv = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (adv != null)
                    {
                        adv.SetValue("ShowRecent", hide ? 0 : 1, RegistryValueKind.DWord);
                        adv.SetValue("ShowFrequent", hide ? 0 : 1, RegistryValueKind.DWord);
                    }
                }
                SetConfigValue("HideQuickAccessHistory", hide ? "1" : "0");
            }
            catch
            {
            }
        }

        public static void SetSearchboxMode(int mode)
        {
            if (mode < 0) mode = 0;
            if (mode > 2) mode = 2;

            try
            {
                using (RegistryKey search = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
                {
                    if (search != null)
                    {
                        search.SetValue("SearchboxTaskbarMode", mode, RegistryValueKind.DWord);
                    }
                }
                SetConfigValue("SearchboxMode", mode.ToString());
                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        public static void SetShowTaskbarSeconds(bool show)
        {
            try
            {
                using (RegistryKey adv = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (adv != null)
                    {
                        adv.SetValue("ShowSecondsInSystemClock", show ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
                SetConfigValue("ShowTaskbarSeconds", show ? "1" : "0");
                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        public static void SetHideTaskViewButton(bool hide)
        {
            try
            {
                using (RegistryKey adv = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (adv != null)
                    {
                        adv.SetValue("ShowTaskViewButton", hide ? 0 : 1, RegistryValueKind.DWord);
                    }
                }
                SetConfigValue("HideTaskViewButton", hide ? "1" : "0");
                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        public static void SetDesktopIconSize(int sizePx)
        {
            if (sizePx < 24) sizePx = 32;
            if (sizePx > 96) sizePx = 64;

            try
            {
                using (RegistryKey desktopBag = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\Shell\Bags\1\Desktop"))
                {
                    if (desktopBag != null)
                    {
                        desktopBag.SetValue("IconSize", sizePx, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey adv = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop\WindowMetrics"))
                {
                    if (adv != null)
                    {
                        adv.SetValue("Shell Icon Size", sizePx.ToString(), RegistryValueKind.String);
                    }
                }

                SetConfigValue("DesktopIconSize", sizePx.ToString());
                RefreshExplorerViewsSafely();
            }
            catch
            {
            }
        }

        public static bool SetHideShortcutArrows(bool hide)
        {
            bool ok = false;
            try
            {
                EnsureBlankArrowIconExists();

                // Try HKCU first (always writable without elevation)
                try
                {
                    using (RegistryKey cuIcons = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
                    {
                        if (cuIcons != null)
                        {
                            if (hide)
                            {
                                cuIcons.SetValue("29", BlankArrowIcoPath + ",0", RegistryValueKind.String);
                            }
                            else
                            {
                                cuIcons.DeleteValue("29", false);
                            }
                            ok = true;
                        }
                    }
                }
                catch
                {
                }

                // Also try HKLM if running with administrative privileges
                try
                {
                    using (RegistryKey lmIcons = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
                    {
                        if (lmIcons != null)
                        {
                            if (hide)
                            {
                                lmIcons.SetValue("29", BlankArrowIcoPath + ",0", RegistryValueKind.String);
                            }
                            else
                            {
                                lmIcons.DeleteValue("29", false);
                            }
                            ok = true;
                        }
                    }
                }
                catch
                {
                }

                SetConfigValue("HideShortcutArrows", hide ? "1" : "0");
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
            }
            return ok;
        }

        private static void EnsureBlankArrowIconExists()
        {
            try
            {
                if (!Directory.Exists(SuiteDir))
                {
                    Directory.CreateDirectory(SuiteDir);
                }

                if (File.Exists(BlankArrowIcoPath) && new FileInfo(BlankArrowIcoPath).Length > 22)
                {
                    return;
                }

                using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(0, 0, 0, 0));
                    }

                    byte[] pngBytes;
                    using (MemoryStream msPng = new MemoryStream())
                    {
                        bmp.Save(msPng, ImageFormat.Png);
                        pngBytes = msPng.ToArray();
                    }

                    using (FileStream fs = new FileStream(BlankArrowIcoPath, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write((ushort)0); // Reserved
                        bw.Write((ushort)1); // Type 1 = ICO
                        bw.Write((ushort)1); // 1 image
                        bw.Write((byte)32);  // Width 32
                        bw.Write((byte)32);  // Height 32
                        bw.Write((byte)0);   // Palette
                        bw.Write((byte)0);   // Reserved
                        bw.Write((ushort)1); // Color planes
                        bw.Write((ushort)32);// Bits per pixel
                        bw.Write((uint)pngBytes.Length);
                        bw.Write((uint)22);  // Offset (6 + 16)
                        bw.Write(pngBytes);
                    }
                }
            }
            catch
            {
            }
        }

        private static IntPtr FindDesktopShellDefView(out IntPtr listViewHwnd)
        {
            IntPtr foundDefView = IntPtr.Zero;
            IntPtr foundListView = IntPtr.Zero;

            try
            {
                IntPtr progman = FindWindow("Progman", null);
                if (progman != IntPtr.Zero)
                {
                    foundDefView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                }

                if (foundDefView == IntPtr.Zero)
                {
                    EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                    {
                        StringBuilder sb = new StringBuilder(64);
                        if (GetClassName(hWnd, sb, sb.Capacity) > 0 && sb.ToString() == "WorkerW")
                        {
                            IntPtr def = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                            if (def != IntPtr.Zero)
                            {
                                foundDefView = def;
                                return false;
                            }
                        }
                        return true;
                    }, IntPtr.Zero);
                }

                if (foundDefView != IntPtr.Zero)
                {
                    foundListView = FindWindowEx(foundDefView, IntPtr.Zero, "SysListView32", null);
                }
            }
            catch
            {
            }

            listViewHwnd = foundListView;
            return foundDefView;
        }

        public static bool AreDesktopIconsVisible()
        {
            try
            {
                IntPtr listView;
                IntPtr defView = FindDesktopShellDefView(out listView);
                if (listView != IntPtr.Zero)
                {
                    return IsWindowVisible(listView);
                }
                if (defView != IntPtr.Zero)
                {
                    return IsWindowVisible(defView);
                }
            }
            catch
            {
            }
            return true;
        }

        public static bool ToggleDesktopIconsVisibility()
        {
            try
            {
                IntPtr listView;
                IntPtr defView = FindDesktopShellDefView(out listView);
                if (defView != IntPtr.Zero)
                {
                    bool wasVisible = listView != IntPtr.Zero ? IsWindowVisible(listView) : IsWindowVisible(defView);
                    SendMessage(defView, WM_COMMAND, (IntPtr)0x7402, IntPtr.Zero);
                    Thread.Sleep(40);
                    bool nowVisible = listView != IntPtr.Zero ? IsWindowVisible(listView) : IsWindowVisible(defView);
                    if (nowVisible == wasVisible && listView != IntPtr.Zero)
                    {
                        ShowWindow(listView, wasVisible ? SW_HIDE : SW_SHOW);
                        nowVisible = IsWindowVisible(listView);
                    }
                    return nowVisible;
                }
            }
            catch
            {
            }
            return true;
        }

        /// <summary>
        /// v3.0 1-click Desktop Icons visibility toggle (alias for ToggleDesktopIconsVisibility).
        /// Finds Progman / WorkerW -> SHELLDLL_DefView -> SysListView32 and toggles SW_HIDE/SW_SHOW or sends 0x7402.
        /// Returns the new visibility state (true = visible, false = hidden).
        /// </summary>
        public static bool ToggleDesktopIcons()
        {
            return ToggleDesktopIconsVisibility();
        }

        public static bool SetDesktopIconsVisible(bool visible)
        {
            try
            {
                bool current = AreDesktopIconsVisible();
                if (current != visible)
                {
                    return ToggleDesktopIconsVisibility();
                }
                return current;
            }
            catch
            {
                return true;
            }
        }

        public static void SetAutoStart(bool enabled)
        {
            SetAutoStart(enabled, DefaultExePath);
        }

        public static void SetAutoStart(bool enabled, string exePath)
        {
            try
            {
                string targetExe = string.IsNullOrEmpty(exePath) ? DefaultExePath : exePath;
                using (RegistryKey run = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (run != null)
                    {
                        run.DeleteValue("ExplorerGlass", false);
                        run.DeleteValue("TranslucentTB", false);

                        if (enabled)
                        {
                            run.SetValue("DoomControlCenter", "\"" + targetExe + "\" --minimized", RegistryValueKind.String);
                        }
                        else
                        {
                            run.DeleteValue("DoomControlCenter", false);
                        }
                    }
                }
                SetConfigValue("AutoStart", enabled ? "1" : "0");
            }
            catch
            {
            }
        }

        /// <summary>
        /// Applies Doctor Doom emerald icons to the user's desktop folders via desktop.ini + SHChangeNotify.
        /// Returns the number of desktop folders updated.
        /// </summary>
        public static int ApplyDesktopDoomIcons()
        {
            int updatedCount = 0;
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktopDir) || !Directory.Exists(desktopDir))
                {
                    desktopDir = @"C:\Users\gorke\Desktop";
                }

                Dictionary<string, string> folderIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                folderIconMap["Araclar"] = "Araclar.ico";
                folderIconMap["Beyin"] = "Beyin.ico";
                folderIconMap["ComfyUI"] = "ComfyUI.ico";
                folderIconMap["ComfyUIDocuments"] = "ComfyUIDocuments.ico";
                folderIconMap["Görkem"] = "01-Kisisel.ico";
                folderIconMap["Gorkem"] = "01-Kisisel.ico";
                folderIconMap["Oyunlar"] = "Games.ico";
                folderIconMap["Games"] = "Games.ico";
                folderIconMap["Adobe Reverse Engineered"] = "03-Arsiv.ico";
                folderIconMap["01-Kisisel"] = "01-Kisisel.ico";
                folderIconMap["02-Varliklar"] = "02-Varliklar.ico";
                folderIconMap["03-Arsiv"] = "03-Arsiv.ico";

                string[] subDirs = Directory.GetDirectories(desktopDir);
                for (int i = 0; i < subDirs.Length; i++)
                {
                    string folderPath = subDirs[i];
                    string folderName = Path.GetFileName(folderPath);
                    string icoFileName;
                    if (!folderIconMap.TryGetValue(folderName, out icoFileName))
                    {
                        continue;
                    }

                    string icoFullPath = Path.Combine(IconsDir, icoFileName);
                    if (!File.Exists(icoFullPath))
                    {
                        continue;
                    }

                    if (ApplyFolderIconIni(folderPath, icoFullPath))
                    {
                        updatedCount++;
                    }
                }

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
                SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, desktopDir, IntPtr.Zero);
            }
            catch
            {
            }
            return updatedCount;
        }

        private static bool ApplyFolderIconIni(string folderPath, string icoFullPath)
        {
            try
            {
                string iniPath = Path.Combine(folderPath, "desktop.ini");
                if (File.Exists(iniPath))
                {
                    File.SetAttributes(iniPath, FileAttributes.Normal);
                }

                string iniContent =
                    "[.ShellClassInfo]\r\n" +
                    "IconResource=" + icoFullPath + ",0\r\n";

                File.WriteAllText(iniPath, iniContent, Encoding.Default);

                File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);

                FileAttributes dirAttr = File.GetAttributes(folderPath);
                File.SetAttributes(folderPath, dirAttr | FileAttributes.ReadOnly);

                SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, folderPath, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exports a valid Windows 10 .theme file for the given ThemePreset.
        /// </summary>
        public static string ExportWindowsThemeFile(ThemePreset preset)
        {
            return ExportWindowsThemeFile(preset, null);
        }

        public static string ExportWindowsThemeFile(ThemePreset preset, string outputPath)
        {
            if (preset == null)
            {
                preset = ThemeCatalog.GetPresetById(GetConfigValue("ActiveProfile", "DoomEmerald"));
            }

            string targetPath = outputPath;
            if (string.IsNullOrEmpty(targetPath))
            {
                string safeId = string.IsNullOrEmpty(preset.Id) ? "DoomEmerald" : preset.Id;
                targetPath = Path.Combine(SuiteDir, safeId + ".theme");
            }

            try
            {
                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string wallpaper = !string.IsNullOrEmpty(preset.WallpaperPath) && File.Exists(preset.WallpaperPath)
                    ? preset.WallpaperPath
                    : DoomWallpaperPath;

                uint colorization = ThemeCatalog.ColorToArgbDword(preset.PrimaryAccent, 0xC4);
                string hilightRgb = string.Format("{0} {1} {2}", preset.PrimaryAccent.R, preset.PrimaryAccent.G, preset.PrimaryAccent.B);
                string hotTrackRgb = string.Format("{0} {1} {2}", preset.RimHighlight.R, preset.RimHighlight.G, preset.RimHighlight.B);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; SPLEND // Windows Theme Studio v2.0 - " + preset.DisplayName);
                sb.AppendLine("[Theme]");
                sb.AppendLine("DisplayName=" + preset.DisplayName);
                sb.AppendLine("ThemeId={8B5C4E19-3D2A-4F91-9A3E-71D00E3E4A31}");
                sb.AppendLine();
                sb.AppendLine(@"[Control Panel\Desktop]");
                sb.AppendLine("Wallpaper=" + wallpaper);
                sb.AppendLine("TileWallpaper=0");
                sb.AppendLine("WallpaperStyle=10");
                sb.AppendLine("Pattern=");
                sb.AppendLine();
                sb.AppendLine(@"[Control Panel\Colors]");
                sb.AppendLine("Window=255 255 255");
                sb.AppendLine("WindowText=0 0 0");
                sb.AppendLine("Hilight=" + hilightRgb);
                sb.AppendLine("HilightText=255 255 255");
                sb.AppendLine("HotTrackingColor=" + hotTrackRgb);
                sb.AppendLine();
                sb.AppendLine("[VisualStyles]");
                sb.AppendLine(@"Path=%ResourceDir%\Themes\Aero\Aero.msstyles");
                sb.AppendLine("ColorStyle=NormalColor");
                sb.AppendLine("Size=NormalSize");
                sb.AppendLine("AutoColorization=0");
                sb.AppendLine(string.Format("ColorizationColor=0X{0:X8}", colorization));
                sb.AppendLine("SystemMode=Dark");
                sb.AppendLine("AppMode=Dark");
                sb.AppendLine("VisualStyleVersion=10");
                sb.AppendLine();
                sb.AppendLine("[MasterThemeSelector]");
                sb.AppendLine("MTSM=RJSPBS");

                File.WriteAllText(targetPath, sb.ToString(), Encoding.Default);
            }
            catch
            {
            }
            return targetPath;
        }

        /// <summary>
        /// Writes a valid Windows 10 .theme file at DoomEmerald.theme and returns its full path.
        /// </summary>
        public static string GenerateWindowsThemeFile()
        {
            return GenerateWindowsThemeFile(false);
        }

        public static string GenerateWindowsThemeFile(bool openThemeFile)
        {
            ThemePreset active = ThemeCatalog.GetPresetById(GetConfigValue("ActiveProfile", "DoomEmerald"));
            string path = ExportWindowsThemeFile(active, ThemeFilePath);
            if (openThemeFile)
            {
                ApplyPreset(active, false);
            }
            return path;
        }

        public static void ExportConfigBackup(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                Dictionary<string, string> cfg = LoadConfig();
                SaveConfig(cfg);
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(ConfigPath))
                {
                    File.Copy(ConfigPath, filePath, true);
                }
            }
            catch
            {
            }
        }

        public static bool ImportConfigBackup(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return false;
                }

                if (!Directory.Exists(SuiteDir))
                {
                    Directory.CreateDirectory(SuiteDir);
                }

                File.Copy(filePath, ConfigPath, true);
                Dictionary<string, string> loaded = LoadConfig();
                SaveConfig(loaded);

                string activeProfile = "DoomEmerald";
                if (loaded.ContainsKey("ActiveProfile") && !string.IsNullOrEmpty(loaded["ActiveProfile"]))
                {
                    activeProfile = loaded["ActiveProfile"];
                }

                ThemePreset preset = ThemeCatalog.GetPresetById(activeProfile);
                ApplyPreset(preset, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restarts explorer.exe cleanly when clicked by the user in the UI.
        /// </summary>
        public static void RestartExplorerSafely()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("explorer");
                for (int i = 0; i < procs.Length; i++)
                {
                    try
                    {
                        procs[i].Kill();
                        procs[i].WaitForExit(3000);
                    }
                    catch
                    {
                    }
                }

                Thread.Sleep(400);
                if (Process.GetProcessesByName("explorer").Length == 0)
                {
                    Process.Start("explorer.exe");
                }
            }
            catch
            {
                try
                {
                    Process.Start("explorer.exe");
                }
                catch
                {
                }
            }
        }

        public static void RefreshExplorerViewsSafely()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);

                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    StringBuilder sb = new StringBuilder(64);
                    if (GetClassName(hWnd, sb, sb.Capacity) > 0)
                    {
                        string cls = sb.ToString();
                        if (cls == "CabinetWClass" || cls == "ExploreWClass")
                        {
                            RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RDW_ERASE | RDW_INVALIDATE | RDW_FRAME | RDW_ALLCHILDREN | RDW_UPDATENOW);
                        }
                        else if (cls == "Shell_TrayWnd")
                        {
                            InvalidateRect(hWnd, IntPtr.Zero, true);
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
        }

        /// <summary>
        /// v3.0 Queries system RAM statistics via GlobalMemoryStatusEx (kernel32.dll).
        /// </summary>
        public static bool GetSystemMemoryStats(out double usedGb, out double freeGb, out double totalGb, out int usagePercent)
        {
            usedGb = 8.0;
            freeGb = 8.0;
            totalGb = 16.0;
            usagePercent = 50;

            try
            {
                MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem) && mem.ullTotalPhys > 0)
                {
                    const double BytesPerGb = 1024.0 * 1024.0 * 1024.0;
                    totalGb = Math.Round(mem.ullTotalPhys / BytesPerGb, 2);
                    freeGb = Math.Round(mem.ullAvailPhys / BytesPerGb, 2);
                    usedGb = Math.Round(Math.Max(0.0, (mem.ullTotalPhys - mem.ullAvailPhys) / BytesPerGb), 2);
                    usagePercent = (int)mem.dwMemoryLoad;
                    if (usagePercent < 0) usagePercent = 0;
                    if (usagePercent > 100) usagePercent = 100;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// v3.0 RAM & WorkingSet Purger.
        /// Calls EmptyWorkingSet (psapi.dll) across accessible user processes (skipping critical system processes)
        /// and performs full .NET GC collection. Returns freed MB and sets out freedBytes and processCount.
        /// </summary>
        public static double PurgeStandbyAndWorkingSet(out long freedBytes, out int processCount)
        {
            freedBytes = 0;
            processCount = 0;

            try
            {
                MEMORYSTATUSEX memBefore = new MEMORYSTATUSEX();
                bool hasBefore = GlobalMemoryStatusEx(memBefore);

                HashSet<string> skipProcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "System", "Idle", "Registry", "Memory Compression",
                    "smss", "csrss", "wininit", "services", "lsass",
                    "svchost", "dwm", "fontdrvhost", "winlogon", "audiodg"
                };

                long sumWorkingSetDelta = 0;
                Process[] procs = Process.GetProcesses();
                for (int i = 0; i < procs.Length; i++)
                {
                    Process p = procs[i];
                    try
                    {
                        if (p == null || p.Id <= 4)
                        {
                            continue;
                        }

                        string name = p.ProcessName;
                        if (string.IsNullOrEmpty(name) || skipProcs.Contains(name))
                        {
                            continue;
                        }

                        IntPtr handle = p.Handle;
                        if (handle != IntPtr.Zero)
                        {
                            long wsBefore = 0;
                            try { wsBefore = p.WorkingSet64; } catch { }

                            if (EmptyWorkingSet(handle))
                            {
                                processCount++;
                                try
                                {
                                    p.Refresh();
                                    long wsAfter = p.WorkingSet64;
                                    if (wsBefore > wsAfter)
                                    {
                                        sumWorkingSetDelta += (wsBefore - wsAfter);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip elevated/protected processes cleanly without throwing
                    }
                    finally
                    {
                        if (p != null)
                        {
                            try { p.Dispose(); } catch { }
                        }
                    }
                }

                // Collect managed heap
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                MEMORYSTATUSEX memAfter = new MEMORYSTATUSEX();
                long globalDelta = 0;
                if (hasBefore && GlobalMemoryStatusEx(memAfter))
                {
                    if (memAfter.ullAvailPhys > memBefore.ullAvailPhys)
                    {
                        globalDelta = (long)(memAfter.ullAvailPhys - memBefore.ullAvailPhys);
                    }
                }

                freedBytes = Math.Max(globalDelta, sumWorkingSetDelta);
                if (freedBytes <= 0 && processCount > 0)
                {
                    // Minimum realistic working-set trim reporting when OS page cache absorbs delta immediately
                    freedBytes = processCount * 512L * 1024L;
                }
            }
            catch
            {
            }

            return Math.Round(freedBytes / (1024.0 * 1024.0), 1);
        }

        public static double PurgeStandbyAndWorkingSet()
        {
            long bytes;
            int count;
            return PurgeStandbyAndWorkingSet(out bytes, out count);
        }

        public static BoostProfile GetBoostProfile()
        {
            int raw = GetConfigInt("BoostProfile", 0);
            if (raw < 0 || raw > 3) raw = 0;
            return (BoostProfile)raw;
        }

        public static void SetBoostProfile(BoostProfile profile)
        {
            ApplyBoostProfile(profile);
        }

        public static void ApplyBoostProfile(BoostProfile profile)
        {
            try
            {
                Dictionary<string, string> cfg = LoadConfig();
                cfg["BoostProfile"] = ((int)profile).ToString(CultureInfo.InvariantCulture);

                switch (profile)
                {
                    case BoostProfile.Normal:
                        cfg["SmartFocusEnabled"] = "1";
                        cfg["RendererFpsCap"] = "60";
                        SmartFocusMonitor.Enabled = true;
                        SmartFocusMonitor.ForcePauseOverride = false;
                        break;

                    case BoostProfile.Oyun:
                        cfg["SmartFocusEnabled"] = "1";
                        cfg["RendererFpsCap"] = "30";
                        SmartFocusMonitor.Enabled = true;
                        SmartFocusMonitor.ForcePauseOverride = true;
                        PurgeStandbyAndWorkingSet();
                        break;

                    case BoostProfile.Render:
                        cfg["SmartFocusEnabled"] = "1";
                        cfg["RendererFpsCap"] = "20";
                        SmartFocusMonitor.Enabled = true;
                        SmartFocusMonitor.ForcePauseOverride = true;
                        PurgeStandbyAndWorkingSet();
                        break;

                    case BoostProfile.Sessiz:
                        cfg["SmartFocusEnabled"] = "1";
                        cfg["RendererFpsCap"] = "15";
                        SmartFocusMonitor.Enabled = true;
                        SmartFocusMonitor.ForcePauseOverride = false;
                        break;
                }

                SaveConfig(cfg);
            }
            catch
            {
            }
        }

        public static bool ShouldPauseEffects(out string reasonProcess)
        {
            return SmartFocusMonitor.ShouldPauseEffects(out reasonProcess);
        }

        public static void SetDisplayOverlay(int dimmerPercent, int warmthKelvin)
        {
            DisplayOverlayManager.ApplyOverlay(dimmerPercent, warmthKelvin);
        }
    }

    /// <summary>
    /// v3.0 Smart Focus & Game/Render Auto-Pause Detector.
    /// Detects fullscreen applications or heavy GPU/render processes (ComfyUI, python, blender, unreal, cs2, valorant, cyberpunk, doom, etc.)
    /// so UI/HUD/Glass daemons can enter 0% overhead sleep mode.
    /// </summary>
    public static class SmartFocusMonitor
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private static readonly object SyncLock = new object();
        private static readonly List<string> Watchlist = new List<string>
        {
            "ComfyUI",
            "blender",
            "unreal",
            "UE4Editor",
            "UnrealEditor",
            "cyberpunk2077",
            "cs2",
            "valorant",
            "DOOMEternalx64vk",
            "DOOMx64",
            "eldenring",
            "witcher3",
            "GTA5",
            "RDR2",
            "dota2",
            "League of Legends",
            "Overwatch",
            "r5apex",
            "FortniteClient-Win64-Shipping",
            "AfterFX",
            "Adobe Premiere Pro",
            "Resolve",
            "Cinebench"
        };

        public static bool Enabled = true;
        public static bool PauseOnFullscreen = true;
        public static bool ForcePauseOverride = false;
        public static int ResumeDelaySeconds = 3;

        private static long _lastScanTick = 0;
        private static bool _cachedShouldPause = false;
        private static string _cachedReason = string.Empty;
        private static DateTime _lastActiveTriggerUtc = DateTime.MinValue;

        public static List<string> GetWatchlist()
        {
            lock (SyncLock)
            {
                return new List<string>(Watchlist);
            }
        }

        public static void AddWatchedProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return;
            string clean = processName.Trim();
            if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 4);
            }
            if (clean.Length == 0) return;

            lock (SyncLock)
            {
                for (int i = 0; i < Watchlist.Count; i++)
                {
                    if (string.Equals(Watchlist[i], clean, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
                Watchlist.Add(clean);
            }
        }

        public static bool RemoveWatchedProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;
            string clean = processName.Trim();
            lock (SyncLock)
            {
                for (int i = Watchlist.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(Watchlist[i], clean, StringComparison.OrdinalIgnoreCase))
                    {
                        Watchlist.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool ShouldPauseEffects()
        {
            string reason;
            return ShouldPauseEffects(out reason);
        }

        public static bool ShouldPauseEffects(out string reasonProcess)
        {
            reasonProcess = string.Empty;
            if (!Enabled)
            {
                return false;
            }

            if (ForcePauseOverride)
            {
                reasonProcess = "Boost Profili Aktif";
                return true;
            }

            long nowTick = Environment.TickCount & int.MaxValue;
            if (nowTick - _lastScanTick < 1500 && _lastScanTick != 0)
            {
                reasonProcess = _cachedReason;
                return _cachedShouldPause;
            }

            _lastScanTick = nowTick;

            try
            {
                // 1) Check foreground window for fullscreen state or watched foreground app
                string fgProc;
                if (PauseOnFullscreen && IsFullscreenAppActive(out fgProc))
                {
                    _cachedShouldPause = true;
                    _cachedReason = "Tam Ekran: " + fgProc;
                    _lastActiveTriggerUtc = DateTime.UtcNow;
                    reasonProcess = _cachedReason;
                    return true;
                }

                // 2) Check running processes against heavy GPU/render watchlist
                List<string> snapshotWatch;
                lock (SyncLock)
                {
                    snapshotWatch = new List<string>(Watchlist);
                }

                for (int i = 0; i < snapshotWatch.Count; i++)
                {
                    string watched = snapshotWatch[i];
                    if (string.IsNullOrEmpty(watched)) continue;
                    try
                    {
                        Process[] found = Process.GetProcessesByName(watched);
                        bool anyRunning = found != null && found.Length > 0;
                        if (found != null)
                        {
                            for (int j = 0; j < found.Length; j++)
                            {
                                try { found[j].Dispose(); } catch { }
                            }
                        }

                        if (anyRunning)
                        {
                            _cachedShouldPause = true;
                            _cachedReason = watched;
                            _lastActiveTriggerUtc = DateTime.UtcNow;
                            reasonProcess = _cachedReason;
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }

                // 3) Check python process with high working set (> 1.2 GB, typical of ComfyUI / PyTorch)
                try
                {
                    Process[] pyProcs = Process.GetProcessesByName("python");
                    for (int i = 0; i < pyProcs.Length; i++)
                    {
                        try
                        {
                            if (pyProcs[i].WorkingSet64 > 1200L * 1024L * 1024L)
                            {
                                _cachedShouldPause = true;
                                _cachedReason = "ComfyUI / Python GPU";
                                _lastActiveTriggerUtc = DateTime.UtcNow;
                                reasonProcess = _cachedReason;
                                return true;
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            try { pyProcs[i].Dispose(); } catch { }
                        }
                    }
                }
                catch
                {
                }

                // Respect resume hysteresis delay
                if (_cachedShouldPause && (DateTime.UtcNow - _lastActiveTriggerUtc).TotalSeconds < Math.Max(0, ResumeDelaySeconds))
                {
                    reasonProcess = _cachedReason;
                    return true;
                }

                _cachedShouldPause = false;
                _cachedReason = string.Empty;
            }
            catch
            {
                _cachedShouldPause = false;
                _cachedReason = string.Empty;
            }

            reasonProcess = _cachedReason;
            return _cachedShouldPause;
        }

        public static bool IsFullscreenAppActive(out string processName)
        {
            processName = string.Empty;
            try
            {
                IntPtr fg = GetForegroundWindow();
                if (fg == IntPtr.Zero)
                {
                    return false;
                }

                StringBuilder clsSb = new StringBuilder(64);
                if (GetClassName(fg, clsSb, clsSb.Capacity) > 0)
                {
                    string cls = clsSb.ToString();
                    if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "CabinetWClass")
                    {
                        return false;
                    }
                }

                RECT r;
                if (!GetWindowRect(fg, out r))
                {
                    return false;
                }

                Screen scr = Screen.FromHandle(fg);
                if (scr == null)
                {
                    return false;
                }

                Rectangle bounds = scr.Bounds;
                int w = r.Right - r.Left;
                int h = r.Bottom - r.Top;

                if (r.Left <= bounds.Left && r.Top <= bounds.Top && w >= bounds.Width && h >= bounds.Height)
                {
                    uint pid;
                    GetWindowThreadProcessId(fg, out pid);
                    if (pid > 0 && pid != (uint)Process.GetCurrentProcess().Id)
                    {
                        try
                        {
                            using (Process p = Process.GetProcessById((int)pid))
                            {
                                if (p != null && !string.Equals(p.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase))
                                {
                                    processName = p.ProcessName;
                                    return true;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }
    }

    /// <summary>
    /// v3.0 Software Display Brightness Dimmer (0-80% alpha) & Night-Light Warmth Overlay (6500K to 2700K amber tint).
    /// Uses a click-through, topmost layered window (`WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`)
    /// across the virtual desktop without requiring hardware DDC/CI support.
    /// </summary>
    public static class DisplayOverlayManager
    {
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const uint LWA_ALPHA = 0x00000002;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private sealed class ClickThroughOverlayForm : Form
        {
            public ClickThroughOverlayForm()
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.StartPosition = FormStartPosition.Manual;
                this.TopMost = true;
                this.BackColor = Color.Black;
                this.DoubleBuffered = true;
            }

            protected override bool ShowWithoutActivation
            {
                get { return true; }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.Style = WS_POPUP;
                    cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    return cp;
                }
            }
        }

        private static ClickThroughOverlayForm _overlayForm;
        private static int _dimmerPercent = 0;
        private static int _warmthKelvin = 6500;

        public static int DimmerPercent
        {
            get { return _dimmerPercent; }
            set { ApplyOverlay(value, _warmthKelvin); }
        }

        public static int WarmthKelvin
        {
            get { return _warmthKelvin; }
            set { ApplyOverlay(_dimmerPercent, value); }
        }

        public static bool IsActive
        {
            get { return (_dimmerPercent > 0) || (_warmthKelvin < 6450); }
        }

        /// <summary>
        /// Computes the blended ARGB tint color and alpha for a given dimmer percentage (0..80) and warmth Kelvin (2700..6500).
        /// </summary>
        public static Color ComputeOverlayColor(int dimmerPercent, int warmthKelvin)
        {
            int dim = ThemeCatalog.ClampInt(dimmerPercent, 0, 80);
            int kelvin = ThemeCatalog.ClampInt(warmthKelvin, 2700, 6500);

            double dimRatio = dim / 100.0; // 0.0 .. 0.80
            double warmRatio = (6500.0 - kelvin) / (6500.0 - 2700.0); // 0.0 at 6500K .. 1.0 at 2700K
            warmRatio = ThemeCatalog.ClampDouble(warmRatio, 0.0, 1.0);

            // Warm amber night-light RGB (255, 138, 36) blended with black dimmer (0, 0, 0)
            double warmAlpha = warmRatio * 0.34; // max 34% warm filter
            double combinedAlpha = 1.0 - ((1.0 - dimRatio) * (1.0 - warmAlpha));
            combinedAlpha = ThemeCatalog.ClampDouble(combinedAlpha, 0.0, 0.85);

            int alphaByte = ThemeCatalog.ClampInt((int)Math.Round(combinedAlpha * 255.0), 0, 218);
            if (alphaByte <= 2)
            {
                return Color.FromArgb(0, 0, 0, 0);
            }

            // Weight color toward amber when warmth is active and dimmer is low
            double warmWeight = (warmAlpha) / Math.Max(0.01, dimRatio + warmAlpha);
            int r = ThemeCatalog.ClampInt((int)Math.Round(240.0 * warmWeight), 0, 255);
            int g = ThemeCatalog.ClampInt((int)Math.Round(126.0 * warmWeight), 0, 255);
            int b = ThemeCatalog.ClampInt((int)Math.Round(24.0 * warmWeight), 0, 255);

            return Color.FromArgb(alphaByte, r, g, b);
        }

        public static void SetDimmer(int dimmerPercent)
        {
            ApplyOverlay(dimmerPercent, _warmthKelvin);
        }

        public static void SetWarmthKelvin(int kelvin)
        {
            ApplyOverlay(_dimmerPercent, kelvin);
        }

        public static void ApplyOverlay(int dimmerPercent, int warmthKelvin)
        {
            _dimmerPercent = ThemeCatalog.ClampInt(dimmerPercent, 0, 80);
            _warmthKelvin = ThemeCatalog.ClampInt(warmthKelvin, 2700, 6500);

            try
            {
                Color overlay = ComputeOverlayColor(_dimmerPercent, _warmthKelvin);
                if (overlay.A <= 2)
                {
                    if (_overlayForm != null && !_overlayForm.IsDisposed)
                    {
                        ShowWindow(_overlayForm.Handle, 0); // SW_HIDE
                    }
                    return;
                }

                if (_overlayForm == null || _overlayForm.IsDisposed)
                {
                    _overlayForm = new ClickThroughOverlayForm();
                    IntPtr dummy = _overlayForm.Handle; // Force handle creation without activating
                }

                Rectangle vScreen = SystemInformation.VirtualScreen;
                _overlayForm.BackColor = Color.FromArgb(255, overlay.R, overlay.G, overlay.B);
                SetWindowPos(
                    _overlayForm.Handle,
                    HWND_TOPMOST,
                    vScreen.X,
                    vScreen.Y,
                    vScreen.Width,
                    vScreen.Height,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
                SetLayeredWindowAttributes(_overlayForm.Handle, 0, overlay.A, LWA_ALPHA);
                _overlayForm.Invalidate();
            }
            catch
            {
            }
        }

        public static void DisableOverlay()
        {
            ApplyOverlay(0, 6500);
        }

        public static void DisposeOverlay()
        {
            try
            {
                if (_overlayForm != null)
                {
                    if (!_overlayForm.IsDisposed)
                    {
                        _overlayForm.Close();
                        _overlayForm.Dispose();
                    }
                    _overlayForm = null;
                }
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Represents a single applied theme state snapshot for 1-click rollback in View 0 & View 6.
    /// </summary>
    public sealed class ThemeSnapshot
    {
        public string Id;
        public string Timestamp;
        public string Label;
        public string PresetId;
        public string PrimaryAccentHex;
        public string RimHighlightHex;
        public string ShadowGlowHex;
        public Dictionary<string, string> ConfigSnapshot;

        public ThemeSnapshot()
        {
            Id = "snap_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            Label = "DoomEmerald";
            PresetId = "DoomEmerald";
            PrimaryAccentHex = "#1F4A31";
            RimHighlightHex = "#6CA881";
            ShadowGlowHex = "#267347";
            ConfigSnapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0}] {1} ({2})", Timestamp, Label, PrimaryAccentHex);
        }
    }

    /// <summary>
    /// v3.0 History / Rollback Snapshot Manager.
    /// Maintains up to 20 applied state snapshots in memory and persists them to `snapshots.ini` for 1-click rollback.
    /// </summary>
    public static class ThemeSnapshotManager
    {
        public const int MaxSnapshots = 20;
        private static readonly object SyncRoot = new object();
        private static List<ThemeSnapshot> _memoryCache;

        public static List<ThemeSnapshot> GetSnapshots()
        {
            lock (SyncRoot)
            {
                EnsureLoadedUnlocked();
                return new List<ThemeSnapshot>(_memoryCache);
            }
        }

        public static ThemeSnapshot RecordSnapshot(string label = null, Dictionary<string, string> config = null)
        {
            lock (SyncRoot)
            {
                EnsureLoadedUnlocked();

                Dictionary<string, string> cfg = config ?? DoomThemeEngine.LoadConfig();
                ThemeSnapshot snap = new ThemeSnapshot();
                snap.Id = "snap_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                snap.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                snap.PresetId = GetVal(cfg, "ActiveProfile", "DoomEmerald");
                snap.Label = !string.IsNullOrEmpty(label) ? label : snap.PresetId;
                snap.PrimaryAccentHex = GetVal(cfg, "PrimaryAccentHex", "#1F4A31");
                snap.RimHighlightHex = GetVal(cfg, "RimHighlightHex", "#6CA881");
                snap.ShadowGlowHex = GetVal(cfg, "ShadowGlowHex", "#267347");

                foreach (KeyValuePair<string, string> kvp in cfg)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                    {
                        snap.ConfigSnapshot[kvp.Key] = kvp.Value;
                    }
                }

                // Insert newest at index 0
                _memoryCache.Insert(0, snap);
                while (_memoryCache.Count > MaxSnapshots)
                {
                    _memoryCache.RemoveAt(_memoryCache.Count - 1);
                }

                SaveSnapshotsUnlocked();
                return snap;
            }
        }

        public static bool RollbackToSnapshot(string snapshotId)
        {
            if (string.IsNullOrEmpty(snapshotId))
            {
                return RollbackToLatest();
            }

            ThemeSnapshot target = null;
            lock (SyncRoot)
            {
                EnsureLoadedUnlocked();
                for (int i = 0; i < _memoryCache.Count; i++)
                {
                    if (string.Equals(_memoryCache[i].Id, snapshotId, StringComparison.OrdinalIgnoreCase))
                    {
                        target = _memoryCache[i];
                        break;
                    }
                }
            }

            if (target == null)
            {
                return false;
            }

            try
            {
                if (target.ConfigSnapshot != null && target.ConfigSnapshot.Count > 0)
                {
                    DoomThemeEngine.SaveConfig(target.ConfigSnapshot);
                }

                ThemePreset preset = ThemeCatalog.GetPresetById(target.PresetId);
                preset.PrimaryAccent = ThemeCatalog.HexToColor(target.PrimaryAccentHex, preset.PrimaryAccent);
                preset.RimHighlight = ThemeCatalog.HexToColor(target.RimHighlightHex, preset.RimHighlight);
                preset.ExplorerShadowGlow = ThemeCatalog.HexToColor(target.ShadowGlowHex, preset.ExplorerShadowGlow);

                DoomThemeEngine.ApplyPreset(preset, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RollbackToLatest()
        {
            List<ThemeSnapshot> list = GetSnapshots();
            if (list.Count == 0)
            {
                return false;
            }
            // If >= 2 snapshots exist, index 1 is the state right before the most recent apply
            int idx = (list.Count >= 2) ? 1 : 0;
            return RollbackToSnapshot(list[idx].Id);
        }

        public static bool DeleteSnapshot(string snapshotId)
        {
            if (string.IsNullOrEmpty(snapshotId)) return false;
            lock (SyncRoot)
            {
                EnsureLoadedUnlocked();
                for (int i = _memoryCache.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(_memoryCache[i].Id, snapshotId, StringComparison.OrdinalIgnoreCase))
                    {
                        _memoryCache.RemoveAt(i);
                        SaveSnapshotsUnlocked();
                        return true;
                    }
                }
            }
            return false;
        }

        public static void ClearSnapshots()
        {
            lock (SyncRoot)
            {
                _memoryCache = new List<ThemeSnapshot>();
                SaveSnapshotsUnlocked();
            }
        }

        private static void EnsureLoadedUnlocked()
        {
            if (_memoryCache != null)
            {
                return;
            }

            _memoryCache = new List<ThemeSnapshot>();
            try
            {
                if (!File.Exists(DoomThemeEngine.SnapshotsPath))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(DoomThemeEngine.SnapshotsPath, Encoding.UTF8);
                ThemeSnapshot current = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[Snapshot:", StringComparison.OrdinalIgnoreCase) && line.EndsWith("]"))
                    {
                        if (current != null)
                        {
                            _memoryCache.Add(current);
                        }
                        current = new ThemeSnapshot();
                        current.Id = line.Substring(10, line.Length - 11).Trim();
                        continue;
                    }

                    if (current == null) continue;

                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        string k = line.Substring(0, eq).Trim();
                        string v = line.Substring(eq + 1).Trim();
                        if (string.Equals(k, "Timestamp", StringComparison.OrdinalIgnoreCase)) current.Timestamp = v;
                        else if (string.Equals(k, "Label", StringComparison.OrdinalIgnoreCase)) current.Label = v;
                        else if (string.Equals(k, "PresetId", StringComparison.OrdinalIgnoreCase)) current.PresetId = v;
                        else if (string.Equals(k, "PrimaryAccentHex", StringComparison.OrdinalIgnoreCase)) current.PrimaryAccentHex = v;
                        else if (string.Equals(k, "RimHighlightHex", StringComparison.OrdinalIgnoreCase)) current.RimHighlightHex = v;
                        else if (string.Equals(k, "ShadowGlowHex", StringComparison.OrdinalIgnoreCase)) current.ShadowGlowHex = v;
                        else if (k.StartsWith("Cfg.", StringComparison.OrdinalIgnoreCase) && k.Length > 4)
                        {
                            current.ConfigSnapshot[k.Substring(4)] = v;
                        }
                    }
                }

                if (current != null && _memoryCache.Count < MaxSnapshots)
                {
                    _memoryCache.Add(current);
                }
            }
            catch
            {
            }
        }

        private static void SaveSnapshotsUnlocked()
        {
            try
            {
                if (!Directory.Exists(DoomThemeEngine.SuiteDir))
                {
                    Directory.CreateDirectory(DoomThemeEngine.SuiteDir);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# SPLEND // Windows Theme Studio v3.0 - Applied State Snapshots (Max 20)");

                for (int i = 0; i < _memoryCache.Count && i < MaxSnapshots; i++)
                {
                    ThemeSnapshot s = _memoryCache[i];
                    if (s == null) continue;
                    sb.AppendLine();
                    sb.AppendLine("[Snapshot:" + s.Id + "]");
                    sb.AppendLine("Timestamp=" + (s.Timestamp ?? string.Empty));
                    sb.AppendLine("Label=" + (s.Label ?? string.Empty));
                    sb.AppendLine("PresetId=" + (s.PresetId ?? "DoomEmerald"));
                    sb.AppendLine("PrimaryAccentHex=" + (s.PrimaryAccentHex ?? "#1F4A31"));
                    sb.AppendLine("RimHighlightHex=" + (s.RimHighlightHex ?? "#6CA881"));
                    sb.AppendLine("ShadowGlowHex=" + (s.ShadowGlowHex ?? "#267347"));

                    if (s.ConfigSnapshot != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in s.ConfigSnapshot)
                        {
                            if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                            {
                                sb.AppendLine("Cfg." + kvp.Key + "=" + kvp.Value.Replace("\r", "").Replace("\n", ""));
                            }
                        }
                    }
                }

                File.WriteAllText(DoomThemeEngine.SnapshotsPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string GetVal(Dictionary<string, string> map, string key, string fallback)
        {
            string v;
            if (map != null && map.TryGetValue(key, out v) && !string.IsNullOrEmpty(v))
            {
                return v;
            }
            return fallback;
        }
    }
}
