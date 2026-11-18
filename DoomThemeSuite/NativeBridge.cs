using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DoomThemeSuite
{
    /// <summary>
    /// Headless JSON-RPC / CLI bridge & background daemon for Tauri v2 + React 19 UI.
    /// Zero external dependencies (.NET Framework 4.x built-ins only).
    /// </summary>
    public static class NativeBridge
    {
        private static Control _uiMarshaler;
        private static System.Windows.Forms.Timer _smartFocusTimer;
        private static readonly object _stdoutLock = new object();

        // ====================================================================
        // Entry Points
        // ====================================================================

        public static int RunCli(string jsonInput)
        {
            ConfigureUtf8Console();
            if (string.IsNullOrWhiteSpace(jsonInput))
            {
                WriteJsonLine(ErrorResponse("Empty JSON command."));
                return 1;
            }

            try
            {
                Dictionary<string, object> req = MiniJson.Deserialize(jsonInput) as Dictionary<string, object>;
                if (req == null)
                {
                    WriteJsonLine(ErrorResponse("Invalid JSON object."));
                    return 1;
                }

                Dictionary<string, object> resp = DispatchCommand(req);
                WriteJsonLine(resp);
                bool ok = resp.ContainsKey("ok") && resp["ok"] is bool && (bool)resp["ok"];
                return ok ? 0 : 1;
            }
            catch (Exception ex)
            {
                WriteJsonLine(ErrorResponse(ex.Message));
                return 1;
            }
        }

        public static int RunDaemon()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ApplyAllFromConfig(false);
            StartSmartFocusLoop();

            Application.Run(new ApplicationContext());
            StopSmartFocusLoop();
            try { DoomGlassDaemon.StopDaemon(true); } catch { }
            try { DesktopHudWidget.Stop(); } catch { }
            try { DisplayOverlayManager.DisposeOverlay(); } catch { }
            return 0;
        }

        public static int RunBridgeLoop()
        {
            ConfigureUtf8Console();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _uiMarshaler = new Control();
            IntPtr unusedHandle = _uiMarshaler.Handle; // force handle creation on STA thread

            StartSmartFocusLoop();

            Thread stdinThread = new Thread(delegate()
            {
                try
                {
                    string line;
                    while ((line = Console.In.ReadLine()) != null)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length == 0)
                            continue;

                        Dictionary<string, object> resp = ExecuteLineOnUiThread(trimmed);
                        WriteJsonLine(resp);
                    }
                }
                catch (Exception ex)
                {
                    WriteJsonLine(ErrorResponse("Bridge stdin error: " + ex.Message));
                }
                finally
                {
                    try
                    {
                        if (_uiMarshaler != null && !_uiMarshaler.IsDisposed && _uiMarshaler.IsHandleCreated)
                        {
                            _uiMarshaler.BeginInvoke((MethodInvoker)delegate
                            {
                                Application.ExitThread();
                            });
                        }
                        else
                        {
                            Application.ExitThread();
                        }
                    }
                    catch { }
                }
            });
            stdinThread.IsBackground = true;
            stdinThread.Start();

            Application.Run(new ApplicationContext());

            StopSmartFocusLoop();
            try { DoomGlassDaemon.StopDaemon(true); } catch { }
            try { DesktopHudWidget.Stop(); } catch { }
            try { DisplayOverlayManager.DisposeOverlay(); } catch { }
            return 0;
        }

        private static Dictionary<string, object> ExecuteLineOnUiThread(string jsonLine)
        {
            try
            {
                Dictionary<string, object> req = MiniJson.Deserialize(jsonLine) as Dictionary<string, object>;
                if (req == null)
                    return ErrorResponse("Invalid JSON object.");

                if (_uiMarshaler != null && !_uiMarshaler.IsDisposed && _uiMarshaler.IsHandleCreated && _uiMarshaler.InvokeRequired)
                {
                    Dictionary<string, object> result = null;
                    _uiMarshaler.Invoke((MethodInvoker)delegate
                    {
                        result = DispatchCommand(req);
                    });
                    return result ?? ErrorResponse("Null response from UI thread.");
                }

                return DispatchCommand(req);
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        private static void ConfigureUtf8Console()
        {
            try { Console.InputEncoding = new UTF8Encoding(false); } catch { }
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
        }

        private static void WriteJsonLine(Dictionary<string, object> obj)
        {
            string json = MiniJson.Serialize(obj);
            lock (_stdoutLock)
            {
                Console.Out.WriteLine(json);
                Console.Out.Flush();
            }
        }

        private static void StartSmartFocusLoop()
        {
            try
            {
                Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
                SmartFocusMonitor.Enabled = GetCfgBool(cfg, "SmartFocusEnabled", true);
                SmartFocusMonitor.ResumeDelaySeconds = GetCfgInt(cfg, "SmartFocusResumeDelaySec", 3);
            }
            catch { }

            if (_smartFocusTimer == null)
            {
                _smartFocusTimer = new System.Windows.Forms.Timer();
                _smartFocusTimer.Interval = 1000;
                _smartFocusTimer.Tick += delegate
                {
                    try
                    {
                        if (SmartFocusMonitor.Enabled)
                        {
                            string reason;
                            bool shouldPause = SmartFocusMonitor.ShouldPauseEffects(out reason);
                            DesktopHudWidget.IsSmartPaused = shouldPause;
                        }
                        else
                        {
                            DesktopHudWidget.IsSmartPaused = false;
                        }
                    }
                    catch { }
                };
                _smartFocusTimer.Start();
            }
        }

        private static void StopSmartFocusLoop()
        {
            if (_smartFocusTimer != null)
            {
                try { _smartFocusTimer.Stop(); } catch { }
                try { _smartFocusTimer.Dispose(); } catch { }
                _smartFocusTimer = null;
            }
        }

        // ====================================================================
        // Command Dispatcher
        // ====================================================================

        private static Dictionary<string, object> DispatchCommand(Dictionary<string, object> req)
        {
            string cmd = GetString(req, "cmd", "").Trim().ToLowerInvariant();
            if (cmd.Length == 0)
                return ErrorResponse("Missing 'cmd' field.");

            Dictionary<string, object> payload = GetDict(req, "payload");

            switch (cmd)
            {
                case "get_state":
                    return OkResponse(BuildStateData());

                case "apply_config":
                    return HandleApplyConfig(payload);

                case "apply_preset":
                    return HandleApplyPreset(payload);

                case "chameleon_extract":
                    return HandleChameleonExtract(payload);

                case "purge_ram":
                    return HandlePurgeRam();

                case "toggle_icons":
                    return HandleToggleIcons(payload);

                case "set_display_overlay":
                    return HandleSetDisplayOverlay(payload);

                case "set_boost_profile":
                    return HandleSetBoostProfile(payload);

                case "pomodoro_action":
                    return HandlePomodoroAction(payload);

                case "hud_action":
                    return HandleHudAction(payload);

                case "rollback_snapshot":
                    return HandleRollbackSnapshot(payload);

                case "export_pkg":
                    return HandleExportPkg(payload);

                case "import_pkg":
                    return HandleImportPkg(payload);

                case "toggle_glass":
                    return HandleToggleGlass(payload);

                default:
                    return ErrorResponse("Unknown command: " + cmd);
            }
        }

        // ====================================================================
        // Command Handlers
        // ====================================================================

        private static Dictionary<string, object> BuildStateData()
        {
            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();

            List<object> presetsList = new List<object>();
            List<ThemePreset> allPresets = ThemeCatalog.GetAllPresets();
            if (allPresets != null)
            {
                for (int i = 0; i < allPresets.Count; i++)
                {
                    presetsList.Add(SerializePreset(allPresets[i]));
                }
            }

            double usedGb, freeGb, totalGb;
            int ramUsagePct;
            if (!DoomThemeEngine.GetSystemMemoryStats(out usedGb, out freeGb, out totalGb, out ramUsagePct))
            {
                usedGb = DesktopHudWidget.LatestRamUsedGb;
                totalGb = DesktopHudWidget.LatestRamTotalGb;
                freeGb = Math.Max(0.0, totalGb - usedGb);
                ramUsagePct = DesktopHudWidget.LatestRamPercent;
            }

            string smartReason = "";
            bool smartPaused = DesktopHudWidget.IsSmartPaused;
            try
            {
                if (SmartFocusMonitor.Enabled)
                {
                    string r;
                    smartPaused = SmartFocusMonitor.ShouldPauseEffects(out r);
                    smartReason = smartPaused ? (r ?? "Tam Ekran Uygulama") : "";
                }
            }
            catch { }

            bool iconsVisible = false;
            try { iconsVisible = DoomThemeEngine.AreDesktopIconsVisible(); } catch { }

            Dictionary<string, object> telemetry = new Dictionary<string, object>();
            telemetry["cpuPercent"] = DesktopHudWidget.LatestCpuPercent;
            telemetry["ramUsedGb"] = Math.Round(usedGb, 2);
            telemetry["ramFreeGb"] = Math.Round(freeGb, 2);
            telemetry["ramTotalGb"] = Math.Round(totalGb, 2);
            telemetry["ramUsagePercent"] = ramUsagePct;
            telemetry["gpuUtilPercent"] = DesktopHudWidget.LatestGpuUtilPercent;
            telemetry["gpuTempC"] = DesktopHudWidget.LatestGpuTempC;
            telemetry["audioPeak"] = Math.Round((double)DesktopHudWidget.CurrentAudioPeakLevel, 4);
            telemetry["pomodoroRunning"] = DesktopHudWidget.PomodoroRunning;
            telemetry["pomodoroPhase"] = DesktopHudWidget.PomodoroPhase ?? "Focus";
            telemetry["pomodoroRemainingSec"] = DesktopHudWidget.PomodoroRemainingSeconds;
            telemetry["pomodoroCompleted"] = DesktopHudWidget.PomodoroCompletedSessions;
            telemetry["smartPaused"] = smartPaused;
            telemetry["smartPauseReason"] = smartReason;
            telemetry["desktopIconsVisible"] = iconsVisible;
            telemetry["glassDaemonRunning"] = DoomGlassDaemon.IsRunning;

            List<object> snapshotsList = new List<object>();
            List<ThemeSnapshot> snaps = ThemeSnapshotManager.GetSnapshots();
            if (snaps != null)
            {
                for (int i = 0; i < snaps.Count; i++)
                {
                    ThemeSnapshot s = snaps[i];
                    Dictionary<string, object> sd = new Dictionary<string, object>();
                    sd["id"] = s.Id ?? "";
                    sd["timestamp"] = s.Timestamp ?? "";
                    sd["label"] = s.Label ?? "";
                    sd["presetId"] = s.PresetId ?? "";
                    sd["primaryAccentHex"] = s.PrimaryAccentHex ?? "#1F4A31";
                    sd["rimHighlightHex"] = s.RimHighlightHex ?? "#6CA881";
                    sd["shadowGlowHex"] = s.ShadowGlowHex ?? "#267347";
                    snapshotsList.Add(sd);
                }
            }

            List<object> cpuHistList = new List<object>();
            float[] cpuHist = DesktopHudWidget.GetCpuHistorySnapshot();
            if (cpuHist != null)
            {
                for (int i = 0; i < cpuHist.Length; i++)
                {
                    cpuHistList.Add(Math.Round((double)cpuHist[i], 2));
                }
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["config"] = cfg;
            data["presets"] = presetsList;
            data["telemetry"] = telemetry;
            data["snapshots"] = snapshotsList;
            data["cpuHistory"] = cpuHistList;
            return data;
        }

        private static Dictionary<string, object> HandleApplyConfig(Dictionary<string, object> payload)
        {
            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
            Dictionary<string, object> source = payload;
            if (payload.ContainsKey("config") && payload["config"] is Dictionary<string, object>)
            {
                source = (Dictionary<string, object>)payload["config"];
            }

            foreach (KeyValuePair<string, object> kvp in source)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null)
                    continue;
                if (string.Equals(kvp.Key, "applyWallpaper", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (kvp.Value is bool)
                    cfg[kvp.Key] = ((bool)kvp.Value) ? "1" : "0";
                else
                    cfg[kvp.Key] = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture);
            }

            bool applyWallpaper = GetBool(payload, "applyWallpaper", false);
            DoomThemeEngine.SaveConfig(cfg);
            ApplyAllFromConfig(applyWallpaper);

            string activeId = GetCfgString(cfg, "ActiveProfile", "DoomEmerald");
            ThemeSnapshotManager.RecordSnapshot(activeId, cfg);

            return OkResponse(BuildStateData());
        }

        private static Dictionary<string, object> HandleApplyPreset(Dictionary<string, object> payload)
        {
            string id = GetString(payload, "id", "");
            if (string.IsNullOrEmpty(id))
                return ErrorResponse("Missing preset 'id'.");

            ThemePreset preset = ThemeCatalog.GetPresetById(id);
            if (preset == null)
                return ErrorResponse("Preset not found: " + id);

            bool applyWallpaper = GetBool(payload, "applyWallpaper", false);

            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
            cfg["ActiveProfile"] = preset.Id;
            cfg["BodyTintHex"] = ColorToHex(preset.ExplorerBodyTint);
            cfg["ShadowGlowHex"] = ColorToHex(preset.ExplorerShadowGlow);
            cfg["RimHighlightHex"] = ColorToHex(preset.RimHighlight);
            cfg["PrimaryAccentHex"] = ColorToHex(preset.PrimaryAccent);
            cfg["TaskbarTintHex"] = ColorToHex(preset.TaskbarTint);
            cfg["EmeraldTintIntensity"] = preset.DefaultTintAlpha.ToString(CultureInfo.InvariantCulture);
            cfg["EmeraldShadowIntensity"] = preset.DefaultShadowAlpha.ToString(CultureInfo.InvariantCulture);
            cfg["ShadowSpread"] = preset.DefaultShadowSpread.ToString(CultureInfo.InvariantCulture);
            DoomThemeEngine.SaveConfig(cfg);

            ApplyAllFromConfig(applyWallpaper);
            ThemeSnapshotManager.RecordSnapshot(preset.DisplayName ?? preset.Id, cfg);

            return OkResponse(BuildStateData());
        }

        private static Dictionary<string, object> HandleChameleonExtract(Dictionary<string, object> payload)
        {
            string imagePath = GetString(payload, "imagePath", "");
            if (string.IsNullOrEmpty(imagePath))
            {
                imagePath = GetCurrentWallpaperPath();
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    imagePath = DoomThemeEngine.DoomWallpaperPath;
                }
            }

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return ErrorResponse("Wallpaper image not found: " + imagePath);

            bool autoApply = GetBool(payload, "autoApply", false);
            string customName = GetString(payload, "name", "ChameleonTheme");

            Color[] swatches = WallpaperPaletteExtractor.ExtractDominantSwatches(imagePath, 6);
            ThemePreset extracted = WallpaperPaletteExtractor.ExtractThemeFromWallpaper(imagePath, customName);
            if (extracted == null)
                return ErrorResponse("Failed to extract theme from wallpaper.");

            double contrastScore = ColorHarmonyGenerator.CalculateContrastRatio(Color.White, extracted.UiBackground);

            List<object> swatchHexes = new List<object>();
            if (swatches != null)
            {
                for (int i = 0; i < swatches.Length; i++)
                {
                    swatchHexes.Add(ColorToHex(swatches[i]));
                }
            }

            if (autoApply)
            {
                ThemeCatalog.SaveCustomPreset(extracted);
                Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
                cfg["ActiveProfile"] = extracted.Id;
                cfg["BodyTintHex"] = ColorToHex(extracted.ExplorerBodyTint);
                cfg["ShadowGlowHex"] = ColorToHex(extracted.ExplorerShadowGlow);
                cfg["RimHighlightHex"] = ColorToHex(extracted.RimHighlight);
                cfg["PrimaryAccentHex"] = ColorToHex(extracted.PrimaryAccent);
                cfg["TaskbarTintHex"] = ColorToHex(extracted.TaskbarTint);
                DoomThemeEngine.SaveConfig(cfg);
                ApplyAllFromConfig(false);
                ThemeSnapshotManager.RecordSnapshot(extracted.DisplayName ?? "Chameleon", cfg);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["imagePath"] = imagePath;
            data["swatches"] = swatchHexes;
            data["contrastScore"] = Math.Round(contrastScore, 2);
            data["preset"] = SerializePreset(extracted);
            data["applied"] = autoApply;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandlePurgeRam()
        {
            long freedBytes;
            int procCount;
            DoomThemeEngine.PurgeStandbyAndWorkingSet(out freedBytes, out procCount);

            double usedGb, freeGb, totalGb;
            int usagePct;
            DoomThemeEngine.GetSystemMemoryStats(out usedGb, out freeGb, out totalGb, out usagePct);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["freedMb"] = Math.Round(freedBytes / (1024.0 * 1024.0), 2);
            data["procCount"] = procCount;
            data["freeGb"] = Math.Round(freeGb, 2);
            data["usagePercent"] = usagePct;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleToggleIcons(Dictionary<string, object> payload)
        {
            bool visible;
            if (payload.ContainsKey("visible"))
            {
                bool target = GetBool(payload, "visible", true);
                DoomThemeEngine.SetDesktopIconsVisible(target);
                visible = DoomThemeEngine.AreDesktopIconsVisible();
            }
            else
            {
                visible = DoomThemeEngine.ToggleDesktopIconsVisibility();
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["visible"] = visible;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleSetDisplayOverlay(Dictionary<string, object> payload)
        {
            int dimmer = ClampInt(GetInt(payload, "dimmerPercent", DisplayOverlayManager.DimmerPercent), 0, 80);
            int warmth = ClampInt(GetInt(payload, "warmthKelvin", DisplayOverlayManager.WarmthKelvin), 2700, 6500);

            if (dimmer > 0 || warmth < 6500)
            {
                DisplayOverlayManager.ApplyOverlay(dimmer, warmth);
            }
            else
            {
                DisplayOverlayManager.DisableOverlay();
            }

            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
            cfg["DisplayDimmerPercent"] = dimmer.ToString(CultureInfo.InvariantCulture);
            cfg["DisplayWarmthKelvin"] = warmth.ToString(CultureInfo.InvariantCulture);
            DoomThemeEngine.SaveConfig(cfg);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["dimmerPercent"] = dimmer;
            data["warmthKelvin"] = warmth;
            data["isActive"] = DisplayOverlayManager.IsActive;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleSetBoostProfile(Dictionary<string, object> payload)
        {
            string raw = GetString(payload, "profile", "Normal").Trim();
            BoostProfile profile = BoostProfile.Normal;
            int num;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
            {
                if (Enum.IsDefined(typeof(BoostProfile), num))
                    profile = (BoostProfile)num;
            }
            else
            {
                if (string.Equals(raw, "Oyun", StringComparison.OrdinalIgnoreCase)) profile = BoostProfile.Oyun;
                else if (string.Equals(raw, "Render", StringComparison.OrdinalIgnoreCase)) profile = BoostProfile.Render;
                else if (string.Equals(raw, "Sessiz", StringComparison.OrdinalIgnoreCase)) profile = BoostProfile.Sessiz;
                else profile = BoostProfile.Normal;
            }

            DoomThemeEngine.ApplyBoostProfile(profile);

            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
            cfg["BoostProfile"] = ((int)profile).ToString(CultureInfo.InvariantCulture);
            DoomThemeEngine.SaveConfig(cfg);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["profile"] = profile.ToString();
            data["profileIndex"] = (int)profile;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandlePomodoroAction(Dictionary<string, object> payload)
        {
            string action = GetString(payload, "action", "").Trim().ToLowerInvariant();
            switch (action)
            {
                case "start_pause":
                    DesktopHudWidget.StartPausePomodoro();
                    break;
                case "reset":
                    DesktopHudWidget.ResetPomodoro();
                    break;
                case "skip":
                    DesktopHudWidget.SkipPomodoroPhase();
                    break;
                case "config":
                    int focusMin = ClampInt(GetInt(payload, "focusMin", DesktopHudWidget.PomodoroFocusMinutes), 1, 180);
                    int shortMin = ClampInt(GetInt(payload, "shortMin", DesktopHudWidget.PomodoroShortBreakMinutes), 1, 60);
                    int longMin = ClampInt(GetInt(payload, "longMin", DesktopHudWidget.PomodoroLongBreakMinutes), 1, 90);
                    DesktopHudWidget.ConfigurePomodoroDurations(focusMin, shortMin, longMin);
                    break;
                default:
                    return ErrorResponse("Unknown pomodoro action: " + action);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["pomodoroRunning"] = DesktopHudWidget.PomodoroRunning;
            data["pomodoroPhase"] = DesktopHudWidget.PomodoroPhase ?? "Focus";
            data["pomodoroRemainingSec"] = DesktopHudWidget.PomodoroRemainingSeconds;
            data["pomodoroCompleted"] = DesktopHudWidget.PomodoroCompletedSessions;
            data["focusMin"] = DesktopHudWidget.PomodoroFocusMinutes;
            data["shortMin"] = DesktopHudWidget.PomodoroShortBreakMinutes;
            data["longMin"] = DesktopHudWidget.PomodoroLongBreakMinutes;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleHudAction(Dictionary<string, object> payload)
        {
            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();

            bool hudEnabled = payload.ContainsKey("hudEnabled")
                ? GetBool(payload, "hudEnabled", DesktopHudWidget.Enabled)
                : GetCfgBool(cfg, "HudEnabled", true);
            bool locked = payload.ContainsKey("locked")
                ? GetBool(payload, "locked", DesktopHudWidget.WidgetsLocked)
                : GetCfgBool(cfg, "WidgetsLocked", true);
            int scale = ClampInt(payload.ContainsKey("scale")
                ? GetInt(payload, "scale", DesktopHudWidget.WidgetScalePercent)
                : GetCfgInt(cfg, "WidgetScale", 100), 60, 160);
            int opacity = ClampInt(payload.ContainsKey("opacity")
                ? GetInt(payload, "opacity", DesktopHudWidget.HudOpacity)
                : GetCfgInt(cfg, "HudOpacity", 230), 60, 255);
            int specStyle = ClampInt(payload.ContainsKey("spectrumStyle")
                ? GetInt(payload, "spectrumStyle", DesktopHudWidget.SpectrumStyle)
                : GetCfgInt(cfg, "SpectrumStyle", 0), 0, 2);
            int specBands = ClampInt(payload.ContainsKey("spectrumBands")
                ? GetInt(payload, "spectrumBands", DesktopHudWidget.SpectrumBandCount)
                : GetCfgInt(cfg, "SpectrumBandCount", 32), 16, 64);
            int specSmoothing = ClampInt(payload.ContainsKey("spectrumSmoothing")
                ? GetInt(payload, "spectrumSmoothing", (int)(DesktopHudWidget.SpectrumSmoothing * 100f))
                : GetCfgInt(cfg, "SpectrumSmoothing", 65), 0, 95);

            cfg["HudEnabled"] = hudEnabled ? "1" : "0";
            cfg["WidgetsLocked"] = locked ? "1" : "0";
            cfg["WidgetScale"] = scale.ToString(CultureInfo.InvariantCulture);
            cfg["HudOpacity"] = opacity.ToString(CultureInfo.InvariantCulture);
            cfg["SpectrumStyle"] = specStyle.ToString(CultureInfo.InvariantCulture);
            cfg["SpectrumBandCount"] = specBands.ToString(CultureInfo.InvariantCulture);
            cfg["SpectrumSmoothing"] = specSmoothing.ToString(CultureInfo.InvariantCulture);

            Dictionary<string, object> widgets = GetDict(payload, "widgets");
            if (widgets != null && widgets.Count > 0)
            {
                ApplyWidgetVisibilityOverrides(widgets, cfg);
            }

            DoomThemeEngine.SaveConfig(cfg);
            ApplyHudFromConfig(cfg);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["hudEnabled"] = DesktopHudWidget.Enabled;
            data["locked"] = DesktopHudWidget.WidgetsLocked;
            data["scale"] = DesktopHudWidget.WidgetScalePercent;
            data["opacity"] = (int)DesktopHudWidget.HudOpacity;
            data["spectrumStyle"] = DesktopHudWidget.SpectrumStyle;
            data["spectrumBands"] = DesktopHudWidget.SpectrumBandCount;
            data["spectrumSmoothing"] = (int)Math.Round(DesktopHudWidget.SpectrumSmoothing * 100f);
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleRollbackSnapshot(Dictionary<string, object> payload)
        {
            string id = GetString(payload, "id", "");
            if (string.IsNullOrEmpty(id))
                return ErrorResponse("Missing snapshot 'id'.");

            bool ok = ThemeSnapshotManager.RollbackToSnapshot(id);
            if (!ok)
                return ErrorResponse("Snapshot not found or rollback failed: " + id);

            ApplyAllFromConfig(false);

            Dictionary<string, object> data = BuildStateData();
            data["rolledBackId"] = id;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleExportPkg(Dictionary<string, object> payload)
        {
            string path = GetString(payload, "path", "");
            if (string.IsNullOrEmpty(path))
                return ErrorResponse("Missing export 'path'.");

            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
            ThemePreset preset = BuildEffectivePresetFromConfig(cfg);
            bool ok = DoomThemePackage.ExportToFile(preset, path, cfg);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["exported"] = ok;
            data["path"] = path;
            return ok ? OkResponse(data) : ErrorResponse("Failed to export package to: " + path);
        }

        private static Dictionary<string, object> HandleImportPkg(Dictionary<string, object> payload)
        {
            string path = GetString(payload, "path", "");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return ErrorResponse("Package file not found: " + path);

            bool isValid;
            string validationMsg;
            DoomThemePackage pkg = DoomThemePackage.ImportFromFile(path, out isValid, out validationMsg);
            if (pkg == null || !isValid)
                return ErrorResponse("Invalid package: " + (validationMsg ?? "unknown error"));

            bool apply = GetBool(payload, "apply", true);
            if (apply && pkg.Preset != null)
            {
                if (!pkg.Preset.IsBuiltIn)
                    ThemeCatalog.SaveCustomPreset(pkg.Preset);

                Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
                if (pkg.Settings != null)
                {
                    foreach (KeyValuePair<string, string> kvp in pkg.Settings)
                    {
                        if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                            cfg[kvp.Key] = kvp.Value;
                    }
                }
                cfg["ActiveProfile"] = pkg.Preset.Id ?? "CustomTheme";
                DoomThemeEngine.SaveConfig(cfg);
                ApplyAllFromConfig(false);
                ThemeSnapshotManager.RecordSnapshot(pkg.Preset.DisplayName ?? pkg.Preset.Id, cfg);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["valid"] = isValid;
            data["message"] = validationMsg ?? "OK";
            data["applied"] = apply;
            data["preset"] = pkg.Preset != null ? SerializePreset(pkg.Preset) : null;
            return OkResponse(data);
        }

        private static Dictionary<string, object> HandleToggleGlass(Dictionary<string, object> payload)
        {
            bool enabled = payload.ContainsKey("enabled")
                ? GetBool(payload, "enabled", !DoomGlassDaemon.IsRunning)
                : !DoomGlassDaemon.IsRunning;

            if (enabled)
            {
                Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
                ApplyGlassFromConfig(cfg, true);
            }
            else
            {
                DoomGlassDaemon.StopDaemon(true);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["glassDaemonRunning"] = DoomGlassDaemon.IsRunning;
            return OkResponse(data);
        }

        // ====================================================================
        // Config -> Engine Synchronization Helpers
        // ====================================================================

        private static void ApplyAllFromConfig(bool applyWallpaper)
        {
            Dictionary<string, string> cfg = DoomThemeEngine.LoadConfig();
            ThemePreset effective = BuildEffectivePresetFromConfig(cfg);

            try { DoomThemeEngine.ApplyPreset(effective, applyWallpaper); } catch { }

            bool shouldStartGlass = GetCfgBool(cfg, "ExplorerGlassEnabled", false)
                || GetCfgBool(cfg, "EmeraldTintAndShadowEnabled", true)
                || GetCfgInt(cfg, "TaskbarMode", 1) != 0;

            try { ApplyGlassFromConfig(cfg, shouldStartGlass); } catch { }
            try { ApplyHudFromConfig(cfg); } catch { }

            try
            {
                SmartFocusMonitor.Enabled = GetCfgBool(cfg, "SmartFocusEnabled", true);
                SmartFocusMonitor.ResumeDelaySeconds = GetCfgInt(cfg, "SmartFocusResumeDelaySec", 3);

                int dimmer = ClampInt(GetCfgInt(cfg, "DisplayDimmerPercent", 0), 0, 80);
                int warmth = ClampInt(GetCfgInt(cfg, "DisplayWarmthKelvin", 6500), 2700, 6500);
                if (dimmer > 0 || warmth < 6500)
                    DisplayOverlayManager.ApplyOverlay(dimmer, warmth);
                else
                    DisplayOverlayManager.DisableOverlay();
            }
            catch { }
        }

        private static ThemePreset BuildEffectivePresetFromConfig(Dictionary<string, string> cfg)
        {
            string activeId = GetCfgString(cfg, "ActiveProfile", "DoomEmerald");
            ThemePreset basePreset = ThemeCatalog.GetPresetById(activeId) ?? ThemeCatalog.GetPresetById("DoomEmerald") ?? new ThemePreset();
            ThemePreset live = basePreset.Clone();

            Color col;
            if (TryParseHexColor(GetCfgString(cfg, "BodyTintHex", ""), out col)) live.ExplorerBodyTint = col;
            if (TryParseHexColor(GetCfgString(cfg, "ShadowGlowHex", ""), out col)) live.ExplorerShadowGlow = col;
            if (TryParseHexColor(GetCfgString(cfg, "RimHighlightHex", ""), out col)) live.RimHighlight = col;
            if (TryParseHexColor(GetCfgString(cfg, "PrimaryAccentHex", ""), out col)) live.PrimaryAccent = col;
            if (TryParseHexColor(GetCfgString(cfg, "TaskbarTintHex", ""), out col)) live.TaskbarTint = col;

            live.DefaultTintAlpha = (byte)ClampInt(GetCfgInt(cfg, "EmeraldTintIntensity", live.DefaultTintAlpha), 0, 140);
            live.DefaultShadowAlpha = (byte)ClampInt(GetCfgInt(cfg, "EmeraldShadowIntensity", live.DefaultShadowAlpha), 0, 255);
            live.DefaultShadowSpread = ClampInt(GetCfgInt(cfg, "ShadowSpread", live.DefaultShadowSpread), 8, 48);
            return live;
        }

        private static void ApplyGlassFromConfig(Dictionary<string, string> cfg, bool startDaemon)
        {
            ThemePreset effective = BuildEffectivePresetFromConfig(cfg);

            bool glassEnabled = GetCfgBool(cfg, "ExplorerGlassEnabled", false);
            int explorerAlpha = ClampInt(GetCfgInt(cfg, "ExplorerAlpha", 238), 160, 255);
            int blurMode = ClampInt(GetCfgInt(cfg, "ExplorerBlurMode", 0), 0, 2);
            int taskbarMode = ClampInt(GetCfgInt(cfg, "TaskbarMode", 1), 0, 3);

            bool tintShadowEnabled = GetCfgBool(cfg, "EmeraldTintAndShadowEnabled", true);
            int tintInt = ClampInt(GetCfgInt(cfg, "EmeraldTintIntensity", 72), 0, 140);
            int shadowInt = ClampInt(GetCfgInt(cfg, "EmeraldShadowIntensity", 185), 0, 255);
            int spread = ClampInt(GetCfgInt(cfg, "ShadowSpread", 28), 8, 48);

            DoomGlassDaemon.ShowRimBorder = GetCfgBool(cfg, "ShowRimBorder", true);
            DoomGlassDaemon.ShadowOnlyActiveWindow = GetCfgBool(cfg, "ShadowOnlyActiveWindow", false);
            DoomGlassDaemon.TaskbarCenterIcons = GetCfgBool(cfg, "TaskbarCenterIcons", false);
            DoomGlassDaemon.CustomTaskbarTintColor = effective.TaskbarTint;
            DoomGlassDaemon.CustomTaskbarAlpha = (byte)ClampInt(GetCfgInt(cfg, "TaskbarTintAlpha", 180), 0, 255);

            DoomGlassDaemon.ApplyPresetOverlayColors(
                effective.ExplorerBodyTint,
                effective.ExplorerShadowGlow,
                effective.RimHighlight,
                spread,
                (byte)tintInt,
                (byte)shadowInt);
            DoomGlassDaemon.UpdateEmeraldOverlaySettings(tintShadowEnabled, tintInt, shadowInt);
            DoomGlassDaemon.UpdateSettings(glassEnabled, (byte)explorerAlpha, blurMode, taskbarMode);

            if (startDaemon)
            {
                DoomGlassDaemon.StartDaemon();
                DoomGlassDaemon.RefreshAllOpenWindows();
                DoomGlassDaemon.SyncAllExplorerOverlays(true);
            }
        }

        private static void ApplyHudFromConfig(Dictionary<string, string> cfg)
        {
            ThemePreset effective = BuildEffectivePresetFromConfig(cfg);
            bool hudEnabled = GetCfgBool(cfg, "HudEnabled", true);
            int layout = ClampInt(GetCfgInt(cfg, "WidgetLayout", 0), 0, 5);
            int scale = ClampInt(GetCfgInt(cfg, "WidgetScale", 100), 60, 160);
            int opacity = ClampInt(GetCfgInt(cfg, "HudOpacity", 230), 60, 255);
            bool locked = GetCfgBool(cfg, "WidgetsLocked", true);

            DesktopHudWidget.Enabled = hudEnabled;
            DesktopHudWidget.ShowClock = GetCfgBool(cfg, "WidgetClock", true);
            DesktopHudWidget.ShowSystemGpu = GetCfgBool(cfg, "WidgetSystemGpu", true);
            DesktopHudWidget.ShowStorage = GetCfgBool(cfg, "WidgetStorage", true);
            DesktopHudWidget.ShowNetwork = GetCfgBool(cfg, "WidgetNetwork", true);
            DesktopHudWidget.ShowMedia = GetCfgBool(cfg, "WidgetMedia", true);
            DesktopHudWidget.ShowNotes = GetCfgBool(cfg, "WidgetNotes", true);
            DesktopHudWidget.ShowSpectrum = GetCfgBool(cfg, "WidgetSpectrum", true);
            DesktopHudWidget.ShowPomodoro = GetCfgBool(cfg, "WidgetPomodoro", true);
            DesktopHudWidget.WidgetsLocked = locked;
            DesktopHudWidget.LayoutPreset = layout;
            DesktopHudWidget.WidgetScalePercent = scale;
            DesktopHudWidget.HudOpacity = (byte)opacity;
            DesktopHudWidget.SpectrumStyle = ClampInt(GetCfgInt(cfg, "SpectrumStyle", 0), 0, 2);
            DesktopHudWidget.SpectrumBandCount = ClampInt(GetCfgInt(cfg, "SpectrumBandCount", 32), 16, 64);
            DesktopHudWidget.SpectrumSmoothing = ClampInt(GetCfgInt(cfg, "SpectrumSmoothing", 65), 0, 95) / 100.0f;

            string title = !string.IsNullOrEmpty(effective.DisplayName) ? effective.DisplayName : effective.Id;
            DesktopHudWidget.StartOrUpdate(
                hudEnabled,
                layout,
                scale,
                (byte)opacity,
                locked,
                effective.UiCard,
                effective.RimHighlight,
                Color.FromArgb(255, 224, 240, 230),
                title);
        }

        private static void ApplyWidgetVisibilityOverrides(Dictionary<string, object> widgets, Dictionary<string, string> cfg)
        {
            foreach (KeyValuePair<string, object> kvp in widgets)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null)
                    continue;

                bool vis = ToBool(kvp.Value, true);
                string k = kvp.Key.Trim().ToLowerInvariant();
                string vStr = vis ? "1" : "0";

                if (k == "clockdate" || k == "widgetclock" || k == "clock") cfg["WidgetClock"] = vStr;
                else if (k == "systemgpu" || k == "widgetsystemgpu") cfg["WidgetSystemGpu"] = vStr;
                else if (k == "storagedrives" || k == "widgetstorage" || k == "storage") cfg["WidgetStorage"] = vStr;
                else if (k == "networktraffic" || k == "widgetnetwork" || k == "network") cfg["WidgetNetwork"] = vStr;
                else if (k == "mediaaudio" || k == "widgetmedia" || k == "media") cfg["WidgetMedia"] = vStr;
                else if (k == "notesuptime" || k == "widgetnotes" || k == "notes") cfg["WidgetNotes"] = vStr;
                else if (k == "spectrum" || k == "widgetspectrum" || k == "audiospectrum") cfg["WidgetSpectrum"] = vStr;
                else if (k == "pomodoro" || k == "widgetpomodoro" || k == "pomodorotimer") cfg["WidgetPomodoro"] = vStr;
            }
        }

        // ====================================================================
        // Serialization & Utility Helpers
        // ====================================================================

        private static Dictionary<string, object> SerializePreset(ThemePreset p)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            if (p == null) return d;
            d["id"] = p.Id ?? "";
            d["displayName"] = p.DisplayName ?? "";
            d["subtitle"] = p.Subtitle ?? "";
            d["isBuiltIn"] = p.IsBuiltIn;
            d["uiBackground"] = ColorToHex(p.UiBackground);
            d["uiCard"] = ColorToHex(p.UiCard);
            d["primaryAccent"] = ColorToHex(p.PrimaryAccent);
            d["inactiveAccent"] = ColorToHex(p.InactiveAccent);
            d["rimHighlight"] = ColorToHex(p.RimHighlight);
            d["explorerBodyTint"] = ColorToHex(p.ExplorerBodyTint);
            d["explorerShadowGlow"] = ColorToHex(p.ExplorerShadowGlow);
            d["taskbarTint"] = ColorToHex(p.TaskbarTint);
            d["secondaryAccent"] = ColorToHex(p.SecondaryAccent);
            d["defaultTintAlpha"] = (int)p.DefaultTintAlpha;
            d["defaultShadowAlpha"] = (int)p.DefaultShadowAlpha;
            d["defaultShadowSpread"] = p.DefaultShadowSpread;
            d["wallpaperPath"] = p.WallpaperPath ?? "";
            d["isFavorite"] = p.IsFavorite;
            d["tags"] = p.Tags ?? "";
            return d;
        }

        private static string GetCurrentWallpaperPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("WallPaper");
                        if (val != null)
                        {
                            string s = val.ToString();
                            if (!string.IsNullOrEmpty(s) && File.Exists(s))
                                return s;
                        }
                    }
                }
            }
            catch { }
            return DoomThemeEngine.DoomWallpaperPath;
        }

        private static Dictionary<string, object> OkResponse(object data)
        {
            Dictionary<string, object> resp = new Dictionary<string, object>();
            resp["ok"] = true;
            resp["data"] = data;
            return resp;
        }

        private static Dictionary<string, object> ErrorResponse(string message)
        {
            Dictionary<string, object> resp = new Dictionary<string, object>();
            resp["ok"] = false;
            resp["error"] = message ?? "Unknown error";
            return resp;
        }

        private static string ColorToHex(Color c)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        private static bool TryParseHexColor(string hex, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrEmpty(hex)) return false;
            string s = hex.Trim();
            if (s.StartsWith("#")) s = s.Substring(1);
            if (s.Length != 6) return false;
            int rgb;
            if (!int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                return false;
            color = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            return true;
        }

        private static int ClampInt(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private static string GetCfgString(Dictionary<string, string> cfg, string key, string fallback)
        {
            string val;
            if (cfg != null && cfg.TryGetValue(key, out val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim();
            return fallback;
        }

        private static int GetCfgInt(Dictionary<string, string> cfg, string key, int fallback)
        {
            string s = GetCfgString(cfg, key, "");
            int v;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        private static bool GetCfgBool(Dictionary<string, string> cfg, string key, bool fallback)
        {
            string s = GetCfgString(cfg, key, "");
            if (s.Length == 0) return fallback;
            return s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, object> GetDict(Dictionary<string, object> obj, string key)
        {
            object val;
            if (obj != null && obj.TryGetValue(key, out val) && val is Dictionary<string, object>)
                return (Dictionary<string, object>)val;
            return new Dictionary<string, object>();
        }

        private static string GetString(Dictionary<string, object> obj, string key, string fallback)
        {
            object val;
            if (obj != null && obj.TryGetValue(key, out val) && val != null)
                return Convert.ToString(val, CultureInfo.InvariantCulture);
            return fallback;
        }

        private static int GetInt(Dictionary<string, object> obj, string key, int fallback)
        {
            object val;
            if (obj == null || !obj.TryGetValue(key, out val) || val == null)
                return fallback;
            if (val is int) return (int)val;
            if (val is long) return (int)(long)val;
            if (val is double) return (int)Math.Round((double)val);
            int parsed;
            if (int.TryParse(Convert.ToString(val, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                return parsed;
            return fallback;
        }

        private static bool GetBool(Dictionary<string, object> obj, string key, bool fallback)
        {
            object val;
            if (obj == null || !obj.TryGetValue(key, out val) || val == null)
                return fallback;
            return ToBool(val, fallback);
        }

        private static bool ToBool(object val, bool fallback)
        {
            if (val is bool) return (bool)val;
            if (val is int) return ((int)val) != 0;
            if (val is long) return ((long)val) != 0L;
            string s = Convert.ToString(val, CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(s)) return fallback;
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1") return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || s == "0") return false;
            return fallback;
        }

        // ====================================================================
        // Self-Contained Tiny JSON Parser & Serializer (No External Deps)
        // ====================================================================

        private static class MiniJson
        {
            public static object Deserialize(string json)
            {
                if (json == null) return null;
                int index = 0;
                return ParseValue(json, ref index);
            }

            public static string Serialize(object obj)
            {
                StringBuilder sb = new StringBuilder(512);
                SerializeValue(obj, sb);
                return sb.ToString();
            }

            private static void SkipWhitespace(string s, ref int idx)
            {
                while (idx < s.Length && char.IsWhiteSpace(s[idx]))
                    idx++;
            }

            private static object ParseValue(string s, ref int idx)
            {
                SkipWhitespace(s, ref idx);
                if (idx >= s.Length)
                    throw new FormatException("Unexpected end of JSON.");

                char c = s[idx];
                if (c == '{') return ParseObject(s, ref idx);
                if (c == '[') return ParseArray(s, ref idx);
                if (c == '"') return ParseString(s, ref idx);
                if (c == 't') { ExpectLiteral(s, ref idx, "true"); return true; }
                if (c == 'f') { ExpectLiteral(s, ref idx, "false"); return false; }
                if (c == 'n') { ExpectLiteral(s, ref idx, "null"); return null; }
                if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber(s, ref idx);

                throw new FormatException("Unexpected token '" + c + "' at index " + idx);
            }

            private static void ExpectLiteral(string s, ref int idx, string lit)
            {
                if (idx + lit.Length > s.Length || string.Compare(s, idx, lit, 0, lit.Length, StringComparison.Ordinal) != 0)
                    throw new FormatException("Expected '" + lit + "' at index " + idx);
                idx += lit.Length;
            }

            private static Dictionary<string, object> ParseObject(string s, ref int idx)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                idx++; // '{'
                SkipWhitespace(s, ref idx);
                if (idx < s.Length && s[idx] == '}')
                {
                    idx++;
                    return dict;
                }

                while (true)
                {
                    SkipWhitespace(s, ref idx);
                    if (idx >= s.Length || s[idx] != '"')
                        throw new FormatException("Expected string key at index " + idx);
                    string key = ParseString(s, ref idx);

                    SkipWhitespace(s, ref idx);
                    if (idx >= s.Length || s[idx] != ':')
                        throw new FormatException("Expected ':' at index " + idx);
                    idx++;

                    object val = ParseValue(s, ref idx);
                    dict[key] = val;

                    SkipWhitespace(s, ref idx);
                    if (idx >= s.Length)
                        throw new FormatException("Unterminated object.");
                    if (s[idx] == '}')
                    {
                        idx++;
                        return dict;
                    }
                    if (s[idx] != ',')
                        throw new FormatException("Expected ',' or '}' at index " + idx);
                    idx++;
                }
            }

            private static List<object> ParseArray(string s, ref int idx)
            {
                List<object> list = new List<object>();
                idx++; // '['
                SkipWhitespace(s, ref idx);
                if (idx < s.Length && s[idx] == ']')
                {
                    idx++;
                    return list;
                }

                while (true)
                {
                    object val = ParseValue(s, ref idx);
                    list.Add(val);

                    SkipWhitespace(s, ref idx);
                    if (idx >= s.Length)
                        throw new FormatException("Unterminated array.");
                    if (s[idx] == ']')
                    {
                        idx++;
                        return list;
                    }
                    if (s[idx] != ',')
                        throw new FormatException("Expected ',' or ']' at index " + idx);
                    idx++;
                }
            }

            private static string ParseString(string s, ref int idx)
            {
                StringBuilder sb = new StringBuilder();
                idx++; // '"'
                while (idx < s.Length)
                {
                    char c = s[idx++];
                    if (c == '"')
                        return sb.ToString();
                    if (c == '\\')
                    {
                        if (idx >= s.Length)
                            throw new FormatException("Unterminated escape sequence.");
                        char esc = s[idx++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (idx + 4 > s.Length)
                                    throw new FormatException("Invalid \\u escape.");
                                int code = int.Parse(s.Substring(idx, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                sb.Append((char)code);
                                idx += 4;
                                break;
                            default:
                                sb.Append(esc);
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                throw new FormatException("Unterminated string.");
            }

            private static object ParseNumber(string s, ref int idx)
            {
                int start = idx;
                bool isFloat = false;
                while (idx < s.Length)
                {
                    char c = s[idx];
                    if ((c >= '0' && c <= '9') || c == '-')
                    {
                        idx++;
                    }
                    else if (c == '.' || c == 'e' || c == 'E' || c == '+')
                    {
                        isFloat = true;
                        idx++;
                    }
                    else
                    {
                        break;
                    }
                }

                string raw = s.Substring(start, idx - start);
                if (isFloat)
                {
                    double d;
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                        return d;
                }
                else
                {
                    long l;
                    if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out l))
                        return l;
                }
                throw new FormatException("Invalid number: " + raw);
            }

            private static void SerializeValue(object value, StringBuilder sb)
            {
                if (value == null)
                {
                    sb.Append("null");
                    return;
                }

                if (value is bool)
                {
                    sb.Append(((bool)value) ? "true" : "false");
                    return;
                }

                string str = value as string;
                if (str != null)
                {
                    SerializeString(str, sb);
                    return;
                }

                if (value is char)
                {
                    SerializeString(value.ToString(), sb);
                    return;
                }

                IDictionary dict = value as IDictionary;
                if (dict != null)
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (DictionaryEntry entry in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        SerializeString(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), sb);
                        sb.Append(':');
                        SerializeValue(entry.Value, sb);
                    }
                    sb.Append('}');
                    return;
                }

                IEnumerable list = value as IEnumerable;
                if (list != null)
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (object item in list)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        SerializeValue(item, sb);
                    }
                    sb.Append(']');
                    return;
                }

                if (value is double)
                {
                    double d = (double)value;
                    if (double.IsNaN(d) || double.IsInfinity(d))
                        sb.Append("0");
                    else
                        sb.Append(d.ToString("G17", CultureInfo.InvariantCulture));
                    return;
                }

                if (value is float)
                {
                    float f = (float)value;
                    if (float.IsNaN(f) || float.IsInfinity(f))
                        sb.Append("0");
                    else
                        sb.Append(f.ToString("G9", CultureInfo.InvariantCulture));
                    return;
                }

                if (value is int || value is long || value is byte || value is sbyte || value is short || value is ushort || value is uint || value is ulong || value is decimal)
                {
                    sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
                }

                SerializeString(Convert.ToString(value, CultureInfo.InvariantCulture), sb);
            }

            private static void SerializeString(string str, StringBuilder sb)
            {
                sb.Append('"');
                for (int i = 0; i < str.Length; i++)
                {
                    char c = str[i];
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20)
                                sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                            else
                                sb.Append(c);
                            break;
                    }
                }
                sb.Append('"');
            }
        }
    }
}
