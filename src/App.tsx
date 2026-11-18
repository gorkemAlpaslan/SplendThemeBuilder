import React, { useCallback, useEffect, useState } from 'react';
import { CheckCircle2, RotateCcw, Sparkles } from 'lucide-react';
import { bridgeCall } from './api/bridge';
import { CommandPaletteModal } from './components/CommandPaletteModal';
import { LeftIconRail } from './components/LeftIconRail';
import { MiniDesktopPreviewDock } from './components/MiniDesktopPreviewDock';
import { TopTelemetryHeader } from './components/TopTelemetryHeader';
import type {
  BoostProfileName,
  BridgeStateData,
  ChameleonResult,
  ConfigMap,
  PurgeRamResult,
  TelemetryState,
  ThemePreset,
  ThemeSnapshot,
  ViewId,
} from './types/theme';
import { hexWithAlpha, normalizeHex, shiftHue } from './utils/color';
import { View0Gallery } from './views/View0Gallery';
import { View1ColorStudio } from './views/View1ColorStudio';
import { View2ExplorerHdr } from './views/View2ExplorerHdr';
import { View3TaskbarDock } from './views/View3TaskbarDock';
import { View4HudSpectrum } from './views/View4HudSpectrum';
import { View5BoostMonitors } from './views/View5BoostMonitors';
import { View6SystemBackup } from './views/View6SystemBackup';

export default function App() {
  const [activeView, setActiveView] = useState<ViewId>(0);
  const [railExpanded, setRailExpanded] = useState<boolean>(true);
  const [previewOpen, setPreviewOpen] = useState<boolean>(true);
  const [paletteOpen, setPaletteOpen] = useState<boolean>(false);

  const [config, setConfig] = useState<ConfigMap>({
    ActiveProfile: 'DoomEmerald',
    PrimaryAccentHex: '#2EE6A6',
    RimHighlightHex: '#6CA881',
    ShadowGlowHex: '#267347',
    BodyTintHex: '#0D1F16',
    TaskbarTintHex: '#08120D',
    ExplorerGlassEnabled: '1',
    ExplorerAlpha: '238',
    ExplorerBlurMode: '0',
    EmeraldTintAndShadowEnabled: '1',
    EmeraldTintIntensity: '72',
    EmeraldShadowIntensity: '185',
    ShadowSpread: '28',
    ShowRimBorder: '1',
    ShadowOnlyActiveWindow: '0',
    HdrWhiteTextGuard: '1',
    HdrWhiteTextGuardEnabled: '1',
    HdrNitsClamp: '220',
    HdrMaxNitsClamp: '220',
    TaskbarMode: '2',
    TaskbarCenterIcons: '1',
    TaskbarCornerRadius: '16',
    TaskbarDockMargin: '8',
    TaskbarMarginPx: '8',
    TaskbarTintAlpha: '190',
    TaskbarSpecularRim: '1',
    TrayHideOverflow: '1',
    TrayCompactClock: '1',
    IslandExpandOnMedia: '1',
    HudEnabled: '1',
    WidgetsLocked: '1',
    WidgetLayout: '0',
    WidgetScale: '100',
    HudOpacity: '230',
    WidgetClock: '1',
    WidgetSystemGpu: '1',
    WidgetStorage: '1',
    WidgetNetwork: '1',
    WidgetMedia: '1',
    WidgetNotes: '1',
    WidgetSpectrum: '1',
    WidgetPomodoro: '1',
    SpectrumStyle: '0',
    SpectrumBandCount: '32',
    SpectrumSmoothing: '65',
    BoostProfile: '0',
    SmartFocusEnabled: '1',
    SmartFocusGpuThreshold: '78',
    SmartFocusResumeDelaySec: '3',
    DisplayDimmerPercent: '0',
    DisplayWarmthKelvin: '6500',
    AutostartEnabled: '1',
    StartMinimizedToTray: '0',
    TargetFpsCap: '60',
    CreateRestorePointBeforeApply: '0',
  });

  const [presets, setPresets] = useState<ThemePreset[]>([]);
  const [snapshots, setSnapshots] = useState<ThemeSnapshot[]>([]);
  const [cpuHistory, setCpuHistory] = useState<number[]>([
    14, 16, 15, 19, 22, 18, 17, 21, 25, 20, 16, 15, 18, 24, 21, 19, 16, 17, 20, 22,
  ]);
  const [telemetry, setTelemetry] = useState<TelemetryState>({
    cpuPercent: 18,
    ramUsedGb: 11.4,
    ramFreeGb: 20.6,
    ramTotalGb: 32,
    ramUsagePercent: 36,
    gpuUtilPercent: 22,
    gpuTempC: 46,
    audioPeak: 0.42,
    pomodoroRunning: false,
    pomodoroPhase: 'Focus',
    pomodoroRemainingSec: 1500,
    pomodoroCompleted: 2,
    smartPaused: false,
    smartPauseReason: '',
    desktopIconsVisible: true,
    glassDaemonRunning: true,
  });

  const [chameleonData, setChameleonData] = useState<ChameleonResult | null>(null);
  const [lastPurgeResult, setLastPurgeResult] = useState<PurgeRamResult | null>(
    null,
  );
  const [hasDraftChanges, setHasDraftChanges] = useState<boolean>(false);
  const [isApplying, setIsApplying] = useState<boolean>(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const showToast = useCallback((msg: string) => {
    setToastMessage(msg);
  }, []);

  useEffect(() => {
    if (!toastMessage) return;
    const t = setTimeout(() => setToastMessage(null), 4200);
    return () => clearTimeout(t);
  }, [toastMessage]);

  const syncFromBridgeData = useCallback(
    (data: BridgeStateData | undefined, overwriteDraft = false) => {
      if (!data) return;
      if (data.config && (overwriteDraft || !hasDraftChanges)) {
        setConfig((prev) => ({ ...prev, ...data.config }));
      }
      if (Array.isArray(data.presets) && data.presets.length > 0) {
        setPresets(data.presets);
      }
      if (Array.isArray(data.snapshots)) {
        setSnapshots(data.snapshots);
      }
      if (data.telemetry) {
        setTelemetry((prev) => ({ ...prev, ...data.telemetry }));
      }
      if (Array.isArray(data.cpuHistory) && data.cpuHistory.length > 0) {
        setCpuHistory(data.cpuHistory);
      }
    },
    [hasDraftChanges],
  );

  // Initial state load + periodic telemetry refresh
  useEffect(() => {
    let mounted = true;
    void bridgeCall<BridgeStateData>('get_state').then((res) => {
      if (mounted && res.ok && res.data) {
        syncFromBridgeData(res.data, true);
      }
    });

    const interval = setInterval(() => {
      void bridgeCall<BridgeStateData>('get_state').then((res) => {
        if (mounted && res.ok && res.data) {
          syncFromBridgeData(res.data, false);
        }
      });
    }, 2000);

    return () => {
      mounted = false;
      clearInterval(interval);
    };
  }, [syncFromBridgeData]);

  // Update CSS root variables whenever config colors change
  const primaryAccent = normalizeHex(config.PrimaryAccentHex || '#2EE6A6');
  const secondaryAccent = shiftHue(primaryAccent, 150);
  const rimHighlight = normalizeHex(config.RimHighlightHex || '#6CA881');
  const shadowGlow = normalizeHex(config.ShadowGlowHex || '#267347');
  const bodyTint = normalizeHex(config.BodyTintHex || '#0D1F16');

  useEffect(() => {
    const root = document.documentElement;
    root.style.setProperty('--accent', primaryAccent);
    root.style.setProperty('--accent-2', secondaryAccent);
    root.style.setProperty('--rim', rimHighlight);
    root.style.setProperty('--shadow-glow', shadowGlow);
    root.style.setProperty('--body-tint', bodyTint);
  }, [primaryAccent, secondaryAccent, rimHighlight, shadowGlow, bodyTint]);

  const handleUpdateConfig = useCallback((patch: Partial<ConfigMap>) => {
    setConfig((prev) => {
      const next: ConfigMap = { ...prev };
      for (const [k, v] of Object.entries(patch)) {
        if (v !== undefined) next[k] = v;
      }
      return next;
    });
    setHasDraftChanges(true);
  }, []);

  const handleApplyConfig = useCallback(async () => {
    setIsApplying(true);
    const res = await bridgeCall<BridgeStateData>('apply_config', {
      config,
      applyWallpaper: false,
    });
    setIsApplying(false);
    setHasDraftChanges(false);
    if (res.ok && res.data) {
      syncFromBridgeData(res.data, true);
    }
    showToast('Değişiklikler DWM, Gezgin ve HUD katmanlarına uygulandı!');
  }, [config, showToast, syncFromBridgeData]);

  const handleApplyPreset = useCallback(
    async (id: string, applyWallpaper = false) => {
      const res = await bridgeCall<BridgeStateData>('apply_preset', {
        id,
        applyWallpaper,
      });
      setHasDraftChanges(false);
      if (res.ok && res.data) {
        syncFromBridgeData(res.data, true);
      }
      const found = presets.find((p) => p.id.toLowerCase() === id.toLowerCase());
      showToast(`'${found?.displayName || id}' teması başarıyla uygulandı`);
    },
    [presets, showToast, syncFromBridgeData],
  );

  const handleToggleFavorite = useCallback((id: string) => {
    setPresets((prev) =>
      prev.map((p) =>
        p.id === id ? { ...p, isFavorite: !p.isFavorite } : p,
      ),
    );
  }, []);

  const handleRunChameleon = useCallback(
    async (imagePath?: string, autoApply = true) => {
      const res = await bridgeCall<ChameleonResult>('chameleon_extract', {
        imagePath: imagePath || '',
        autoApply,
        name: 'Chameleon OKLab',
      });
      if (res.ok && res.data) {
        setChameleonData(res.data);
        if (autoApply && res.data.preset) {
          setConfig((prev) => ({
            ...prev,
            ActiveProfile: res.data!.preset.id,
            PrimaryAccentHex: res.data!.preset.primaryAccent,
            RimHighlightHex: res.data!.preset.rimHighlight,
            ShadowGlowHex: res.data!.preset.explorerShadowGlow,
            BodyTintHex: res.data!.preset.explorerBodyTint,
          }));
          showToast(
            `Chameleon OKLab: 6 baskın renk çıkarıldı (WCAG ${res.data.contrastScore}:1)`,
          );
        } else {
          showToast('Chameleon OKLab: 6 duvar kağıdı rengi analiz edildi');
        }
      }
    },
    [showToast],
  );

  const handleRollbackSnapshot = useCallback(
    async (id: string) => {
      const targetId = id || snapshots[0]?.id;
      if (!targetId) {
        showToast('Geri yüklenecek snapshot bulunamadı');
        return;
      }
      const res = await bridgeCall<BridgeStateData>('rollback_snapshot', {
        id: targetId,
      });
      setHasDraftChanges(false);
      if (res.ok && res.data) {
        syncFromBridgeData(res.data, true);
      }
      showToast(`Snapshot '${targetId}' durumuna geri dönüldü`);
    },
    [showToast, snapshots, syncFromBridgeData],
  );

  const handlePurgeRam = useCallback(async () => {
    const res = await bridgeCall<PurgeRamResult>('purge_ram');
    if (res.ok && res.data) {
      setLastPurgeResult(res.data);
      setTelemetry((prev) => ({
        ...prev,
        ramFreeGb: res.data!.freeGb,
        ramUsagePercent: res.data!.usagePercent,
      }));
      showToast(
        `RAM Temizlendi: ${res.data.freedMb} MB bellek boşaltıldı (${res.data.procCount} süreç)`,
      );
    }
  }, [showToast]);

  const handleToggleDesktopIcons = useCallback(async () => {
    const res = await bridgeCall<{ visible: boolean }>('toggle_icons');
    if (res.ok && res.data) {
      setTelemetry((prev) => ({
        ...prev,
        desktopIconsVisible: res.data!.visible,
      }));
      showToast(
        res.data.visible
          ? 'Masaüstü simgeleri görünür yapıldı'
          : 'Masaüstü simgeleri gizlendi (OLED Koruma)',
      );
    }
  }, [showToast]);

  const handleSetDisplayOverlay = useCallback(
    async (dimmerPercent: number, warmthKelvin: number) => {
      setConfig((prev) => ({
        ...prev,
        DisplayDimmerPercent: String(dimmerPercent),
        DisplayWarmthKelvin: String(warmthKelvin),
      }));
      await bridgeCall('set_display_overlay', {
        dimmerPercent,
        warmthKelvin,
      });
    },
    [],
  );

  const handleSetBoostProfile = useCallback(
    async (profile: BoostProfileName) => {
      const res = await bridgeCall<{ profile: string; profileIndex: number }>(
        'set_boost_profile',
        { profile },
      );
      if (res.ok && res.data) {
        setConfig((prev) => ({
          ...prev,
          BoostProfile: String(res.data!.profileIndex),
        }));
      }
      showToast(`Boost Kipi '${profile}' olarak etkinleştirildi`);
    },
    [showToast],
  );

  const boostProfileName: BoostProfileName = (() => {
    const raw = config.BoostProfile || '0';
    if (raw === '1' || raw === 'Oyun') return 'Oyun';
    if (raw === '2' || raw === 'Render') return 'Render';
    if (raw === '3' || raw === 'Sessiz') return 'Sessiz';
    return 'Normal';
  })();

  const handleCycleBoostProfile = useCallback(() => {
    const order: BoostProfileName[] = ['Normal', 'Oyun', 'Render', 'Sessiz'];
    const next = order[(order.indexOf(boostProfileName) + 1) % order.length];
    void handleSetBoostProfile(next);
  }, [boostProfileName, handleSetBoostProfile]);

  const handlePomodoroAction = useCallback(
    async (
      action: 'start_pause' | 'reset' | 'skip' | 'config',
      extra?: { focusMin?: number; shortMin?: number; longMin?: number },
    ) => {
      const res = await bridgeCall<{
        pomodoroRunning: boolean;
        pomodoroPhase: string;
        pomodoroRemainingSec: number;
        pomodoroCompleted: number;
      }>('pomodoro_action', { action, ...extra });
      if (res.ok && res.data) {
        setTelemetry((prev) => ({
          ...prev,
          pomodoroRunning: res.data!.pomodoroRunning,
          pomodoroPhase: res.data!.pomodoroPhase,
          pomodoroRemainingSec: res.data!.pomodoroRemainingSec,
          pomodoroCompleted: res.data!.pomodoroCompleted,
        }));
      }
    },
    [],
  );

  const handleHudAction = useCallback(
    async (payload: Record<string, unknown>) => {
      await bridgeCall('hud_action', payload);
    },
    [],
  );

  const handleToggleGlassDaemon = useCallback(
    async (enabled?: boolean) => {
      const res = await bridgeCall<{ glassDaemonRunning: boolean }>(
        'toggle_glass',
        enabled !== undefined ? { enabled } : {},
      );
      if (res.ok && res.data) {
        setTelemetry((prev) => ({
          ...prev,
          glassDaemonRunning: res.data!.glassDaemonRunning,
        }));
        showToast(
          res.data.glassDaemonRunning
            ? 'DoomGlassDaemon başlatıldı'
            : 'DoomGlassDaemon durduruldu',
        );
      }
    },
    [showToast],
  );

  const handleExportPackage = useCallback(
    async (path: string) => {
      const res = await bridgeCall('export_pkg', { path });
      if (res.ok) {
        showToast(`.doomtheme paketi dışa aktarıldı: ${path.split('\\').pop()}`);
      }
    },
    [showToast],
  );

  const handleImportPackage = useCallback(
    async (path: string) => {
      const res = await bridgeCall<{ message?: string }>('import_pkg', {
        path,
        apply: true,
      });
      if (res.ok) {
        showToast(res.data?.message || '.doomtheme paketi içe aktarıldı');
        const stateRes = await bridgeCall<BridgeStateData>('get_state');
        if (stateRes.ok && stateRes.data) {
          syncFromBridgeData(stateRes.data, true);
        }
      }
    },
    [showToast, syncFromBridgeData],
  );

  // Global Keyboard Shortcuts (Ctrl+Shift+Space, Ctrl+S, Ctrl+Z, Alt+1..7)
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.shiftKey && e.code === 'Space') {
        e.preventDefault();
        setPaletteOpen((p) => !p);
      } else if (e.ctrlKey && e.key.toLowerCase() === 's') {
        e.preventDefault();
        void handleApplyConfig();
      } else if (e.ctrlKey && e.key.toLowerCase() === 'z') {
        e.preventDefault();
        void handleRollbackSnapshot('');
      } else if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'p') {
        e.preventDefault();
        void handlePurgeRam();
      } else if (e.altKey && e.key.toLowerCase() === 'i') {
        e.preventDefault();
        void handleToggleDesktopIcons();
      } else if (e.altKey && e.key >= '1' && e.key <= '7') {
        e.preventDefault();
        setActiveView((Number(e.key) - 1) as ViewId);
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [
    handleApplyConfig,
    handlePurgeRam,
    handleRollbackSnapshot,
    handleToggleDesktopIcons,
  ]);

  const hdrGuardEnabled =
    config.HdrWhiteTextGuard !== '0' && config.HdrWhiteTextGuardEnabled !== '0';
  const hdrNits = Number(config.HdrNitsClamp ?? config.HdrMaxNitsClamp ?? 220);

  return (
    <div className="relative flex h-screen w-screen overflow-hidden bg-[#07080C] text-[#F2F4F8] select-none">
      {/* L0 Dual Radial Ambient Glows (Opus 5.5 Spec) */}
      <div
        style={{
          background: `radial-gradient(circle at 15% 0%, ${hexWithAlpha(
            primaryAccent,
            0.18,
          )} 0%, transparent 60%)`,
        }}
        className="pointer-events-none fixed inset-0 z-0 animate-ambient"
      />
      <div
        style={{
          background: `radial-gradient(circle at 100% 100%, ${hexWithAlpha(
            secondaryAccent,
            0.13,
          )} 0%, transparent 52%)`,
        }}
        className="pointer-events-none fixed inset-0 z-0 animate-ambient"
      />

      {/* Zone A — Collapsible Left Icon Rail */}
      <LeftIconRail
        activeView={activeView}
        onSelectView={setActiveView}
        expanded={railExpanded}
        onToggleExpanded={() => setRailExpanded((p) => !p)}
        accentHex={primaryAccent}
        boostProfile={boostProfileName}
        onCycleBoostProfile={handleCycleBoostProfile}
        onQuickChameleon={() => void handleRunChameleon(undefined, true)}
        glassDaemonRunning={telemetry.glassDaemonRunning}
      />

      {/* Center Column: Zone B Header + Zone C Workspace */}
      <div className="relative z-10 flex min-w-0 flex-1 flex-col overflow-hidden">
        {/* Zone B — Top Frameless Titlebar & Telemetry Header */}
        <TopTelemetryHeader
          activeView={activeView}
          telemetry={telemetry}
          cpuHistory={cpuHistory}
          accentHex={primaryAccent}
          hasDraftChanges={hasDraftChanges}
          isApplying={isApplying}
          hdrGuardEnabled={hdrGuardEnabled}
          hdrNits={hdrNits}
          previewOpen={previewOpen}
          onTogglePreview={() => setPreviewOpen((p) => !p)}
          onOpenCommandPalette={() => setPaletteOpen(true)}
          onUndo={() => void handleRollbackSnapshot('')}
          onApply={() => void handleApplyConfig()}
        />

        {/* Zone C + Zone D Container */}
        <div className="flex min-h-0 flex-1 overflow-hidden">
          {/* Zone C — 12-Column Responsive Bento Grid Workspace */}
          <main className="flex-1 overflow-y-auto p-5">
            <div className="mx-auto max-w-[1600px]">
              {activeView === 0 && (
                <View0Gallery
                  config={config}
                  presets={presets}
                  snapshots={snapshots}
                  telemetry={telemetry}
                  chameleonData={chameleonData}
                  onApplyPreset={(id, wp) => void handleApplyPreset(id, wp)}
                  onToggleFavorite={handleToggleFavorite}
                  onUpdateConfig={handleUpdateConfig}
                  onRunChameleon={(path, auto) =>
                    void handleRunChameleon(path, auto)
                  }
                  onRollbackSnapshot={(id) => void handleRollbackSnapshot(id)}
                  onExportPackage={(path) => void handleExportPackage(path)}
                  onImportPackage={(path) => void handleImportPackage(path)}
                  onSelectView={setActiveView}
                  showToast={showToast}
                />
              )}

              {activeView === 1 && (
                <View1ColorStudio
                  config={config}
                  onUpdateConfig={handleUpdateConfig}
                  showToast={showToast}
                />
              )}

              {activeView === 2 && (
                <View2ExplorerHdr
                  config={config}
                  glassDaemonRunning={telemetry.glassDaemonRunning}
                  onUpdateConfig={handleUpdateConfig}
                  onToggleGlassDaemon={(en) => void handleToggleGlassDaemon(en)}
                  showToast={showToast}
                />
              )}

              {activeView === 3 && (
                <View3TaskbarDock
                  config={config}
                  onUpdateConfig={handleUpdateConfig}
                  showToast={showToast}
                />
              )}

              {activeView === 4 && (
                <View4HudSpectrum
                  config={config}
                  telemetry={telemetry}
                  onUpdateConfig={handleUpdateConfig}
                  onHudAction={(p) => void handleHudAction(p)}
                  onPomodoroAction={(act, ex) => void handlePomodoroAction(act, ex)}
                  showToast={showToast}
                />
              )}

              {activeView === 5 && (
                <View5BoostMonitors
                  config={config}
                  telemetry={telemetry}
                  lastPurgeResult={lastPurgeResult}
                  onUpdateConfig={handleUpdateConfig}
                  onSetBoostProfile={(prof) => void handleSetBoostProfile(prof)}
                  onPurgeRam={() => void handlePurgeRam()}
                  onSetDisplayOverlay={(dim, k) =>
                    void handleSetDisplayOverlay(dim, k)
                  }
                  onToggleDesktopIcons={() => void handleToggleDesktopIcons()}
                />
              )}

              {activeView === 6 && (
                <View6SystemBackup
                  config={config}
                  snapshots={snapshots}
                  glassDaemonRunning={telemetry.glassDaemonRunning}
                  onUpdateConfig={handleUpdateConfig}
                  onToggleGlassDaemon={(en) => void handleToggleGlassDaemon(en)}
                  onRollbackSnapshot={(id) => void handleRollbackSnapshot(id)}
                  onResetDefaults={() => void handleApplyPreset('DoomEmerald', false)}
                  onTestPing={() => {
                    void bridgeCall('get_state');
                    showToast('IPC Ping başarılı: DoomControlCenter.exe --bridge yanıt verdi');
                  }}
                  showToast={showToast}
                />
              )}
            </div>
          </main>

          {/* Zone D — Collapsible Right Live Mini Desktop Preview Dock */}
          <MiniDesktopPreviewDock
            open={previewOpen}
            onToggleOpen={() => setPreviewOpen((p) => !p)}
            config={config}
            telemetry={telemetry}
            accentHex={primaryAccent}
            onSelectView={setActiveView}
          />
        </div>
      </div>

      {/* Global Command Palette Modal (Ctrl+Shift+Space) */}
      <CommandPaletteModal
        open={paletteOpen}
        onClose={() => setPaletteOpen(false)}
        presets={presets}
        accentHex={primaryAccent}
        onSelectView={setActiveView}
        onApplyPreset={(id) => void handleApplyPreset(id, false)}
        onApplyConfig={() => void handleApplyConfig()}
        onUndo={() => void handleRollbackSnapshot('')}
        onPurgeRam={() => void handlePurgeRam()}
        onToggleIcons={() => void handleToggleDesktopIcons()}
        onToggleGlass={() => void handleToggleGlassDaemon()}
        onQuickChameleon={() => void handleRunChameleon(undefined, true)}
        onToggleWidget={(key) => {
          const next = config[key] === '0' ? '1' : '0';
          handleUpdateConfig({ [key]: next });
          void handleHudAction({
            widgets: { [key.replace(/^Widget/, '')]: next === '1' },
          });
          showToast(`${key} masaüstü widget görünürlüğü değiştirildi`);
        }}
      />

      {/* Floating Toast Notification with Quick Undo */}
      {toastMessage && (
        <div
          style={{
            borderColor: hexWithAlpha(primaryAccent, 0.55),
            boxShadow: `0 16px 40px rgba(0,0,0,0.65), 0 0 24px ${hexWithAlpha(
              primaryAccent,
              0.25,
            )}`,
          }}
          className="fixed bottom-5 left-1/2 z-50 flex -translate-x-1/2 items-center gap-3 rounded-2xl border bg-[#10131C]/95 px-4 py-2.5 backdrop-blur-2xl animate-view-enter"
        >
          <CheckCircle2 className="h-4 w-4 shrink-0" style={{ color: primaryAccent }} />
          <span className="text-xs font-medium text-[#F2F4F8]">{toastMessage}</span>
          <button
            onClick={() => void handleRollbackSnapshot('')}
            className="ml-1 flex items-center gap-1 rounded-lg bg-white/8 px-2 py-1 font-mono text-[10px] font-semibold text-[#A3A9B8] hover:bg-white/15 hover:text-white"
          >
            <RotateCcw className="h-3 w-3" />
            <span>Geri Al</span>
          </button>
          <Sparkles className="h-3.5 w-3.5 text-[#6B7183]" />
        </div>
      )}
    </div>
  );
}
