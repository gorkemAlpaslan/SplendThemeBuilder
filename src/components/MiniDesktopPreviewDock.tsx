import React, { useState } from 'react';
import {
  ChevronLeft,
  ChevronRight,
  Columns2,
  FolderOpen,
  Monitor,
  Music,
  Sparkles,
  Sun,
  Timer,
  Wifi,
} from 'lucide-react';
import type { ConfigMap, TelemetryState, ViewId } from '../types/theme';
import { hexWithAlpha, kelvinToWarmthOverlay, shiftHue } from '../utils/color';

interface LiveDesktopCanvasProps {
  config: ConfigMap;
  telemetry: TelemetryState;
  heightClass?: string;
  isHero?: boolean;
  onSelectView?: (view: ViewId) => void;
}

export const LiveDesktopCanvas: React.FC<LiveDesktopCanvasProps> = ({
  config,
  telemetry,
  heightClass = 'aspect-[16/10]',
  isHero = false,
  onSelectView,
}) => {
  const [splitPercent, setSplitPercent] = useState<number>(isHero ? 26 : 0);
  const [showSplit, setShowSplit] = useState<boolean>(isHero);
  const [hdrSim, setHdrSim] = useState<boolean>(true);
  const [activeMonitor, setActiveMonitor] = useState<1 | 2>(1);

  const primaryAccent = config.PrimaryAccentHex || '#2EE6A6';
  const rimHighlight = config.RimHighlightHex || '#6CA881';
  const shadowGlow = config.ShadowGlowHex || '#267347';
  const bodyTint = config.BodyTintHex || '#0D1F16';
  const taskbarTint = config.TaskbarTintHex || '#08120D';
  const secondaryAccent = shiftHue(primaryAccent, 150);

  const tintIntensity = Number(config.EmeraldTintIntensity ?? 72);
  const shadowIntensity = Number(config.EmeraldShadowIntensity ?? 185);
  const shadowSpread = Number(config.ShadowSpread ?? 28);
  const showRim = config.ShowRimBorder !== '0';
  const blurMode = Number(config.ExplorerBlurMode ?? 0); // 0=HDR Clear, 1=Acrylic, 2=SDR Blur, 3=Mica

  const taskbarMode = Number(config.TaskbarMode ?? 2); // 0=Classic, 1=Clear, 2=FloatingDock, 3=DynamicIsland, 4=Segmented
  const centerIcons = config.TaskbarCenterIcons !== '0';
  const taskbarRadius = Number(config.TaskbarCornerRadius ?? 16);
  const taskbarMargin = Number(config.TaskbarDockMargin ?? config.TaskbarMarginPx ?? 8);

  const hudEnabled = config.HudEnabled !== '0';
  const showClock = config.WidgetClock !== '0';
  const showSys = config.WidgetSystemGpu !== '0';
  const showSpectrum = config.WidgetSpectrum !== '0';
  const showPomodoro = config.WidgetPomodoro !== '0';
  const showMedia = config.WidgetMedia !== '0';
  const showNet = config.WidgetNetwork !== '0';

  const dimmerPct = Number(config.DisplayDimmerPercent ?? 0);
  const warmthKelvin = Number(config.DisplayWarmthKelvin ?? 6500);
  const warmth = kelvinToWarmthOverlay(warmthKelvin);

  const formatPomodoro = (sec: number) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  };

  const materialLabel =
    blurMode === 0
      ? 'HDR Net Cam'
      : blurMode === 1
        ? 'Acrylic 24px'
        : blurMode === 2
          ? 'SDR Blur'
          : 'Mica AltTab';

  return (
    <div className="flex flex-col gap-2.5">
      {/* Canvas Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-2 text-[11px]">
        <div className="flex items-center gap-1.5">
          <button
            onClick={() => {
              const next = !showSplit;
              setShowSplit(next);
              setSplitPercent(next ? 34 : 0);
            }}
            style={
              showSplit
                ? {
                    backgroundColor: hexWithAlpha(primaryAccent, 0.18),
                    borderColor: hexWithAlpha(primaryAccent, 0.45),
                    color: primaryAccent,
                  }
                : undefined
            }
            className="l2-pill flex items-center gap-1.5 rounded-lg px-2 py-1 font-medium text-[#A3A9B8]"
          >
            <Columns2 className="h-3 w-3" />
            <span>Önce / Sonra</span>
          </button>

          <button
            onClick={() => setHdrSim((p) => !p)}
            style={
              hdrSim
                ? {
                    backgroundColor: hexWithAlpha('#2EE6A6', 0.14),
                    borderColor: hexWithAlpha('#2EE6A6', 0.4),
                    color: '#2EE6A6',
                  }
                : undefined
            }
            className="l2-pill flex items-center gap-1 rounded-lg px-2 py-1 font-mono text-[10px] text-[#A3A9B8]"
          >
            <Sun className="h-3 w-3" />
            <span>{hdrSim ? '10-bit scRGB' : '8-bit SDR'}</span>
          </button>
        </div>

        <div className="flex items-center gap-1">
          <button
            onClick={() => setActiveMonitor(1)}
            className={`rounded-md px-2 py-0.5 font-mono text-[10px] transition-colors ${
              activeMonitor === 1
                ? 'bg-white/15 text-white'
                : 'text-[#6B7183] hover:text-white'
            }`}
          >
            MON 1
          </button>
          <button
            onClick={() => setActiveMonitor(2)}
            className={`rounded-md px-2 py-0.5 font-mono text-[10px] transition-colors ${
              activeMonitor === 2
                ? 'bg-white/15 text-white'
                : 'text-[#6B7183] hover:text-white'
            }`}
          >
            MON 2
          </button>
        </div>
      </div>

      {/* Main 16:10 Desktop Viewport */}
      <div
        className={`relative w-full overflow-hidden rounded-xl border border-white/15 bg-[#05070A] shadow-2xl select-none ${heightClass}`}
      >
        {/* AFTER LAYER: Full Doom Control Center v3.0 Themed Desktop */}
        <div
          style={{
            background: `
              radial-gradient(circle at 22% 18%, ${hexWithAlpha(primaryAccent, 0.28)} 0%, transparent 55%),
              radial-gradient(circle at 82% 76%, ${hexWithAlpha(secondaryAccent, 0.22)} 0%, transparent 52%),
              radial-gradient(circle at 50% 100%, ${hexWithAlpha(shadowGlow, 0.32)} 0%, transparent 60%),
              linear-gradient(160deg, #06080D 0%, ${bodyTint} 55%, #05060A 100%)
            `,
            filter: hdrSim ? 'contrast(1.05) saturate(1.12)' : 'none',
          }}
          className="absolute inset-0 p-3 flex flex-col justify-between"
        >
          {/* Decorative cyber grid lines on wallpaper */}
          <div
            style={{
              backgroundImage: `linear-gradient(to right, rgba(255,255,255,0.025) 1px, transparent 1px), linear-gradient(to bottom, rgba(255,255,255,0.025) 1px, transparent 1px)`,
              backgroundSize: '24px 24px',
            }}
            className="pointer-events-none absolute inset-0"
          />

          {/* Desktop Icons Column (left) */}
          {telemetry.desktopIconsVisible && (
            <div className="relative z-10 flex flex-col gap-2 self-start pt-0.5 pl-0.5">
              {['Bu Bilgisayar', 'Doom Vault', 'Render Studio'].map((lbl, i) => (
                <div
                  key={lbl}
                  className="flex flex-col items-center gap-0.5 rounded-md p-1 hover:bg-white/8"
                >
                  <div
                    style={{
                      backgroundColor: hexWithAlpha(primaryAccent, 0.2),
                      borderColor: hexWithAlpha(rimHighlight, 0.5),
                    }}
                    className="flex h-5 w-5 items-center justify-center rounded border"
                  >
                    <FolderOpen
                      className="h-3 w-3"
                      style={{ color: i === 0 ? primaryAccent : rimHighlight }}
                    />
                  </div>
                  <span className="text-[8px] font-medium text-[#E2E8F0]/80">
                    {lbl}
                  </span>
                </div>
              ))}
            </div>
          )}

          {/* Simulated Explorer Window (Center-Left) */}
          <div
            onClick={() => onSelectView?.(2)}
            style={{
              backgroundColor: hexWithAlpha(
                bodyTint,
                Math.max(0.38, Math.min(0.92, tintIntensity / 120)),
              ),
              borderColor: showRim ? rimHighlight : 'rgba(255,255,255,0.12)',
              boxShadow: `0 12px ${Math.round(shadowSpread * 1.1)}px ${hexWithAlpha(
                shadowGlow,
                shadowIntensity / 255,
              )}, inset 0 1px 0 rgba(255,255,255,0.22)`,
              backdropFilter: blurMode === 0 ? 'blur(4px)' : 'blur(16px)',
            }}
            className="absolute top-[14%] left-[14%] z-10 w-[52%] cursor-pointer rounded-xl border transition-all duration-200 hover:scale-[1.01]"
          >
            {/* Explorer Titlebar */}
            <div className="flex items-center justify-between border-b border-white/10 px-2.5 py-1.5">
              <div className="flex items-center gap-1.5">
                <FolderOpen className="h-3 w-3" style={{ color: primaryAccent }} />
                <span className="font-mono text-[9px] font-semibold text-[#F2F4F8]">
                  Gezgin • {materialLabel}
                </span>
              </div>
              <div className="flex items-center gap-1">
                <span className="h-1.5 w-1.5 rounded-full bg-white/25" />
                <span className="h-1.5 w-1.5 rounded-full bg-white/25" />
                <span
                  className="h-1.5 w-1.5 rounded-full"
                  style={{ backgroundColor: '#FF4D5E' }}
                />
              </div>
            </div>

            {/* Explorer Content */}
            <div className="grid grid-cols-3 gap-1.5 p-2.5">
              {['Shaders.hlsl', 'DoomTheme.ini', 'OKLab_LUT.cube'].map((item) => (
                <div
                  key={item}
                  style={{
                    backgroundColor: hexWithAlpha(primaryAccent, 0.09),
                    borderColor: hexWithAlpha(rimHighlight, 0.25),
                  }}
                  className="flex flex-col gap-1 rounded-lg border p-1.5"
                >
                  <div
                    className="h-2 w-8 rounded-full"
                    style={{ backgroundColor: hexWithAlpha(primaryAccent, 0.65) }}
                  />
                  <span className="truncate font-mono text-[8px] text-[#E2E8F0]">
                    {item}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Desktop HUD Widgets Cluster (Right side of canvas) */}
          {hudEnabled && (
            <div
              onClick={() => onSelectView?.(4)}
              className="absolute top-2.5 right-2.5 z-10 flex w-[30%] cursor-pointer flex-col gap-1.5"
            >
              {/* Clock + CPU/RAM HUD */}
              {(showClock || showSys) && (
                <div
                  style={{
                    borderColor: hexWithAlpha(rimHighlight, 0.45),
                    backgroundColor: 'rgba(12, 15, 22, 0.76)',
                  }}
                  className="rounded-lg border p-1.5 backdrop-blur-md"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-mono text-[10px] font-bold text-white">
                      10:42
                    </span>
                    <span
                      className="font-mono text-[8px] font-semibold"
                      style={{ color: primaryAccent }}
                    >
                      CPU {telemetry.cpuPercent}%
                    </span>
                  </div>
                  <div className="mt-1 h-1 w-full overflow-hidden rounded-full bg-white/10">
                    <div
                      style={{
                        width: `${telemetry.cpuPercent}%`,
                        backgroundColor: primaryAccent,
                      }}
                      className="h-full transition-all duration-300"
                    />
                  </div>
                </div>
              )}

              {/* Spectrum + Media HUD */}
              {(showSpectrum || showMedia) && (
                <div
                  style={{
                    borderColor: hexWithAlpha(rimHighlight, 0.4),
                    backgroundColor: 'rgba(12, 15, 22, 0.76)',
                  }}
                  className="rounded-lg border p-1.5 backdrop-blur-md"
                >
                  <div className="mb-1 flex items-center justify-between text-[8px] text-[#A3A9B8]">
                    <span className="flex items-center gap-1 truncate">
                      <Music className="h-2.5 w-2.5" style={{ color: primaryAccent }} />
                      BFG Division
                    </span>
                    <span className="font-mono text-[7px]">WASAPI</span>
                  </div>
                  <div className="flex h-4 items-end gap-[2px]">
                    {[42, 78, 92, 60, 85, 48, 70, 95, 64, 52, 80, 44].map((h, idx) => (
                      <div
                        key={idx}
                        style={{
                          height: `${Math.max(20, (h * (0.6 + telemetry.audioPeak * 0.6)) % 100)}%`,
                          background: `linear-gradient(to top, ${primaryAccent}, ${secondaryAccent})`,
                        }}
                        className="flex-1 rounded-t-xs transition-all duration-150"
                      />
                    ))}
                  </div>
                </div>
              )}

              {/* Pomodoro + Network mini HUD */}
              {(showPomodoro || showNet) && (
                <div
                  style={{
                    borderColor: hexWithAlpha(rimHighlight, 0.4),
                    backgroundColor: 'rgba(12, 15, 22, 0.76)',
                  }}
                  className="flex items-center justify-between rounded-lg border px-2 py-1 backdrop-blur-md"
                >
                  <span className="flex items-center gap-1 font-mono text-[8px] text-[#F2F4F8]">
                    <Timer className="h-2.5 w-2.5" style={{ color: primaryAccent }} />
                    {formatPomodoro(telemetry.pomodoroRemainingSec)}
                  </span>
                  <span className="flex items-center gap-0.5 font-mono text-[7.5px] text-[#2EE6A6]">
                    <Wifi className="h-2.5 w-2.5" />
                    48Mb/s
                  </span>
                </div>
              )}
            </div>
          )}

          {/* Simulated Taskbar / Dock at Bottom */}
          <div
            onClick={() => onSelectView?.(3)}
            style={{
              paddingBottom: taskbarMode >= 2 ? `${Math.min(12, taskbarMargin)}px` : '0px',
              paddingLeft: taskbarMode === 0 || taskbarMode === 1 ? '0px' : '12px',
              paddingRight: taskbarMode === 0 || taskbarMode === 1 ? '0px' : '12px',
            }}
            className={`relative z-10 mt-auto flex w-full cursor-pointer items-center ${
              taskbarMode === 3
                ? 'justify-center'
                : taskbarMode === 4
                  ? 'justify-between'
                  : 'justify-center'
            }`}
          >
            <div
              style={{
                backgroundColor:
                  taskbarMode === 1
                    ? 'rgba(10, 12, 18, 0.22)'
                    : hexWithAlpha(taskbarTint, 0.85),
                borderColor: hexWithAlpha(rimHighlight, 0.5),
                borderRadius:
                  taskbarMode === 0 || taskbarMode === 1
                    ? '0px'
                    : `${Math.min(24, taskbarRadius)}px`,
                boxShadow: `0 -1px 0 ${hexWithAlpha(primaryAccent, 0.5)}, 0 6px 20px rgba(0,0,0,0.6)`,
                width:
                  taskbarMode === 3
                    ? '46%'
                    : taskbarMode === 2
                      ? '78%'
                      : '100%',
              }}
              className="flex h-7 items-center justify-between border px-3 backdrop-blur-xl transition-all duration-200"
            >
              <div className="flex items-center gap-1">
                <span
                  className="h-2.5 w-2.5 rounded-sm"
                  style={{ backgroundColor: primaryAccent }}
                />
                <span className="font-mono text-[8px] text-[#A3A9B8]">
                  {taskbarMode === 3 ? 'ISLAND' : taskbarMode === 2 ? 'DOCK' : 'WIN11'}
                </span>
              </div>

              <div
                className={`flex items-center gap-1.5 ${
                  centerIcons ? 'mx-auto' : 'ml-2 mr-auto'
                }`}
              >
                {[primaryAccent, secondaryAccent, rimHighlight, '#4DB8FF'].map(
                  (c, idx) => (
                    <span
                      key={idx}
                      style={{
                        backgroundColor: hexWithAlpha(c, 0.28),
                        borderColor: c,
                      }}
                      className="h-3.5 w-3.5 rounded-md border transition-transform hover:-translate-y-0.5"
                    />
                  ),
                )}
              </div>

              <span className="font-mono text-[8px] text-[#E2E8F0]">10:42</span>
            </div>
          </div>

          {/* Display Dimmer + Kelvin Night-Light Filter Overlay */}
          {(dimmerPct > 0 || warmthKelvin < 6500) && (
            <div
              style={{
                backgroundColor: `rgba(0, 0, 0, ${(dimmerPct / 100) * 0.78})`,
                boxShadow:
                  warmth.alpha > 0
                    ? `inset 0 0 120px ${hexWithAlpha(warmth.hex, warmth.alpha)}`
                    : 'none',
              }}
              className="pointer-events-none absolute inset-0 z-20 transition-all duration-200"
            >
              {warmth.alpha > 0 && (
                <div
                  style={{
                    backgroundColor: hexWithAlpha(warmth.hex, warmth.alpha * 0.55),
                    mixBlendMode: 'multiply',
                  }}
                  className="absolute inset-0"
                />
              )}
            </div>
          )}
        </div>

        {/* BEFORE LAYER: Stock Un-themed Windows 11 SDR (Clipped by splitPercent) */}
        {showSplit && splitPercent > 0 && (
          <div
            style={{ width: `${splitPercent}%` }}
            className="pointer-events-none absolute inset-y-0 left-0 z-20 overflow-hidden border-r-2 border-white/80 bg-[#1B2432]"
          >
            <div className="flex h-full w-full flex-col justify-between bg-gradient-to-br from-[#1E293B] to-[#0F172A] p-3">
              <div className="inline-flex w-fit items-center rounded bg-black/60 px-1.5 py-0.5 font-mono text-[8px] font-bold text-white/80">
                ÖNCE (STOK SDR)
              </div>
              <div className="my-auto ml-4 w-44 rounded border border-white/20 bg-[#1E1E1E] p-2 shadow">
                <div className="mb-1 text-[8px] text-gray-300">Stok Dosya Gezgini</div>
                <div className="h-6 rounded bg-[#2D2D2D]" />
              </div>
              <div className="h-5 w-full bg-[#111827]/90" />
            </div>
          </div>
        )}

        {/* Interactive Split Slider Range Input */}
        {showSplit && (
          <input
            type="range"
            min={0}
            max={100}
            value={splitPercent}
            onChange={(e) => setSplitPercent(Number(e.target.value))}
            title="Önce / Sonra Karşılaştırma Sürgüsü"
            className="absolute inset-0 z-30 h-full w-full cursor-ew-resize opacity-0"
          />
        )}

        {/* Badge Overlay Bottom-Left */}
        <div className="pointer-events-none absolute bottom-9 left-2.5 z-20 flex items-center gap-1.5">
          <span className="rounded bg-black/65 px-1.5 py-0.5 font-mono text-[9px] text-[#A3A9B8] backdrop-blur-md">
            {config.ActiveProfile || 'DoomEmerald'}
          </span>
          {(dimmerPct > 0 || warmthKelvin < 6500) && (
            <span className="rounded bg-[#FFB547]/20 px-1.5 py-0.5 font-mono text-[9px] text-[#FFB547]">
              Dim {dimmerPct}% • {warmthKelvin}K
            </span>
          )}
        </div>
      </div>
    </div>
  );
};

interface MiniDesktopPreviewDockProps {
  open: boolean;
  onToggleOpen: () => void;
  config: ConfigMap;
  telemetry: TelemetryState;
  accentHex: string;
  onSelectView: (view: ViewId) => void;
}

export const MiniDesktopPreviewDock: React.FC<MiniDesktopPreviewDockProps> = ({
  open,
  onToggleOpen,
  config,
  telemetry,
  accentHex,
  onSelectView,
}) => {
  if (!open) {
    return (
      <aside className="relative z-20 flex h-full w-11 shrink-0 flex-col items-center justify-between border-l border-white/10 bg-[#0A0C12]/85 py-3 backdrop-blur-2xl select-none">
        <button
          onClick={onToggleOpen}
          title="Canlı Masaüstü Önizleme Dock'unu Aç"
          className="l2-pill flex h-8 w-8 items-center justify-center rounded-lg text-[#A3A9B8] hover:text-white"
        >
          <ChevronLeft className="h-4 w-4" />
        </button>
        <div
          onClick={onToggleOpen}
          className="cursor-pointer font-mono text-[10px] font-semibold tracking-widest text-[#6B7183] uppercase [writing-mode:vertical-rl] hover:text-white"
        >
          CANLI MASAÜSTÜ TUVALİ (16:10)
        </div>
        <Monitor className="h-4 w-4" style={{ color: accentHex }} />
      </aside>
    );
  }

  return (
    <aside className="relative z-20 flex h-full w-[348px] shrink-0 flex-col justify-between overflow-y-auto border-l border-white/10 bg-[#0A0C12]/85 p-4 backdrop-blur-2xl select-none">
      <div className="flex flex-col gap-4">
        {/* Dock Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Sparkles className="h-4 w-4" style={{ color: accentHex }} />
            <div>
              <h3 className="font-['Outfit'] text-xs font-bold tracking-wider text-[#F2F4F8] uppercase">
                Canlı Masaüstü Tuvali
              </h3>
              <p className="text-[11px] text-[#6B7183]">
                16:10 Gerçek Zamanlı DWM & HUD Sentezi
              </p>
            </div>
          </div>
          <button
            onClick={onToggleOpen}
            title="Önizleme Dock'unu Daralt"
            className="l2-pill flex h-7 w-7 items-center justify-center rounded-lg text-[#A3A9B8] hover:text-white"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>

        {/* 16:10 Live Preview Canvas */}
        <LiveDesktopCanvas
          config={config}
          telemetry={telemetry}
          onSelectView={onSelectView}
        />

        {/* Active Tokens Summary Card */}
        <div className="bento-card p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="font-mono text-[10px] font-semibold tracking-wider text-[#A3A9B8] uppercase">
              Aktif Renk & Işık Tokenları
            </span>
            <button
              onClick={() => onSelectView(1)}
              className="font-mono text-[10px] hover:underline"
              style={{ color: accentHex }}
            >
              Stüdyoda Düzenle ›
            </button>
          </div>
          <div className="grid grid-cols-2 gap-2">
            {[
              { label: 'Primary Accent', hex: config.PrimaryAccentHex || '#2EE6A6' },
              { label: 'Specular Rim', hex: config.RimHighlightHex || '#6CA881' },
              { label: '360° Glow', hex: config.ShadowGlowHex || '#267347' },
              { label: 'Explorer Tint', hex: config.BodyTintHex || '#0D1F16' },
            ].map((tok) => (
              <div
                key={tok.label}
                className="l2-pill flex items-center gap-2 rounded-lg p-1.5"
              >
                <span
                  style={{ backgroundColor: tok.hex }}
                  className="h-5 w-5 shrink-0 rounded-md border border-white/20 shadow-sm"
                />
                <div className="min-w-0">
                  <div className="truncate text-[10px] text-[#A3A9B8]">
                    {tok.label}
                  </div>
                  <div className="font-mono text-[10px] font-semibold text-[#F2F4F8]">
                    {tok.hex.toUpperCase()}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Live Engine Status Metrics */}
        <div className="bento-card p-3">
          <div className="mb-2 font-mono text-[10px] font-semibold tracking-wider text-[#A3A9B8] uppercase">
            Motor & Katman Durumu
          </div>
          <div className="flex flex-col gap-1.5 text-xs">
            <div className="flex items-center justify-between">
              <span className="text-[#A3A9B8]">DWM Cam Daemon</span>
              <span
                className="font-mono text-[11px] font-semibold"
                style={{
                  color: telemetry.glassDaemonRunning ? '#2EE6A6' : '#FFB547',
                }}
              >
                {telemetry.glassDaemonRunning ? 'AKTİF (60Hz)' : 'BEKLEMEDE'}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-[#A3A9B8]">Gölge Yayılımı (Spread)</span>
              <span className="font-mono text-[11px] text-[#F2F4F8]">
                {config.ShadowSpread || 28} px
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-[#A3A9B8]">Smart Focus Kipi</span>
              <span
                className="font-mono text-[11px]"
                style={{
                  color: telemetry.smartPaused ? '#FFB547' : '#2EE6A6',
                }}
              >
                {telemetry.smartPaused
                  ? `DURAKLATILDI (${telemetry.smartPauseReason})`
                  : 'İZLENİYOR'}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-[#A3A9B8]">Masaüstü İkonları</span>
              <span className="font-mono text-[11px] text-[#F2F4F8]">
                {telemetry.desktopIconsVisible ? 'Görünür' : 'Gizli'}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Footer note */}
      <div className="mt-3 border-t border-white/8 pt-2.5 text-center font-mono text-[10px] text-[#6B7183]">
        Tuvaldeki öğelere tıklayarak ilgili modüle gidebilirsiniz
      </div>
    </aside>
  );
};
