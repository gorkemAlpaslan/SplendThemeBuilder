using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DoomThemeSuite
{
    /// <summary>
    /// Core theme preset model for SPLEND // Windows Theme Studio v2.0.
    /// Holds all window, DWM, Explorer body/shadow, taskbar, UI surface, and icon colors.
    /// </summary>
    public sealed class ThemePreset
    {
        public string Id;
        public string DisplayName;
        public string Subtitle;
        public bool IsBuiltIn;
        public Color UiBackground;       // Studio window dark background
        public Color UiCard;             // Studio card surface
        public Color PrimaryAccent;      // DWM active titlebar, Start menu, desktop selection box
        public Color InactiveAccent;     // DWM inactive titlebar (prevents grey fallback)
        public Color RimHighlight;       // Window border rim & UI active border/accent
        public Color ExplorerBodyTint;   // File Explorer interior body tint RGB
        public Color ExplorerShadowGlow; // File Explorer 360° outer drop shadow RGB
        public Color TaskbarTint;        // Taskbar tint RGB (for tinted taskbar mode)
        public byte DefaultTintAlpha;    // 0..140
        public byte DefaultShadowAlpha;  // 0..255
        public int DefaultShadowSpread;  // 8..48 (default 28)
        public string WallpaperPath;     // Optional associated wallpaper path
        public bool IsFavorite;          // v3.0 gallery favorite flag
        public string Tags;              // v3.0 gallery filter/search tags
        public Color SecondaryAccent;    // v3.0 dual-radial ambient glow accent2

        public ThemePreset()
        {
            Id = "CustomTheme";
            DisplayName = "Özel Tema";
            Subtitle = "Özel renk profili";
            IsBuiltIn = false;
            UiBackground = Color.FromArgb(255, 11, 18, 14);
            UiCard = Color.FromArgb(255, 18, 29, 23);
            PrimaryAccent = Color.FromArgb(255, 31, 74, 49);
            InactiveAccent = Color.FromArgb(255, 23, 54, 36);
            RimHighlight = Color.FromArgb(255, 108, 168, 129);
            ExplorerBodyTint = Color.FromArgb(255, 14, 54, 32);
            ExplorerShadowGlow = Color.FromArgb(255, 38, 115, 71);
            TaskbarTint = Color.FromArgb(255, 12, 36, 22);
            DefaultTintAlpha = 72;
            DefaultShadowAlpha = 185;
            DefaultShadowSpread = 28;
            WallpaperPath = string.Empty;
            IsFavorite = false;
            Tags = "Dark, Cyber, Glass";
            SecondaryAccent = Color.FromArgb(255, 58, 134, 255);
        }

        public ThemePreset Clone()
        {
            ThemePreset copy = new ThemePreset();
            copy.Id = this.Id;
            copy.DisplayName = this.DisplayName;
            copy.Subtitle = this.Subtitle;
            copy.IsBuiltIn = this.IsBuiltIn;
            copy.UiBackground = this.UiBackground;
            copy.UiCard = this.UiCard;
            copy.PrimaryAccent = this.PrimaryAccent;
            copy.InactiveAccent = this.InactiveAccent;
            copy.RimHighlight = this.RimHighlight;
            copy.ExplorerBodyTint = this.ExplorerBodyTint;
            copy.ExplorerShadowGlow = this.ExplorerShadowGlow;
            copy.TaskbarTint = this.TaskbarTint;
            copy.DefaultTintAlpha = this.DefaultTintAlpha;
            copy.DefaultShadowAlpha = this.DefaultShadowAlpha;
            copy.DefaultShadowSpread = this.DefaultShadowSpread;
            copy.WallpaperPath = this.WallpaperPath ?? string.Empty;
            copy.IsFavorite = this.IsFavorite;
            copy.Tags = this.Tags ?? string.Empty;
            copy.SecondaryAccent = this.SecondaryAccent;
            return copy;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(DisplayName))
            {
                return DisplayName;
            }
            if (!string.IsNullOrEmpty(Id))
            {
                return Id;
            }
            return "ThemePreset";
        }
    }

    /// <summary>
    /// Built-in & custom theme preset catalog, INI persistence, readable AccentPalette builder,
    /// and Win32/DWM color conversion utilities.
    /// </summary>
    public static class ThemeCatalog
    {
        public static readonly string CustomThemesPath = @"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomThemeSuite\custom_themes.ini";

        private static readonly string DefaultDoomWallpaper = @"C:\Users\gorke\Documents\wallpapers\doctor-doom-5120x2194-26922.jpg";
        private static readonly object SyncRoot = new object();

        public static List<ThemePreset> GetBuiltInPresets()
        {
            List<ThemePreset> list = new List<ThemePreset>(10);

            // 1) DoomEmerald — Doctor Doom Emerald
            list.Add(CreateBuiltIn(
                "DoomEmerald",
                "Doctor Doom Emerald",
                "Zümrüt zırh • Derin obsidyen atmosfer",
                "#0B120E", // UiBackground
                "#121D17", // UiCard
                "#1F4A31", // PrimaryAccent
                "#173624", // InactiveAccent
                "#6CA881", // RimHighlight
                "#0E3620", // ExplorerBodyTint
                "#267347", // ExplorerShadowGlow
                "#0C2416", // TaskbarTint
                72,
                185,
                28,
                DefaultDoomWallpaper));

            // 2) ObsidianStealth — Obsidian Stealth
            list.Add(CreateBuiltIn(
                "ObsidianStealth",
                "Obsidian Stealth",
                "Karbon siyah • Ultra düşük dikkat dağıtma",
                "#090C0B", // UiBackground
                "#111614", // UiCard
                "#121815", // PrimaryAccent
                "#0D1210", // InactiveAccent
                "#8FA895", // RimHighlight
                "#0D1210", // ExplorerBodyTint
                "#3A4D42", // ExplorerShadowGlow
                "#0A0E0C", // TaskbarTint
                56,
                165,
                24,
                string.Empty));

            // 3) LatveriaGold — Latveria Royal Gold
            list.Add(CreateBuiltIn(
                "LatveriaGold",
                "Latveria Royal Gold",
                "Koyu bronz-zümrüt • Kraliyet altın yaldız",
                "#11100B", // UiBackground
                "#1B1911", // UiCard
                "#3A3520", // PrimaryAccent
                "#282518", // InactiveAccent
                "#E6CF93", // RimHighlight
                "#262212", // ExplorerBodyTint
                "#BFA66A", // ExplorerShadowGlow
                "#1F1C10", // TaskbarTint
                68,
                180,
                28,
                string.Empty));

            // 4) CyberpunkCrimson — Cyberpunk Crimson
            list.Add(CreateBuiltIn(
                "CyberpunkCrimson",
                "Cyberpunk Crimson",
                "Kızıl obsidyen • Neon yakut keskinliği",
                "#12080B", // UiBackground
                "#1D0E13", // UiCard
                "#4A121E", // PrimaryAccent
                "#330D16", // InactiveAccent
                "#FF5C77", // RimHighlight
                "#2E0A12", // ExplorerBodyTint
                "#E63956", // ExplorerShadowGlow
                "#240910", // TaskbarTint
                70,
                190,
                28,
                string.Empty));

            // 5) MidnightSapphire — Midnight Sapphire
            list.Add(CreateBuiltIn(
                "MidnightSapphire",
                "Midnight Sapphire",
                "Derin gece mavisi • Siber safir ışıltı",
                "#08101C", // UiBackground
                "#101B2D", // UiCard
                "#122B4A", // PrimaryAccent
                "#0D1F36", // InactiveAccent
                "#64DFDF", // RimHighlight
                "#0B1D36", // ExplorerBodyTint
                "#3A86FF", // ExplorerShadowGlow
                "#0A182E", // TaskbarTint
                70,
                185,
                28,
                string.Empty));

            // 6) ArcaneAmethyst — Arcane Amethyst
            list.Add(CreateBuiltIn(
                "ArcaneAmethyst",
                "Arcane Amethyst",
                "Siber mor • Mistik ametist aura",
                "#0E0817", // UiBackground
                "#180F26", // UiCard
                "#2D164A", // PrimaryAccent
                "#201036", // InactiveAccent
                "#C77DFF", // RimHighlight
                "#1D0D33", // ExplorerBodyTint
                "#9D4EDD", // ExplorerShadowGlow
                "#190B2B", // TaskbarTint
                72,
                190,
                30,
                string.Empty));

            // 7) SolarAmber — Solar Amber
            list.Add(CreateBuiltIn(
                "SolarAmber",
                "Solar Amber",
                "Volkanik kehribar • Sıcak plazma parıltısı",
                "#130B06", // UiBackground
                "#1F130B", // UiCard
                "#4A2810", // PrimaryAccent
                "#361D0C", // InactiveAccent
                "#E9C46A", // RimHighlight
                "#301808", // ExplorerBodyTint
                "#F4A261", // ExplorerShadowGlow
                "#261307", // TaskbarTint
                68,
                180,
                28,
                string.Empty));

            // 8) ArcticNord — Arctic Nord Frost
            list.Add(CreateBuiltIn(
                "ArcticNord",
                "Arctic Nord Frost",
                "Kuzey arduvaz • Buzul cam berraklığı",
                "#0B1217", // UiBackground
                "#131E26", // UiCard
                "#1E3240", // PrimaryAccent
                "#162530", // InactiveAccent
                "#88C0D0", // RimHighlight
                "#14232E", // ExplorerBodyTint
                "#5E81AC", // ExplorerShadowGlow
                "#101D26", // TaskbarTint
                64,
                175,
                26,
                string.Empty));

            // 9) MatrixPhosphor — Matrix Phosphor
            list.Add(CreateBuiltIn(
                "MatrixPhosphor",
                "Matrix Phosphor",
                "Terminal siyah • Fosforlu CRT yeşili",
                "#050D07", // UiBackground
                "#0B180E", // UiCard
                "#0A2912", // PrimaryAccent
                "#071D0D", // InactiveAccent
                "#39FF88", // RimHighlight
                "#061F0C", // ExplorerBodyTint
                "#00E65C", // ExplorerShadowGlow
                "#051709", // TaskbarTint
                76,
                195,
                28,
                string.Empty));

            // 10) FactoryDark — Fabrika Koyu (Sıfırla)
            list.Add(CreateBuiltIn(
                "FactoryDark",
                "Fabrika Koyu (Sıfırla)",
                "Standart Windows 10 koyu mod varsayılanları",
                "#121212", // UiBackground
                "#1E1E1E", // UiCard
                "#1F1F1F", // PrimaryAccent
                "#2B2B2B", // InactiveAccent
                "#7A7A7A", // RimHighlight
                "#191919", // ExplorerBodyTint
                "#000000", // ExplorerShadowGlow
                "#101010", // TaskbarTint
                0,
                0,
                28,
                string.Empty));

            return list;
        }

        public static List<ThemePreset> GetCustomPresets()
        {
            lock (SyncRoot)
            {
                return ReadCustomPresetsUnlocked();
            }
        }

        public static List<ThemePreset> GetAllPresets()
        {
            List<ThemePreset> builtIns = GetBuiltInPresets();
            List<ThemePreset> customs = GetCustomPresets();

            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<ThemePreset> combined = new List<ThemePreset>(builtIns.Count + customs.Count);

            for (int i = 0; i < builtIns.Count; i++)
            {
                seenIds.Add(builtIns[i].Id);
                combined.Add(builtIns[i]);
            }

            for (int i = 0; i < customs.Count; i++)
            {
                ThemePreset c = customs[i];
                if (c != null && !string.IsNullOrEmpty(c.Id) && !seenIds.Contains(c.Id))
                {
                    seenIds.Add(c.Id);
                    combined.Add(c);
                }
            }

            return combined;
        }

        public static ThemePreset GetPresetById(string id)
        {
            List<ThemePreset> all = GetAllPresets();
            if (!string.IsNullOrEmpty(id))
            {
                string trimmed = id.Trim();
                for (int i = 0; i < all.Count; i++)
                {
                    if (string.Equals(all[i].Id, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        return all[i].Clone();
                    }
                }
            }

            // Fallback to DoomEmerald
            return GetBuiltInPresets()[0].Clone();
        }

        public static void SaveCustomPreset(ThemePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                ThemePreset toSave = preset.Clone();
                toSave.IsBuiltIn = false;

                if (string.IsNullOrEmpty(toSave.DisplayName) || toSave.DisplayName.Trim().Length == 0)
                {
                    toSave.DisplayName = "Özel Tema";
                }
                else
                {
                    toSave.DisplayName = CleanIniText(toSave.DisplayName.Trim());
                }

                if (string.IsNullOrEmpty(toSave.Subtitle))
                {
                    toSave.Subtitle = "Kullanıcı tanımlı özel tema";
                }
                else
                {
                    toSave.Subtitle = CleanIniText(toSave.Subtitle.Trim());
                }

                string cleanId = SanitizePresetId(toSave.Id);
                if (string.IsNullOrEmpty(cleanId))
                {
                    cleanId = SanitizePresetId(toSave.DisplayName);
                }
                if (string.IsNullOrEmpty(cleanId))
                {
                    cleanId = "Custom_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                }

                if (IsBuiltInPresetId(cleanId))
                {
                    cleanId = cleanId + "_Custom";
                }

                toSave.Id = cleanId;
                toSave.DefaultTintAlpha = ClampByte(toSave.DefaultTintAlpha, 0, 140);
                toSave.DefaultShadowAlpha = ClampByte(toSave.DefaultShadowAlpha, 0, 255);
                toSave.DefaultShadowSpread = ClampInt(toSave.DefaultShadowSpread, 8, 48);
                toSave.WallpaperPath = CleanIniText(toSave.WallpaperPath ?? string.Empty);

                // Reflect normalized ID and built-in status back to caller's instance
                preset.Id = toSave.Id;
                preset.IsBuiltIn = false;

                List<ThemePreset> existing = ReadCustomPresetsUnlocked();
                bool replaced = false;
                for (int i = 0; i < existing.Count; i++)
                {
                    if (string.Equals(existing[i].Id, toSave.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        existing[i] = toSave;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    existing.Add(toSave);
                }

                WriteCustomPresetsUnlocked(existing);
            }
        }

        public static bool DeleteCustomPreset(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            string target = id.Trim();
            if (target.Length == 0 || IsBuiltInPresetId(target))
            {
                return false;
            }

            lock (SyncRoot)
            {
                List<ThemePreset> existing = ReadCustomPresetsUnlocked();
                int removedCount = 0;
                for (int i = existing.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(existing[i].Id, target, StringComparison.OrdinalIgnoreCase))
                    {
                        existing.RemoveAt(i);
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    WriteCustomPresetsUnlocked(existing);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Builds a 32-byte (8 x RGBA) Windows 10 Explorer AccentPalette.
        /// Slots [0..2] are guaranteed bright ice/mint/pastel-tinted colors so HDR/Dark Mode accented text is 100% legible.
        /// Slots [3..7] step smoothly from lifted primaryAccent through exact primaryAccent down to deep obsidian.
        /// </summary>
        public static byte[] BuildReadableAccentPalette(Color primaryAccent, Color rimHighlight)
        {
            double rimH, rimS, rimL;
            RgbToHsl(rimHighlight, out rimH, out rimS, out rimL);

            double priH, priS, priL;
            RgbToHsl(primaryAccent, out priH, out priS, out priL);

            // Pick tint hue for bright slots [0..2] from rimHighlight if chromatic, else primaryAccent
            bool isChromatic = (rimS >= 0.08) || (priS >= 0.08);
            double lightHue = (rimS >= 0.08) ? rimH : priH;
            double baseLightSat = Math.Max(rimS, priS * 0.85);

            Color slot0;
            Color slot1;
            Color slot2;

            if (!isChromatic)
            {
                // Crisp neutral ice-silver for monochrome / FactoryDark themes
                slot0 = Color.FromArgb(255, 236, 240, 242);
                slot1 = Color.FromArgb(255, 210, 216, 220);
                slot2 = Color.FromArgb(255, 180, 188, 194);
            }
            else
            {
                double s0 = ClampDouble(baseLightSat * 0.65, 0.32, 0.72);
                double s1 = ClampDouble(baseLightSat * 0.75, 0.36, 0.78);
                double s2 = ClampDouble(baseLightSat * 0.85, 0.40, 0.82);

                slot0 = EnsureMinPerceivedLuminance(HslToRgb(lightHue, s0, 0.91), 224);
                slot1 = EnsureMinPerceivedLuminance(HslToRgb(lightHue, s1, 0.82), 198);
                slot2 = EnsureMinPerceivedLuminance(HslToRgb(lightHue, s2, 0.72), 170);
            }

            // Slots [3..7]: Smooth ramp through primaryAccent down to deep obsidian
            // Slot [3]: slightly lifted primary accent
            double liftedL = ClampDouble(Math.Max(priL + 0.07, priL * 1.28), 0.14, 0.42);
            double liftedS = isChromatic ? ClampDouble(priS * 1.05, 0.08, 0.85) : priS;
            Color slot3 = HslToRgb(priH, liftedS, liftedL);

            // Slot [4]: exact primary accent (Windows 10 main surface accent)
            Color slot4 = Color.FromArgb(255, primaryAccent.R, primaryAccent.G, primaryAccent.B);

            // Slot [5..7]: progressive darkening toward deep obsidian while preserving subtle hue undertone
            Color slot5 = ScaleColorToDark(primaryAccent, 0.74, 6);
            Color slot6 = ScaleColorToDark(primaryAccent, 0.52, 4);
            Color slot7 = ScaleColorToDark(primaryAccent, 0.32, 2);

            Color[] slots = new Color[] { slot0, slot1, slot2, slot3, slot4, slot5, slot6, slot7 };
            byte[] palette = new byte[32];
            for (int i = 0; i < 8; i++)
            {
                int offset = i * 4;
                palette[offset + 0] = slots[i].R;
                palette[offset + 1] = slots[i].G;
                palette[offset + 2] = slots[i].B;
                palette[offset + 3] = 0x00;
            }

            return palette;
        }

        /// <summary>
        /// Converts a System.Drawing.Color into Win32/DWM ABGR DWORD (0xAABBGGRR).
        /// </summary>
        public static uint ColorToAbgrDword(Color c, byte alpha = 255)
        {
            return ((uint)alpha << 24)
                 | ((uint)c.B << 16)
                 | ((uint)c.G << 8)
                 | (uint)c.R;
        }

        /// <summary>
        /// Converts a System.Drawing.Color into DWM Colorization ARGB DWORD (0xAARRGGBB).
        /// </summary>
        public static uint ColorToArgbDword(Color c, byte alpha = 0xC4)
        {
            return ((uint)alpha << 24)
                 | ((uint)c.R << 16)
                 | ((uint)c.G << 8)
                 | (uint)c.B;
        }

        /// <summary>
        /// Formats a Color as uppercase #RRGGBB hex string.
        /// </summary>
        public static string ColorToHex(Color c)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        /// <summary>
        /// Parses #RRGGBB, RRGGBB, #AARRGGBB, 0xRRGGBB, or #RGB safely without throwing.
        /// </summary>
        public static Color HexToColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return fallback;
            }

            string s = hex.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal))
            {
                s = s.Substring(1);
            }
            else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(2);
            }

            s = s.Trim();
            uint parsed;
            if (s.Length == 6)
            {
                if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
                {
                    int r = (int)((parsed >> 16) & 0xFF);
                    int g = (int)((parsed >> 8) & 0xFF);
                    int b = (int)(parsed & 0xFF);
                    return Color.FromArgb(255, r, g, b);
                }
            }
            else if (s.Length == 8)
            {
                if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
                {
                    int a = (int)((parsed >> 24) & 0xFF);
                    int r = (int)((parsed >> 16) & 0xFF);
                    int g = (int)((parsed >> 8) & 0xFF);
                    int b = (int)(parsed & 0xFF);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            else if (s.Length == 3)
            {
                if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
                {
                    int r = (int)(((parsed >> 8) & 0xF) * 17);
                    int g = (int)(((parsed >> 4) & 0xF) * 17);
                    int b = (int)((parsed & 0xF) * 17);
                    return Color.FromArgb(255, r, g, b);
                }
            }

            return fallback;
        }

        #region Internal Helpers

        private static ThemePreset CreateBuiltIn(
            string id,
            string displayName,
            string subtitle,
            string uiBgHex,
            string uiCardHex,
            string primaryHex,
            string inactiveHex,
            string rimHex,
            string bodyTintHex,
            string shadowGlowHex,
            string taskbarTintHex,
            byte tintAlpha,
            byte shadowAlpha,
            int shadowSpread,
            string wallpaperPath)
        {
            ThemePreset p = new ThemePreset();
            p.Id = id;
            p.DisplayName = displayName;
            p.Subtitle = subtitle;
            p.IsBuiltIn = true;
            p.UiBackground = HexToColor(uiBgHex, Color.FromArgb(255, 11, 18, 14));
            p.UiCard = HexToColor(uiCardHex, Color.FromArgb(255, 18, 29, 23));
            p.PrimaryAccent = HexToColor(primaryHex, Color.FromArgb(255, 31, 74, 49));
            p.InactiveAccent = HexToColor(inactiveHex, Color.FromArgb(255, 23, 54, 36));
            p.RimHighlight = HexToColor(rimHex, Color.FromArgb(255, 108, 168, 129));
            p.ExplorerBodyTint = HexToColor(bodyTintHex, Color.FromArgb(255, 14, 54, 32));
            p.ExplorerShadowGlow = HexToColor(shadowGlowHex, Color.FromArgb(255, 38, 115, 71));
            p.TaskbarTint = HexToColor(taskbarTintHex, Color.FromArgb(255, 12, 36, 22));
            p.DefaultTintAlpha = tintAlpha;
            p.DefaultShadowAlpha = shadowAlpha;
            p.DefaultShadowSpread = shadowSpread;
            p.WallpaperPath = wallpaperPath ?? string.Empty;
            return p;
        }

        private static bool IsBuiltInPresetId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            List<ThemePreset> builtIns = GetBuiltInPresets();
            for (int i = 0; i < builtIns.Count; i++)
            {
                if (string.Equals(builtIns[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        internal static string SanitizePresetId(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];
                switch (ch)
                {
                    case 'ğ': sb.Append('g'); continue;
                    case 'Ğ': sb.Append('G'); continue;
                    case 'ü': sb.Append('u'); continue;
                    case 'Ü': sb.Append('U'); continue;
                    case 'ş': sb.Append('s'); continue;
                    case 'Ş': sb.Append('S'); continue;
                    case 'ı': sb.Append('i'); continue;
                    case 'İ': sb.Append('I'); continue;
                    case 'ö': sb.Append('o'); continue;
                    case 'Ö': sb.Append('O'); continue;
                    case 'ç': sb.Append('c'); continue;
                    case 'Ç': sb.Append('C'); continue;
                }

                if ((ch >= 'a' && ch <= 'z') ||
                    (ch >= 'A' && ch <= 'Z') ||
                    (ch >= '0' && ch <= '9') ||
                    ch == '_' || ch == '-')
                {
                    sb.Append(ch);
                }
                else if (ch == ' ' || ch == '.')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    {
                        sb.Append('_');
                    }
                }
            }

            string cleaned = sb.ToString().Trim('_', '-');
            if (cleaned.Length > 48)
            {
                cleaned = cleaned.Substring(0, 48).TrimEnd('_', '-');
            }
            return cleaned;
        }

        private static string CleanIniText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            return text.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static List<ThemePreset> ReadCustomPresetsUnlocked()
        {
            List<ThemePreset> result = new List<ThemePreset>();
            try
            {
                if (!File.Exists(CustomThemesPath))
                {
                    return result;
                }

                string[] lines = File.ReadAllLines(CustomThemesPath, Encoding.UTF8);
                Dictionary<string, string> currentSection = null;
                string currentHeaderId = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                    {
                        if (currentSection != null && currentSection.Count > 0)
                        {
                            ThemePreset parsed = BuildPresetFromSection(currentHeaderId, currentSection);
                            if (parsed != null)
                            {
                                result.Add(parsed);
                            }
                        }

                        string header = line.Substring(1, line.Length - 2).Trim();
                        if (header.StartsWith("Preset:", StringComparison.OrdinalIgnoreCase))
                        {
                            header = header.Substring(7).Trim();
                        }
                        currentHeaderId = header;
                        currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        continue;
                    }

                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        if (currentSection == null)
                        {
                            currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        string k = line.Substring(0, eq).Trim();
                        string v = line.Substring(eq + 1).Trim();
                        if (k.Length > 0)
                        {
                            currentSection[k] = v;
                        }
                    }
                }

                if (currentSection != null && currentSection.Count > 0)
                {
                    ThemePreset last = BuildPresetFromSection(currentHeaderId, currentSection);
                    if (last != null)
                    {
                        result.Add(last);
                    }
                }
            }
            catch
            {
                // Return safely parsed presets so far on any I/O issue
            }

            return result;
        }

        private static ThemePreset BuildPresetFromSection(string headerId, Dictionary<string, string> map)
        {
            ThemePreset p = new ThemePreset();
            p.IsBuiltIn = false;

            string rawId = GetMapVal(map, "Id", headerId);
            p.Id = SanitizePresetId(rawId);
            if (string.IsNullOrEmpty(p.Id))
            {
                return null;
            }

            p.DisplayName = GetMapVal(map, "DisplayName", p.Id);
            p.Subtitle = GetMapVal(map, "Subtitle", "Özel renk profili");
            p.UiBackground = HexToColor(GetMapVal(map, "UiBackground", null), p.UiBackground);
            p.UiCard = HexToColor(GetMapVal(map, "UiCard", null), p.UiCard);
            p.PrimaryAccent = HexToColor(GetMapVal(map, "PrimaryAccent", null), p.PrimaryAccent);
            p.InactiveAccent = HexToColor(GetMapVal(map, "InactiveAccent", null), ScaleColorToDark(p.PrimaryAccent, 0.72, 10));
            p.RimHighlight = HexToColor(GetMapVal(map, "RimHighlight", null), p.RimHighlight);
            p.ExplorerBodyTint = HexToColor(GetMapVal(map, "ExplorerBodyTint", null), p.ExplorerBodyTint);
            p.ExplorerShadowGlow = HexToColor(GetMapVal(map, "ExplorerShadowGlow", null), p.ExplorerShadowGlow);
            p.TaskbarTint = HexToColor(GetMapVal(map, "TaskbarTint", null), p.TaskbarTint);

            int tintAlpha;
            if (int.TryParse(GetMapVal(map, "DefaultTintAlpha", "72"), NumberStyles.Integer, CultureInfo.InvariantCulture, out tintAlpha))
            {
                p.DefaultTintAlpha = ClampByte(tintAlpha, 0, 140);
            }

            int shadowAlpha;
            if (int.TryParse(GetMapVal(map, "DefaultShadowAlpha", "185"), NumberStyles.Integer, CultureInfo.InvariantCulture, out shadowAlpha))
            {
                p.DefaultShadowAlpha = ClampByte(shadowAlpha, 0, 255);
            }

            int spread;
            if (int.TryParse(GetMapVal(map, "DefaultShadowSpread", "28"), NumberStyles.Integer, CultureInfo.InvariantCulture, out spread))
            {
                p.DefaultShadowSpread = ClampInt(spread, 8, 48);
            }

            p.WallpaperPath = GetMapVal(map, "WallpaperPath", string.Empty);
            return p;
        }

        private static string GetMapVal(Dictionary<string, string> map, string key, string fallback)
        {
            string val;
            if (map != null && map.TryGetValue(key, out val) && val != null)
            {
                return val;
            }
            return fallback;
        }

        private static void WriteCustomPresetsUnlocked(List<ThemePreset> presets)
        {
            try
            {
                string dir = Path.GetDirectoryName(CustomThemesPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# SPLEND // Windows Theme Studio v2.0 - Custom Theme Presets");

                if (presets != null)
                {
                    for (int i = 0; i < presets.Count; i++)
                    {
                        ThemePreset p = presets[i];
                        if (p == null || string.IsNullOrEmpty(p.Id))
                        {
                            continue;
                        }

                        sb.AppendLine();
                        sb.AppendLine("[Preset:" + p.Id + "]");
                        sb.AppendLine("Id=" + p.Id);
                        sb.AppendLine("DisplayName=" + CleanIniText(p.DisplayName ?? p.Id));
                        sb.AppendLine("Subtitle=" + CleanIniText(p.Subtitle ?? string.Empty));
                        sb.AppendLine("UiBackground=" + ColorToHex(p.UiBackground));
                        sb.AppendLine("UiCard=" + ColorToHex(p.UiCard));
                        sb.AppendLine("PrimaryAccent=" + ColorToHex(p.PrimaryAccent));
                        sb.AppendLine("InactiveAccent=" + ColorToHex(p.InactiveAccent));
                        sb.AppendLine("RimHighlight=" + ColorToHex(p.RimHighlight));
                        sb.AppendLine("ExplorerBodyTint=" + ColorToHex(p.ExplorerBodyTint));
                        sb.AppendLine("ExplorerShadowGlow=" + ColorToHex(p.ExplorerShadowGlow));
                        sb.AppendLine("TaskbarTint=" + ColorToHex(p.TaskbarTint));
                        sb.AppendLine("DefaultTintAlpha=" + p.DefaultTintAlpha.ToString(CultureInfo.InvariantCulture));
                        sb.AppendLine("DefaultShadowAlpha=" + p.DefaultShadowAlpha.ToString(CultureInfo.InvariantCulture));
                        sb.AppendLine("DefaultShadowSpread=" + ClampInt(p.DefaultShadowSpread, 8, 48).ToString(CultureInfo.InvariantCulture));
                        sb.AppendLine("WallpaperPath=" + CleanIniText(p.WallpaperPath ?? string.Empty));
                    }
                }

                File.WriteAllText(CustomThemesPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Avoid throwing on locked disk errors
            }
        }

        private static Color ScaleColorToDark(Color c, double factor, int minChannelFloor)
        {
            int r = ClampInt((int)Math.Round(c.R * factor), minChannelFloor, 255);
            int g = ClampInt((int)Math.Round(c.G * factor), minChannelFloor, 255);
            int b = ClampInt((int)Math.Round(c.B * factor), minChannelFloor, 255);
            return Color.FromArgb(255, r, g, b);
        }

        private static Color EnsureMinPerceivedLuminance(Color c, int minLum)
        {
            double lum = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            if (lum >= minLum)
            {
                return c;
            }

            // Blend smoothly toward pure white until target perceived luminance is met
            double deficit = (minLum - lum) / Math.Max(1.0, 255.0 - lum);
            deficit = ClampDouble(deficit, 0.0, 1.0);
            int r = ClampInt((int)Math.Round(c.R + (255 - c.R) * deficit), 0, 255);
            int g = ClampInt((int)Math.Round(c.G + (255 - c.G) * deficit), 0, 255);
            int b = ClampInt((int)Math.Round(c.B + (255 - c.B) * deficit), 0, 255);
            return Color.FromArgb(255, r, g, b);
        }

        internal static void RgbToHsl(Color c, out double h, out double s, out double l)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            l = (max + min) * 0.5;

            double delta = max - min;
            if (delta < 0.00001)
            {
                h = 0.0;
                s = 0.0;
                return;
            }

            s = (l > 0.5) ? (delta / (2.0 - max - min)) : (delta / (max + min));

            if (max == r)
            {
                h = ((g - b) / delta) + (g < b ? 6.0 : 0.0);
            }
            else if (max == g)
            {
                h = ((b - r) / delta) + 2.0;
            }
            else
            {
                h = ((r - g) / delta) + 4.0;
            }

            h *= 60.0;
            if (h < 0.0) h += 360.0;
            if (h >= 360.0) h -= 360.0;
        }

        internal static Color HslToRgb(double h, double s, double l)
        {
            h = h % 360.0;
            if (h < 0.0) h += 360.0;
            s = ClampDouble(s, 0.0, 1.0);
            l = ClampDouble(l, 0.0, 1.0);

            if (s < 0.0001)
            {
                int v = ClampInt((int)Math.Round(l * 255.0), 0, 255);
                return Color.FromArgb(255, v, v, v);
            }

            double q = (l < 0.5) ? (l * (1.0 + s)) : (l + s - l * s);
            double p = 2.0 * l - q;
            double hk = h / 360.0;

            double tr = HueToChannel(p, q, hk + (1.0 / 3.0));
            double tg = HueToChannel(p, q, hk);
            double tb = HueToChannel(p, q, hk - (1.0 / 3.0));

            int r = ClampInt((int)Math.Round(tr * 255.0), 0, 255);
            int g = ClampInt((int)Math.Round(tg * 255.0), 0, 255);
            int b = ClampInt((int)Math.Round(tb * 255.0), 0, 255);
            return Color.FromArgb(255, r, g, b);
        }

        private static double HueToChannel(double p, double q, double t)
        {
            if (t < 0.0) t += 1.0;
            if (t > 1.0) t -= 1.0;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }

        internal static int ClampInt(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        internal static byte ClampByte(int val, int min, int max)
        {
            if (val < min) return (byte)min;
            if (val > max) return (byte)max;
            return (byte)val;
        }

        internal static double ClampDouble(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        #endregion
    }

    /// <summary>
    /// GDI+ "Chameleon" Wallpaper Auto-Palette Extractor.
    /// Loads any wallpaper image without file-locking, resizes to 96x96, clusters vivid mid-tones
    /// and atmospheric dark tones, and synthesizes a high-contrast Dark Mode ThemePreset.
    /// </summary>
    public static class WallpaperPaletteExtractor
    {
        private const int SampleSize = 96;
        private const int HueBinCount = 24; // 15-degree hue buckets

        public static ThemePreset ExtractThemeFromWallpaper(string imagePath, string customName = null)
        {
            string displayTitle = BuildDisplayTitle(imagePath, customName);
            string generatedId = BuildCustomId(imagePath, customName);

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                ThemePreset fallback = ThemeCatalog.GetBuiltInPresets()[0].Clone();
                fallback.Id = generatedId;
                fallback.DisplayName = displayTitle;
                fallback.Subtitle = "Varsayılan koyu atmosfer profili";
                fallback.IsBuiltIn = false;
                fallback.WallpaperPath = imagePath ?? string.Empty;
                return fallback;
            }

            byte[] pixelBuffer = null;
            try
            {
                byte[] fileBytes = File.ReadAllBytes(imagePath);
                using (MemoryStream ms = new MemoryStream(fileBytes))
                using (Image sourceImage = Image.FromStream(ms, false, false))
                using (Bitmap resized = new Bitmap(SampleSize, SampleSize, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = SmoothingMode.HighQuality == g.SmoothingMode ? PixelOffsetMode.HighQuality : PixelOffsetMode.Default;
                        g.DrawImage(sourceImage, new Rectangle(0, 0, SampleSize, SampleSize));
                    }

                    BitmapData bmpData = resized.LockBits(
                        new Rectangle(0, 0, SampleSize, SampleSize),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);
                    try
                    {
                        int byteCount = Math.Abs(bmpData.Stride) * SampleSize;
                        pixelBuffer = new byte[byteCount];
                        Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, byteCount);
                    }
                    finally
                    {
                        resized.UnlockBits(bmpData);
                    }
                }
            }
            catch
            {
                ThemePreset fallback = ThemeCatalog.GetBuiltInPresets()[0].Clone();
                fallback.Id = generatedId;
                fallback.DisplayName = displayTitle;
                fallback.Subtitle = "Duvar kağıdı okunamadı • Zümrüt varsayılan";
                fallback.IsBuiltIn = false;
                fallback.WallpaperPath = imagePath;
                return fallback;
            }

            return AnalyzePixelsAndBuildPreset(pixelBuffer, imagePath, generatedId, displayTitle);
        }

        private static ThemePreset AnalyzePixelsAndBuildPreset(
            byte[] bgraPixels,
            string imagePath,
            string presetId,
            string displayName)
        {
            double[] vividBinWeight = new double[HueBinCount];
            double[] vividBinR = new double[HueBinCount];
            double[] vividBinG = new double[HueBinCount];
            double[] vividBinB = new double[HueBinCount];

            double[] darkBinWeight = new double[HueBinCount];
            double[] darkBinR = new double[HueBinCount];
            double[] darkBinG = new double[HueBinCount];
            double[] darkBinB = new double[HueBinCount];

            double globalDarkWeight = 0.0;
            double globalDarkR = 0.0;
            double globalDarkG = 0.0;
            double globalDarkB = 0.0;

            double totalChromaticWeight = 0.0;
            double totalOpaqueWeight = 0.0;

            int stride = SampleSize * 4;
            double halfSize = (SampleSize - 1) * 0.5;

            for (int y = 0; y < SampleSize; y++)
            {
                // Slight center-region preference so ultrawide letterbox borders do not dominate
                double ny = (y - halfSize) / halfSize;
                int rowOffset = y * stride;

                for (int x = 0; x < SampleSize; x++)
                {
                    int idx = rowOffset + (x * 4);
                    byte b = bgraPixels[idx + 0];
                    byte g = bgraPixels[idx + 1];
                    byte r = bgraPixels[idx + 2];
                    byte a = bgraPixels[idx + 3];

                    if (a < 128)
                    {
                        continue;
                    }

                    double nx = (x - halfSize) / halfSize;
                    double distSq = (nx * nx + ny * ny) * 0.5; // 0 at center, 1 at corners
                    double spatialWeight = 1.25 - (0.35 * distSq);

                    Color px = Color.FromArgb(255, r, g, b);
                    double h, s, l;
                    ThemeCatalog.RgbToHsl(px, out h, out s, out l);

                    totalOpaqueWeight += spatialWeight;

                    int bin = (int)(h / (360.0 / HueBinCount)) % HueBinCount;
                    if (bin < 0) bin += HueBinCount;

                    // 1) Vivid mid-tones for ExplorerShadowGlow & RimHighlight
                    if (s >= 0.12 && l >= 0.08 && l <= 0.90)
                    {
                        double midToneCurve = 1.0 - Math.Min(1.0, Math.Abs(l - 0.48) / 0.44);
                        double satBoost = Math.Pow(s, 1.30);
                        double vw = spatialWeight * satBoost * (0.30 + 0.70 * midToneCurve);

                        vividBinWeight[bin] += vw;
                        vividBinR[bin] += r * vw;
                        vividBinG[bin] += g * vw;
                        vividBinB[bin] += b * vw;
                        totalChromaticWeight += vw;
                    }

                    // 2) Deep atmospheric dark tones for PrimaryAccent, ExplorerBodyTint, UiBackground, UiCard
                    if (l >= 0.03 && l <= 0.44)
                    {
                        double darkCurve = 1.0 - Math.Min(1.0, Math.Abs(l - 0.18) / 0.28);
                        double dw = spatialWeight * (0.35 + 0.65 * darkCurve);

                        globalDarkWeight += dw;
                        globalDarkR += r * dw;
                        globalDarkG += g * dw;
                        globalDarkB += b * dw;

                        if (s >= 0.06)
                        {
                            double cdw = dw * (0.40 + 0.60 * s);
                            darkBinWeight[bin] += cdw;
                            darkBinR[bin] += r * cdw;
                            darkBinG[bin] += g * cdw;
                            darkBinB[bin] += b * cdw;
                        }
                    }
                }
            }

            bool isMonochrome = (totalOpaqueWeight <= 0.001) || ((totalChromaticWeight / Math.Max(1.0, totalOpaqueWeight)) < 0.018);

            if (isMonochrome)
            {
                return BuildMonochromePreset(imagePath, presetId, displayName, globalDarkWeight, globalDarkR, globalDarkG, globalDarkB);
            }

            // Find dominant vivid hue cluster using circular 3-bin neighborhood smoothing
            int bestBin = 0;
            double bestScore = -1.0;
            for (int i = 0; i < HueBinCount; i++)
            {
                int prev = (i + HueBinCount - 1) % HueBinCount;
                int next = (i + 1) % HueBinCount;
                double smoothed = vividBinWeight[i] + 0.35 * (vividBinWeight[prev] + vividBinWeight[next]);
                if (smoothed > bestScore)
                {
                    bestScore = smoothed;
                    bestBin = i;
                }
            }

            // Compute weighted average vivid color across winning cluster (bestBin +/- 1)
            Color vividRepresentative = GetClusterAverageColor(vividBinWeight, vividBinR, vividBinG, vividBinB, bestBin, Color.FromArgb(255, 38, 115, 71));
            double vivH, vivS, vivL;
            ThemeCatalog.RgbToHsl(vividRepresentative, out vivH, out vivS, out vivL);

            // Check for a strong secondary accent hue (>= 3 bins away) for nuanced RimHighlight
            int secondBin = -1;
            double secondScore = 0.0;
            for (int i = 0; i < HueBinCount; i++)
            {
                int circularDist = Math.Abs(i - bestBin);
                if (circularDist > HueBinCount / 2) circularDist = HueBinCount - circularDist;
                if (circularDist < 2) continue;

                int prev = (i + HueBinCount - 1) % HueBinCount;
                int next = (i + 1) % HueBinCount;
                double smoothed = vividBinWeight[i] + 0.25 * (vividBinWeight[prev] + vividBinWeight[next]);
                if (smoothed > secondScore)
                {
                    secondScore = smoothed;
                    secondBin = i;
                }
            }

            double rimHue = vivH;
            if (secondBin >= 0 && secondScore >= bestScore * 0.42)
            {
                Color secondColor = GetClusterAverageColor(vividBinWeight, vividBinR, vividBinG, vividBinB, secondBin, vividRepresentative);
                double secH, secS, secL;
                ThemeCatalog.RgbToHsl(secondColor, out secH, out secS, out secL);
                // Blend secondary accent hue subtly when harmonious, or adopt for vivid rim contrast
                rimHue = secH;
            }

            // Determine atmospheric dark hue/saturation aligned with wallpaper's dark mood
            double darkClusterW = GetClusterWeight(darkBinWeight, bestBin);
            double darkH = vivH;
            double darkS = ThemeCatalog.ClampDouble(vivS * 0.72, 0.26, 0.62);

            if (darkClusterW > 1.5)
            {
                Color darkRep = GetClusterAverageColor(darkBinWeight, darkBinR, darkBinG, darkBinB, bestBin, vividRepresentative);
                double dh, ds, dl;
                ThemeCatalog.RgbToHsl(darkRep, out dh, out ds, out dl);
                darkH = dh;
                darkS = ThemeCatalog.ClampDouble(Math.Max(ds, vivS * 0.55), 0.24, 0.65);
            }

            // Synthesize dark-mode-safe, high-contrast studio surfaces & accents
            Color uiBackground = ThemeCatalog.HslToRgb(
                darkH,
                ThemeCatalog.ClampDouble(darkS * 0.55, 0.14, 0.36),
                0.055);

            Color uiCard = ThemeCatalog.HslToRgb(
                darkH,
                ThemeCatalog.ClampDouble(darkS * 0.62, 0.16, 0.40),
                0.105);

            Color explorerBodyTint = ThemeCatalog.HslToRgb(
                darkH,
                ThemeCatalog.ClampDouble(darkS * 0.95, 0.30, 0.68),
                0.125);

            Color taskbarTint = ThemeCatalog.HslToRgb(
                darkH,
                ThemeCatalog.ClampDouble(darkS * 0.80, 0.24, 0.56),
                0.095);

            Color primaryAccent = ThemeCatalog.HslToRgb(
                darkH,
                ThemeCatalog.ClampDouble(darkS * 0.90, 0.28, 0.64),
                0.205);

            Color inactiveAccent = ThemeCatalog.HslToRgb(
                darkH,
                ThemeCatalog.ClampDouble(darkS * 0.72, 0.20, 0.50),
                0.145);

            Color explorerShadowGlow = ThemeCatalog.HslToRgb(
                vivH,
                ThemeCatalog.ClampDouble(Math.Max(vivS * 1.15, 0.52), 0.48, 0.92),
                ThemeCatalog.ClampDouble(vivL, 0.42, 0.56));

            Color rimHighlight = ThemeCatalog.HslToRgb(
                rimHue,
                ThemeCatalog.ClampDouble(Math.Max(vivS, 0.48), 0.44, 0.88),
                ThemeCatalog.ClampDouble(Math.Max(vivL + 0.20, 0.68), 0.66, 0.78));

            ThemePreset preset = new ThemePreset();
            preset.Id = presetId;
            preset.DisplayName = displayName;
            preset.Subtitle = string.Format(
                CultureInfo.InvariantCulture,
                "Chameleon oto-palet ({0} • {1})",
                ThemeCatalog.ColorToHex(primaryAccent),
                ThemeCatalog.ColorToHex(explorerShadowGlow));
            preset.IsBuiltIn = false;
            preset.UiBackground = uiBackground;
            preset.UiCard = uiCard;
            preset.PrimaryAccent = primaryAccent;
            preset.InactiveAccent = inactiveAccent;
            preset.RimHighlight = rimHighlight;
            preset.ExplorerBodyTint = explorerBodyTint;
            preset.ExplorerShadowGlow = explorerShadowGlow;
            preset.TaskbarTint = taskbarTint;
            preset.DefaultTintAlpha = 70;
            preset.DefaultShadowAlpha = 185;
            preset.DefaultShadowSpread = 28;
            preset.WallpaperPath = imagePath;

            return preset;
        }

        private static ThemePreset BuildMonochromePreset(
            string imagePath,
            string presetId,
            string displayName,
            double darkW,
            double darkR,
            double darkG,
            double darkB)
        {
            double baseH = 160.0;
            double baseS = 0.05;
            if (darkW > 0.1)
            {
                Color avg = Color.FromArgb(
                    255,
                    ThemeCatalog.ClampInt((int)Math.Round(darkR / darkW), 0, 255),
                    ThemeCatalog.ClampInt((int)Math.Round(darkG / darkW), 0, 255),
                    ThemeCatalog.ClampInt((int)Math.Round(darkB / darkW), 0, 255));
                double l;
                ThemeCatalog.RgbToHsl(avg, out baseH, out baseS, out l);
                baseS = ThemeCatalog.ClampDouble(baseS, 0.0, 0.08);
            }

            ThemePreset p = new ThemePreset();
            p.Id = presetId;
            p.DisplayName = displayName;
            p.IsBuiltIn = false;
            p.UiBackground = ThemeCatalog.HslToRgb(baseH, baseS, 0.05);
            p.UiCard = ThemeCatalog.HslToRgb(baseH, baseS, 0.10);
            p.PrimaryAccent = ThemeCatalog.HslToRgb(baseH, baseS, 0.18);
            p.InactiveAccent = ThemeCatalog.HslToRgb(baseH, baseS, 0.12);
            p.ExplorerBodyTint = ThemeCatalog.HslToRgb(baseH, baseS, 0.10);
            p.TaskbarTint = ThemeCatalog.HslToRgb(baseH, baseS, 0.08);
            p.ExplorerShadowGlow = ThemeCatalog.HslToRgb(baseH, baseS, 0.42);
            p.RimHighlight = ThemeCatalog.HslToRgb(baseH, baseS, 0.72);
            p.DefaultTintAlpha = 58;
            p.DefaultShadowAlpha = 170;
            p.DefaultShadowSpread = 26;
            p.WallpaperPath = imagePath;
            p.Subtitle = string.Format(
                CultureInfo.InvariantCulture,
                "Chameleon monokrom ({0} • {1})",
                ThemeCatalog.ColorToHex(p.PrimaryAccent),
                ThemeCatalog.ColorToHex(p.RimHighlight));
            return p;
        }

        private static double GetClusterWeight(double[] weights, int centerBin)
        {
            int prev = (centerBin + HueBinCount - 1) % HueBinCount;
            int next = (centerBin + 1) % HueBinCount;
            return weights[prev] + weights[centerBin] + weights[next];
        }

        private static Color GetClusterAverageColor(
            double[] weights,
            double[] sumR,
            double[] sumG,
            double[] sumB,
            int centerBin,
            Color fallback)
        {
            int prev = (centerBin + HueBinCount - 1) % HueBinCount;
            int next = (centerBin + 1) % HueBinCount;

            double w = weights[prev] * 0.5 + weights[centerBin] + weights[next] * 0.5;
            if (w <= 0.0001)
            {
                return fallback;
            }

            double r = (sumR[prev] * 0.5 + sumR[centerBin] + sumR[next] * 0.5) / w;
            double g = (sumG[prev] * 0.5 + sumG[centerBin] + sumG[next] * 0.5) / w;
            double b = (sumB[prev] * 0.5 + sumB[centerBin] + sumB[next] * 0.5) / w;

            return Color.FromArgb(
                255,
                ThemeCatalog.ClampInt((int)Math.Round(r), 0, 255),
                ThemeCatalog.ClampInt((int)Math.Round(g), 0, 255),
                ThemeCatalog.ClampInt((int)Math.Round(b), 0, 255));
        }

        private static string BuildDisplayTitle(string imagePath, string customName)
        {
            if (!string.IsNullOrEmpty(customName) && customName.Trim().Length > 0)
            {
                return customName.Trim();
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    string stem = Path.GetFileNameWithoutExtension(imagePath);
                    if (!string.IsNullOrEmpty(stem))
                    {
                        string pretty = stem.Replace('_', ' ').Replace('-', ' ').Trim();
                        if (pretty.Length > 28)
                        {
                            pretty = pretty.Substring(0, 28).Trim();
                        }
                        if (pretty.Length > 0)
                        {
                            return "Chameleon • " + pretty;
                        }
                    }
                }
                catch
                {
                }
            }

            return "Chameleon Özel Tema";
        }

        private static string BuildCustomId(string imagePath, string customName)
        {
            string seed = customName;
            if (string.IsNullOrEmpty(seed) && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    seed = Path.GetFileNameWithoutExtension(imagePath);
                }
                catch
                {
                    seed = null;
                }
            }

            string clean = ThemeCatalog.SanitizePresetId(seed);
            if (string.IsNullOrEmpty(clean))
            {
                clean = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            }

            if (!clean.StartsWith("Chameleon_", StringComparison.OrdinalIgnoreCase))
            {
                clean = "Chameleon_" + clean;
            }

            if (clean.Length > 48)
            {
                clean = clean.Substring(0, 48).TrimEnd('_', '-');
            }
            return clean;
        }

        /// <summary>
        /// Extracts 6 dominant/harmonious color swatches from a wallpaper image for the v3.0 Chameleon OKLab/HSL Analyzer card.
        /// Never locks the file and falls back gracefully if the file is missing.
        /// </summary>
        public static Color[] ExtractDominantSwatches(string imagePath, int count = 6)
        {
            if (count <= 0) count = 6;
            ThemePreset extracted = ExtractThemeFromWallpaper(imagePath, null);
            Color[] baseSwatches = new Color[]
            {
                extracted.PrimaryAccent,
                extracted.RimHighlight,
                extracted.ExplorerShadowGlow,
                extracted.ExplorerBodyTint,
                extracted.UiCard,
                extracted.UiBackground
            };

            if (count == 6)
            {
                return baseSwatches;
            }

            Color[] result = new Color[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = baseSwatches[i % baseSwatches.Length];
            }
            return result;
        }
    }

    /// <summary>
    /// Color harmony modes for v3.0 Color & Light Studio.
    /// </summary>
    public enum HarmonyMode
    {
        Mono = 0,
        Analog = 1,
        Complement = 2,
        Triad = 3,
        Split = 4,
        Tetrad = 5
    }

    /// <summary>
    /// v3.0 Color Harmony Generator & WCAG 2.1 Contrast Analyzer.
    /// Generates 5 harmonious swatches for any base Color and HarmonyMode, and computes WCAG contrast ratios.
    /// </summary>
    public static class ColorHarmonyGenerator
    {
        /// <summary>
        /// Returns 5 harmonious colors for the given base color and HarmonyMode.
        /// Slot [0] is the base/primary tone, [1..4] are balanced companion/accent/surface tones.
        /// </summary>
        public static Color[] GenerateHarmony(Color baseColor, HarmonyMode mode)
        {
            double h, s, l;
            ThemeCatalog.RgbToHsl(baseColor, out h, out s, out l);

            double baseSat = ThemeCatalog.ClampDouble(Math.Max(s, 0.25), 0.15, 0.95);
            double baseLum = ThemeCatalog.ClampDouble(l, 0.18, 0.78);

            Color[] swatches = new Color[5];

            switch (mode)
            {
                case HarmonyMode.Mono:
                    swatches[0] = ThemeCatalog.HslToRgb(h, baseSat, ThemeCatalog.ClampDouble(baseLum, 0.22, 0.55));
                    swatches[1] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.85, 0.15, 0.90), 0.72);
                    swatches[2] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 1.05, 0.25, 0.95), 0.48);
                    swatches[3] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.75, 0.15, 0.80), 0.24);
                    swatches[4] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.55, 0.10, 0.65), 0.11);
                    break;

                case HarmonyMode.Analog:
                    swatches[0] = baseColor;
                    swatches[1] = ThemeCatalog.HslToRgb((h + 28.0) % 360.0, ThemeCatalog.ClampDouble(baseSat * 1.05, 0.30, 0.92), 0.62);
                    swatches[2] = ThemeCatalog.HslToRgb((h + 332.0) % 360.0, ThemeCatalog.ClampDouble(baseSat * 1.05, 0.30, 0.92), 0.56);
                    swatches[3] = ThemeCatalog.HslToRgb((h + 16.0) % 360.0, ThemeCatalog.ClampDouble(baseSat * 0.85, 0.22, 0.80), 0.34);
                    swatches[4] = ThemeCatalog.HslToRgb((h + 344.0) % 360.0, ThemeCatalog.ClampDouble(baseSat * 0.65, 0.16, 0.68), 0.16);
                    break;

                case HarmonyMode.Complement:
                    double compH = (h + 180.0) % 360.0;
                    swatches[0] = baseColor;
                    swatches[1] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.92, 0.32, 0.90), 0.68);
                    swatches[2] = ThemeCatalog.HslToRgb(compH, ThemeCatalog.ClampDouble(baseSat * 1.05, 0.40, 0.92), 0.58);
                    swatches[3] = ThemeCatalog.HslToRgb(compH, ThemeCatalog.ClampDouble(baseSat * 0.80, 0.28, 0.82), 0.36);
                    swatches[4] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.65, 0.18, 0.70), 0.14);
                    break;

                case HarmonyMode.Triad:
                    double triad1 = (h + 120.0) % 360.0;
                    double triad2 = (h + 240.0) % 360.0;
                    swatches[0] = baseColor;
                    swatches[1] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.95, 0.35, 0.90), 0.68);
                    swatches[2] = ThemeCatalog.HslToRgb(triad1, ThemeCatalog.ClampDouble(baseSat * 0.95, 0.38, 0.90), 0.58);
                    swatches[3] = ThemeCatalog.HslToRgb(triad2, ThemeCatalog.ClampDouble(baseSat * 0.90, 0.35, 0.88), 0.52);
                    swatches[4] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.60, 0.16, 0.68), 0.14);
                    break;

                case HarmonyMode.Split:
                    double split1 = (h + 150.0) % 360.0;
                    double split2 = (h + 210.0) % 360.0;
                    swatches[0] = baseColor;
                    swatches[1] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.95, 0.35, 0.90), 0.68);
                    swatches[2] = ThemeCatalog.HslToRgb(split1, ThemeCatalog.ClampDouble(baseSat * 1.0, 0.38, 0.92), 0.60);
                    swatches[3] = ThemeCatalog.HslToRgb(split2, ThemeCatalog.ClampDouble(baseSat * 0.95, 0.36, 0.90), 0.52);
                    swatches[4] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.65, 0.18, 0.70), 0.15);
                    break;

                case HarmonyMode.Tetrad:
                default:
                    double tet1 = (h + 90.0) % 360.0;
                    double tet2 = (h + 180.0) % 360.0;
                    double tet3 = (h + 270.0) % 360.0;
                    swatches[0] = baseColor;
                    swatches[1] = ThemeCatalog.HslToRgb(tet1, ThemeCatalog.ClampDouble(baseSat * 0.92, 0.35, 0.88), 0.62);
                    swatches[2] = ThemeCatalog.HslToRgb(tet2, ThemeCatalog.ClampDouble(baseSat * 0.95, 0.38, 0.90), 0.56);
                    swatches[3] = ThemeCatalog.HslToRgb(tet3, ThemeCatalog.ClampDouble(baseSat * 0.90, 0.35, 0.88), 0.50);
                    swatches[4] = ThemeCatalog.HslToRgb(h, ThemeCatalog.ClampDouble(baseSat * 0.65, 0.18, 0.72), 0.16);
                    break;
            }

            return swatches;
        }

        public static Color[] Generate(Color baseColor, HarmonyMode mode)
        {
            return GenerateHarmony(baseColor, mode);
        }

        public static Color[] GenerateHarmony(Color baseColor, int modeIndex)
        {
            if (modeIndex < 0) modeIndex = 0;
            if (modeIndex > 5) modeIndex = modeIndex % 6;
            return GenerateHarmony(baseColor, (HarmonyMode)modeIndex);
        }

        /// <summary>
        /// Calculates relative sRGB luminance per WCAG 2.1 specification (0.0 = pure black, 1.0 = pure white).
        /// </summary>
        public static double GetRelativeLuminance(Color c)
        {
            double r = LinearizeChannel(c.R / 255.0);
            double g = LinearizeChannel(c.G / 255.0);
            double b = LinearizeChannel(c.B / 255.0);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static double LinearizeChannel(double v)
        {
            return (v <= 0.03928) ? (v / 12.92) : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Calculates WCAG 2.1 contrast ratio between foreground and background (range 1.0 to 21.0).
        /// </summary>
        public static double CalculateContrastRatio(Color fg, Color bg)
        {
            double lum1 = GetRelativeLuminance(fg);
            double lum2 = GetRelativeLuminance(bg);
            double brightest = Math.Max(lum1, lum2);
            double darkest = Math.Min(lum1, lum2);
            return Math.Round((brightest + 0.05) / (darkest + 0.05), 2);
        }

        /// <summary>
        /// Checks whether foreground and background meet WCAG AA contrast requirements (4.5:1 for normal text).
        /// </summary>
        public static bool PassesWcagAA(Color fg, Color bg)
        {
            return CalculateContrastRatio(fg, bg) >= 4.5;
        }

        /// <summary>
        /// Checks whether foreground and background meet WCAG AA contrast requirements (3.0:1 for large text/icons, 4.5:1 for normal text).
        /// </summary>
        public static bool PassesWcagAA(Color fg, Color bg, bool largeText)
        {
            double threshold = largeText ? 3.0 : 4.5;
            return CalculateContrastRatio(fg, bg) >= threshold;
        }

        /// <summary>
        /// Checks whether foreground and background meet WCAG AAA contrast requirements (7.0:1).
        /// </summary>
        public static bool PassesWcagAAA(Color fg, Color bg)
        {
            return CalculateContrastRatio(fg, bg) >= 7.0;
        }

        /// <summary>
        /// Adjusts foreground lightness in HSL steps until it achieves at least minRatio contrast against bg.
        /// </summary>
        public static Color EnsureContrast(Color fg, Color bg, double minRatio = 4.5)
        {
            if (CalculateContrastRatio(fg, bg) >= minRatio)
            {
                return fg;
            }

            double h, s, l;
            ThemeCatalog.RgbToHsl(fg, out h, out s, out l);
            double bgLum = GetRelativeLuminance(bg);
            bool lighten = bgLum < 0.45;

            Color candidate = fg;
            for (int step = 1; step <= 20; step++)
            {
                double delta = step * 0.04;
                double newL = lighten ? Math.Min(0.98, l + delta) : Math.Max(0.04, l - delta);
                candidate = ThemeCatalog.HslToRgb(h, s, newL);
                if (CalculateContrastRatio(candidate, bg) >= minRatio)
                {
                    return candidate;
                }
            }
            return lighten ? Color.FromArgb(255, 242, 244, 248) : Color.FromArgb(255, 10, 11, 16);
        }

        public static Color GetOptimalTextColor(Color bg)
        {
            double lum = GetRelativeLuminance(bg);
            return (lum < 0.36) ? Color.FromArgb(255, 242, 244, 248) : Color.FromArgb(255, 10, 11, 16);
        }
    }

    /// <summary>
    /// v3.0 Boost & Performance Profiles for View 5 (Oyun/Render Boost & Monitör Merkezi).
    /// </summary>
    public enum BoostProfile
    {
        Normal = 0,
        Oyun = 1,
        Render = 2,
        Sessiz = 3
    }

    /// <summary>
    /// v3.0 `.doomtheme` Preset Package serializer/deserializer with validation and diff summary generation.
    /// Exports and imports a ThemePreset along with taskbar, Explorer, and HUD widget settings.
    /// </summary>
    public sealed class DoomThemePackage
    {
        public string FormatVersion;
        public string ExportedAt;
        public string Author;
        public ThemePreset Preset;
        public Dictionary<string, string> Settings;

        public DoomThemePackage()
        {
            FormatVersion = "3.0";
            ExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            Author = Environment.UserName ?? "SplendUser";
            Preset = new ThemePreset();
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static DoomThemePackage Create(ThemePreset preset, Dictionary<string, string> settings = null)
        {
            DoomThemePackage pkg = new DoomThemePackage();
            pkg.Preset = (preset != null) ? preset.Clone() : new ThemePreset();
            if (settings != null)
            {
                foreach (KeyValuePair<string, string> kvp in settings)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                    {
                        pkg.Settings[kvp.Key] = kvp.Value;
                    }
                }
            }
            return pkg;
        }

        public string Serialize()
        {
            return Serialize(this.Preset, this.Settings);
        }

        public static string Serialize(ThemePreset preset, Dictionary<string, string> settings = null)
        {
            if (preset == null)
            {
                preset = new ThemePreset();
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# SPLEND // DoomTheme Package (.doomtheme) v3.0");
            sb.AppendLine("{");
            sb.AppendLine("  \"Format\": \"DoomThemePackage\",");
            sb.AppendLine("  \"Version\": \"3.0\",");
            sb.AppendLine("  \"ExportedAt\": \"" + CleanValue(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)) + "\",");
            sb.AppendLine("  \"Id\": \"" + CleanValue(preset.Id) + "\",");
            sb.AppendLine("  \"DisplayName\": \"" + CleanValue(preset.DisplayName) + "\",");
            sb.AppendLine("  \"Subtitle\": \"" + CleanValue(preset.Subtitle) + "\",");
            sb.AppendLine("  \"Tags\": \"" + CleanValue(preset.Tags ?? "Dark, Cyber, v3.0") + "\",");
            sb.AppendLine("  \"UiBackground\": \"" + ThemeCatalog.ColorToHex(preset.UiBackground) + "\",");
            sb.AppendLine("  \"UiCard\": \"" + ThemeCatalog.ColorToHex(preset.UiCard) + "\",");
            sb.AppendLine("  \"PrimaryAccent\": \"" + ThemeCatalog.ColorToHex(preset.PrimaryAccent) + "\",");
            sb.AppendLine("  \"InactiveAccent\": \"" + ThemeCatalog.ColorToHex(preset.InactiveAccent) + "\",");
            sb.AppendLine("  \"RimHighlight\": \"" + ThemeCatalog.ColorToHex(preset.RimHighlight) + "\",");
            sb.AppendLine("  \"SecondaryAccent\": \"" + ThemeCatalog.ColorToHex(preset.SecondaryAccent) + "\",");
            sb.AppendLine("  \"ExplorerBodyTint\": \"" + ThemeCatalog.ColorToHex(preset.ExplorerBodyTint) + "\",");
            sb.AppendLine("  \"ExplorerShadowGlow\": \"" + ThemeCatalog.ColorToHex(preset.ExplorerShadowGlow) + "\",");
            sb.AppendLine("  \"TaskbarTint\": \"" + ThemeCatalog.ColorToHex(preset.TaskbarTint) + "\",");
            sb.AppendLine("  \"DefaultTintAlpha\": \"" + preset.DefaultTintAlpha.ToString(CultureInfo.InvariantCulture) + "\",");
            sb.AppendLine("  \"DefaultShadowAlpha\": \"" + preset.DefaultShadowAlpha.ToString(CultureInfo.InvariantCulture) + "\",");
            sb.AppendLine("  \"DefaultShadowSpread\": \"" + preset.DefaultShadowSpread.ToString(CultureInfo.InvariantCulture) + "\",");
            sb.AppendLine("  \"WallpaperPath\": \"" + CleanValue(preset.WallpaperPath) + "\"");

            if (settings != null && settings.Count > 0)
            {
                string[] exportedSettingKeys = new string[]
                {
                    "ExplorerGlassEnabled", "ExplorerAlpha", "ExplorerBlurMode",
                    "EmeraldTintAndShadowEnabled", "EmeraldTintIntensity", "EmeraldShadowIntensity",
                    "ShadowSpread", "ShowRimBorder", "ShadowOnlyActiveWindow",
                    "TaskbarMode", "TaskbarStyleMode", "TaskbarCenterIcons", "TaskbarTintAlpha",
                    "TaskbarCornerRadius", "TaskbarMarginPx", "ShowTaskbarSeconds", "HideTaskViewButton",
                    "HudEnabled", "HudOpacity", "WidgetScale", "WidgetLayout",
                    "WidgetClock", "WidgetSystemGpu", "WidgetStorage", "WidgetNetwork",
                    "WidgetMedia", "WidgetNotes", "WidgetSpectrum", "WidgetPomodoro",
                    "SpectrumStyle", "SpectrumBandCount", "SpectrumSmoothing",
                    "BoostProfile", "HdrWhiteTextGuardEnabled", "HdrMaxNitsClamp"
                };

                for (int i = 0; i < exportedSettingKeys.Length; i++)
                {
                    string k = exportedSettingKeys[i];
                    string v;
                    if (settings.TryGetValue(k, out v) && v != null)
                    {
                        sb.AppendLine("  \"Setting." + k + "\": \"" + CleanValue(v) + "\"");
                    }
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        public static bool ExportToFile(ThemePreset preset, string filePath, Dictionary<string, string> settings = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string content = Serialize(preset, settings);
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static DoomThemePackage ImportFromFile(string filePath)
        {
            bool valid;
            string msg;
            return ImportFromFile(filePath, out valid, out msg);
        }

        public static DoomThemePackage ImportFromFile(string filePath, out bool isValid, out string validationMessage)
        {
            isValid = false;
            validationMessage = "Dosya bulunamadı.";
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return null;
                }
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                return Deserialize(content, out isValid, out validationMessage);
            }
            catch (Exception ex)
            {
                validationMessage = "Okuma hatası: " + ex.Message;
                return null;
            }
        }

        public static bool ValidateFile(string filePath, out string validationMessage)
        {
            bool valid;
            ImportFromFile(filePath, out valid, out validationMessage);
            return valid;
        }

        public static DoomThemePackage Deserialize(string content, out bool isValid, out string validationMessage)
        {
            isValid = false;
            validationMessage = "Boş veya geçersiz .doomtheme içeriği.";
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line == "{" || line == "}" || line.StartsWith("["))
                {
                    continue;
                }

                // Support both JSON-like "Key": "Value", and INI-like Key=Value
                int sepIdx = line.IndexOf(':');
                int eqIdx = line.IndexOf('=');
                int splitAt = -1;
                if (sepIdx > 0 && (eqIdx < 0 || sepIdx < eqIdx))
                {
                    splitAt = sepIdx;
                }
                else if (eqIdx > 0)
                {
                    splitAt = eqIdx;
                }

                if (splitAt > 0)
                {
                    string rawKey = line.Substring(0, splitAt).Trim().Trim('"', '\'', ' ');
                    string rawVal = line.Substring(splitAt + 1).Trim().TrimEnd(',').Trim().Trim('"', '\'');
                    if (rawKey.Length > 0)
                    {
                        fields[rawKey] = rawVal;
                    }
                }
            }

            if (fields.Count == 0)
            {
                return null;
            }

            // Require at least Id or DisplayName and PrimaryAccent
            if (!fields.ContainsKey("PrimaryAccent") && !fields.ContainsKey("Id") && !fields.ContainsKey("DisplayName"))
            {
                validationMessage = "Şema doğrulaması başarısız: PrimaryAccent veya Id alanı eksik.";
                return null;
            }

            DoomThemePackage pkg = new DoomThemePackage();
            if (fields.ContainsKey("Version")) pkg.FormatVersion = fields["Version"];
            if (fields.ContainsKey("ExportedAt")) pkg.ExportedAt = fields["ExportedAt"];

            ThemePreset p = new ThemePreset();
            p.IsBuiltIn = false;
            p.Id = ThemeCatalog.SanitizePresetId(GetField(fields, "Id", GetField(fields, "DisplayName", "ImportedTheme")));
            if (string.IsNullOrEmpty(p.Id)) p.Id = "Imported_" + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture);
            p.DisplayName = GetField(fields, "DisplayName", p.Id);
            p.Subtitle = GetField(fields, "Subtitle", "İçe aktarılan .doomtheme paketi");
            p.Tags = GetField(fields, "Tags", "Imported, Custom");
            p.UiBackground = ThemeCatalog.HexToColor(GetField(fields, "UiBackground", null), p.UiBackground);
            p.UiCard = ThemeCatalog.HexToColor(GetField(fields, "UiCard", null), p.UiCard);
            p.PrimaryAccent = ThemeCatalog.HexToColor(GetField(fields, "PrimaryAccent", null), p.PrimaryAccent);
            p.InactiveAccent = ThemeCatalog.HexToColor(GetField(fields, "InactiveAccent", null), p.InactiveAccent);
            p.RimHighlight = ThemeCatalog.HexToColor(GetField(fields, "RimHighlight", null), p.RimHighlight);
            p.SecondaryAccent = ThemeCatalog.HexToColor(GetField(fields, "SecondaryAccent", null), p.SecondaryAccent);
            p.ExplorerBodyTint = ThemeCatalog.HexToColor(GetField(fields, "ExplorerBodyTint", null), p.ExplorerBodyTint);
            p.ExplorerShadowGlow = ThemeCatalog.HexToColor(GetField(fields, "ExplorerShadowGlow", null), p.ExplorerShadowGlow);
            p.TaskbarTint = ThemeCatalog.HexToColor(GetField(fields, "TaskbarTint", null), p.TaskbarTint);

            int tintAlpha;
            if (int.TryParse(GetField(fields, "DefaultTintAlpha", "72"), NumberStyles.Integer, CultureInfo.InvariantCulture, out tintAlpha))
            {
                p.DefaultTintAlpha = ThemeCatalog.ClampByte(tintAlpha, 0, 140);
            }
            int shadowAlpha;
            if (int.TryParse(GetField(fields, "DefaultShadowAlpha", "185"), NumberStyles.Integer, CultureInfo.InvariantCulture, out shadowAlpha))
            {
                p.DefaultShadowAlpha = ThemeCatalog.ClampByte(shadowAlpha, 0, 255);
            }
            int spread;
            if (int.TryParse(GetField(fields, "DefaultShadowSpread", "28"), NumberStyles.Integer, CultureInfo.InvariantCulture, out spread))
            {
                p.DefaultShadowSpread = ThemeCatalog.ClampInt(spread, 8, 48);
            }
            p.WallpaperPath = GetField(fields, "WallpaperPath", string.Empty);

            pkg.Preset = p;

            foreach (KeyValuePair<string, string> kvp in fields)
            {
                if (kvp.Key.StartsWith("Setting.", StringComparison.OrdinalIgnoreCase) && kvp.Key.Length > 8)
                {
                    string settingKey = kvp.Key.Substring(8);
                    pkg.Settings[settingKey] = kvp.Value;
                }
            }

            isValid = true;
            validationMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Geçerli .doomtheme v{0} paketi: {1} ({2} renk + {3} ayar)",
                pkg.FormatVersion,
                p.DisplayName,
                9,
                pkg.Settings.Count);
            return pkg;
        }

        public string GetDiffSummary(ThemePreset currentPreset, Dictionary<string, string> currentConfig = null)
        {
            return BuildDiffSummary(currentPreset, this, currentConfig);
        }

        public static string BuildDiffSummary(ThemePreset currentPreset, ThemePreset importedPreset)
        {
            DoomThemePackage pkg = Create(importedPreset, null);
            return BuildDiffSummary(currentPreset, pkg, null);
        }

        public static string BuildDiffSummary(ThemePreset currentPreset, DoomThemePackage importedPackage, Dictionary<string, string> currentConfig = null)
        {
            if (importedPackage == null || importedPackage.Preset == null)
            {
                return "Fark özeti: Geçersiz paket.";
            }

            if (currentPreset == null)
            {
                currentPreset = ThemeCatalog.GetBuiltInPresets()[0];
            }

            ThemePreset next = importedPackage.Preset;
            List<string> changes = new List<string>();

            if (!string.Equals(currentPreset.DisplayName, next.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                changes.Add("Tema: " + currentPreset.DisplayName + " -> " + next.DisplayName);
            }
            if (ThemeCatalog.ColorToHex(currentPreset.PrimaryAccent) != ThemeCatalog.ColorToHex(next.PrimaryAccent))
            {
                changes.Add("Vurgu: " + ThemeCatalog.ColorToHex(currentPreset.PrimaryAccent) + " -> " + ThemeCatalog.ColorToHex(next.PrimaryAccent));
            }
            if (ThemeCatalog.ColorToHex(currentPreset.RimHighlight) != ThemeCatalog.ColorToHex(next.RimHighlight))
            {
                changes.Add("Kenar Işıltısı: " + ThemeCatalog.ColorToHex(currentPreset.RimHighlight) + " -> " + ThemeCatalog.ColorToHex(next.RimHighlight));
            }
            if (ThemeCatalog.ColorToHex(currentPreset.ExplorerShadowGlow) != ThemeCatalog.ColorToHex(next.ExplorerShadowGlow))
            {
                changes.Add("360° Gölge: " + ThemeCatalog.ColorToHex(currentPreset.ExplorerShadowGlow) + " -> " + ThemeCatalog.ColorToHex(next.ExplorerShadowGlow));
            }
            if (currentPreset.DefaultShadowSpread != next.DefaultShadowSpread)
            {
                changes.Add("Gölge Yayılımı: " + currentPreset.DefaultShadowSpread + "px -> " + next.DefaultShadowSpread + "px");
            }

            int settingDiffs = 0;
            if (importedPackage.Settings != null && currentConfig != null)
            {
                foreach (KeyValuePair<string, string> kvp in importedPackage.Settings)
                {
                    string curVal;
                    if (!currentConfig.TryGetValue(kvp.Key, out curVal) || !string.Equals(curVal, kvp.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        settingDiffs++;
                    }
                }
            }
            else if (importedPackage.Settings != null)
            {
                settingDiffs = importedPackage.Settings.Count;
            }

            if (settingDiffs > 0)
            {
                changes.Add(settingDiffs + " sistem/widget ayarı güncellenecek");
            }

            if (changes.Count == 0)
            {
                return "Mevcut aktif tema ile birebir aynı.";
            }

            return string.Join(" • ", changes.ToArray());
        }

        private static string GetField(Dictionary<string, string> map, string key, string fallback)
        {
            string val;
            if (map != null && map.TryGetValue(key, out val) && !string.IsNullOrEmpty(val))
            {
                return val;
            }
            return fallback;
        }

        private static string CleanValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }

    /// <summary>
    /// Model for Global Command Palette entries (`Ctrl+Shift+Space` / Top Command Bar in v3.0).
    /// Supports prefix filtering (`>` commands, `#` themes, `@` widgets, `?` help) and fuzzy matching.
    /// </summary>
    public sealed class CommandPaletteItem
    {
        public string Id;
        public string Prefix;
        public string Title;
        public string Category;
        public string Shortcut;
        public string Keywords;
        public string Subtitle;
        public int TargetViewIndex;

        public CommandPaletteItem()
        {
            Id = string.Empty;
            Prefix = ">";
            Title = string.Empty;
            Category = "Komut";
            Shortcut = string.Empty;
            Keywords = string.Empty;
            Subtitle = string.Empty;
            TargetViewIndex = -1;
        }

        public CommandPaletteItem(
            string id,
            string prefix,
            string title,
            string category,
            string shortcut,
            string keywords,
            int targetViewIndex = -1,
            string subtitle = "")
        {
            Id = id ?? string.Empty;
            Prefix = string.IsNullOrEmpty(prefix) ? ">" : prefix;
            Title = title ?? string.Empty;
            Category = category ?? "Komut";
            Shortcut = shortcut ?? string.Empty;
            Keywords = keywords ?? string.Empty;
            TargetViewIndex = targetViewIndex;
            Subtitle = subtitle ?? string.Empty;
        }

        public bool Matches(string rawQuery)
        {
            return ScoreMatch(rawQuery) > 0;
        }

        public int ScoreMatch(string rawQuery)
        {
            if (string.IsNullOrEmpty(rawQuery))
            {
                return 10;
            }

            string q = rawQuery.Trim();
            if (q.Length == 0)
            {
                return 10;
            }

            if (q.StartsWith(">") || q.StartsWith("#") || q.StartsWith("@") || q.StartsWith("?"))
            {
                string reqPrefix = q.Substring(0, 1);
                if (!string.Equals(this.Prefix, reqPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
                q = q.Substring(1).Trim();
                if (q.Length == 0)
                {
                    return 50;
                }
            }

            if (Title != null && Title.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }
            if (Title != null && Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 80;
            }
            if (Category != null && Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 60;
            }
            if (Keywords != null && Keywords.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 50;
            }
            if (Id != null && Id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 40;
            }

            // Multi-token fuzzy check
            string[] tokens = q.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
            {
                string haystack = (Title + " " + Category + " " + Keywords + " " + Id).ToLowerInvariant();
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (!haystack.Contains(tokens[i].ToLowerInvariant()))
                    {
                        return 0;
                    }
                }
                return 35;
            }

            return 0;
        }
    }

    /// <summary>
    /// Built-in catalog of Global Command Palette items covering all 7 Bento Views, themes, widgets, and system actions.
    /// </summary>
    public static class CommandPaletteCatalog
    {
        public static List<CommandPaletteItem> GetDefaultItems()
        {
            List<CommandPaletteItem> items = new List<CommandPaletteItem>(32);

            // Navigation & Studio Actions (>)
            items.Add(new CommandPaletteItem("cmd_apply", ">", "Aktif Temayı Uygula", "Stüdyo", "Ctrl+S", "apply uygula kaydet", -1));
            items.Add(new CommandPaletteItem("cmd_rollback", ">", "Son Değişikliği Geri Al (Rollback)", "Stüdyo", "Ctrl+Z", "undo geri al snapshot", 0));
            items.Add(new CommandPaletteItem("cmd_purge_ram", ">", "RAM ve WorkingSet Belleği Temizle", "Performans", "Ctrl+Shift+M", "ram memory purge temizle boost", 5));
            items.Add(new CommandPaletteItem("cmd_toggle_icons", ">", "Masaüstü İkonlarını Gizle / Göster", "Masaüstü", "Ctrl+Alt+D", "icons masaüstü ikon gizle", 5));
            items.Add(new CommandPaletteItem("cmd_toggle_glass", ">", "Gezgin 360° Neon Gölge & Cam Aç/Kapat", "Gezgin", "Ctrl+Alt+G", "explorer glass shadow gölge", 2));
            items.Add(new CommandPaletteItem("cmd_fix_hdr", ">", "10-bit HDR Beyaz Metin & ClearType Onar", "HDR Motoru", "", "hdr text cleartype beyaz metin", 2));
            items.Add(new CommandPaletteItem("cmd_pomodoro_toggle", ">", "Pomodoro Odak Sayacını Başlat / Duraklat", "Odak", "Ctrl+Alt+P", "pomodoro focus timer odak", 4));
            items.Add(new CommandPaletteItem("cmd_boost_game", ">", "Boost Profili: Oyun Moduna Geç", "Boost", "", "oyun game boost", 5));
            items.Add(new CommandPaletteItem("cmd_boost_render", ">", "Boost Profili: Render / ComfyUI Moduna Geç", "Boost", "", "render comfyui blender boost", 5));
            items.Add(new CommandPaletteItem("cmd_boost_normal", ">", "Boost Profili: Normal Moda Dön", "Boost", "", "normal standart boost", 5));
            items.Add(new CommandPaletteItem("cmd_export_pkg", ">", "Temayı .doomtheme Paketi Olarak Dışa Aktar", "Paket", "Ctrl+E", "export doomtheme dışa aktar", 0));
            items.Add(new CommandPaletteItem("cmd_import_pkg", ">", ".doomtheme Paketi İçe Aktar", "Paket", "Ctrl+I", "import doomtheme içe aktar", 0));

            // 7 Bento Views (>)
            items.Add(new CommandPaletteItem("view_0", ">", "Görünüm 1: Tema Galerisi & Canlı Tuval", "Görünüm", "Alt+1", "galeri tema chameleon canvas", 0));
            items.Add(new CommandPaletteItem("view_1", ">", "Görünüm 2: Renk & Işık Stüdyosu", "Görünüm", "Alt+2", "renk hsv harmony wcag", 1));
            items.Add(new CommandPaletteItem("view_2", ">", "Görünüm 3: Gezgin & 10-bit HDR Motoru", "Görünüm", "Alt+3", "gezgin explorer hdr mica acrylic", 2));
            items.Add(new CommandPaletteItem("view_3", ">", "Görünüm 4: Görev Çubuğu & Dock", "Görünüm", "Alt+4", "taskbar dock dynamic island görev çubuğu", 3));
            items.Add(new CommandPaletteItem("view_4", ">", "Görünüm 5: Masaüstü HUD & Ses Spektrumu", "Görünüm", "Alt+5", "hud widget spectrum pomodoro", 4));
            items.Add(new CommandPaletteItem("view_5", ">", "Görünüm 6: Oyun/Render Boost & Monitör Merkezi", "Görünüm", "Alt+6", "boost ram dimmer night light kelvin", 5));
            items.Add(new CommandPaletteItem("view_6", ">", "Görünüm 7: Sistem, Kısayollar & Yedekleme", "Görünüm", "Alt+7", "sistem kısayol yedek backup log", 6));

            // Built-in Themes (#)
            List<ThemePreset> presets = ThemeCatalog.GetAllPresets();
            for (int i = 0; i < presets.Count; i++)
            {
                ThemePreset p = presets[i];
                items.Add(new CommandPaletteItem(
                    "theme_" + p.Id,
                    "#",
                    "Tema: " + p.DisplayName,
                    "Tema",
                    "",
                    p.Id + " " + p.DisplayName + " " + p.Subtitle,
                    0));
            }

            // 8 HUD Widgets (@)
            items.Add(new CommandPaletteItem("widget_clock", "@", "Widget: Saat & Takvim HUD", "Widget", "", "clock saat tarih hud", 4));
            items.Add(new CommandPaletteItem("widget_sys", "@", "Widget: Sistem CPU / RAM / GPU HUD", "Widget", "", "system cpu ram gpu hud", 4));
            items.Add(new CommandPaletteItem("widget_storage", "@", "Widget: Disk & Depolama HUD", "Widget", "", "storage disk nvme ssd hud", 4));
            items.Add(new CommandPaletteItem("widget_net", "@", "Widget: Ağ Trafiği HUD", "Widget", "", "network ağ hız indirme hud", 4));
            items.Add(new CommandPaletteItem("widget_media", "@", "Widget: Medya & Now Playing HUD", "Widget", "", "media müzik şarkı hud", 4));
            items.Add(new CommandPaletteItem("widget_notes", "@", "Widget: Hızlı Notlar & Uptime HUD", "Widget", "", "notes notlar uptime hud", 4));
            items.Add(new CommandPaletteItem("widget_spectrum", "@", "Widget: Canlı Ses Spektrumu (Audio Spectrum)", "Widget", "", "spectrum ses ekolayzer fft bar wave ring", 4));
            items.Add(new CommandPaletteItem("widget_pomodoro", "@", "Widget: Pomodoro & Derin Odak Sayacı", "Widget", "", "pomodoro focus timer odak mola", 4));

            return items;
        }

        public static List<CommandPaletteItem> Search(string query, IEnumerable<CommandPaletteItem> source = null)
        {
            IEnumerable<CommandPaletteItem> pool = source ?? GetDefaultItems();
            List<KeyValuePair<int, CommandPaletteItem>> scored = new List<KeyValuePair<int, CommandPaletteItem>>();

            foreach (CommandPaletteItem item in pool)
            {
                if (item == null) continue;
                int score = item.ScoreMatch(query);
                if (score > 0)
                {
                    scored.Add(new KeyValuePair<int, CommandPaletteItem>(score, item));
                }
            }

            scored.Sort(delegate(KeyValuePair<int, CommandPaletteItem> a, KeyValuePair<int, CommandPaletteItem> b)
            {
                int cmp = b.Key.CompareTo(a.Key);
                if (cmp != 0) return cmp;
                return string.Compare(a.Value.Title, b.Value.Title, StringComparison.OrdinalIgnoreCase);
            });

            List<CommandPaletteItem> results = new List<CommandPaletteItem>(scored.Count);
            for (int i = 0; i < scored.Count; i++)
            {
                results.Add(scored[i].Value);
            }
            return results;
        }
    }
}
