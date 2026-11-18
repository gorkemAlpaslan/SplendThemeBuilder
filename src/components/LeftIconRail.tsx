import React from 'react';
import {
  Activity,
  ChevronLeft,
  ChevronRight,
  Layers,
  LayoutGrid,
  Palette,
  PanelBottom,
  Rocket,
  Settings2,
  Sparkles,
  Zap,
} from 'lucide-react';
import type { BoostProfileName, ViewId } from '../types/theme';
import { hexWithAlpha } from '../utils/color';

export interface NavItemSpec {
  id: ViewId;
  label: string;
  shortLabel: string;
  badge?: string;
  icon: React.ComponentType<{ className?: string; style?: React.CSSProperties }>;
}

export const NAV_VIEWS: NavItemSpec[] = [
  {
    id: 0,
    label: 'Tema Galerisi & Tuval',
    shortLabel: 'Galeri',
    badge: 'V0',
    icon: LayoutGrid,
  },
  {
    id: 1,
    label: 'Renk & Işık Stüdyosu',
    shortLabel: 'Stüdyo',
    badge: 'V1',
    icon: Palette,
  },
  {
    id: 2,
    label: 'Gezgin & 10-bit HDR',
    shortLabel: 'HDR Cam',
    badge: 'V2',
    icon: Layers,
  },
  {
    id: 3,
    label: 'Görev Çubuğu & Dock',
    shortLabel: 'Dock',
    badge: 'V3',
    icon: PanelBottom,
  },
  {
    id: 4,
    label: 'Masaüstü HUD & Ses',
    shortLabel: 'HUD 8x',
    badge: 'V4',
    icon: Activity,
  },
  {
    id: 5,
    label: 'Oyun/Render Boost',
    shortLabel: 'Boost',
    badge: 'V5',
    icon: Rocket,
  },
  {
    id: 6,
    label: 'Sistem & Yedekleme',
    shortLabel: 'Sistem',
    badge: 'V6',
    icon: Settings2,
  },
];

interface LeftIconRailProps {
  activeView: ViewId;
  onSelectView: (view: ViewId) => void;
  expanded: boolean;
  onToggleExpanded: () => void;
  accentHex: string;
  boostProfile: BoostProfileName;
  onCycleBoostProfile: () => void;
  onQuickChameleon: () => void;
  glassDaemonRunning: boolean;
}

export const LeftIconRail: React.FC<LeftIconRailProps> = ({
  activeView,
  onSelectView,
  expanded,
  onToggleExpanded,
  accentHex,
  boostProfile,
  onCycleBoostProfile,
  onQuickChameleon,
  glassDaemonRunning,
}) => {
  return (
    <aside
      style={{
        width: expanded ? '224px' : '64px',
        transition: 'width 240ms cubic-bezier(0.22, 1, 0.36, 1)',
      }}
      className="relative z-20 flex h-full shrink-0 flex-col justify-between border-r border-white/10 bg-[#0A0C12]/85 backdrop-blur-2xl select-none"
    >
      {/* Top Brand / Collapse Toggle */}
      <div>
        <div className="flex h-[52px] items-center justify-between border-b border-white/8 px-3">
          <button
            onClick={() => onSelectView(0)}
            title="SplendThemeBuilder — Doom Control Center v3.0"
            className="group flex items-center gap-2.5 overflow-hidden text-left focus:outline-none"
          >
            <div
              style={{
                background: `linear-gradient(135deg, ${accentHex}, ${hexWithAlpha(accentHex, 0.28)})`,
                boxShadow: `0 0 18px ${hexWithAlpha(accentHex, 0.45)}`,
              }}
              className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl border border-white/25 text-xs font-extrabold tracking-wider text-[#07080C] transition-transform duration-200 group-hover:scale-105"
            >
              DC
            </div>
            {expanded && (
              <div className="min-w-0">
                <div className="truncate font-['Outfit'] text-sm font-bold tracking-tight text-[#F2F4F8]">
                  DOOM STUDIO
                </div>
                <div className="flex items-center gap-1.5 text-[10px] font-medium tracking-wider text-[#A3A9B8] uppercase">
                  <span
                    className="h-1.5 w-1.5 rounded-full"
                    style={{
                      backgroundColor: glassDaemonRunning ? '#2EE6A6' : '#FFB547',
                      boxShadow: glassDaemonRunning ? '0 0 8px #2EE6A6' : 'none',
                    }}
                  />
                  v3.0 Bento Deck
                </div>
              </div>
            )}
          </button>

          {expanded && (
            <button
              onClick={onToggleExpanded}
              title="Rayı Daralt"
              className="flex h-7 w-7 items-center justify-center rounded-lg text-[#A3A9B8] hover:bg-white/8 hover:text-white"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
          )}
        </div>

        {/* Navigation Views */}
        <nav className="mt-3 flex flex-col gap-1.5 px-2" aria-label="Ana Görünümler">
          {NAV_VIEWS.map((item) => {
            const Icon = item.icon;
            const isActive = activeView === item.id;
            return (
              <button
                key={item.id}
                onClick={() => onSelectView(item.id)}
                title={`${item.label} (${item.badge})`}
                style={
                  isActive
                    ? {
                        backgroundColor: hexWithAlpha(accentHex, 0.16),
                        borderColor: hexWithAlpha(accentHex, 0.42),
                        boxShadow: `0 0 20px ${hexWithAlpha(accentHex, 0.18)}, inset 0 1px 0 rgba(255,255,255,0.14)`,
                      }
                    : undefined
                }
                className={`group relative flex h-11 w-full items-center gap-3 rounded-xl border px-2.5 text-left transition-all duration-200 ${
                  isActive
                    ? 'text-[#F2F4F8]'
                    : 'border-transparent text-[#A3A9B8] hover:border-white/8 hover:bg-white/5 hover:text-[#F2F4F8]'
                }`}
              >
                {/* 3px Neon Left Indicator Bar */}
                <span
                  style={{
                    backgroundColor: isActive ? accentHex : 'transparent',
                    boxShadow: isActive ? `0 0 12px ${accentHex}` : 'none',
                    transform: isActive ? 'scaleY(1)' : 'scaleY(0.2)',
                  }}
                  className="absolute top-2 bottom-2 -left-1 w-[3px] rounded-r-full transition-all duration-200"
                />

                <div
                  style={
                    isActive
                      ? {
                          backgroundColor: hexWithAlpha(accentHex, 0.24),
                          color: accentHex,
                        }
                      : undefined
                  }
                  className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg transition-colors duration-200 group-hover:text-white"
                >
                  <Icon className="h-[18px] w-[18px]" />
                </div>

                {expanded && (
                  <div className="flex min-w-0 flex-1 items-center justify-between">
                    <span className="truncate text-[12.5px] font-medium tracking-tight">
                      {item.label}
                    </span>
                    <span className="ml-1 rounded bg-white/6 px-1.5 py-0.5 font-mono text-[10px] text-[#6B7183]">
                      {item.badge}
                    </span>
                  </div>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Bottom Quick Pills */}
      <div className="flex flex-col gap-2 border-t border-white/8 p-2">
        {/* Quick Chameleon Auto-Theme Button */}
        <button
          onClick={onQuickChameleon}
          title="Chameleon OKLab: Duvar Kağıdından Otomatik Renk Çıkar"
          className="l2-pill flex h-10 w-full items-center justify-center gap-2.5 rounded-xl px-2.5 text-xs font-medium text-[#F2F4F8] hover:border-white/20"
        >
          <Sparkles className="h-4 w-4 shrink-0" style={{ color: accentHex }} />
          {expanded && (
            <div className="flex min-w-0 flex-1 items-center justify-between">
              <span className="truncate">Chameleon OKLab</span>
              <span className="rounded bg-white/10 px-1.5 py-0.5 font-mono text-[10px] text-[#A3A9B8]">
                OTO
              </span>
            </div>
          )}
        </button>

        {/* Active Boost Profile Pill */}
        <button
          onClick={onCycleBoostProfile}
          title="Boost Profilini Değiştir (Normal / Oyun / Render / Sessiz)"
          className="l2-pill flex h-10 w-full items-center justify-center gap-2.5 rounded-xl px-2.5 text-xs font-medium text-[#F2F4F8]"
        >
          <Zap
            className="h-4 w-4 shrink-0"
            style={{
              color:
                boostProfile === 'Oyun'
                  ? '#FF4D5E'
                  : boostProfile === 'Render'
                    ? '#FFB547'
                    : boostProfile === 'Sessiz'
                      ? '#4DB8FF'
                      : '#2EE6A6',
            }}
          />
          {expanded && (
            <div className="flex min-w-0 flex-1 items-center justify-between">
              <span className="truncate text-[#A3A9B8]">Boost Kip</span>
              <span
                className="rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold"
                style={{
                  backgroundColor: hexWithAlpha(accentHex, 0.2),
                  color: accentHex,
                }}
              >
                {boostProfile.toUpperCase()}
              </span>
            </div>
          )}
        </button>

        {/* Expand/Collapse Toggle at bottom */}
        <button
          onClick={onToggleExpanded}
          title={expanded ? 'Sol Rayı Daralt (64px)' : 'Sol Rayı Genişlet (224px)'}
          className="flex h-8 w-full items-center justify-center gap-1.5 rounded-lg text-[11px] text-[#6B7183] transition-colors hover:bg-white/6 hover:text-[#F2F4F8]"
        >
          {expanded ? (
            <>
              <ChevronLeft className="h-3.5 w-3.5" />
              <span>Daralt</span>
            </>
          ) : (
            <ChevronRight className="h-4 w-4" />
          )}
        </button>
      </div>
    </aside>
  );
};
