export type ViewId = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export type HarmonyMode =
  | 'Mono'
  | 'Analog'
  | 'Complement'
  | 'Triad'
  | 'Split'
  | 'Tetrad';

export type BoostProfileName = 'Normal' | 'Oyun' | 'Render' | 'Sessiz';

export interface ThemePreset {
  id: string;
  displayName: string;
  subtitle: string;
  isBuiltIn: boolean;
  uiBackground: string;
  uiCard: string;
  primaryAccent: string;
  inactiveAccent: string;
  rimHighlight: string;
  explorerBodyTint: string;
  explorerShadowGlow: string;
  taskbarTint: string;
  secondaryAccent: string;
  defaultTintAlpha: number;
  defaultShadowAlpha: number;
  defaultShadowSpread: number;
  wallpaperPath: string;
  isFavorite: boolean;
  tags: string;
}

export interface ThemeSnapshot {
  id: string;
  timestamp: string;
  label: string;
  presetId: string;
  primaryAccentHex: string;
  rimHighlightHex: string;
  shadowGlowHex: string;
}

export interface TelemetryState {
  cpuPercent: number;
  ramUsedGb: number;
  ramFreeGb: number;
  ramTotalGb: number;
  ramUsagePercent: number;
  gpuUtilPercent: number;
  gpuTempC: number;
  audioPeak: number;
  pomodoroRunning: boolean;
  pomodoroPhase: string;
  pomodoroRemainingSec: number;
  pomodoroCompleted: number;
  smartPaused: boolean;
  smartPauseReason: string;
  desktopIconsVisible: boolean;
  glassDaemonRunning: boolean;
}

export type ConfigMap = Record<string, string>;

export interface BridgeStateData {
  config: ConfigMap;
  presets: ThemePreset[];
  telemetry: TelemetryState;
  snapshots: ThemeSnapshot[];
  cpuHistory: number[];
}

export interface ChameleonResult {
  imagePath: string;
  swatches: string[];
  contrastScore: number;
  preset: ThemePreset;
  applied: boolean;
}

export interface PurgeRamResult {
  freedMb: number;
  procCount: number;
  freeGb: number;
  usagePercent: number;
}

export interface DiagnosticLogEntry {
  id: string;
  timestamp: string;
  cmd: string;
  status: 'ok' | 'error' | 'mock';
  durationMs: number;
  summary: string;
}

export interface ShortcutBinding {
  id: string;
  action: string;
  binding: string;
  scope: 'Global' | 'Uygulama' | 'Masaüstü HUD';
  description: string;
}

export interface BezierControlPoints {
  p1x: number;
  p1y: number;
  p2x: number;
  p2y: number;
}
