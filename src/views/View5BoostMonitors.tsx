import React, { useState } from 'react';
import {
  Cpu,
  Eye,
  EyeOff,
  Flame,
  Gauge,
  Moon,
  Plus,
  Rocket,
  ShieldAlert,
  Sparkles,
  Sun,
  Trash2,
  Zap,
} from 'lucide-react';
import type {
  BoostProfileName,
  ConfigMap,
  PurgeRamResult,
  TelemetryState,
} from '../types/theme';
import { hexWithAlpha, kelvinToWarmthOverlay } from '../utils/color';

interface View5BoostMonitorsProps {
  config: ConfigMap;
  telemetry: TelemetryState;
  lastPurgeResult: PurgeRamResult | null;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  onSetBoostProfile: (profile: BoostProfileName) => void;
  onPurgeRam: () => void;
  onSetDisplayOverlay: (dimmerPercent: number, warmthKelvin: number) => void;
  onToggleDesktopIcons: () => void;
}

const BOOST_PROFILES: Array<{
  id: BoostProfileName;
  index: string;
  title: string;
  desc: string;
  color: string;
}> = [
  {
    id: 'Normal',
    index: '0',
    title: 'Normal Stüdyo',
    desc: '60Hz DWM cam, 360° aura ve tüm 8 HUD widget tam kapasite aktif',
    color: '#2EE6A6',
  },
  {
    id: 'Oyun',
    index: '1',
    title: 'Oyun Boost (Ultra Düşük Gecikme)',
    desc: 'Tam ekran oyunda HUD ve spektrumu duraklatır, maksimum FPS sağlar',
    color: '#FF4D5E',
  },
  {
    id: 'Render',
    index: '2',
    title: 'Render / AI (CUDA & VRAM Koruma)',
    desc: 'Blender / ComfyUI çalışırken GPU efektlerini uyku kipine alır ve RAM temizler',
    color: '#FFB547',
  },
  {
    id: 'Sessiz',
    index: '3',
    title: 'Sessiz & Gece Odak',
    desc: 'Düşük parlaklık, 3800K sıcak gece filtresi ve minimal telemetri',
    color: '#4DB8FF',
  },
];

export const View5BoostMonitors: React.FC<View5BoostMonitorsProps> = ({
  config,
  telemetry,
  lastPurgeResult,
  onUpdateConfig,
  onSetBoostProfile,
  onPurgeRam,
  onSetDisplayOverlay,
  onToggleDesktopIcons,
}) => {
  const [watchlist, setWatchlist] = useState<string[]>([
    'ComfyUI / python.exe',
    'blender.exe',
    'Cyberpunk2077.exe',
    ' DOOMx64vk.exe',
    'UnrealEditor.exe',
  ]);
  const [newProc, setNewProc] = useState<string>('');

  const primaryAccent = config.PrimaryAccentHex || '#2EE6A6';
  const boostIndex = config.BoostProfile || '0';
  const smartFocusEnabled = config.SmartFocusEnabled !== '0';
  const gpuThreshold = Number(config.SmartFocusGpuThreshold ?? 78);
  const resumeDelay = Number(config.SmartFocusResumeDelaySec ?? 3);

  const dimmerPct = Number(config.DisplayDimmerPercent ?? 0);
  const warmthKelvin = Number(config.DisplayWarmthKelvin ?? 6500);
  const warmthInfo = kelvinToWarmthOverlay(warmthKelvin);

  const handleAddProcess = () => {
    const clean = newProc.trim();
    if (!clean) return;
    if (!watchlist.includes(clean)) {
      setWatchlist((prev) => [...prev, clean]);
    }
    setNewProc('');
  };

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* BOOST PROFILES HERO BAR 12x1 */}
      <div className="bento-card col-span-12 p-5">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Rocket className="h-4 w-4" style={{ color: primaryAccent }} />
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              Performans & Boost Profil Seçici
            </h3>
          </div>
          <span className="font-mono text-xs text-[#A3A9B8]">
            1-Tıkla GPU/RAM Kaynak Optimizasyonu
          </span>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {BOOST_PROFILES.map((prof) => {
            const isActive =
              boostIndex === prof.index ||
              boostIndex.toLowerCase() === prof.id.toLowerCase();
            return (
              <button
                key={prof.id}
                onClick={() => onSetBoostProfile(prof.id)}
                style={
                  isActive
                    ? {
                        backgroundColor: hexWithAlpha(prof.color, 0.16),
                        borderColor: prof.color,
                        boxShadow: `0 8px 24px ${hexWithAlpha(prof.color, 0.22)}`,
                      }
                    : undefined
                }
                className="l2-pill flex flex-col justify-between rounded-xl p-3.5 text-left transition-all hover:-translate-y-0.5"
              >
                <div className="flex items-center justify-between">
                  <span className="font-['Outfit'] text-xs font-bold text-[#F2F4F8]">
                    {prof.title}
                  </span>
                  <Zap className="h-4 w-4" style={{ color: prof.color }} />
                </div>
                <p className="mt-1.5 text-[11px] leading-snug text-[#A3A9B8]">
                  {prof.desc}
                </p>
              </button>
            );
          })}
        </div>
      </div>

      {/* SMART FOCUS AUTO-PAUSE 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Cpu className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Smart Focus Otomatik Efekt Duraklatıcı
              </h3>
            </div>

            <div className="flex items-center gap-2">
              <span
                className="h-2 w-2 rounded-full"
                style={{
                  backgroundColor: telemetry.smartPaused ? '#FFB547' : '#2EE6A6',
                  boxShadow: `0 0 8px ${telemetry.smartPaused ? '#FFB547' : '#2EE6A6'}`,
                }}
              />
              <span className="font-mono text-[10px] font-bold text-[#F2F4F8]">
                {telemetry.smartPaused ? 'EFEKTLER UYKUDA' : 'AKTİF İZLENİYOR'}
              </span>
            </div>
          </div>

          <p className="mt-1 text-xs text-[#A3A9B8]">
            Tam ekran oyun, Blender veya ComfyUI algılandığında ya da GPU yükü eşiği
            aştığında masaüstü spektrum ve aura animasyonlarını sıfır yük kipine alır.
          </p>

          {/* Toggle */}
          <div className="mt-3 flex items-center justify-between rounded-xl border border-white/8 bg-white/[0.03] px-3 py-2.5">
            <span className="text-xs font-semibold text-[#F2F4F8]">
              Tam Ekran & Ağır GPU Süreçlerinde Otomatik Duraklat
            </span>
            <button
              onClick={() =>
                onUpdateConfig({
                  SmartFocusEnabled: smartFocusEnabled ? '0' : '1',
                })
              }
              style={{
                backgroundColor: smartFocusEnabled
                  ? primaryAccent
                  : 'rgba(255,255,255,0.12)',
              }}
              className="relative h-5 w-9 rounded-full transition-colors"
            >
              <span
                className={`absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white transition-transform ${
                  smartFocusEnabled ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          {/* Watchlist Process Chips */}
          <div className="mt-3.5">
            <div className="mb-1.5 font-mono text-[10px] text-[#6B7183] uppercase">
              İZLENEN SÜREÇ LİSTESİ (PROCESS WATCHLIST)
            </div>
            <div className="flex flex-wrap gap-1.5">
              {watchlist.map((proc) => (
                <span
                  key={proc}
                  className="l2-pill flex items-center gap-1.5 rounded-lg px-2.5 py-1 font-mono text-[11px] text-[#E2E8F0]"
                >
                  <span>{proc}</span>
                  <button
                    onClick={() =>
                      setWatchlist((prev) => prev.filter((p) => p !== proc))
                    }
                    className="text-[#6B7183] hover:text-[#FF4D5E]"
                  >
                    ×
                  </button>
                </span>
              ))}
            </div>

            <div className="mt-2 flex gap-2">
              <input
                type="text"
                value={newProc}
                onChange={(e) => setNewProc(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleAddProcess();
                }}
                placeholder="Örn: DaVinciResolve.exe…"
                className="l2-pill flex-1 rounded-lg px-2.5 py-1 font-mono text-xs text-white placeholder-[#6B7183] focus:outline-none"
              />
              <button
                onClick={handleAddProcess}
                className="l2-pill flex items-center gap-1 rounded-lg px-3 py-1 text-xs font-semibold text-white"
              >
                <Plus className="h-3.5 w-3.5" style={{ color: primaryAccent }} />
                <span>Ekle</span>
              </button>
            </div>
          </div>
        </div>

        {/* GPU Threshold & Resume Delay Sliders */}
        <div className="mt-4 grid grid-cols-2 gap-3 border-t border-white/8 pt-3">
          <div>
            <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
              <span>GPU YÜK EŞİĞİ</span>
              <span>%{gpuThreshold}</span>
            </div>
            <input
              type="range"
              min={50}
              max={95}
              value={gpuThreshold}
              onChange={(e) =>
                onUpdateConfig({ SmartFocusGpuThreshold: e.target.value })
              }
              className="cyber-slider w-full"
            />
          </div>

          <div>
            <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
              <span>DEVAM GECİKMESİ</span>
              <span>{resumeDelay} sn</span>
            </div>
            <input
              type="range"
              min={1}
              max={15}
              value={resumeDelay}
              onChange={(e) =>
                onUpdateConfig({ SmartFocusResumeDelaySec: e.target.value })
              }
              className="cyber-slider w-full"
            />
          </div>
        </div>
      </div>

      {/* RAM & WORKINGSET PURGER 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Gauge className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                RAM & WorkingSet Bellek Temizleyici
              </h3>
            </div>
            <span className="font-mono text-xs font-bold text-[#4DB8FF]">
              %{telemetry.ramUsagePercent} KULLANIMDA
            </span>
          </div>

          <p className="mt-1 text-xs text-[#A3A9B8]">
            Win32 <code className="text-white">EmptyWorkingSet</code> API çağrısıyla arka
            plandaki süreçlerin atıl önbelleklerini boşaltıp oyun ve AI modelleri için
            anında fiziksel RAM açar.
          </p>

          {/* Big Tabular Metrics */}
          <div className="mt-4 grid grid-cols-3 gap-3">
            <div className="l2-pill rounded-xl p-3">
              <div className="font-mono text-[10px] text-[#6B7183]">BOŞTA RAM</div>
              <div className="mt-1 font-mono text-2xl font-bold text-[#2EE6A6]">
                {telemetry.ramFreeGb.toFixed(1)} <span className="text-xs">GB</span>
              </div>
            </div>

            <div className="l2-pill rounded-xl p-3">
              <div className="font-mono text-[10px] text-[#6B7183]">KULLANILAN</div>
              <div className="mt-1 font-mono text-2xl font-bold text-[#F2F4F8]">
                {telemetry.ramUsedGb.toFixed(1)} <span className="text-xs">GB</span>
              </div>
            </div>

            <div className="l2-pill rounded-xl p-3">
              <div className="font-mono text-[10px] text-[#6B7183]">SON KAZANIM</div>
              <div
                className="mt-1 font-mono text-2xl font-bold"
                style={{ color: primaryAccent }}
              >
                {lastPurgeResult ? `${lastPurgeResult.freedMb}M` : 'Hazır'}
              </div>
            </div>
          </div>

          {/* RAM Usage Progress Bar */}
          <div className="mt-4">
            <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
              <span>FİZİKSEL BELLEK DAĞILIMI ({telemetry.ramTotalGb} GB TOPLAM)</span>
              <span>%{telemetry.ramUsagePercent}</span>
            </div>
            <div className="h-2.5 w-full overflow-hidden rounded-full bg-white/10">
              <div
                style={{
                  width: `${telemetry.ramUsagePercent}%`,
                  background: `linear-gradient(90deg, ${primaryAccent}, #4DB8FF)`,
                }}
                className="h-full transition-all duration-300"
              />
            </div>
          </div>
        </div>

        <button
          onClick={onPurgeRam}
          style={{
            backgroundColor: primaryAccent,
            color: '#07080C',
            boxShadow: `0 0 24px ${hexWithAlpha(primaryAccent, 0.35)}`,
          }}
          className="mt-4 flex h-11 w-full items-center justify-center gap-2 rounded-xl text-xs font-extrabold tracking-wide uppercase transition-all hover:brightness-110 active:scale-98"
        >
          <Trash2 className="h-4 w-4" />
          <span>Şimdi Temizle (Standby & WorkingSet Boşalt)</span>
        </button>
      </div>

      {/* DISPLAY DIMMER & KELVIN NIGHT-LIGHT CENTER 8x3 */}
      <div className="bento-card col-span-12 lg:col-span-8 p-5 flex flex-col justify-between">
        <div>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <Sun className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Monitör Karartma (Dimmer) & Kelvin Gece Işığı Merkezi
              </h3>
            </div>
            <span className="font-mono text-xs text-[#FFB547]">
              {warmthInfo.label}
            </span>
          </div>

          <p className="mt-1 text-xs text-[#A3A9B8]">
            Donanım parlaklığının altına inmek için tıklama geçirgen (click-through)
            katmanlı siyah filtre (%0–%80) ve sıcak kehribar gece okuma filtresi
            (2700K–6500K) uygulayın.
          </p>

          <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
            {/* Dimmer Slider */}
            <div className="l2-pill rounded-xl p-3.5">
              <div className="mb-2 flex items-center justify-between">
                <span className="flex items-center gap-1.5 text-xs font-bold text-[#F2F4F8]">
                  <Moon className="h-3.5 w-3.5 text-[#4DB8FF]" />
                  Ekran Karartma (Dimmer)
                </span>
                <span className="font-mono text-xs font-bold text-white">
                  %{dimmerPct}
                </span>
              </div>
              <input
                type="range"
                min={0}
                max={80}
                step={5}
                value={dimmerPct}
                onChange={(e) =>
                  onSetDisplayOverlay(Number(e.target.value), warmthKelvin)
                }
                className="cyber-slider w-full"
              />
              <div className="mt-1.5 flex justify-between font-mono text-[10px] text-[#6B7183]">
                <span>%0 (Kapalı)</span>
                <span>%40 (Gece)</span>
                <span>%80 (Maks OLED Koruma)</span>
              </div>
            </div>

            {/* Warmth Kelvin Slider */}
            <div className="l2-pill rounded-xl p-3.5">
              <div className="mb-2 flex items-center justify-between">
                <span className="flex items-center gap-1.5 text-xs font-bold text-[#F2F4F8]">
                  <Flame className="h-3.5 w-3.5 text-[#FFB547]" />
                  Renk Sıcaklığı (Kelvin)
                </span>
                <span className="font-mono text-xs font-bold text-[#FFB547]">
                  {warmthKelvin}K
                </span>
              </div>
              <input
                type="range"
                min={2700}
                max={6500}
                step={100}
                value={warmthKelvin}
                onChange={(e) =>
                  onSetDisplayOverlay(dimmerPct, Number(e.target.value))
                }
                className="cyber-slider w-full"
              />
              <div className="mt-1.5 flex justify-between font-mono text-[10px] text-[#6B7183]">
                <span>2700K (Kehribar)</span>
                <span>4500K (Stüdyo)</span>
                <span>6500K (Saf D65)</span>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Kelvin Presets */}
        <div className="mt-4 flex flex-wrap items-center gap-2">
          {[
            { label: 'Sıfırla (6500K • %0)', dim: 0, k: 6500 },
            { label: 'Stüdyo Okuma (4800K • %15)', dim: 15, k: 4800 },
            { label: 'Gece Kodlama (3600K • %30)', dim: 30, k: 3600 },
            { label: 'Derin Karanlık Oda (2800K • %55)', dim: 55, k: 2800 },
          ].map((p) => (
            <button
              key={p.label}
              onClick={() => onSetDisplayOverlay(p.dim, p.k)}
              className="l2-pill rounded-lg px-3 py-1.5 font-mono text-[11px] text-[#E2E8F0] hover:border-white/25"
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>

      {/* 1-CLICK DESKTOP ICONS TOGGLE 4x3 */}
      <div className="bento-card col-span-12 lg:col-span-4 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              Masaüstü Simgeleri & OLED Koruma
            </h3>
            <Sparkles className="h-4 w-4" style={{ color: primaryAccent }} />
          </div>
          <p className="mt-1 text-xs text-[#A3A9B8]">
            Temiz bir masaüstü tuvali ve OLED yanma (burn-in) koruması için masaüstü
            kısayol simgelerini tek tıkla gizleyin veya gösterin.
          </p>

          <div className="my-5 flex flex-col items-center justify-center rounded-2xl border border-white/10 bg-[#06080D] p-5 text-center">
            {telemetry.desktopIconsVisible ? (
              <Eye className="h-9 w-9" style={{ color: primaryAccent }} />
            ) : (
              <EyeOff className="h-9 w-9 text-[#FFB547]" />
            )}
            <div className="mt-2 font-['Outfit'] text-sm font-bold text-white">
              Masaüstü Simgeleri: {telemetry.desktopIconsVisible ? 'GÖRÜNÜR' : 'GİZLİ'}
            </div>
            <div className="mt-0.5 font-mono text-[11px] text-[#6B7183]">
              Kısayol: Alt + I veya Komut Paleti
            </div>
          </div>
        </div>

        <button
          onClick={onToggleDesktopIcons}
          style={{
            backgroundColor: hexWithAlpha(primaryAccent, 0.2),
            borderColor: hexWithAlpha(primaryAccent, 0.5),
          }}
          className="flex h-10 w-full items-center justify-center gap-2 rounded-xl border text-xs font-bold text-white hover:brightness-110"
        >
          {telemetry.desktopIconsVisible ? (
            <>
              <EyeOff className="h-4 w-4" />
              <span>Masaüstü İkonlarını Gizle</span>
            </>
          ) : (
            <>
              <Eye className="h-4 w-4" />
              <span>Masaüstü İkonlarını Göster</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
};
