import React, { useState } from 'react';
import {
  AlertTriangle,
  CheckCircle2,
  FolderOpen,
  Layers,
  Play,
  ShieldCheck,
  Sparkles,
  Sun,
} from 'lucide-react';
import type { ConfigMap } from '../types/theme';
import { hexWithAlpha } from '../utils/color';

interface View2ExplorerHdrProps {
  config: ConfigMap;
  glassDaemonRunning: boolean;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  onToggleGlassDaemon: (enabled?: boolean) => void;
  showToast: (msg: string) => void;
}

const GLASS_MATERIALS = [
  {
    id: '0',
    title: 'HDR Net (Clear)',
    subtitle: 'Sıfır bulanıklık, 10-bit scRGB keskin kontrast ve saf zümrüt/neon tint',
    badge: 'ÖNERİLEN HDR',
  },
  {
    id: '1',
    title: 'DWM Acrylic',
    subtitle: '24px Gauss bulanıklığı + gürültü dokusu ile derin buzlu cam katmanı',
    badge: 'ACRYLIC',
  },
  {
    id: '2',
    title: 'SDR Blur',
    subtitle: 'Klasik Aero / DWM yarı saydam bulanıklık ve düşük GPU yükü',
    badge: 'DÜŞÜK GPU',
  },
  {
    id: '3',
    title: 'Windows 11 Mica',
    subtitle: 'Duvar kağıdı rengini pencere tabanına yediren yumuşak stüdyo yüzeyi',
    badge: 'MICA',
  },
];

export const View2ExplorerHdr: React.FC<View2ExplorerHdrProps> = ({
  config,
  glassDaemonRunning,
  onUpdateConfig,
  onToggleGlassDaemon,
  showToast,
}) => {
  const [focusedWindow, setFocusedWindow] = useState<'A' | 'B'>('A');
  const [simulating, setSimulating] = useState<boolean>(false);
  const [springStiffness, setSpringStiffness] = useState<number>(380);
  const [springDamping, setSpringDamping] = useState<number>(28);

  const primaryAccent = config.PrimaryAccentHex || '#2EE6A6';
  const rimHighlight = config.RimHighlightHex || '#6CA881';
  const shadowGlow = config.ShadowGlowHex || '#267347';
  const bodyTint = config.BodyTintHex || '#0D1F16';

  const blurMode = config.ExplorerBlurMode || '0';
  const explorerAlpha = Number(config.ExplorerAlpha ?? 238);
  const tintIntensity = Number(config.EmeraldTintIntensity ?? 72);
  const shadowIntensity = Number(config.EmeraldShadowIntensity ?? 185);
  const shadowSpread = Number(config.ShadowSpread ?? 28);

  const hdrGuard =
    config.HdrWhiteTextGuard !== '0' && config.HdrWhiteTextGuardEnabled !== '0';
  const hdrNits = Number(config.HdrNitsClamp ?? config.HdrMaxNitsClamp ?? 220);

  const showRim = config.ShowRimBorder !== '0';
  const shadowOnlyActive = config.ShadowOnlyActiveWindow === '1';

  const runFocusSimulation = () => {
    if (simulating) return;
    setSimulating(true);
    setFocusedWindow('B');
    setTimeout(() => setFocusedWindow('A'), 750);
    setTimeout(() => setFocusedWindow('B'), 1500);
    setTimeout(() => {
      setFocusedWindow('A');
      setSimulating(false);
    }, 2250);
  };

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* EXPLORER GLASS MATERIAL SELECTOR 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Layers className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Dosya Gezgini Cam & Materyal Motoru
              </h3>
            </div>

            <button
              onClick={() => onToggleGlassDaemon(!glassDaemonRunning)}
              style={{
                backgroundColor: glassDaemonRunning
                  ? hexWithAlpha('#2EE6A6', 0.16)
                  : hexWithAlpha('#FFB547', 0.16),
                borderColor: glassDaemonRunning ? '#2EE6A6' : '#FFB547',
                color: glassDaemonRunning ? '#2EE6A6' : '#FFB547',
              }}
              className="rounded-full border px-2.5 py-0.5 font-mono text-[10px] font-bold"
            >
              {glassDaemonRunning ? 'DAEMON AKTİF' : 'DAEMON DURDURULDU'}
            </button>
          </div>

          <p className="mt-1 text-xs text-[#A3A9B8]">
            Windows Gezgin pencerelerine enjekte edilen DWM kompozisyon materyalini ve
            yarı saydam zümrüt/neon gövde katmanını seçin.
          </p>

          {/* 4 Material Cards */}
          <div className="mt-4 grid grid-cols-2 gap-2.5">
            {GLASS_MATERIALS.map((mat) => {
              const isSelected = blurMode === mat.id;
              return (
                <button
                  key={mat.id}
                  onClick={() =>
                    onUpdateConfig({
                      ExplorerBlurMode: mat.id,
                      ExplorerGlassEnabled: '1',
                    })
                  }
                  style={
                    isSelected
                      ? {
                          backgroundColor: hexWithAlpha(primaryAccent, 0.16),
                          borderColor: primaryAccent,
                          boxShadow: `0 0 18px ${hexWithAlpha(primaryAccent, 0.2)}`,
                        }
                      : undefined
                  }
                  className="l2-pill flex flex-col justify-between rounded-xl p-3 text-left"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-['Outfit'] text-xs font-bold text-[#F2F4F8]">
                      {mat.title}
                    </span>
                    <span
                      style={isSelected ? { color: primaryAccent } : undefined}
                      className="rounded bg-white/8 px-1.5 py-0.5 font-mono text-[9px] text-[#A3A9B8]"
                    >
                      {mat.badge}
                    </span>
                  </div>
                  <p className="mt-1.5 text-[11px] leading-snug text-[#A3A9B8]">
                    {mat.subtitle}
                  </p>
                </button>
              );
            })}
          </div>
        </div>

        {/* Opacity & Tint Sliders */}
        <div className="mt-4 grid grid-cols-1 gap-3 pt-3 border-t border-white/8 sm:grid-cols-2">
          <div>
            <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
              <span>PENCERE ALFA (OPACITY)</span>
              <span>{explorerAlpha} / 255</span>
            </div>
            <input
              type="range"
              min={160}
              max={255}
              value={explorerAlpha}
              onChange={(e) => onUpdateConfig({ ExplorerAlpha: e.target.value })}
              className="cyber-slider w-full"
            />
          </div>

          <div>
            <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
              <span>CAM TINT YOĞUNLUĞU</span>
              <span>{tintIntensity} / 140</span>
            </div>
            <input
              type="range"
              min={0}
              max={140}
              value={tintIntensity}
              onChange={(e) =>
                onUpdateConfig({ EmeraldTintIntensity: e.target.value })
              }
              className="cyber-slider w-full"
            />
          </div>
        </div>
      </div>

      {/* 10-BIT scRGB WHITE-TEXT GUARD & HDR ENGINE 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Sun className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                10-bit scRGB HDR Beyaz Parlama Koruması
              </h3>
            </div>
            <span className="rounded-full border border-[#2EE6A6]/40 bg-[#2EE6A6]/15 px-2.5 py-0.5 font-mono text-[10px] font-bold text-[#2EE6A6]">
              10-BIT LUT READY
            </span>
          </div>

          <p className="mt-1 text-xs text-[#A3A9B8]">
            OLED ve Mini-LED HDR monitörlerde dosya adlarındaki aşırı parlak beyaz
            patlamasını (halo/bloom) algısal nits tavanıyla sınırlandırır.
          </p>

          {/* White-Text Guard Toggle */}
          <div className="mt-4 flex items-center justify-between rounded-xl border border-white/10 bg-white/[0.03] p-3">
            <div className="flex items-center gap-2.5">
              <ShieldCheck
                className="h-5 w-5"
                style={{ color: hdrGuard ? '#2EE6A6' : '#6B7183' }}
              />
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">
                  scRGB White-Text Luminance Guard
                </div>
                <div className="text-[11px] text-[#A3A9B8]">
                  Gezgin metinlerini {hdrNits} nits tavanında sabitler, göz yorgunluğunu önler
                </div>
              </div>
            </div>
            <button
              onClick={() => {
                const next = hdrGuard ? '0' : '1';
                onUpdateConfig({
                  HdrWhiteTextGuard: next,
                  HdrWhiteTextGuardEnabled: next,
                });
              }}
              style={{
                backgroundColor: hdrGuard ? primaryAccent : 'rgba(255,255,255,0.12)',
              }}
              className="relative h-5 w-9 shrink-0 rounded-full transition-colors"
            >
              <span
                className={`absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white transition-transform ${
                  hdrGuard ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          {/* Nits Slider (80..400 nits) */}
          <div className="mt-4">
            <div className="mb-1.5 flex items-center justify-between font-mono text-xs">
              <span className="text-[#A3A9B8]">MAKSİMUM METİN PARLAKLIĞI (NITS)</span>
              <span
                className="font-bold"
                style={{ color: hdrNits > 300 ? '#FFB547' : '#2EE6A6' }}
              >
                {hdrNits} nits
              </span>
            </div>
            <input
              type="range"
              min={80}
              max={400}
              step={5}
              value={hdrNits}
              onChange={(e) =>
                onUpdateConfig({
                  HdrNitsClamp: e.target.value,
                  HdrMaxNitsClamp: e.target.value,
                })
              }
              className="cyber-slider w-full"
            />
            <div className="mt-1 flex justify-between font-mono text-[10px] text-[#6B7183]">
              <span>80 nits (Sinema)</span>
              <span>203 nits (ITU-R BT.2408 Referans)</span>
              <span>400 nits (Tepe)</span>
            </div>
          </div>

          {/* Live Warning or Safe Status */}
          <div className="mt-3">
            {hdrNits > 280 ? (
              <div className="flex items-center justify-between rounded-xl border border-[#FFB547]/40 bg-[#FFB547]/10 px-3 py-2 text-xs text-[#FFB547]">
                <span className="flex items-center gap-2">
                  <AlertTriangle className="h-4 w-4 shrink-0" />
                  <span>
                    {hdrNits} nits karanlık odada OLED alt-piksel harelenmesine yol açabilir.
                  </span>
                </span>
                <button
                  onClick={() => {
                    onUpdateConfig({
                      HdrWhiteTextGuard: '1',
                      HdrWhiteTextGuardEnabled: '1',
                      HdrNitsClamp: '205',
                      HdrMaxNitsClamp: '205',
                    });
                    showToast('HDR metin tavanı 205 nits BT.2408 referansına sabitlendi');
                  }}
                  className="ml-2 shrink-0 rounded-lg bg-[#FFB547] px-2.5 py-1 font-mono text-[10px] font-bold text-black"
                >
                  205n Sabitle
                </button>
              </div>
            ) : (
              <div className="flex items-center justify-between rounded-xl border border-[#2EE6A6]/30 bg-[#2EE6A6]/10 px-3 py-2 text-xs text-[#2EE6A6]">
                <span className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 shrink-0" />
                  <span>
                    10-bit scRGB beyaz tavanı optimal aralıkta ({hdrNits} nits • Halosuz).
                  </span>
                </span>
                <span className="font-mono text-[10px] font-bold">BT.2408 OK</span>
              </div>
            )}
          </div>
        </div>

        {/* Gamut Preview Bar */}
        <div className="mt-4 pt-3 border-t border-white/8">
          <div className="mb-1 flex justify-between font-mono text-[10px] text-[#6B7183]">
            <span>REC.2020 / DCI-P3 10-BIT RENK GAMUTU ÖNİZLEMESİ</span>
            <span>DİTHER: 2% NOISE</span>
          </div>
          <div
            style={{
              background: `linear-gradient(90deg, ${bodyTint} 0%, ${shadowGlow} 35%, ${primaryAccent} 70%, ${rimHighlight} 100%)`,
            }}
            className="h-3 w-full rounded-full border border-white/20 shadow-inner"
          />
        </div>
      </div>

      {/* BORDER PHYSICS & DUAL-WINDOW FOCUS SIMULATOR 12x2 */}
      <div className="bento-card col-span-12 p-5">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              Çift Pencere Kenar Fiziği & Odak Geçiş Simülatörü (Border Physics)
            </h3>
            <p className="text-xs text-[#A3A9B8]">
              Aktif ve arka plan pencereleri arasındaki 1px speküler rim, 360° gölge
              yayılımı ve yay fiziğini canlı test edin.
            </p>
          </div>

          <button
            onClick={runFocusSimulation}
            style={{
              backgroundColor: hexWithAlpha(primaryAccent, 0.2),
              borderColor: hexWithAlpha(primaryAccent, 0.5),
            }}
            className="flex items-center gap-2 rounded-xl border px-3.5 py-1.5 text-xs font-semibold text-white hover:brightness-110"
          >
            <Play className="h-3.5 w-3.5" style={{ color: primaryAccent }} />
            <span>{simulating ? 'Simülasyon Çalışıyor…' : 'Odak Geçişini Simüle Et'}</span>
          </button>
        </div>

        <div className="grid grid-cols-1 gap-5 lg:grid-cols-12 items-center">
          {/* Physics Controls (5 cols) */}
          <div className="lg:col-span-5 flex flex-col gap-3">
            <div className="grid grid-cols-2 gap-2.5">
              <button
                onClick={() =>
                  onUpdateConfig({ ShowRimBorder: showRim ? '0' : '1' })
                }
                style={
                  showRim
                    ? {
                        borderColor: hexWithAlpha(primaryAccent, 0.5),
                        backgroundColor: hexWithAlpha(primaryAccent, 0.14),
                      }
                    : undefined
                }
                className="l2-pill flex items-center justify-between rounded-xl p-3 text-left"
              >
                <div>
                  <div className="text-xs font-bold text-[#F2F4F8]">1px Speküler Rim</div>
                  <div className="text-[10px] text-[#A3A9B8]">Üst kristal ışık çizgisi</div>
                </div>
                <span className="font-mono text-xs font-bold" style={{ color: primaryAccent }}>
                  {showRim ? 'AÇIK' : 'KAPALI'}
                </span>
              </button>

              <button
                onClick={() =>
                  onUpdateConfig({
                    ShadowOnlyActiveWindow: shadowOnlyActive ? '0' : '1',
                  })
                }
                style={
                  shadowOnlyActive
                    ? {
                        borderColor: hexWithAlpha(primaryAccent, 0.5),
                        backgroundColor: hexWithAlpha(primaryAccent, 0.14),
                      }
                    : undefined
                }
                className="l2-pill flex items-center justify-between rounded-xl p-3 text-left"
              >
                <div>
                  <div className="text-xs font-bold text-[#F2F4F8]">Yalnız Aktif Pencere</div>
                  <div className="text-[10px] text-[#A3A9B8]">Arka plan gölgesini kıs</div>
                </div>
                <span className="font-mono text-xs font-bold" style={{ color: primaryAccent }}>
                  {shadowOnlyActive ? 'EVET' : 'TÜMÜ'}
                </span>
              </button>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                  <span>YAY SERTLİĞİ (STIFFNESS)</span>
                  <span>k = {springStiffness}</span>
                </div>
                <input
                  type="range"
                  min={180}
                  max={600}
                  value={springStiffness}
                  onChange={(e) => setSpringStiffness(Number(e.target.value))}
                  className="cyber-slider w-full"
                />
              </div>

              <div>
                <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                  <span>SÖNÜMLEME (DAMPING)</span>
                  <span>d = {springDamping}</span>
                </div>
                <input
                  type="range"
                  min={12}
                  max={50}
                  value={springDamping}
                  onChange={(e) => setSpringDamping(Number(e.target.value))}
                  className="cyber-slider w-full"
                />
              </div>
            </div>
          </div>

          {/* Interactive Dual-Window Arena (7 cols) */}
          <div className="lg:col-span-7 relative grid grid-cols-2 gap-4 rounded-2xl border border-white/10 bg-[#06080E] p-4">
            {(['A', 'B'] as const).map((winId) => {
              const isFocused = focusedWindow === winId;
              const activeBorder = showRim
                ? isFocused
                  ? rimHighlight
                  : 'rgba(255,255,255,0.14)'
                : 'transparent';
              const activeGlow =
                isFocused || !shadowOnlyActive
                  ? `0 10px ${isFocused ? shadowSpread : Math.round(shadowSpread * 0.45)}px ${hexWithAlpha(
                      shadowGlow,
                      isFocused
                        ? shadowIntensity / 255
                        : (shadowIntensity / 255) * 0.35,
                    )}`
                  : 'none';

              return (
                <div
                  key={winId}
                  onClick={() => setFocusedWindow(winId)}
                  style={{
                    backgroundColor: hexWithAlpha(
                      bodyTint,
                      isFocused ? 0.85 : 0.48,
                    ),
                    borderColor: activeBorder,
                    boxShadow: activeGlow,
                    transform: isFocused ? 'scale(1.02) translateY(-2px)' : 'scale(0.98)',
                    transition: `all ${Math.round(90000 / springStiffness)}ms cubic-bezier(0.22, 1, 0.36, 1)`,
                  }}
                  className="cursor-pointer rounded-xl border p-3.5 select-none"
                >
                  <div className="flex items-center justify-between border-b border-white/10 pb-2">
                    <div className="flex items-center gap-1.5">
                      <FolderOpen
                        className="h-3.5 w-3.5"
                        style={{
                          color: isFocused ? primaryAccent : '#6B7183',
                        }}
                      />
                      <span className="font-mono text-xs font-bold text-[#F2F4F8]">
                        Pencere {winId}
                      </span>
                    </div>
                    <span
                      style={{
                        backgroundColor: isFocused
                          ? hexWithAlpha(primaryAccent, 0.2)
                          : 'rgba(255,255,255,0.06)',
                        color: isFocused ? primaryAccent : '#6B7183',
                      }}
                      className="rounded px-1.5 py-0.5 font-mono text-[9px] font-bold"
                    >
                      {isFocused ? 'ODAKTA (AKTİF)' : 'ARKA PLAN'}
                    </span>
                  </div>

                  <div className="mt-3 space-y-1.5">
                    <div
                      className="h-2 w-3/4 rounded-full"
                      style={{
                        backgroundColor: isFocused
                          ? hexWithAlpha(primaryAccent, 0.55)
                          : 'rgba(255,255,255,0.12)',
                      }}
                    />
                    <div className="h-2 w-1/2 rounded-full bg-white/10" />
                    <div className="pt-1 font-mono text-[10px] text-[#A3A9B8]">
                      {isFocused
                        ? `Rim: ${rimHighlight} • Spread: ${shadowSpread}px`
                        : 'Odaklamak için bu pencereye tıklayın'}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
};
