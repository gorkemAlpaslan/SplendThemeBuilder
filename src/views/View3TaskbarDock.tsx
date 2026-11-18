import React from 'react';
import {
  AlignCenter,
  AlignLeft,
  Bell,
  Clock,
  Layers,
  PanelBottom,
  Sparkles,
  Volume2,
  Wifi,
} from 'lucide-react';
import type { ConfigMap } from '../types/theme';
import { hexWithAlpha } from '../utils/color';

interface View3TaskbarDockProps {
  config: ConfigMap;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  showToast: (msg: string) => void;
}

const TASKBAR_MODES = [
  {
    id: '0',
    name: 'Classic Bar',
    desc: 'Tam genişlikte opak zümrüt/obsidyen bar ve üst neon sınır çizgisi',
  },
  {
    id: '1',
    name: 'Clear (Saydam)',
    desc: 'Arka planı tamamen şeffaf, yalnız yüzen uygulama simgeleri',
  },
  {
    id: '2',
    name: 'Floating Dock',
    desc: 'Kenarlardan boşluklu, yuvarlatılmış cam kapsül dock ve dış aura',
  },
  {
    id: '3',
    name: 'Dynamic Island',
    desc: 'Merkezde kompakt hap; medya veya bildirim geldiğinde akıcı genişler',
  },
  {
    id: '4',
    name: 'Segmented',
    desc: 'Sol Başlat adası, merkez Uygulama adası ve sağ Sistem Tepsisi adası',
  },
];

export const View3TaskbarDock: React.FC<View3TaskbarDockProps> = ({
  config,
  onUpdateConfig,
  showToast,
}) => {
  const primaryAccent = config.PrimaryAccentHex || '#2EE6A6';
  const rimHighlight = config.RimHighlightHex || '#6CA881';
  const taskbarTint = config.TaskbarTintHex || '#08120D';

  const taskbarMode = config.TaskbarMode || '2';
  const centerIcons = config.TaskbarCenterIcons !== '0';
  const cornerRadius = Number(config.TaskbarCornerRadius ?? 16);
  const dockMargin = Number(config.TaskbarDockMargin ?? config.TaskbarMarginPx ?? 8);
  const taskbarAlpha = Number(config.TaskbarTintAlpha ?? 190);
  const specularRim = config.TaskbarSpecularRim !== '0';
  const hideOverflow = config.TrayHideOverflow !== '0';
  const compactClock = config.TrayCompactClock !== '0';
  const islandExpand = config.IslandExpandOnMedia !== '0';

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* MODE SELECTOR 12x2: 5 Visual Mini-Illustrations */}
      <div className="bento-card col-span-12 p-5">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="flex items-center gap-2">
              <PanelBottom className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Görev Çubuğu & Dock Mimari Seçici
              </h3>
            </div>
            <p className="mt-0.5 text-xs text-[#A3A9B8]">
              Windows 11 görev çubuğunu tam genişlikten macOS tarzı Floating Dock veya
              Dynamic Island kapsülüne dönüştürün.
            </p>
          </div>
          <span className="font-mono text-xs font-semibold" style={{ color: primaryAccent }}>
            AKTİF KİP: {TASKBAR_MODES.find((m) => m.id === taskbarMode)?.name}
          </span>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
          {TASKBAR_MODES.map((m) => {
            const isSel = taskbarMode === m.id;
            return (
              <button
                key={m.id}
                onClick={() => {
                  onUpdateConfig({ TaskbarMode: m.id });
                  showToast(`Görev çubuğu kipi '${m.name}' olarak ayarlandı`);
                }}
                style={
                  isSel
                    ? {
                        backgroundColor: hexWithAlpha(primaryAccent, 0.16),
                        borderColor: primaryAccent,
                        boxShadow: `0 8px 24px ${hexWithAlpha(primaryAccent, 0.2)}`,
                      }
                    : undefined
                }
                className="l2-pill flex flex-col justify-between rounded-xl p-3.5 text-left transition-all hover:-translate-y-0.5"
              >
                {/* Visual Mini Illustration */}
                <div className="mb-3 flex h-14 w-full items-end justify-center rounded-lg border border-white/10 bg-[#06080D] p-2">
                  {m.id === '0' && (
                    <div
                      style={{
                        backgroundColor: taskbarTint,
                        borderTopColor: primaryAccent,
                      }}
                      className="flex h-4 w-full items-center justify-center gap-1 border-t-2"
                    >
                      <span className="h-1.5 w-1.5 rounded-full bg-white/70" />
                      <span className="h-1.5 w-1.5 rounded-full bg-white/70" />
                      <span className="h-1.5 w-1.5 rounded-full bg-white/70" />
                    </div>
                  )}
                  {m.id === '1' && (
                    <div className="flex h-4 w-full items-center justify-center gap-1.5">
                      <span
                        style={{ backgroundColor: primaryAccent }}
                        className="h-2 w-2 rounded-sm"
                      />
                      <span className="h-2 w-2 rounded-sm bg-white/60" />
                      <span className="h-2 w-2 rounded-sm bg-white/60" />
                    </div>
                  )}
                  {m.id === '2' && (
                    <div
                      style={{
                        backgroundColor: hexWithAlpha(taskbarTint, 0.9),
                        borderColor: rimHighlight,
                      }}
                      className="flex h-4 w-4/5 items-center justify-center gap-1 rounded-full border"
                    >
                      <span
                        style={{ backgroundColor: primaryAccent }}
                        className="h-1.5 w-1.5 rounded-full"
                      />
                      <span className="h-1.5 w-1.5 rounded-full bg-white/70" />
                      <span className="h-1.5 w-1.5 rounded-full bg-white/70" />
                    </div>
                  )}
                  {m.id === '3' && (
                    <div
                      style={{
                        backgroundColor: '#0A0D14',
                        borderColor: primaryAccent,
                        boxShadow: `0 0 10px ${hexWithAlpha(primaryAccent, 0.4)}`,
                      }}
                      className="flex h-4 w-1/2 items-center justify-center gap-1 rounded-full border"
                    >
                      <span
                        style={{ backgroundColor: primaryAccent }}
                        className="h-1.5 w-3 rounded-full"
                      />
                      <span className="h-1.5 w-1.5 rounded-full bg-white/70" />
                    </div>
                  )}
                  {m.id === '4' && (
                    <div className="flex h-4 w-full items-center justify-between gap-1">
                      <div
                        style={{ borderColor: rimHighlight }}
                        className="h-full w-5 rounded-md border bg-white/10"
                      />
                      <div
                        style={{ borderColor: primaryAccent }}
                        className="flex h-full flex-1 items-center justify-center gap-1 rounded-md border bg-white/10"
                      >
                        <span className="h-1.5 w-1.5 rounded-full bg-white/80" />
                        <span className="h-1.5 w-1.5 rounded-full bg-white/80" />
                      </div>
                      <div
                        style={{ borderColor: rimHighlight }}
                        className="h-full w-6 rounded-md border bg-white/10"
                      />
                    </div>
                  )}
                </div>

                <div>
                  <div className="font-['Outfit'] text-xs font-bold text-[#F2F4F8]">
                    {m.name}
                  </div>
                  <p className="mt-1 text-[11px] leading-snug text-[#A3A9B8]">
                    {m.desc}
                  </p>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* LAYOUT & GEOMETRY 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
            Hizalama & Geometri Parametreleri
          </h3>
          <p className="mt-0.5 text-xs text-[#A3A9B8]">
            Simge hizalamasını, kapsül köşe yarıçapını ve ekran alt kenarından olan
            yüzme boşluğunu ayarlayın.
          </p>

          {/* Centering Segmented Control */}
          <div className="mt-4 grid grid-cols-2 gap-2.5">
            <button
              onClick={() => onUpdateConfig({ TaskbarCenterIcons: '1' })}
              style={
                centerIcons
                  ? {
                      backgroundColor: hexWithAlpha(primaryAccent, 0.18),
                      borderColor: primaryAccent,
                    }
                  : undefined
              }
              className="l2-pill flex items-center justify-center gap-2 rounded-xl py-2.5 text-xs font-semibold text-[#F2F4F8]"
            >
              <AlignCenter className="h-4 w-4" style={{ color: primaryAccent }} />
              <span>Merkezde Hizala (Dock)</span>
            </button>

            <button
              onClick={() => onUpdateConfig({ TaskbarCenterIcons: '0' })}
              style={
                !centerIcons
                  ? {
                      backgroundColor: hexWithAlpha(primaryAccent, 0.18),
                      borderColor: primaryAccent,
                    }
                  : undefined
              }
              className="l2-pill flex items-center justify-center gap-2 rounded-xl py-2.5 text-xs font-semibold text-[#F2F4F8]"
            >
              <AlignLeft className="h-4 w-4" style={{ color: primaryAccent }} />
              <span>Solda Hizala (Klasik)</span>
            </button>
          </div>

          {/* Sliders */}
          <div className="mt-4 space-y-3.5">
            <div>
              <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                <span>KAPSÜL KÖŞE YARIÇAPI (CORNER RADIUS)</span>
                <span>{cornerRadius} px</span>
              </div>
              <input
                type="range"
                min={0}
                max={28}
                value={cornerRadius}
                onChange={(e) =>
                  onUpdateConfig({ TaskbarCornerRadius: e.target.value })
                }
                className="cyber-slider w-full"
              />
            </div>

            <div>
              <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                <span>YÜZME BOŞLUĞU (DOCK MARGIN)</span>
                <span>{dockMargin} px</span>
              </div>
              <input
                type="range"
                min={0}
                max={24}
                value={dockMargin}
                onChange={(e) =>
                  onUpdateConfig({
                    TaskbarDockMargin: e.target.value,
                    TaskbarMarginPx: e.target.value,
                  })
                }
                className="cyber-slider w-full"
              />
            </div>
          </div>
        </div>
      </div>

      {/* MATERIAL & SPECULAR LINE 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Layers className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Materyal, Tint & Speküler Işık Çizgisi
              </h3>
            </div>
            <span className="font-mono text-xs text-[#A3A9B8]">{taskbarTint}</span>
          </div>

          <div className="mt-4 space-y-3.5">
            <div>
              <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                <span>DOCK TINT SAYDAMLIĞI (ALPHA)</span>
                <span>{taskbarAlpha} / 255</span>
              </div>
              <input
                type="range"
                min={0}
                max={255}
                value={taskbarAlpha}
                onChange={(e) =>
                  onUpdateConfig({ TaskbarTintAlpha: e.target.value })
                }
                className="cyber-slider w-full"
              />
            </div>

            {/* Toggles */}
            <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
              <button
                onClick={() =>
                  onUpdateConfig({ TaskbarSpecularRim: specularRim ? '0' : '1' })
                }
                className="l2-pill flex items-center justify-between rounded-xl p-3 text-left"
              >
                <div>
                  <div className="text-xs font-bold text-[#F2F4F8]">Üst Speküler Çizgi</div>
                  <div className="text-[10px] text-[#A3A9B8]">1px neon vurgu hattı</div>
                </div>
                <span
                  className="font-mono text-xs font-bold"
                  style={{ color: specularRim ? '#2EE6A6' : '#6B7183' }}
                >
                  {specularRim ? 'AÇIK' : 'KAPALI'}
                </span>
              </button>

              <button
                onClick={() =>
                  onUpdateConfig({ IslandExpandOnMedia: islandExpand ? '0' : '1' })
                }
                className="l2-pill flex items-center justify-between rounded-xl p-3 text-left"
              >
                <div>
                  <div className="text-xs font-bold text-[#F2F4F8]">Ada Medya Genişleme</div>
                  <div className="text-[10px] text-[#A3A9B8]">Müzik çalarken büyüt</div>
                </div>
                <span
                  className="font-mono text-xs font-bold"
                  style={{ color: islandExpand ? '#2EE6A6' : '#6B7183' }}
                >
                  {islandExpand ? 'AÇIK' : 'KAPALI'}
                </span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* SYSTEM TRAY POLISH 12x2 */}
      <div className="bento-card col-span-12 p-5">
        <div className="mb-3 flex items-center justify-between">
          <div>
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              Sistem Tepsisi (System Tray) & Saat Cilası
            </h3>
            <p className="text-xs text-[#A3A9B8]">
              Sağ alt tepsi kalabalığını sadeleştirin ve tabular mono saat görünümünü
              özelleştirin.
            </p>
          </div>
          <Sparkles className="h-4 w-4" style={{ color: primaryAccent }} />
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <button
            onClick={() =>
              onUpdateConfig({ TrayHideOverflow: hideOverflow ? '0' : '1' })
            }
            className="l2-pill flex items-center justify-between rounded-xl p-3.5 text-left"
          >
            <div>
              <div className="text-xs font-bold text-[#F2F4F8]">Taşma Okunu Gizle (^)</div>
              <div className="text-[11px] text-[#A3A9B8]">Minimal tepsi görünümü</div>
            </div>
            <span
              className="font-mono text-xs font-bold"
              style={{ color: hideOverflow ? '#2EE6A6' : '#6B7183' }}
            >
              {hideOverflow ? 'AKTİF' : 'PASİF'}
            </span>
          </button>

          <button
            onClick={() =>
              onUpdateConfig({ TrayCompactClock: compactClock ? '0' : '1' })
            }
            className="l2-pill flex items-center justify-between rounded-xl p-3.5 text-left"
          >
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4" style={{ color: primaryAccent }} />
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">Tek Satır Mono Saat</div>
                <div className="text-[11px] text-[#A3A9B8]">Tarihi gizle, yalnız HH:mm</div>
              </div>
            </div>
            <span
              className="font-mono text-xs font-bold"
              style={{ color: compactClock ? '#2EE6A6' : '#6B7183' }}
            >
              {compactClock ? 'HH:mm' : 'TAM'}
            </span>
          </button>

          <div className="l2-pill flex items-center justify-between rounded-xl p-3.5">
            <div className="flex items-center gap-2">
              <Wifi className="h-4 w-4 text-[#4DB8FF]" />
              <Volume2 className="h-4 w-4 text-[#2EE6A6]" />
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">Birleşik Ağ & Ses Hapı</div>
                <div className="text-[11px] text-[#A3A9B8]">L2 cam kapsül gruplaması</div>
              </div>
            </div>
            <span className="font-mono text-[10px] font-bold text-[#2EE6A6]">
              AKTİF
            </span>
          </div>

          <div className="l2-pill flex items-center justify-between rounded-xl p-3.5">
            <div className="flex items-center gap-2">
              <Bell className="h-4 w-4" style={{ color: primaryAccent }} />
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">Neon Bildirim Rozeti</div>
                <div className="text-[11px] text-[#A3A9B8]">Primary Accent nokta</div>
              </div>
            </div>
            <span
              style={{ backgroundColor: primaryAccent }}
              className="h-2.5 w-2.5 rounded-full shadow"
            />
          </div>
        </div>
      </div>
    </div>
  );
};
