import { invoke } from '@tauri-apps/api/core';
import { getCurrentWindow } from '@tauri-apps/api/window';
import type {
  BoostProfileName,
  BridgeStateData,
  ChameleonResult,
  ConfigMap,
  DiagnosticLogEntry,
  PurgeRamResult,
  ThemePreset,
  ThemeSnapshot,
} from '../types/theme';
import { calculateContrastRatio, shiftHue } from '../utils/color';

export interface BridgeResponse<T = unknown> {
  ok: boolean;
  data?: T;
  error?: string;
}

export interface AppInfo {
  version: string;
  productName: string;
  bridgePath: string;
  bridgeExists: boolean;
}

export function isTauriRuntime(): boolean {
  return (
    typeof window !== 'undefined' &&
    ('__TAURI_INTERNALS__' in window || '__TAURI__' in window)
  );
}

const DEFAULT_PRESETS: ThemePreset[] = [
  {
    id: 'DoomEmerald',
    displayName: 'Doom Emerald v3',
    subtitle: 'Obsidyen zümrüt cam, neon speküler kenar ve derin yeşil aura',
    isBuiltIn: true,
    uiBackground: '#07080C',
    uiCard: '#0F1814',
    primaryAccent: '#2EE6A6',
    inactiveAccent: '#1F4A31',
    rimHighlight: '#6CA881',
    explorerBodyTint: '#0D1F16',
    explorerShadowGlow: '#267347',
    taskbarTint: '#08120D',
    secondaryAccent: '#7C5CFF',
    defaultTintAlpha: 72,
    defaultShadowAlpha: 185,
    defaultShadowSpread: 28,
    wallpaperPath: 'C:\\Users\\gorke\\Documents\\Project Icons\\Desktop-DoomGreen\\DoomEmerald.jpg',
    isFavorite: true,
    tags: 'hdr,emerald,dark,glass,flagship',
  },
  {
    id: 'SplendCyberViolet',
    displayName: 'Splend Cyber Violet',
    subtitle: 'Opus 5.5 referans mor-ultraviyole stüdyo ve elektrik mavisi çift aura',
    isBuiltIn: true,
    uiBackground: '#07080C',
    uiCard: '#131420',
    primaryAccent: '#7C5CFF',
    inactiveAccent: '#392E6B',
    rimHighlight: '#A594FF',
    explorerBodyTint: '#111022',
    explorerShadowGlow: '#5B3DF5',
    taskbarTint: '#0A0B14',
    secondaryAccent: '#00E5FF',
    defaultTintAlpha: 68,
    defaultShadowAlpha: 195,
    defaultShadowSpread: 32,
    wallpaperPath: '',
    isFavorite: true,
    tags: 'cyber,violet,studio,hdr,neon',
  },
  {
    id: 'CrimsonHellfire',
    displayName: 'Slayer Crimson Hellfire',
    subtitle: 'Arjent plazma kırmızısı, volkanik kor rim ve keskin siyah kontrast',
    isBuiltIn: true,
    uiBackground: '#090608',
    uiCard: '#1A0E12',
    primaryAccent: '#FF3355',
    inactiveAccent: '#5C1B28',
    rimHighlight: '#FF7A8A',
    explorerBodyTint: '#1F0C11',
    explorerShadowGlow: '#C81E3A',
    taskbarTint: '#12070A',
    secondaryAccent: '#FF9E2C',
    defaultTintAlpha: 76,
    defaultShadowAlpha: 210,
    defaultShadowSpread: 30,
    wallpaperPath: '',
    isFavorite: true,
    tags: 'gaming,crimson,doom,aggressive',
  },
  {
    id: 'ArcticGlacier',
    displayName: 'Arctic Cryo 10-bit',
    subtitle: 'Buzul cam kırılması, 10-bit scRGB beyaz koruma ve siber camgöbeği',
    isBuiltIn: true,
    uiBackground: '#06090E',
    uiCard: '#0E1722',
    primaryAccent: '#38BDF8',
    inactiveAccent: '#1E425E',
    rimHighlight: '#93E2FF',
    explorerBodyTint: '#0C1824',
    explorerShadowGlow: '#0284C7',
    taskbarTint: '#081018',
    secondaryAccent: '#818CF8',
    defaultTintAlpha: 60,
    defaultShadowAlpha: 175,
    defaultShadowSpread: 26,
    wallpaperPath: '',
    isFavorite: false,
    tags: 'hdr,arctic,cyan,minimal,clean',
  },
  {
    id: 'ObsidianGold',
    displayName: 'Obsidian Sovereign Gold',
    subtitle: 'Lüks karbon siyah zemin üzerinde 24K şampanya altın speküler hatlar',
    isBuiltIn: true,
    uiBackground: '#080808',
    uiCard: '#161410',
    primaryAccent: '#F5B841',
    inactiveAccent: '#5E471C',
    rimHighlight: '#FDE08D',
    explorerBodyTint: '#18140C',
    explorerShadowGlow: '#B87D14',
    taskbarTint: '#0E0C08',
    secondaryAccent: '#38BDF8',
    defaultTintAlpha: 64,
    defaultShadowAlpha: 180,
    defaultShadowSpread: 24,
    wallpaperPath: '',
    isFavorite: true,
    tags: 'luxury,gold,minimal,warm',
  },
  {
    id: 'TokyoSynthwave',
    displayName: 'Neo-Tokyo 2089',
    subtitle: 'Gece yarısı macenta ve siber turkuaz çift katmanlı neon yansıma',
    isBuiltIn: true,
    uiBackground: '#08060E',
    uiCard: '#171026',
    primaryAccent: '#F43F8E',
    inactiveAccent: '#581D45',
    rimHighlight: '#FF85BA',
    explorerBodyTint: '#190E29',
    explorerShadowGlow: '#BE185D',
    taskbarTint: '#0D0818',
    secondaryAccent: '#2DD4BF',
    defaultTintAlpha: 74,
    defaultShadowAlpha: 200,
    defaultShadowSpread: 34,
    wallpaperPath: '',
    isFavorite: false,
    tags: 'synthwave,neon,pink,cyber',
  },
  {
    id: 'MatrixOperator',
    displayName: 'Construct Phosphor',
    subtitle: 'CRT fosfor yeşili, terminal keskinliği ve sıfır dikkat dağıtıcı odak',
    isBuiltIn: true,
    uiBackground: '#040805',
    uiCard: '#0A140C',
    primaryAccent: '#22C55E',
    inactiveAccent: '#14532D',
    rimHighlight: '#86EFAC',
    explorerBodyTint: '#071209',
    explorerShadowGlow: '#15803D',
    taskbarTint: '#050C06',
    secondaryAccent: '#10B981',
    defaultTintAlpha: 80,
    defaultShadowAlpha: 190,
    defaultShadowSpread: 24,
    wallpaperPath: '',
    isFavorite: false,
    tags: 'matrix,code,green,focus',
  },
  {
    id: 'SolarAmber',
    displayName: 'Helios Solar Flare',
    subtitle: 'Sıcak kehribar, düşük mavi ışık gece stüdyosu ve bakır parıltı',
    isBuiltIn: true,
    uiBackground: '#090705',
    uiCard: '#1A130D',
    primaryAccent: '#FF8A2B',
    inactiveAccent: '#633712',
    rimHighlight: '#FFBA7A',
    explorerBodyTint: '#1F140A',
    explorerShadowGlow: '#C2590A',
    taskbarTint: '#120C07',
    secondaryAccent: '#F43F5E',
    defaultTintAlpha: 70,
    defaultShadowAlpha: 185,
    defaultShadowSpread: 28,
    wallpaperPath: '',
    isFavorite: false,
    tags: 'warm,amber,night,studio',
  },
  {
    id: 'AbyssalBiolum',
    displayName: 'Abyssal Trench Biolum',
    subtitle: 'Okyanus çukuru laciverti ve biyolüminesan akuamarin parıltısı',
    isBuiltIn: true,
    uiBackground: '#04080D',
    uiCard: '#0A1520',
    primaryAccent: '#14B8A6',
    inactiveAccent: '#114E48',
    rimHighlight: '#5EEAD4',
    explorerBodyTint: '#081822',
    explorerShadowGlow: '#0F766E',
    taskbarTint: '#060E14',
    secondaryAccent: '#6366F1',
    defaultTintAlpha: 66,
    defaultShadowAlpha: 175,
    defaultShadowSpread: 28,
    wallpaperPath: '',
    isFavorite: false,
    tags: 'ocean,teal,deep,calm',
  },
  {
    id: 'MonochromeTitanium',
    displayName: 'Titanium Reference Pro',
    subtitle: 'Renk düzeltme (Color Grading) için nötr D65 gri ve saf gümüş rim',
    isBuiltIn: true,
    uiBackground: '#08090B',
    uiCard: '#14161B',
    primaryAccent: '#E2E8F0',
    inactiveAccent: '#475569',
    rimHighlight: '#FFFFFF',
    explorerBodyTint: '#121418',
    explorerShadowGlow: '#64748B',
    taskbarTint: '#0C0D10',
    secondaryAccent: '#94A3B8',
    defaultTintAlpha: 55,
    defaultShadowAlpha: 150,
    defaultShadowSpread: 20,
    wallpaperPath: '',
    isFavorite: false,
    tags: 'minimal,monochrome,pro,reference',
  },
];

let mockState: BridgeStateData = {
  config: {
    ActiveProfile: 'DoomEmerald',
    PrimaryAccentHex: '#2EE6A6',
    RimHighlightHex: '#6CA881',
    ShadowGlowHex: '#267347',
    BodyTintHex: '#0D1F16',
    TaskbarTintHex: '#08120D',
    ExplorerGlassEnabled: '1',
    ExplorerAlpha: '238',
    ExplorerBlurMode: '0', // 0=HDR Clear, 1=Acrylic, 2=SDR Blur, 3=Mica
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
    TaskbarMode: '2', // 0=Classic, 1=Clear, 2=FloatingDock, 3=DynamicIsland, 4=Segmented
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
    BoostProfile: '0', // 0=Normal, 1=Oyun, 2=Render, 3=Sessiz
    SmartFocusEnabled: '1',
    SmartFocusGpuThreshold: '78',
    SmartFocusResumeDelaySec: '3',
    DisplayDimmerPercent: '0',
    DisplayWarmthKelvin: '6500',
    AutostartEnabled: '1',
    StartMinimizedToTray: '0',
    TargetFpsCap: '60',
    CreateRestorePointBeforeApply: '0',
  },
  presets: DEFAULT_PRESETS,
  telemetry: {
    cpuPercent: 18,
    ramUsedGb: 11.4,
    ramFreeGb: 20.6,
    ramTotalGb: 32.0,
    ramUsagePercent: 36,
    gpuUtilPercent: 22,
    gpuTempC: 46,
    audioPeak: 0.42,
    pomodoroRunning: false,
    pomodoroPhase: 'Focus',
    pomodoroRemainingSec: 25 * 60,
    pomodoroCompleted: 2,
    smartPaused: false,
    smartPauseReason: '',
    desktopIconsVisible: true,
    glassDaemonRunning: true,
  },
  snapshots: [
    {
      id: 'snap-01',
      timestamp: '10:24:18',
      label: 'Doom Emerald v3',
      presetId: 'DoomEmerald',
      primaryAccentHex: '#2EE6A6',
      rimHighlightHex: '#6CA881',
      shadowGlowHex: '#267347',
    },
    {
      id: 'snap-02',
      timestamp: '09:58:04',
      label: 'Splend Cyber Violet',
      presetId: 'SplendCyberViolet',
      primaryAccentHex: '#7C5CFF',
      rimHighlightHex: '#A594FF',
      shadowGlowHex: '#5B3DF5',
    },
    {
      id: 'snap-03',
      timestamp: '09:15:42',
      label: 'Slayer Crimson Hellfire',
      presetId: 'CrimsonHellfire',
      primaryAccentHex: '#FF3355',
      rimHighlightHex: '#FF7A8A',
      shadowGlowHex: '#C81E3A',
    },
  ],
  cpuHistory: [
    14, 16, 15, 19, 22, 18, 17, 21, 25, 20, 16, 15, 18, 24, 21, 19, 16, 17, 20, 22, 19, 17, 16, 18,
  ],
};

const diagnosticListeners: Array<(logs: DiagnosticLogEntry[]) => void> = [];
let diagnosticLogs: DiagnosticLogEntry[] = [
  {
    id: 'init-0',
    timestamp: new Date().toLocaleTimeString('tr-TR'),
    cmd: 'system_boot',
    status: isTauriRuntime() ? 'ok' : 'mock',
    durationMs: 4,
    summary: isTauriRuntime()
      ? 'Tauri v2 Native IPC köprüsü hazır (DoomControlCenter.exe --bridge).'
      : 'Tarayıcı önizleme kipi aktif (Mock Engine + Canlı Telemetri).',
  },
];

function pushDiagnosticLog(entry: Omit<DiagnosticLogEntry, 'id' | 'timestamp'>) {
  const full: DiagnosticLogEntry = {
    ...entry,
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
    timestamp: new Date().toLocaleTimeString('tr-TR'),
  };
  diagnosticLogs = [full, ...diagnosticLogs].slice(0, 80);
  diagnosticListeners.forEach((fn) => fn(diagnosticLogs));
}

export function subscribeDiagnostics(fn: (logs: DiagnosticLogEntry[]) => void): () => void {
  diagnosticListeners.push(fn);
  fn(diagnosticLogs);
  return () => {
    const idx = diagnosticListeners.indexOf(fn);
    if (idx >= 0) diagnosticListeners.splice(idx, 1);
  };
}

export function clearDiagnostics(): void {
  diagnosticLogs = [];
  diagnosticListeners.forEach((fn) => fn(diagnosticLogs));
}

function tickMockTelemetry(): void {
  const prevCpu = mockState.telemetry.cpuPercent;
  const delta = Math.round((Math.random() - 0.48) * 7);
  const nextCpu = Math.max(4, Math.min(92, prevCpu + delta));
  const nextGpu = Math.max(6, Math.min(96, mockState.telemetry.gpuUtilPercent + Math.round((Math.random() - 0.48) * 6)));
  const nextTemp = Math.max(38, Math.min(79, Math.round(42 + nextGpu * 0.32)));
  const nextAudio = Number((0.15 + Math.random() * 0.72).toFixed(3));

  let rem = mockState.telemetry.pomodoroRemainingSec;
  if (mockState.telemetry.pomodoroRunning && rem > 0) {
    rem = Math.max(0, rem - 2);
  }

  mockState = {
    ...mockState,
    telemetry: {
      ...mockState.telemetry,
      cpuPercent: nextCpu,
      gpuUtilPercent: nextGpu,
      gpuTempC: nextTemp,
      audioPeak: nextAudio,
      pomodoroRemainingSec: rem,
    },
    cpuHistory: [...mockState.cpuHistory.slice(1), nextCpu],
  };
}

export async function bridgeCall<T = unknown>(
  cmd: string,
  payload: Record<string, unknown> = {},
): Promise<BridgeResponse<T>> {
  const t0 = performance.now();

  if (isTauriRuntime()) {
    try {
      const raw = await invoke<string>('bridge_call', {
        commandJson: JSON.stringify({ cmd, payload }),
      });
      const parsed = JSON.parse(raw) as BridgeResponse<T>;
      const durationMs = Math.round(performance.now() - t0);

      if (cmd !== 'get_state') {
        pushDiagnosticLog({
          cmd,
          status: parsed.ok ? 'ok' : 'error',
          durationMs,
          summary: parsed.ok
            ? `IPC '${cmd}' başarıyla yürütüldü (${durationMs}ms)`
            : `IPC hatası: ${parsed.error ?? 'Bilinmeyen hata'}`,
        });
      }
      if (parsed.ok) {
        return parsed;
      }
    } catch (err) {
      const durationMs = Math.round(performance.now() - t0);
      if (cmd !== 'get_state') {
        pushDiagnosticLog({
          cmd,
          status: 'mock',
          durationMs,
          summary: `Native köprü meşgul (${String(err).slice(0, 60)}), yerel motor kullanıldı.`,
        });
      }
    }
  }

  // Fallback / Browser simulation
  const sim = simulateBridgeCommand(cmd, payload);
  const durationMs = Math.max(1, Math.round(performance.now() - t0));
  if (cmd !== 'get_state') {
    pushDiagnosticLog({
      cmd,
      status: isTauriRuntime() ? 'mock' : 'ok',
      durationMs,
      summary: `${cmd} -> ${JSON.stringify(payload).slice(0, 72)}`,
    });
  }
  return sim as BridgeResponse<T>;
}

function simulateBridgeCommand(
  cmd: string,
  payload: Record<string, unknown>,
): BridgeResponse<unknown> {
  switch (cmd) {
    case 'get_state': {
      tickMockTelemetry();
      return { ok: true, data: mockState };
    }

    case 'apply_config': {
      const nextCfg: ConfigMap = { ...mockState.config };
      const src =
        payload.config && typeof payload.config === 'object'
          ? (payload.config as Record<string, unknown>)
          : payload;
      for (const [k, v] of Object.entries(src)) {
        if (k === 'applyWallpaper' || v === undefined || v === null) continue;
        if (typeof v === 'boolean') nextCfg[k] = v ? '1' : '0';
        else nextCfg[k] = String(v);
      }
      const snap: ThemeSnapshot = {
        id: `snap-${Date.now().toString(36)}`,
        timestamp: new Date().toLocaleTimeString('tr-TR'),
        label: nextCfg.ActiveProfile || 'Custom Studio',
        presetId: nextCfg.ActiveProfile || 'Custom',
        primaryAccentHex: nextCfg.PrimaryAccentHex || '#2EE6A6',
        rimHighlightHex: nextCfg.RimHighlightHex || '#6CA881',
        shadowGlowHex: nextCfg.ShadowGlowHex || '#267347',
      };
      mockState = {
        ...mockState,
        config: nextCfg,
        snapshots: [snap, ...mockState.snapshots].slice(0, 20),
      };
      return { ok: true, data: mockState };
    }

    case 'apply_preset': {
      const id = String(payload.id || 'DoomEmerald');
      const found =
        mockState.presets.find((p) => p.id.toLowerCase() === id.toLowerCase()) ||
        mockState.presets[0];
      const nextCfg: ConfigMap = {
        ...mockState.config,
        ActiveProfile: found.id,
        PrimaryAccentHex: found.primaryAccent,
        RimHighlightHex: found.rimHighlight,
        ShadowGlowHex: found.explorerShadowGlow,
        BodyTintHex: found.explorerBodyTint,
        TaskbarTintHex: found.taskbarTint,
        EmeraldTintIntensity: String(found.defaultTintAlpha),
        EmeraldShadowIntensity: String(found.defaultShadowAlpha),
        ShadowSpread: String(found.defaultShadowSpread),
      };
      const snap: ThemeSnapshot = {
        id: `snap-${Date.now().toString(36)}`,
        timestamp: new Date().toLocaleTimeString('tr-TR'),
        label: found.displayName,
        presetId: found.id,
        primaryAccentHex: found.primaryAccent,
        rimHighlightHex: found.rimHighlight,
        shadowGlowHex: found.explorerShadowGlow,
      };
      mockState = {
        ...mockState,
        config: nextCfg,
        snapshots: [snap, ...mockState.snapshots].slice(0, 20),
      };
      return { ok: true, data: mockState };
    }

    case 'chameleon_extract': {
      const baseAccent = mockState.config.PrimaryAccentHex || '#2EE6A6';
      const swatches = [
        baseAccent,
        shiftHue(baseAccent, 28, 0.9, 0.95),
        shiftHue(baseAccent, -32, 0.85, 0.88),
        shiftHue(baseAccent, 145, 0.92, 0.92),
        shiftHue(baseAccent, 180, 0.75, 0.65),
        '#0D1518',
      ];
      const extractedPreset: ThemePreset = {
        id: `Chameleon_${Date.now().toString().slice(-4)}`,
        displayName: String(payload.name || 'Chameleon OKLab Studio'),
        subtitle: 'Duvar kağıdından K-Means OKLab kümeleme ile otomatik üretildi',
        isBuiltIn: false,
        uiBackground: '#07080C',
        uiCard: '#10161B',
        primaryAccent: swatches[0],
        inactiveAccent: swatches[4],
        rimHighlight: swatches[1],
        explorerBodyTint: swatches[5],
        explorerShadowGlow: swatches[2],
        taskbarTint: '#090D10',
        secondaryAccent: swatches[3],
        defaultTintAlpha: 72,
        defaultShadowAlpha: 190,
        defaultShadowSpread: 28,
        wallpaperPath: String(payload.imagePath || 'CurrentWallpaper.jpg'),
        isFavorite: true,
        tags: 'chameleon,oklab,auto',
      };
      const autoApply = Boolean(payload.autoApply);
      if (autoApply) {
        mockState = {
          ...mockState,
          presets: [extractedPreset, ...mockState.presets],
          config: {
            ...mockState.config,
            ActiveProfile: extractedPreset.id,
            PrimaryAccentHex: extractedPreset.primaryAccent,
            RimHighlightHex: extractedPreset.rimHighlight,
            ShadowGlowHex: extractedPreset.explorerShadowGlow,
            BodyTintHex: extractedPreset.explorerBodyTint,
          },
        };
      }
      const result: ChameleonResult = {
        imagePath: extractedPreset.wallpaperPath,
        swatches,
        contrastScore: calculateContrastRatio('#F2F4F8', '#07080C'),
        preset: extractedPreset,
        applied: autoApply,
      };
      return { ok: true, data: result };
    }

    case 'purge_ram': {
      const freedMb = Number((380 + Math.random() * 640).toFixed(1));
      const freedGb = freedMb / 1024;
      const nextUsed = Math.max(4.2, Number((mockState.telemetry.ramUsedGb - freedGb).toFixed(2)));
      const nextFree = Number((mockState.telemetry.ramTotalGb - nextUsed).toFixed(2));
      const nextPct = Math.round((nextUsed / mockState.telemetry.ramTotalGb) * 100);
      mockState = {
        ...mockState,
        telemetry: {
          ...mockState.telemetry,
          ramUsedGb: nextUsed,
          ramFreeGb: nextFree,
          ramUsagePercent: nextPct,
        },
      };
      const res: PurgeRamResult = {
        freedMb,
        procCount: 68 + Math.floor(Math.random() * 24),
        freeGb: nextFree,
        usagePercent: nextPct,
      };
      return { ok: true, data: res };
    }

    case 'toggle_icons': {
      const nextVis =
        typeof payload.visible === 'boolean'
          ? payload.visible
          : !mockState.telemetry.desktopIconsVisible;
      mockState = {
        ...mockState,
        telemetry: {
          ...mockState.telemetry,
          desktopIconsVisible: nextVis,
        },
      };
      return { ok: true, data: { visible: nextVis } };
    }

    case 'set_display_overlay': {
      const dimmerPercent = Number(payload.dimmerPercent ?? mockState.config.DisplayDimmerPercent ?? 0);
      const warmthKelvin = Number(payload.warmthKelvin ?? mockState.config.DisplayWarmthKelvin ?? 6500);
      mockState = {
        ...mockState,
        config: {
          ...mockState.config,
          DisplayDimmerPercent: String(dimmerPercent),
          DisplayWarmthKelvin: String(warmthKelvin),
        },
      };
      return {
        ok: true,
        data: {
          dimmerPercent,
          warmthKelvin,
          isActive: dimmerPercent > 0 || warmthKelvin < 6500,
        },
      };
    }

    case 'set_boost_profile': {
      const profile = (payload.profile as BoostProfileName) || 'Normal';
      const idxMap: Record<BoostProfileName, number> = {
        Normal: 0,
        Oyun: 1,
        Render: 2,
        Sessiz: 3,
      };
      const profileIndex = idxMap[profile] ?? 0;
      mockState = {
        ...mockState,
        config: {
          ...mockState.config,
          BoostProfile: String(profileIndex),
        },
        telemetry: {
          ...mockState.telemetry,
          smartPaused: profile === 'Oyun' || profile === 'Render',
          smartPauseReason:
            profile === 'Oyun'
              ? 'Oyun Boost Kipi Aktif'
              : profile === 'Render'
                ? 'Render / CUDA Boost Kipi Aktif'
                : '',
        },
      };
      return { ok: true, data: { profile, profileIndex } };
    }

    case 'pomodoro_action': {
      const action = String(payload.action || 'start_pause');
      let running = mockState.telemetry.pomodoroRunning;
      let phase = mockState.telemetry.pomodoroPhase;
      let rem = mockState.telemetry.pomodoroRemainingSec;
      let completed = mockState.telemetry.pomodoroCompleted;

      if (action === 'start_pause') {
        running = !running;
      } else if (action === 'reset') {
        running = false;
        rem = Number(payload.focusMin || 25) * 60;
        phase = 'Focus';
      } else if (action === 'skip') {
        if (phase === 'Focus') {
          phase = 'ShortBreak';
          rem = 5 * 60;
          completed = (completed + 1) % 5;
        } else {
          phase = 'Focus';
          rem = 25 * 60;
        }
      } else if (action === 'config') {
        const focusMin = Number(payload.focusMin || 25);
        rem = focusMin * 60;
      }

      mockState = {
        ...mockState,
        telemetry: {
          ...mockState.telemetry,
          pomodoroRunning: running,
          pomodoroPhase: phase,
          pomodoroRemainingSec: rem,
          pomodoroCompleted: completed,
        },
      };
      return {
        ok: true,
        data: {
          pomodoroRunning: running,
          pomodoroPhase: phase,
          pomodoroRemainingSec: rem,
          pomodoroCompleted: completed,
        },
      };
    }

    case 'hud_action': {
      const nextCfg = { ...mockState.config };
      if (payload.hudEnabled !== undefined) nextCfg.HudEnabled = payload.hudEnabled ? '1' : '0';
      if (payload.locked !== undefined) nextCfg.WidgetsLocked = payload.locked ? '1' : '0';
      if (payload.scale !== undefined) nextCfg.WidgetScale = String(payload.scale);
      if (payload.opacity !== undefined) nextCfg.HudOpacity = String(payload.opacity);
      if (payload.spectrumStyle !== undefined) nextCfg.SpectrumStyle = String(payload.spectrumStyle);
      if (payload.spectrumBands !== undefined) nextCfg.SpectrumBandCount = String(payload.spectrumBands);
      if (payload.spectrumSmoothing !== undefined)
        nextCfg.SpectrumSmoothing = String(payload.spectrumSmoothing);

      if (payload.widgets && typeof payload.widgets === 'object') {
        const w = payload.widgets as Record<string, boolean>;
        if (w.Clock !== undefined) nextCfg.WidgetClock = w.Clock ? '1' : '0';
        if (w.SystemGpu !== undefined) nextCfg.WidgetSystemGpu = w.SystemGpu ? '1' : '0';
        if (w.Storage !== undefined) nextCfg.WidgetStorage = w.Storage ? '1' : '0';
        if (w.Network !== undefined) nextCfg.WidgetNetwork = w.Network ? '1' : '0';
        if (w.Media !== undefined) nextCfg.WidgetMedia = w.Media ? '1' : '0';
        if (w.Notes !== undefined) nextCfg.WidgetNotes = w.Notes ? '1' : '0';
        if (w.Spectrum !== undefined) nextCfg.WidgetSpectrum = w.Spectrum ? '1' : '0';
        if (w.Pomodoro !== undefined) nextCfg.WidgetPomodoro = w.Pomodoro ? '1' : '0';
      }
      mockState = { ...mockState, config: nextCfg };
      return { ok: true, data: nextCfg };
    }

    case 'rollback_snapshot': {
      const id = String(payload.id || '');
      const snap = mockState.snapshots.find((s) => s.id === id);
      if (snap) {
        mockState = {
          ...mockState,
          config: {
            ...mockState.config,
            ActiveProfile: snap.presetId,
            PrimaryAccentHex: snap.primaryAccentHex,
            RimHighlightHex: snap.rimHighlightHex,
            ShadowGlowHex: snap.shadowGlowHex,
          },
        };
      }
      return { ok: true, data: { ...mockState, rolledBackId: id } };
    }

    case 'toggle_glass': {
      const nextRun =
        typeof payload.enabled === 'boolean'
          ? payload.enabled
          : !mockState.telemetry.glassDaemonRunning;
      mockState = {
        ...mockState,
        telemetry: { ...mockState.telemetry, glassDaemonRunning: nextRun },
        config: { ...mockState.config, ExplorerGlassEnabled: nextRun ? '1' : '0' },
      };
      return { ok: true, data: { glassDaemonRunning: nextRun } };
    }

    case 'export_pkg': {
      return {
        ok: true,
        data: { exported: true, path: String(payload.path || 'DoomEmerald.doomtheme') },
      };
    }

    case 'import_pkg': {
      return {
        ok: true,
        data: {
          valid: true,
          message: 'Paket şeması v3.0 doğrulandı (SHA-256 & Renk Tokenları TAMAM)',
          applied: true,
          preset: mockState.presets[0],
        },
      };
    }

    default:
      return { ok: true, data: mockState };
  }
}

export async function openFileDialog(
  title: string,
  filters: Array<{ name: string; extensions: string[] }>,
): Promise<string | null> {
  if (isTauriRuntime()) {
    try {
      return await invoke<string | null>('open_file_dialog', { title, filters });
    } catch {
      return null;
    }
  }
  return 'C:\\Users\\gorke\\Documents\\Project Icons\\Desktop-DoomGreen\\CustomTheme.doomtheme';
}

export async function saveFileDialog(
  title: string,
  defaultName: string,
  filters: Array<{ name: string; extensions: string[] }>,
): Promise<string | null> {
  if (isTauriRuntime()) {
    try {
      return await invoke<string | null>('save_file_dialog', {
        title,
        defaultName,
        filters,
      });
    } catch {
      return null;
    }
  }
  return `C:\\Users\\gorke\\Documents\\Project Icons\\Desktop-DoomGreen\\${defaultName}`;
}

export async function fetchAppInfo(): Promise<AppInfo> {
  if (isTauriRuntime()) {
    try {
      return await invoke<AppInfo>('app_info');
    } catch {
      // ignore
    }
  }
  return {
    version: '3.0.0',
    productName: 'SplendThemeBuilder',
    bridgePath: 'C:\\Users\\gorke\\Documents\\Project Icons\\Desktop-DoomGreen\\DoomControlCenter.exe',
    bridgeExists: true,
  };
}

export async function minimizeAppWindow(): Promise<void> {
  if (isTauriRuntime()) {
    try {
      await getCurrentWindow().minimize();
    } catch {
      // ignore
    }
  }
}

export async function toggleMaximizeAppWindow(): Promise<void> {
  if (isTauriRuntime()) {
    try {
      await getCurrentWindow().toggleMaximize();
    } catch {
      // ignore
    }
  }
}

export async function closeAppWindow(): Promise<void> {
  if (isTauriRuntime()) {
    try {
      await getCurrentWindow().close();
    } catch {
      // ignore
    }
  }
}
