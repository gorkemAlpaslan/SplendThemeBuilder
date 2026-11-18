import React from 'react';
import {
  Check,
  Command,
  Eye,
  EyeOff,
  Minus,
  RotateCcw,
  Search,
  Square,
  X,
} from 'lucide-react';
import {
  closeAppWindow,
  minimizeAppWindow,
  toggleMaximizeAppWindow,
} from '../api/bridge';
import type { TelemetryState, ViewId } from '../types/theme';
import { hexWithAlpha, relativeLuminance } from '../utils/color';
import { NAV_VIEWS } from './LeftIconRail';

interface TopTelemetryHeaderProps {
  activeView: ViewId;
  telemetry: TelemetryState;
  cpuHistory: number[];
  accentHex: string;
  hasDraftChanges: boolean;
  isApplying: boolean;
  hdrGuardEnabled: boolean;
  hdrNits: number;
  previewOpen: boolean;
  onTogglePreview: () => void;
  onOpenCommandPalette: () => void;
  onUndo: () => void;
  onApply: () => void;
}

const SparklineSvg: React.FC<{ values: number[]; color: string }> = ({
  values,
  color,
}) => {
  const samples = values.length > 1 ? values.slice(-24) : [15, 18, 16, 20, 19, 22];
  const width = 46;
  const height = 16;
  const max = Math.max(100, ...samples);
  const min = 0;

  const points = samples
    .map((v, i) => {
      const x = (i / Math.max(1, samples.length - 1)) * width;
      const y = height - ((v - min) / Math.max(1, max - min)) * (height - 3) - 1.5;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(' ');

  return (
    <svg width={width} height={height} className="overflow-visible">
      <polyline
        fill="none"
        stroke={color}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
        points={points}
      />
    </svg>
  );
};

export const TopTelemetryHeader: React.FC<TopTelemetryHeaderProps> = ({
  activeView,
  telemetry,
  cpuHistory,
  accentHex,
  hasDraftChanges,
  isApplying,
  hdrGuardEnabled,
  hdrNits,
  previewOpen,
  onTogglePreview,
  onOpenCommandPalette,
  onUndo,
  onApply,
}) => {
  const currentViewMeta =
    NAV_VIEWS.find((v) => v.id === activeView) || NAV_VIEWS[0];
  const accentTextDark = relativeLuminance(accentHex) > 0.42;

  const ramHistory = React.useMemo(() => {
    const base = telemetry.ramUsagePercent || 35;
    return cpuHistory.map((c, idx) =>
      Math.max(10, Math.min(98, Math.round(base + Math.sin(idx * 0.5) * 4 + (c % 5) - 2))),
    );
  }, [cpuHistory, telemetry.ramUsagePercent]);

  const gpuHistory = React.useMemo(() => {
    const base = telemetry.gpuTempC || 46;
    return cpuHistory.map((c, idx) =>
      Math.max(30, Math.min(90, Math.round(base + Math.cos(idx * 0.6) * 3 + (c % 4) - 2))),
    );
  }, [cpuHistory, telemetry.gpuTempC]);

  return (
    <header
      data-tauri-drag-region
      className="relative z-20 flex h-[52px] w-full shrink-0 items-center justify-between border-b border-white/10 bg-[#090B10]/85 px-4 backdrop-blur-2xl select-none"
    >
      {/* Left: Breadcrumb + Draft status */}
      <div
        data-tauri-drag-region
        className="flex min-w-0 items-center gap-2.5"
      >
        <span className="font-mono text-xs font-semibold tracking-wider text-[#6B7183] uppercase">
          Doom CC
        </span>
        <span className="text-[#464B5A]">›</span>
        <span className="truncate font-['Outfit'] text-sm font-semibold tracking-tight text-[#F2F4F8]">
          {currentViewMeta.label}
        </span>
        {hasDraftChanges ? (
          <span
            style={{
              backgroundColor: hexWithAlpha('#FFB547', 0.16),
              borderColor: hexWithAlpha('#FFB547', 0.4),
              color: '#FFB547',
            }}
            className="ml-1 flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] font-medium"
          >
            <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-[#FFB547]" />
            TASLAK
          </span>
        ) : (
          <span className="ml-1 hidden items-center gap-1 rounded-full border border-white/8 bg-white/4 px-2 py-0.5 font-mono text-[10px] text-[#6B7183] xl:flex">
            SENKRON
          </span>
        )}
      </div>

      {/* Center: Interactive Command Bar Pill (Ctrl+Shift+Space) */}
      <div className="mx-3 flex flex-1 justify-center max-w-[420px]">
        <button
          onClick={onOpenCommandPalette}
          className="l2-pill group flex h-8 w-full items-center justify-between rounded-full px-3.5 text-xs text-[#A3A9B8] hover:border-white/25 hover:text-[#F2F4F8]"
        >
          <div className="flex items-center gap-2 truncate">
            <Search
              className="h-3.5 w-3.5 shrink-0 transition-transform group-hover:scale-110"
              style={{ color: accentHex }}
            />
            <span className="truncate">
              Komut, #tema veya @widget ara…
            </span>
          </div>
          <kbd className="ml-2 flex shrink-0 items-center gap-1 rounded bg-white/8 px-1.5 py-0.5 font-mono text-[10px] text-[#A3A9B8]">
            <Command className="h-2.5 w-2.5" />
            <span>Ctrl+Shift+Space</span>
          </kbd>
        </button>
      </div>

      {/* Right: Telemetry Chips + Undo + Apply + Window Controls */}
      <div className="flex items-center gap-2">
        {/* Live Sparkline Telemetry Chips */}
        <div className="hidden items-center gap-1.5 2xl:flex">
          {/* CPU Chip */}
          <div
            title="Canlı CPU Kullanımı (24 örnek sparkline)"
            className="l2-pill flex h-7 items-center gap-2 rounded-lg px-2.5 font-mono text-[11px]"
          >
            <span className="text-[#6B7183]">CPU</span>
            <span className="font-semibold text-[#F2F4F8]">
              {telemetry.cpuPercent}%
            </span>
            <SparklineSvg values={cpuHistory} color={accentHex} />
          </div>

          {/* RAM Chip */}
          <div
            title={`Bellek: ${telemetry.ramUsedGb} GB / ${telemetry.ramTotalGb} GB (${telemetry.ramFreeGb} GB Boş)`}
            className="l2-pill flex h-7 items-center gap-2 rounded-lg px-2.5 font-mono text-[11px]"
          >
            <span className="text-[#6B7183]">RAM</span>
            <span className="font-semibold text-[#F2F4F8]">
              {telemetry.ramUsedGb.toFixed(1)}G
            </span>
            <SparklineSvg values={ramHistory} color="#4DB8FF" />
          </div>

          {/* GPU Chip */}
          <div
            title={`GPU Yükü: %${telemetry.gpuUtilPercent} • Sıcaklık: ${telemetry.gpuTempC}°C`}
            className="l2-pill flex h-7 items-center gap-2 rounded-lg px-2.5 font-mono text-[11px]"
          >
            <span className="text-[#6B7183]">GPU</span>
            <span className="font-semibold text-[#F2F4F8]">
              {telemetry.gpuTempC}°C
            </span>
            <SparklineSvg values={gpuHistory} color="#2EE6A6" />
          </div>

          {/* HDR 10-bit Chip */}
          <div
            title={`10-bit scRGB HDR Beyaz Koruma: ${hdrGuardEnabled ? `Aktif (${hdrNits} nits)` : 'Kapalı'}`}
            style={
              hdrGuardEnabled
                ? {
                    borderColor: hexWithAlpha(accentHex, 0.35),
                    backgroundColor: hexWithAlpha(accentHex, 0.1),
                  }
                : undefined
            }
            className="l2-pill flex h-7 items-center gap-1.5 rounded-lg px-2 font-mono text-[10px]"
          >
            <span
              className="h-1.5 w-1.5 rounded-full"
              style={{
                backgroundColor: hdrGuardEnabled ? '#2EE6A6' : '#6B7183',
              }}
            />
            <span className="font-semibold text-[#F2F4F8]">
              {hdrGuardEnabled ? `HDR10 · ${hdrNits}n` : 'SDR'}
            </span>
          </div>
        </div>

        {/* Preview Dock Toggle */}
        <button
          onClick={onTogglePreview}
          title={previewOpen ? 'Canlı Masaüstü Önizlemesini Gizle' : 'Canlı Masaüstü Önizlemesini Göster'}
          className="l2-pill flex h-8 items-center gap-1.5 rounded-lg px-2.5 text-xs font-medium text-[#A3A9B8] hover:text-white"
        >
          {previewOpen ? (
            <Eye className="h-3.5 w-3.5" style={{ color: accentHex }} />
          ) : (
            <EyeOff className="h-3.5 w-3.5" />
          )}
          <span className="hidden lg:inline">Önizleme</span>
        </button>

        {/* Undo (Ctrl+Z) Button */}
        <button
          onClick={onUndo}
          title="Son Snapshot'a Geri Dön (Ctrl+Z)"
          className="l2-pill flex h-8 items-center gap-1.5 rounded-lg px-2.5 text-xs font-medium text-[#A3A9B8] hover:text-white active:scale-95"
        >
          <RotateCcw className="h-3.5 w-3.5" />
          <span className="hidden md:inline">Geri Al</span>
        </button>

        {/* Primary Apply (Uygula) Button */}
        <button
          onClick={onApply}
          disabled={isApplying}
          style={{
            backgroundColor: accentHex,
            color: accentTextDark ? '#07080C' : '#FFFFFF',
            boxShadow: `0 0 22px ${hexWithAlpha(accentHex, 0.45)}`,
          }}
          className="flex h-8 items-center gap-1.5 rounded-lg px-3.5 text-xs font-bold tracking-wide transition-all duration-150 hover:brightness-110 active:scale-95 disabled:opacity-60"
        >
          <Check className="h-3.5 w-3.5 stroke-[2.5]" />
          <span>{isApplying ? 'Uygulanıyor…' : 'Uygula'}</span>
        </button>

        {/* Divider */}
        <div className="mx-0.5 h-4 w-[1px] bg-white/10" />

        {/* Custom Tauri Frameless Window Controls */}
        <div className="flex items-center gap-1">
          <button
            onClick={() => void minimizeAppWindow()}
            title="Küçült"
            className="flex h-7 w-7 items-center justify-center rounded-lg text-[#A3A9B8] transition-colors hover:bg-white/10 hover:text-white"
          >
            <Minus className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={() => void toggleMaximizeAppWindow()}
            title="Ekranı Kapla / Geri Yükle"
            className="flex h-7 w-7 items-center justify-center rounded-lg text-[#A3A9B8] transition-colors hover:bg-white/10 hover:text-white"
          >
            <Square className="h-3 w-3" />
          </button>
          <button
            onClick={() => void closeAppWindow()}
            title="Kapat"
            className="flex h-7 w-7 items-center justify-center rounded-lg text-[#A3A9B8] transition-colors hover:bg-[#FF4D5E] hover:text-white"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
    </header>
  );
};
