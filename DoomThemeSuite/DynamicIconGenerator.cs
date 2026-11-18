using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DoomThemeSuite
{
    public static class DynamicIconGenerator
    {
        public static readonly string GeneratedIconsRoot = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\GeneratedIcons";

        public static readonly string[] FolderTypes = new string[]
        {
            "01-Kisisel",
            "02-Varliklar",
            "03-Arsiv",
            "Araclar",
            "Beyin",
            "ComfyUI",
            "ComfyUIDocuments",
            "Games"
        };

        private static readonly int[] IcoFrameSizes = new int[] { 256, 64, 32, 16 };

        // ====================================================================
        // Win32 Shell Notification P/Invoke
        // ====================================================================

        private const int SHCNE_UPDATEITEM = 0x00002000;
        private const int SHCNE_UPDATEDIR = 0x00001000;
        private const int SHCNE_ASSOCCHANGED = 0x08000000;

        private const uint SHCNF_IDLIST = 0x0000;
        private const uint SHCNF_PATHW = 0x0005;
        private const uint SHCNF_FLUSHNOWAIT = 0x2000;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, string dwItem1, IntPtr dwItem2);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        // ====================================================================
        // Public API
        // ====================================================================

        /// <summary>
        /// Generates all 8 custom themed multi-resolution PNG-encoded .ico files
        /// plus an 800x420 preview.png into GeneratedIcons\&lt;preset.Id&gt;\ and returns the directory path.
        /// </summary>
        public static string GenerateIconSetForPreset(ThemePreset preset)
        {
            ThemePreset effective = EnsurePreset(preset);
            string safeFolderId = SanitizeFolderId(effective.Id);
            string outDir = Path.Combine(GeneratedIconsRoot, safeFolderId);

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            Bitmap[] renderedMasters = new Bitmap[FolderTypes.Length];
            try
            {
                for (int i = 0; i < FolderTypes.Length; i++)
                {
                    string folderKind = FolderTypes[i];
                    Bitmap master512 = RenderFolderMaster512(effective, folderKind);
                    renderedMasters[i] = master512;

                    string icoPath = Path.Combine(outDir, folderKind + ".ico");
                    WriteMultiResolutionPngIco(master512, icoPath);
                }

                string previewPath = Path.Combine(outDir, "preview.png");
                WriteIconSetPreviewSheet(effective, renderedMasters, previewPath);
            }
            finally
            {
                for (int i = 0; i < renderedMasters.Length; i++)
                {
                    if (renderedMasters[i] != null)
                    {
                        renderedMasters[i].Dispose();
                        renderedMasters[i] = null;
                    }
                }
            }

            return outDir;
        }

        /// <summary>
        /// Renders a single folder icon bitmap at the requested pixel size for live Studio UI previews.
        /// </summary>
        public static Bitmap RenderSingleFolderPreview(ThemePreset preset, string glyphKind, int size)
        {
            if (size < 16) size = 16;
            if (size > 512) size = 512;

            ThemePreset effective = EnsurePreset(preset);
            string canonicalKind = NormalizeGlyphKind(glyphKind);

            if (size == 512)
            {
                return RenderFolderMaster512(effective, canonicalKind);
            }

            using (Bitmap master = RenderFolderMaster512(effective, canonicalKind))
            {
                return ResizeHighQuality(master, size, size);
            }
        }

        /// <summary>
        /// Generates the themed icon set for preset (or restores default Windows yellow folders for "FactoryDark")
        /// and applies desktop.ini + shell icon refresh across matching folders on the user's Desktop.
        /// </summary>
        public static int ApplyPresetIconsToDesktop(ThemePreset preset)
        {
            if (preset == null)
            {
                return 0;
            }

            if (string.Equals(preset.Id, "FactoryDark", StringComparison.OrdinalIgnoreCase))
            {
                return RestoreDefaultWindowsFolderIcons();
            }

            int updatedCount = 0;
            try
            {
                string iconDir = GenerateIconSetForPreset(preset);
                Dictionary<string, string> folderIconMap = BuildDesktopFolderIconMap();
                List<string> desktopDirs = GetCandidateDesktopDirectories();

                for (int d = 0; d < desktopDirs.Count; d++)
                {
                    string desktopDir = desktopDirs[d];
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

                        string icoFullPath = Path.Combine(iconDir, icoFileName);
                        if (!File.Exists(icoFullPath))
                        {
                            continue;
                        }

                        if (ApplyFolderDesktopIni(folderPath, icoFullPath))
                        {
                            updatedCount++;
                        }
                    }
                }

                NotifyShellIconRefresh(desktopDirs);
            }
            catch
            {
            }

            return updatedCount;
        }

        /// <summary>
        /// Removes custom IconResource entries in desktop.ini and clears folder ReadOnly attributes
        /// on Desktop folders so standard Windows yellow folders return cleanly.
        /// </summary>
        public static int RestoreDefaultWindowsFolderIcons()
        {
            int restoredCount = 0;
            try
            {
                Dictionary<string, string> folderIconMap = BuildDesktopFolderIconMap();
                List<string> desktopDirs = GetCandidateDesktopDirectories();

                for (int d = 0; d < desktopDirs.Count; d++)
                {
                    string desktopDir = desktopDirs[d];
                    string[] subDirs = Directory.GetDirectories(desktopDir);

                    for (int i = 0; i < subDirs.Length; i++)
                    {
                        string folderPath = subDirs[i];
                        string folderName = Path.GetFileName(folderPath);
                        string iniPath = Path.Combine(folderPath, "desktop.ini");

                        bool isMappedFolder = folderIconMap.ContainsKey(folderName);
                        bool hasCustomThemeIcon = false;

                        if (!isMappedFolder && File.Exists(iniPath))
                        {
                            try
                            {
                                string rawIni = File.ReadAllText(iniPath, Encoding.Default);
                                if (rawIni.IndexOf("Desktop-DoomGreen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    rawIni.IndexOf("GeneratedIcons", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    hasCustomThemeIcon = true;
                                }
                            }
                            catch
                            {
                            }
                        }

                        if (!isMappedFolder && !hasCustomThemeIcon)
                        {
                            continue;
                        }

                        if (RemoveCustomIconFromFolder(folderPath, iniPath))
                        {
                            restoredCount++;
                        }
                    }
                }

                NotifyShellIconRefresh(desktopDirs);
            }
            catch
            {
            }

            return restoredCount;
        }

        // ====================================================================
        // Desktop Folder Mapping & desktop.ini Helpers
        // ====================================================================

        private static Dictionary<string, string> BuildDesktopFolderIconMap()
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            map["Araclar"] = "Araclar.ico";
            map["Beyin"] = "Beyin.ico";
            map["ComfyUI"] = "ComfyUI.ico";
            map["ComfyUIDocuments"] = "ComfyUIDocuments.ico";
            map["Görkem"] = "01-Kisisel.ico";
            map["Gorkem"] = "01-Kisisel.ico";
            map["Oyunlar"] = "Games.ico";
            map["Games"] = "Games.ico";
            map["Adobe Reverse Engineered"] = "03-Arsiv.ico";
            map["01-Kisisel"] = "01-Kisisel.ico";
            map["02-Varliklar"] = "02-Varliklar.ico";
            map["03-Arsiv"] = "03-Arsiv.ico";
            return map;
        }

        private static List<string> GetCandidateDesktopDirectories()
        {
            List<string> dirs = new List<string>();
            string primaryDesktop = @"C:\Users\gorke\Desktop";
            if (Directory.Exists(primaryDesktop))
            {
                dirs.Add(primaryDesktop);
            }

            try
            {
                string specialDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!string.IsNullOrEmpty(specialDesktop) && Directory.Exists(specialDesktop))
                {
                    bool exists = false;
                    for (int i = 0; i < dirs.Count; i++)
                    {
                        if (string.Equals(
                            Path.GetFullPath(dirs[i]).TrimEnd('\\'),
                            Path.GetFullPath(specialDesktop).TrimEnd('\\'),
                            StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        dirs.Add(specialDesktop);
                    }
                }
            }
            catch
            {
            }

            return dirs;
        }

        private static bool ApplyFolderDesktopIni(string folderPath, string icoFullPath)
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

        private static bool RemoveCustomIconFromFolder(string folderPath, string iniPath)
        {
            bool changed = false;
            try
            {
                if (File.Exists(iniPath))
                {
                    File.SetAttributes(iniPath, FileAttributes.Normal);

                    string[] lines = File.ReadAllLines(iniPath, Encoding.Default);
                    List<string> keptLines = new List<string>();
                    bool hasMeaningfulCustomLine = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string trimmed = lines[i].Trim();
                        if (trimmed.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                        {
                            changed = true;
                            continue;
                        }

                        keptLines.Add(lines[i]);
                        if (trimmed.Length > 0 &&
                            !trimmed.StartsWith(";", StringComparison.Ordinal) &&
                            !string.Equals(trimmed, "[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase))
                        {
                            hasMeaningfulCustomLine = true;
                        }
                    }

                    if (!hasMeaningfulCustomLine)
                    {
                        File.Delete(iniPath);
                        changed = true;
                    }
                    else
                    {
                        File.WriteAllLines(iniPath, keptLines.ToArray(), Encoding.Default);
                        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                    }
                }

                FileAttributes dirAttr = File.GetAttributes(folderPath);
                if ((dirAttr & FileAttributes.ReadOnly) != 0)
                {
                    FileAttributes cleared = dirAttr & ~FileAttributes.ReadOnly;
                    if (cleared == 0)
                    {
                        cleared = FileAttributes.Directory;
                    }
                    File.SetAttributes(folderPath, cleared);
                    changed = true;
                }

                if (changed)
                {
                    SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, folderPath, IntPtr.Zero);
                }
            }
            catch
            {
                return false;
            }

            return changed;
        }

        private static void NotifyShellIconRefresh(List<string> desktopDirs)
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
                if (desktopDirs != null)
                {
                    for (int i = 0; i < desktopDirs.Count; i++)
                    {
                        SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, desktopDirs[i], IntPtr.Zero);
                    }
                }
            }
            catch
            {
            }

            try
            {
                string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string ie4uinitPath = Path.Combine(sysDir, "ie4uinit.exe");
                if (!File.Exists(ie4uinitPath))
                {
                    ie4uinitPath = "ie4uinit.exe";
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = ie4uinitPath;
                psi.Arguments = "-show";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(1500);
                    }
                }
            }
            catch
            {
            }
        }

        // ====================================================================
        // Multi-Resolution Vista+ PNG-Encoded .ICO Writer
        // ====================================================================

        private static void WriteMultiResolutionPngIco(Bitmap master512, string icoPath)
        {
            int count = IcoFrameSizes.Length;
            byte[][] pngFrames = new byte[count][];

            for (int i = 0; i < count; i++)
            {
                int sz = IcoFrameSizes[i];
                using (Bitmap resized = ResizeHighQuality(master512, sz, sz))
                using (MemoryStream pngMs = new MemoryStream())
                {
                    resized.Save(pngMs, ImageFormat.Png);
                    pngFrames[i] = pngMs.ToArray();
                }
            }

            using (MemoryStream icoMs = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(icoMs))
            {
                // ICONDIR Header (6 bytes): 00 00 01 00 <count:uint16>
                bw.Write((ushort)0);
                bw.Write((ushort)1);
                bw.Write((ushort)count);

                int currentDataOffset = 6 + (count * 16);

                // ICONDIRENTRY Table (16 bytes per frame)
                for (int i = 0; i < count; i++)
                {
                    int sz = IcoFrameSizes[i];
                    byte bDim = (sz >= 256) ? (byte)0 : (byte)sz;
                    uint payloadLen = (uint)pngFrames[i].Length;

                    bw.Write(bDim);                      // bWidth (0 means 256)
                    bw.Write(bDim);                      // bHeight (0 means 256)
                    bw.Write((byte)0);                   // bColorCount
                    bw.Write((byte)0);                   // bReserved
                    bw.Write((ushort)1);                 // wPlanes
                    bw.Write((ushort)32);                // wBitCount (32bpp ARGB)
                    bw.Write(payloadLen);                // dwBytesInRes
                    bw.Write((uint)currentDataOffset);   // dwImageOffset

                    currentDataOffset += pngFrames[i].Length;
                }

                // Raw PNG payloads
                for (int i = 0; i < count; i++)
                {
                    bw.Write(pngFrames[i]);
                }

                bw.Flush();
                File.WriteAllBytes(icoPath, icoMs.ToArray());
            }
        }

        private static void WriteIconSetPreviewSheet(ThemePreset preset, Bitmap[] masters, string previewPath)
        {
            const int sheetW = 800;
            const int sheetH = 420;

            using (Bitmap sheet = new Bitmap(sheetW, sheetH, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    Color bgTop = MixColors(Color.FromArgb(10, 14, 12), preset.UiBackground, 0.55f);
                    Color bgBot = MixColors(Color.FromArgb(7, 10, 9), preset.ExplorerBodyTint, 0.25f);
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, sheetW, sheetH),
                        bgTop,
                        bgBot,
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bgBrush, 0, 0, sheetW, sheetH);
                    }

                    // Header bar
                    string titleText = string.IsNullOrEmpty(preset.DisplayName) ? "Custom Theme Icons" : preset.DisplayName;
                    using (Font headerFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold, GraphicsUnit.Point))
                    using (SolidBrush headerBrush = new SolidBrush(EnsureMinBrightness(preset.RimHighlight, 0.70f)))
                    {
                        g.DrawString("SPLEND // ICON SUITE · " + titleText.ToUpperInvariant(), headerFont, headerBrush, 52f, 12f);
                    }

                    const int iconSize = 132;
                    const int gapX = 56;
                    const int startX = 52; // (800 - (4*132 + 3*56)) / 2 = 52

                    using (Font labelFont = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point))
                    using (SolidBrush labelBrush = new SolidBrush(Color.FromArgb(225, 232, 238, 235)))
                    {
                        for (int i = 0; i < FolderTypes.Length && i < masters.Length; i++)
                        {
                            int col = i % 4;
                            int row = i / 4;

                            int drawX = startX + col * (iconSize + gapX);
                            int drawY = (row == 0) ? 38 : 228;

                            if (masters[i] != null)
                            {
                                g.DrawImage(masters[i], new Rectangle(drawX, drawY, iconSize, iconSize));
                            }

                            string label = FolderTypes[i];
                            g.DrawString(label, labelFont, labelBrush, drawX + 10f, drawY + iconSize + 4f);
                        }
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    sheet.Save(ms, ImageFormat.Png);
                    File.WriteAllBytes(previewPath, ms.ToArray());
                }
            }
        }

        // ====================================================================
        // 512x512 Master Folder & Vector Glyph Renderer
        // ====================================================================

        private static Bitmap RenderFolderMaster512(ThemePreset preset, string canonicalKind)
        {
            const int S = 512;
            Bitmap bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Resolve harmonious palette for ANY preset (including FactoryDark or custom presets)
                Color primary = preset.PrimaryAccent;
                Color bodyTint = preset.ExplorerBodyTint;
                Color shadowGlow = preset.ExplorerShadowGlow;
                Color rim = preset.RimHighlight;

                if (string.Equals(preset.Id, "FactoryDark", StringComparison.OrdinalIgnoreCase))
                {
                    primary = Color.FromArgb(46, 56, 68);
                    bodyTint = Color.FromArgb(22, 28, 36);
                    shadowGlow = Color.FromArgb(90, 128, 168);
                    rim = Color.FromArgb(148, 178, 208);
                }

                // 1. Soft multi-layer drop shadow underneath folder body
                for (int layer = 7; layer >= 1; layer--)
                {
                    float spread = layer * 3.4f;
                    int alpha = 6 + (8 - layer) * 3; // subtle soft falloff
                    RectangleF shadowRect = new RectangleF(
                        38f - spread * 0.55f,
                        140f - spread * 0.25f + 10f,
                        436f + spread * 1.1f,
                        286f + spread * 0.95f);

                    using (GraphicsPath shadowPath = CreateRoundedRectF(shadowRect, 40f + spread * 0.35f))
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                // 2. Back Folder Tab (PrimaryAccent darkened gradient)
                Color tabTop = MixColors(EnsureMinBrightness(primary, 0.24f), rim, 0.16f);
                Color tabBottom = ScaleBrightness(primary, 0.52f);
                using (GraphicsPath backTabPath = CreateBackFolderTabPath())
                using (LinearGradientBrush tabBrush = new LinearGradientBrush(
                    new RectangleF(36f, 68f, 440f, 150f),
                    tabTop,
                    tabBottom,
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(tabBrush, backTabPath);
                }

                // 3. Subtle Inner Sheet peeking above front folder body
                RectangleF sheetRect = new RectangleF(54f, 112f, 404f, 56f);
                Color sheetTop = MixColors(EnsureMinBrightness(primary, 0.30f), rim, 0.30f);
                Color sheetBot = ScaleBrightness(primary, 0.65f);
                using (GraphicsPath sheetPath = CreateRoundedRectF(sheetRect, 20f))
                using (LinearGradientBrush sheetBrush = new LinearGradientBrush(
                    sheetRect,
                    Color.FromArgb(235, sheetTop),
                    Color.FromArgb(235, sheetBot),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(sheetBrush, sheetPath);
                    using (Pen sheetRimPen = new Pen(Color.FromArgb(75, EnsureMinBrightness(rim, 0.65f)), 1.6f))
                    {
                        g.DrawPath(sheetRimPen, sheetPath);
                    }
                }

                // 4. Front Rounded Folder Body (ExplorerBodyTint -> deep dark obsidian gradient)
                RectangleF frontRect = new RectangleF(36f, 132f, 440f, 292f);
                Color frontTop = MixColors(Color.FromArgb(14, 20, 18), EnsureMinBrightness(bodyTint, 0.14f), 0.78f);
                Color frontBottom = MixColors(Color.FromArgb(7, 10, 9), ScaleBrightness(bodyTint, 0.45f), 0.55f);

                using (GraphicsPath frontPath = CreateRoundedRectF(frontRect, 38f))
                {
                    using (LinearGradientBrush frontBrush = new LinearGradientBrush(
                        frontRect,
                        frontTop,
                        frontBottom,
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(frontBrush, frontPath);
                    }

                    // 5. Radial Glow in ExplorerShadowGlow behind center glyph (smooth Gaussian falloff)
                    g.SetClip(frontPath);
                    RectangleF glowBounds = new RectangleF(16f, 108f, 480f, 340f);
                    using (GraphicsPath glowPath = new GraphicsPath())
                    {
                        glowPath.AddEllipse(glowBounds);
                        using (PathGradientBrush pgb = new PathGradientBrush(glowPath))
                        {
                            Color vividGlow = BoostGlowColor(shadowGlow, rim);
                            pgb.CenterPoint = new PointF(256f, 278f);

                            ColorBlend cb = new ColorBlend(4);
                            cb.Positions = new float[] { 0.0f, 0.28f, 0.62f, 1.0f };
                            cb.Colors = new Color[]
                            {
                                Color.FromArgb(0, vividGlow.R, vividGlow.G, vividGlow.B),
                                Color.FromArgb(12, vividGlow.R, vividGlow.G, vividGlow.B),
                                Color.FromArgb(52, vividGlow.R, vividGlow.G, vividGlow.B),
                                Color.FromArgb(115, vividGlow.R, vividGlow.G, vividGlow.B)
                            };
                            pgb.InterpolationColors = cb;
                            g.FillPath(pgb, glowPath);
                        }
                    }
                    g.ResetClip();
                }

                // 6. Crisp 3px+ Rim Border blending RimHighlight -> PrimaryAccent
                RectangleF rimRect = new RectangleF(37.75f, 133.75f, 436.5f, 288.5f);
                Color rimStart = EnsureMinBrightness(rim, 0.58f);
                Color rimEnd = MixColors(rimStart, EnsureMinBrightness(primary, 0.34f), 0.52f);
                using (GraphicsPath rimPath = CreateRoundedRectF(rimRect, 36.5f))
                using (LinearGradientBrush rimBrush = new LinearGradientBrush(
                    rimRect,
                    Color.FromArgb(245, rimStart),
                    Color.FromArgb(225, rimEnd),
                    LinearGradientMode.Vertical))
                using (Pen rimPen = new Pen(rimBrush, 3.5f))
                {
                    rimPen.LineJoin = LineJoin.Round;
                    g.DrawPath(rimPen, rimPath);
                }

                // 7. Crisp Center Vector Glyph
                Color glyphLight = MixColors(Color.FromArgb(242, 246, 243), rimStart, 0.10f);
                Color glyphVivid = EnsureMinBrightness(MixColors(rim, shadowGlow, 0.42f), 0.66f);
                Color glyphSecondary = MixColors(EnsureMinBrightness(rim, 0.72f), Color.FromArgb(236, 208, 148), 0.46f);
                Color occludeDark = MixColors(frontTop, frontBottom, 0.5f);

                DrawCenterGlyph(g, canonicalKind, glyphLight, glyphVivid, glyphSecondary, occludeDark);
            }

            return bmp;
        }

        private static void DrawCenterGlyph(
            Graphics g,
            string kind,
            Color glyphLight,
            Color glyphVivid,
            Color glyphSecondary,
            Color occludeDark)
        {
            if (string.Equals(kind, "01-Kisisel", StringComparison.OrdinalIgnoreCase))
            {
                // Person head + curved shoulders + accent dot
                using (Pen lightPen = CreateRoundPen(glyphLight, 15f))
                {
                    g.DrawEllipse(lightPen, 224f, 210f, 64f, 64f);
                    g.DrawArc(lightPen, 192f, 292f, 128f, 86f, 196f, 148f);
                }
                using (SolidBrush dotBrush = new SolidBrush(glyphSecondary))
                {
                    g.FillEllipse(dotBrush, 297f, 206f, 19f, 19f);
                }
            }
            else if (string.Equals(kind, "02-Varliklar", StringComparison.OrdinalIgnoreCase))
            {
                // Back rounded media frame in vivid accent
                RectangleF backFrame = new RectangleF(238f, 212f, 92f, 92f);
                using (GraphicsPath backPath = CreateRoundedRectF(backFrame, 16f))
                using (Pen vividPen = CreateRoundPen(glyphVivid, 13f))
                {
                    g.DrawPath(vividPen, backPath);
                }

                // Front rounded media frame occluding back frame + filled mountains inside
                RectangleF frontFrame = new RectangleF(192f, 236f, 98f, 98f);
                using (GraphicsPath frontPath = CreateRoundedRectF(frontFrame, 16f))
                {
                    using (SolidBrush darkFill = new SolidBrush(occludeDark))
                    {
                        g.FillPath(darkFill, frontPath);
                    }

                    PointF[] mountain = new PointF[]
                    {
                        new PointF(204f, 320f),
                        new PointF(231f, 282f),
                        new PointF(252f, 305f),
                        new PointF(266f, 290f),
                        new PointF(282f, 320f)
                    };
                    using (SolidBrush mountainBrush = new SolidBrush(glyphSecondary))
                    {
                        g.FillPolygon(mountainBrush, mountain);
                    }

                    using (Pen lightPen = CreateRoundPen(glyphLight, 13.5f))
                    {
                        g.DrawPath(lightPen, frontPath);
                    }
                }
            }
            else if (string.Equals(kind, "03-Arsiv", StringComparison.OrdinalIgnoreCase))
            {
                // Archive box: lower body + top lid + center slot handle
                RectangleF bodyBox = new RectangleF(196f, 252f, 120f, 82f);
                RectangleF lidBox = new RectangleF(184f, 222f, 144f, 30f);

                using (GraphicsPath bodyPath = CreateRoundedRectF(bodyBox, 12f))
                using (GraphicsPath lidPath = CreateRoundedRectF(lidBox, 9f))
                using (SolidBrush darkFill = new SolidBrush(occludeDark))
                using (Pen lightPen = CreateRoundPen(glyphLight, 13.5f))
                {
                    g.FillPath(darkFill, bodyPath);
                    g.DrawPath(lightPen, bodyPath);
                    g.FillPath(darkFill, lidPath);
                    g.DrawPath(lightPen, lidPath);
                }

                using (Pen slotPen = CreateRoundPen(glyphSecondary, 13.5f))
                {
                    g.DrawLine(slotPen, 238f, 286f, 274f, 286f);
                }
            }
            else if (string.Equals(kind, "Araclar", StringComparison.OrdinalIgnoreCase))
            {
                // </> Code brackets + accent slash
                using (Pen lightPen = CreateRoundPen(glyphLight, 15f))
                {
                    PointF[] leftChevron = new PointF[]
                    {
                        new PointF(218f, 244f),
                        new PointF(184f, 278f),
                        new PointF(218f, 312f)
                    };
                    PointF[] rightChevron = new PointF[]
                    {
                        new PointF(294f, 244f),
                        new PointF(328f, 278f),
                        new PointF(294f, 312f)
                    };
                    g.DrawLines(lightPen, leftChevron);
                    g.DrawLines(lightPen, rightChevron);
                }

                using (Pen slashPen = CreateRoundPen(glyphSecondary, 14.5f))
                {
                    g.DrawLine(slashPen, 268f, 232f, 244f, 324f);
                }
            }
            else if (string.Equals(kind, "Beyin", StringComparison.OrdinalIgnoreCase))
            {
                // 6-node neural graph (5 outer pentagon nodes + 1 central hub node)
                PointF center = new PointF(256f, 280f);
                PointF[] outer = new PointF[5];
                const float radius = 63f;

                for (int i = 0; i < 5; i++)
                {
                    double angleRad = (-90.0 + i * 72.0) * Math.PI / 180.0;
                    outer[i] = new PointF(
                        center.X + (float)(Math.Cos(angleRad) * radius),
                        center.Y + (float)(Math.Sin(angleRad) * radius));
                }

                using (Pen edgePen = CreateRoundPen(glyphVivid, 8.5f))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        PointF next = outer[(i + 1) % 5];
                        g.DrawLine(edgePen, outer[i], next);
                        g.DrawLine(edgePen, outer[i], center);
                    }
                }

                using (SolidBrush haloBrush = new SolidBrush(occludeDark))
                using (SolidBrush nodeLightBrush = new SolidBrush(glyphLight))
                using (SolidBrush hubBrush = new SolidBrush(glyphSecondary))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        g.FillEllipse(haloBrush, outer[i].X - 14.5f, outer[i].Y - 14.5f, 29f, 29f);
                        g.FillEllipse(nodeLightBrush, outer[i].X - 11.5f, outer[i].Y - 11.5f, 23f, 23f);
                    }

                    g.FillEllipse(haloBrush, center.X - 17.5f, center.Y - 17.5f, 35f, 35f);
                    g.FillEllipse(hubBrush, center.X - 14f, center.Y - 14f, 28f, 28f);
                }
            }
            else if (string.Equals(kind, "ComfyUI", StringComparison.OrdinalIgnoreCase))
            {
                // 3 connected workflow nodes
                using (Pen wirePen = CreateRoundPen(glyphVivid, 9.5f))
                {
                    PointF[] topWire = new PointF[]
                    {
                        new PointF(236f, 248f),
                        new PointF(260f, 248f),
                        new PointF(260f, 298f),
                        new PointF(280f, 298f)
                    };
                    PointF[] botWire = new PointF[]
                    {
                        new PointF(236f, 310f),
                        new PointF(260f, 310f),
                        new PointF(260f, 298f)
                    };
                    g.DrawLines(wirePen, topWire);
                    g.DrawLines(wirePen, botWire);
                }

                RectangleF nodeTopLeft = new RectangleF(184f, 224f, 54f, 48f);
                RectangleF nodeBotLeft = new RectangleF(184f, 286f, 54f, 48f);
                RectangleF nodeRight = new RectangleF(276f, 274f, 54f, 48f);

                using (GraphicsPath p1 = CreateRoundedRectF(nodeTopLeft, 12f))
                using (GraphicsPath p2 = CreateRoundedRectF(nodeBotLeft, 12f))
                using (GraphicsPath p3 = CreateRoundedRectF(nodeRight, 12f))
                using (SolidBrush lightBrush = new SolidBrush(glyphLight))
                using (SolidBrush secBrush = new SolidBrush(glyphSecondary))
                using (Pen topBorder = CreateRoundPen(glyphSecondary, 3.5f))
                {
                    g.FillPath(lightBrush, p1);
                    g.DrawPath(topBorder, p1);
                    g.FillPath(lightBrush, p2);
                    g.FillPath(secBrush, p3);
                }
            }
            else if (string.Equals(kind, "ComfyUIDocuments", StringComparison.OrdinalIgnoreCase))
            {
                // Document outline + 3 lines + bottom-right node badge
                RectangleF docRect = new RectangleF(208f, 216f, 88f, 118f);
                using (GraphicsPath docPath = CreateRoundedRectF(docRect, 14f))
                using (Pen lightPen = CreateRoundPen(glyphLight, 13f))
                {
                    g.DrawPath(lightPen, docPath);
                }

                using (Pen linePen = CreateRoundPen(glyphVivid, 10.5f))
                {
                    g.DrawLine(linePen, 230f, 248f, 274f, 248f);
                    g.DrawLine(linePen, 230f, 274f, 274f, 274f);
                    g.DrawLine(linePen, 230f, 300f, 258f, 300f);
                }

                using (SolidBrush haloBrush = new SolidBrush(occludeDark))
                using (SolidBrush badgeBrush = new SolidBrush(glyphSecondary))
                {
                    g.FillEllipse(haloBrush, 275f, 298f, 44f, 44f);
                    g.FillEllipse(badgeBrush, 279f, 302f, 36f, 36f);
                }
            }
            else
            {
                // Games: Rounded pill gamepad + D-pad + 3 action buttons
                RectangleF padRect = new RectangleF(178f, 242f, 156f, 72f);
                using (GraphicsPath padPath = CreateRoundedRectF(padRect, 36f))
                using (Pen lightPen = CreateRoundPen(glyphLight, 13.5f))
                {
                    g.DrawPath(lightPen, padPath);
                }

                using (Pen dpadPen = CreateRoundPen(glyphVivid, 10.5f))
                {
                    g.DrawLine(dpadPen, 204f, 278f, 232f, 278f);
                    g.DrawLine(dpadPen, 218f, 264f, 218f, 292f);
                }

                using (SolidBrush vividBrush = new SolidBrush(glyphVivid))
                using (SolidBrush lightBrush = new SolidBrush(glyphLight))
                using (SolidBrush secBrush = new SolidBrush(glyphSecondary))
                {
                    g.FillEllipse(vividBrush, 285f, 258f, 14f, 14f);
                    g.FillEllipse(lightBrush, 273f, 280f, 14f, 14f);
                    g.FillEllipse(secBrush, 295f, 280f, 15f, 15f);
                }
            }
        }

        // ====================================================================
        // Geometry & Color Helpers
        // ====================================================================

        private static Pen CreateRoundPen(Color color, float width)
        {
            Pen pen = new Pen(color, width);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            return pen;
        }

        private static GraphicsPath CreateBackFolderTabPath()
        {
            GraphicsPath path = new GraphicsPath();
            // Back folder spans x=36..476, top-left tab rises to y=68 across x=36..190, then steps smoothly to y=98 across x=212..476
            path.AddArc(36f, 68f, 44f, 44f, 180f, 90f);
            path.AddLine(58f, 68f, 174f, 68f);
            path.AddBezier(
                new PointF(174f, 68f),
                new PointF(194f, 68f),
                new PointF(196f, 98f),
                new PointF(216f, 98f));
            path.AddLine(216f, 98f, 448f, 98f);
            path.AddArc(428f, 98f, 48f, 48f, 270f, 90f);
            path.AddLine(476f, 122f, 476f, 210f);
            path.AddLine(476f, 210f, 36f, 210f);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateRoundedRectF(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0f || rect.Height <= 0f)
            {
                return path;
            }

            float maxRadius = Math.Min(rect.Width, rect.Height) * 0.5f;
            if (radius > maxRadius) radius = maxRadius;
            if (radius <= 0.5f)
            {
                path.AddRectangle(rect);
                return path;
            }

            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180f, 90f);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270f, 90f);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        private static Bitmap ResizeHighQuality(Bitmap source, int width, int height)
        {
            Bitmap target = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(target))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, width, height));
            }
            return target;
        }

        private static ThemePreset EnsurePreset(ThemePreset preset)
        {
            if (preset != null)
            {
                return preset;
            }
            return ThemeCatalog.GetPresetById("DoomEmerald");
        }

        private static string SanitizeFolderId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "DoomEmerald";
            }

            StringBuilder sb = new StringBuilder(id.Length);
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '-' || c == '_')
                {
                    sb.Append(c);
                }
                else if (c == ' ')
                {
                    sb.Append('_');
                }
            }

            return sb.Length > 0 ? sb.ToString() : "CustomTheme";
        }

        private static string NormalizeGlyphKind(string glyphKind)
        {
            if (string.IsNullOrEmpty(glyphKind))
            {
                return "Araclar";
            }

            for (int i = 0; i < FolderTypes.Length; i++)
            {
                if (string.Equals(FolderTypes[i], glyphKind, StringComparison.OrdinalIgnoreCase))
                {
                    return FolderTypes[i];
                }
            }

            string lower = glyphKind.ToLowerInvariant();
            if (lower.Contains("kisisel") || lower.Contains("personal") || lower.Contains("user") || lower.Contains("gorkem"))
                return "01-Kisisel";
            if (lower.Contains("varlik") || lower.Contains("asset") || lower.Contains("media") || lower.Contains("image"))
                return "02-Varliklar";
            if (lower.Contains("arsiv") || lower.Contains("archiv") || lower.Contains("box"))
                return "03-Arsiv";
            if (lower.Contains("arac") || lower.Contains("code") || lower.Contains("dev") || lower.Contains("tool"))
                return "Araclar";
            if (lower.Contains("beyin") || lower.Contains("brain") || lower.Contains("neural"))
                return "Beyin";
            if (lower.Contains("doc"))
                return "ComfyUIDocuments";
            if (lower.Contains("comfy") || lower.Contains("node") || lower.Contains("flow"))
                return "ComfyUI";
            if (lower.Contains("game") || lower.Contains("oyun"))
                return "Games";

            return "Araclar";
        }

        private static Color MixColors(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            int r = ClampByte((int)Math.Round(a.R + (b.R - a.R) * t));
            int g = ClampByte((int)Math.Round(a.G + (b.G - a.G) * t));
            int bl = ClampByte((int)Math.Round(a.B + (b.B - a.B) * t));
            return Color.FromArgb(255, r, g, bl);
        }

        private static Color ScaleBrightness(Color c, float factor)
        {
            int r = ClampByte((int)Math.Round(c.R * factor));
            int g = ClampByte((int)Math.Round(c.G * factor));
            int b = ClampByte((int)Math.Round(c.B * factor));
            return Color.FromArgb(255, r, g, b);
        }

        private static Color EnsureMinBrightness(Color c, float minPeak01)
        {
            int maxChannel = Math.Max(c.R, Math.Max(c.G, c.B));
            int targetMin = ClampByte((int)Math.Round(minPeak01 * 255f));
            if (maxChannel >= targetMin)
            {
                return Color.FromArgb(255, c.R, c.G, c.B);
            }

            if (maxChannel == 0)
            {
                return Color.FromArgb(255, targetMin, targetMin, targetMin);
            }

            float lift = (float)(targetMin - maxChannel) / 255f;
            return MixColors(c, Color.White, lift * 0.92f);
        }

        private static Color BoostGlowColor(Color shadowGlow, Color rim)
        {
            Color blended = MixColors(shadowGlow, rim, 0.25f);
            return EnsureMinBrightness(blended, 0.42f);
        }

        private static int ClampByte(int val)
        {
            if (val < 0) return 0;
            if (val > 255) return 255;
            return val;
        }
    }
}
