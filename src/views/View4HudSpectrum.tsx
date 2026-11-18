import React, { useEffect, useRef, useState } from 'react';
import {
  Activity,
  Clock,
  Cpu,
  HardDrive,
  Lock,
  Music,
  Pause,
  Play,
  RotateCcw,
  SkipForward,
  StickyNote,
  Timer,
  Unlock,
  Wifi,
} from 'lucide-react';
import type { ConfigMap, TelemetryState } from '../types/theme';
import { hexWithAlpha, shiftHue } from '../utils/color';

interface View4HudSpectrumProps {
  config: ConfigMap;
  telemetry: TelemetryState;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  onHudAction: (payload: Record<string, unknown>) => void;
  onPomodoroAction: (
    action: 'start_pause' | 'reset' | 'skip' | 'config',
    extra?: { focusMin?: number; shortMin?: number; longMin?: number },
  ) => void;
  showToast: (msg: string) => void;
}

interface WidgetCardDef {
  id: string;
  configKey: string;
  hudKey: string;
  title: string;
  subtitle: string;
  badge: string;
  icon: React.ComponentType<{ className?: string; style?: React.CSSProperties }>;
}

const HUD_WIDGETS: WidgetCardDef[] = [
  {
    id: 'clock',
    configKey: 'WidgetClock',
    hudKey: 'Clock',
    title: 'Saat & Milisaniye Tarih',
    subtitle: 'Tabular mono saat, gün ve aktif tema profili rozeti',
    badge: 'W1',
    icon: Clock,
  },
  {
    id: 'sys',
    configKey: 'WidgetSystemGpu',
    hudKey: 'SystemGpu',
    title: 'CPU / RAM / GPU Telemetri',
    subtitle: 'Gerçek zamanlı yük çubukları, sıcaklık ve 24-örnek sparkline',
    badge: 'W2',
    icon: Cpu,
  },
  {
    id: 'storage',
    configKey: 'WidgetStorage',
    hudKey: 'Storage',
    title: 'NVMe & Disk Sürücüleri',
    subtitle: 'Sürücü doluluk yüzdeleri ve okuma/yazma aktivite halkası',
    badge: 'W3',
    icon: HardDrive,
  },
  {
    id: 'network',
    configKey: 'WidgetNetwork',
    hudKey: 'Network',
    title: 'Ağ Trafiği & Gecikme',
    subtitle: 'Anlık İndirme/Yükleme hızı (Mbps) ve ping grafiği',
    badge: 'W4',
    icon: Wifi,
  },
  {
    id: 'media',
    configKey: 'WidgetMedia',
    hudKey: 'Media',
    title: 'Şimdi Çalıyor (SMTC)',
    subtitle: 'Kayan şarkı adı, sanatçı ve medya kontrol düğmeleri',
    badge: 'W5',
    icon: Music,
  },
  {
    id: 'notes',
    configKey: 'WidgetNotes',
    hudKey: 'Notes',
    title: 'Hızlı Notlar & Uptime',
    subtitle: 'Masaüstü yapışkan siber notlar ve sistem çalışma süresi',
    badge: 'W6',
    icon: StickyNote,
  },
  {
    id: 'spectrum',
    configKey: 'WidgetSpectrum',
    hudKey: 'Spectrum',
    title: 'Canlı Ses Spektrumu (FFT)',
    subtitle: 'WASAPI loopback 16/32/64 bantlı gerçek zamanlı ses görselleştirici',
    badge: 'W7 • YENİ',
    icon: Activity,
  },
  {
    id: 'pomodoro',
    configKey: 'WidgetPomodoro',
    hudKey: 'Pomodoro',
    title: 'Pomodoro Odak Halkası',
    subtitle: '25/5/15 dk dairesel odak sayacı, faz rozeti ve oturum noktaları',
    badge: 'W8 • YENİ',
    icon: Timer,
  },
];

export const View4HudSpectrum: React.FC<View4HudSpectrumProps> = ({
  config,
  telemetry,
  onUpdateConfig,
  onHudAction,
  onPomodoroAction,
  showToast,
}) => {
  const [selectedWidgetId, setSelectedWidgetId] = useState<string>('spectrum');
  const [focusMin, setFocusMin] = useState<number>(25);
  const [shortMin, setShortMin] = useState<number>(5);
  const [longMin, setLongMin] = useState<number>(15);

  const spectrumCanvasRef = useRef<HTMLCanvasElement | null>(null);

  const primaryAccent = config.PrimaryAccentHex || '#2EE6A6';
  const rimHighlight = config.RimHighlightHex || '#6CA881';
  const secondaryAccent = shiftHue(primaryAccent, 150);

  const hudEnabled = config.HudEnabled !== '0';
  const widgetsLocked = config.WidgetsLocked !== '0';
  const widgetScale = Number(config.WidgetScale ?? 100);
  const hudOpacity = Number(config.HudOpacity ?? 230);
  const widgetLayout = Number(config.WidgetLayout ?? 0);

  const spectrumStyle = Number(config.SpectrumStyle ?? 0); // 0=Bars, 1=Mirror, 2=Wave, 3=RadialRing
  const spectrumBands = Number(config.SpectrumBandCount ?? 32);
  const spectrumSmoothing = Number(config.SpectrumSmoothing ?? 65);

  // Live 60fps Audio Spectrum Canvas Renderer
  useEffect(() => {
    const canvas = spectrumCanvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animId = 0;
    let phase = 0;
    const bandValues = new Array(64).fill(0.2);
    const peakValues = new Array(64).fill(0.25);

    const render = () => {
      phase += 0.08;
      const w = canvas.width;
      const h = canvas.height;
      ctx.clearRect(0, 0, w, h);

      const count = Math.max(16, Math.min(64, spectrumBands));
      const smoothFactor = Math.max(0.1, Math.min(0.95, spectrumSmoothing / 100));
      const basePeak = Math.max(0.2, telemetry.audioPeak || 0.45);

      for (let i = 0; i < count; i++) {
        const target =
          basePeak *
          (0.45 +
            0.35 * Math.sin(phase + i * 0.35) +
            0.25 * Math.cos(phase * 1.7 - i * 0.2));
        bandValues[i] =
          bandValues[i] * smoothFactor + Math.max(0.06, Math.min(0.98, target)) * (1 - smoothFactor);
        if (bandValues[i] > peakValues[i]) {
          peakValues[i] = bandValues[i];
        } else {
          peakValues[i] = Math.max(0.06, peakValues[i] - 0.008);
        }
      }

      const grad = ctx.createLinearGradient(0, h, 0, 0);
      grad.addColorStop(0, primaryAccent);
      grad.addColorStop(1, secondaryAccent);

      if (spectrumStyle === 0) {
        // 0 = Bars + Peak Hold Dots
        const gap = 3;
        const barW = Math.max(2, (w - gap * (count - 1) - 16) / count);
        for (let i = 0; i < count; i++) {
          const x = 8 + i * (barW + gap);
          const barH = bandValues[i] * (h - 22);
          const y = h - 8 - barH;

          ctx.fillStyle = grad;
          ctx.beginPath();
          ctx.roundRect(x, y, barW, barH, 2);
          ctx.fill();

          // Peak dot
          const py = h - 10 - peakValues[i] * (h - 22);
          ctx.fillStyle = '#FFFFFF';
          ctx.fillRect(x, py, barW, 2);
        }
      } else if (spectrumStyle === 1) {
        // 1 = Mirror
        const gap = 3;
        const barW = Math.max(2, (w - gap * (count - 1) - 16) / count);
        const midY = h / 2;
        for (let i = 0; i < count; i++) {
          const x = 8 + i * (barW + gap);
          const halfH = bandValues[i] * (h * 0.42);
          ctx.fillStyle = grad;
          ctx.beginPath();
          ctx.roundRect(x, midY - halfH, barW, halfH * 2, 2);
          ctx.fill();
        }
      } else if (spectrumStyle === 2) {
        // 2 = Smooth Neon Wave
        ctx.beginPath();
        ctx.moveTo(8, h - 8);
        for (let i = 0; i < count; i++) {
          const x = 8 + (i / (count - 1)) * (w - 16);
          const y = h - 10 - bandValues[i] * (h - 24);
          ctx.lineTo(x, y);
        }
        ctx.strokeStyle = primaryAccent;
        ctx.lineWidth = 2.5;
        ctx.stroke();

        ctx.lineTo(w - 8, h - 8);
        ctx.closePath();
        ctx.fillStyle = hexWithAlpha(primaryAccent, 0.2);
        ctx.fill();
      } else {
        // 3 = Radial Ring
        const cx = w / 2;
        const cy = h / 2;
        const baseR = Math.min(w, h) * 0.24;
        for (let i = 0; i < count; i++) {
          const ang = (i / count) * Math.PI * 2;
          const len = bandValues[i] * (Math.min(w, h) * 0.22);
          const x1 = cx + Math.cos(ang) * baseR;
          const y1 = cy + Math.sin(ang) * baseR;
          const x2 = cx + Math.cos(ang) * (baseR + len);
          const y2 = cy + Math.sin(ang) * (baseR + len);

          ctx.beginPath();
          ctx.moveTo(x1, y1);
          ctx.lineTo(x2, y2);
          ctx.strokeStyle = i % 2 === 0 ? primaryAccent : secondaryAccent;
          ctx.lineWidth = 2.5;
          ctx.lineCap = 'round';
          ctx.stroke();
        }
      }

      animId = requestAnimationFrame(render);
    };

    animId = requestAnimationFrame(render);
    return () => cancelAnimationFrame(animId);
  }, [
    primaryAccent,
    secondaryAccent,
    spectrumBands,
    spectrumSmoothing,
    spectrumStyle,
    telemetry.audioPeak,
  ]);

  const formatTime = (sec: number) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  };

  const totalPomodoroSec = Math.max(1, focusMin * 60);
  const pomodoroProgress = Math.max(
    0,
    Math.min(1, 1 - telemetry.pomodoroRemainingSec / totalPomodoroSec),
  );
  const ringCircumference = 2 * Math.PI * 52;

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* 8-WIDGET INTERACTIVE DESKTOP BOARD 8x5 */}
      <div className="bento-card col-span-12 xl:col-span-8 p-5 flex flex-col justify-between">
        <div>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                8 Katmanlı 32-bit ARGB Masaüstü HUD Panosu
              </h3>
              <p className="text-xs text-[#A3A9B8]">
                Her bir masaüstü widget'ını tek tıkla etkinleştirin veya denetleyicide
                özelleştirin.
              </p>
            </div>

            {/* Layout Preset Selector */}
            <div className="flex items-center gap-1.5">
              {['Sağ Köşe', 'Sol Köşe', 'Üst Kokpit', 'Geniş Stüdyo'].map(
                (lbl, idx) => (
                  <button
                    key={lbl}
                    onClick={() => {
                      onUpdateConfig({ WidgetLayout: String(idx) });
                      onHudAction({ hudEnabled, scale: widgetScale, opacity: hudOpacity });
                      showToast(`HUD yerleşimi '${lbl}' olarak güncellendi`);
                    }}
                    style={
                      widgetLayout === idx
                        ? {
                            backgroundColor: hexWithAlpha(primaryAccent, 0.2),
                            borderColor: primaryAccent,
                            color: '#FFFFFF',
                          }
                        : undefined
                    }
                    className="l2-pill rounded-lg px-2.5 py-1 font-mono text-[10px] text-[#A3A9B8]"
                  >
                    {lbl}
                  </button>
                ),
              )}
            </div>
          </div>

          {/* 8 Widget Cards Grid */}
          <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {HUD_WIDGETS.map((w) => {
              const Icon = w.icon;
              const isEnabled = config[w.configKey] !== '0';
              const isSelected = selectedWidgetId === w.id;

              return (
                <div
                  key={w.id}
                  onClick={() => setSelectedWidgetId(w.id)}
                  style={
                    isSelected
                      ? {
                          borderColor: primaryAccent,
                          boxShadow: `0 0 20px ${hexWithAlpha(primaryAccent, 0.2)}`,
                        }
                      : undefined
                  }
                  className="l2-pill flex cursor-pointer flex-col justify-between rounded-xl p-3.5 transition-all hover:-translate-y-0.5"
                >
                  <div>
                    <div className="flex items-center justify-between">
                      <div
                        style={{
                          backgroundColor: isEnabled
                            ? hexWithAlpha(primaryAccent, 0.2)
                            : 'rgba(255,255,255,0.06)',
                          color: isEnabled ? primaryAccent : '#6B7183',
                        }}
                        className="flex h-8 w-8 items-center justify-center rounded-lg"
                      >
                        <Icon className="h-4 w-4" />
                      </div>

                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          const next = !isEnabled;
                          onUpdateConfig({ [w.configKey]: next ? '1' : '0' });
                          onHudAction({
                            widgets: { [w.hudKey]: next },
                          });
                        }}
                        style={{
                          backgroundColor: isEnabled
                            ? primaryAccent
                            : 'rgba(255,255,255,0.12)',
                        }}
                        className="relative h-5 w-9 rounded-full transition-colors"
                      >
                        <span
                          className={`absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white transition-transform ${
                            isEnabled ? 'translate-x-4' : 'translate-x-0'
                          }`}
                        />
                      </button>
                    </div>

                    <div className="mt-2.5 flex items-center justify-between">
                      <h4 className="font-['Outfit'] text-xs font-bold text-[#F2F4F8]">
                        {w.title}
                      </h4>
                    </div>
                    <p className="mt-1 text-[11px] leading-snug text-[#A3A9B8]">
                      {w.subtitle}
                    </p>
                  </div>

                  <div className="mt-3 flex items-center justify-between border-t border-white/8 pt-2 font-mono text-[10px]">
                    <span className="text-[#6B7183]">{w.badge}</span>
                    <span
                      style={{ color: isEnabled ? '#2EE6A6' : '#6B7183' }}
                      className="font-bold"
                    >
                      {isEnabled ? 'AKTİF' : 'GİZLİ'}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* WIDGET INSPECTOR 4x5 */}
      <div className="bento-card col-span-12 xl:col-span-4 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              HUD Denetleyici & Global Ayarlar
            </h3>
            <span
              className="rounded-full px-2 py-0.5 font-mono text-[10px] font-bold"
              style={{
                backgroundColor: hexWithAlpha(primaryAccent, 0.18),
                color: primaryAccent,
              }}
            >
              {HUD_WIDGETS.find((w) => w.id === selectedWidgetId)?.badge}
            </span>
          </div>

          <p className="mt-1 text-xs text-[#A3A9B8]">
            Tüm masaüstü widget katmanlarının ölçeğini, saydamlığını ve fare tıklama
            geçirgenliğini (Click-Through) yönetin.
          </p>

          {/* Master HUD & Lock Toggles */}
          <div className="mt-4 space-y-2.5">
            <button
              onClick={() => {
                const next = !hudEnabled;
                onUpdateConfig({ HudEnabled: next ? '1' : '0' });
                onHudAction({ hudEnabled: next });
              }}
              className="l2-pill flex w-full items-center justify-between rounded-xl p-3 text-left"
            >
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">
                  Masaüstü HUD Motoru
                </div>
                <div className="text-[11px] text-[#A3A9B8]">
                  8 katmanlı masaüstü bileşenlerini göster
                </div>
              </div>
              <span
                className="font-mono text-xs font-bold"
                style={{ color: hudEnabled ? '#2EE6A6' : '#FF4D5E' }}
              >
                {hudEnabled ? 'AÇIK' : 'KAPALI'}
              </span>
            </button>

            <button
              onClick={() => {
                const next = !widgetsLocked;
                onUpdateConfig({ WidgetsLocked: next ? '1' : '0' });
                onHudAction({ locked: next });
              }}
              className="l2-pill flex w-full items-center justify-between rounded-xl p-3 text-left"
            >
              <div className="flex items-center gap-2">
                {widgetsLocked ? (
                  <Lock className="h-4 w-4" style={{ color: primaryAccent }} />
                ) : (
                  <Unlock className="h-4 w-4 text-[#FFB547]" />
                )}
                <div>
                  <div className="text-xs font-bold text-[#F2F4F8]">
                    Konum Kilidi & Click-Through
                  </div>
                  <div className="text-[11px] text-[#A3A9B8]">
                    Kilidi açarak masaüstünde sürükleyebilirsiniz
                  </div>
                </div>
              </div>
              <span
                className="font-mono text-xs font-bold"
                style={{ color: widgetsLocked ? '#2EE6A6' : '#FFB547' }}
              >
                {widgetsLocked ? 'KİLİTLİ' : 'SERBEST'}
              </span>
            </button>
          </div>

          {/* Scale & Opacity Sliders */}
          <div className="mt-4 space-y-3.5">
            <div>
              <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                <span>WIDGET ÖLÇEĞİ (DPI SCALE)</span>
                <span>%{widgetScale}</span>
              </div>
              <input
                type="range"
                min={60}
                max={160}
                step={5}
                value={widgetScale}
                onChange={(e) => {
                  const val = Number(e.target.value);
                  onUpdateConfig({ WidgetScale: String(val) });
                  onHudAction({ scale: val });
                }}
                className="cyber-slider w-full"
              />
            </div>

            <div>
              <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                <span>CAM YÜZEY SAYDAMLIĞI (ALPHA)</span>
                <span>{hudOpacity} / 255</span>
              </div>
              <input
                type="range"
                min={60}
                max={255}
                value={hudOpacity}
                onChange={(e) => {
                  const val = Number(e.target.value);
                  onUpdateConfig({ HudOpacity: String(val) });
                  onHudAction({ opacity: val });
                }}
                className="cyber-slider w-full"
              />
            </div>
          </div>
        </div>

        <div className="mt-4 rounded-xl border border-white/8 bg-white/[0.03] p-3 font-mono text-[11px] text-[#A3A9B8]">
          Rim Vurgusu: <span className="text-white">{rimHighlight}</span> • Smart Focus:{' '}
          <span className="text-[#2EE6A6]">Otomatik FPS Korumalı</span>
        </div>
      </div>

      {/* AUDIO SPECTRUM STUDIO 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <Activity className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Canlı Ses Spektrumu & WASAPI Görselleştirici
              </h3>
            </div>
            <span className="rounded bg-white/8 px-2 py-0.5 font-mono text-[10px] text-[#2EE6A6]">
              WASAPI LOOPBACK • 60 FPS
            </span>
          </div>

          {/* 4 Spectrum Style Pills */}
          <div className="mt-3 grid grid-cols-4 gap-2">
            {[
              { id: 0, label: 'Bars (Çubuk)' },
              { id: 1, label: 'Mirror (Ayna)' },
              { id: 2, label: 'Wave (Dalga)' },
              { id: 3, label: 'Radial Ring' },
            ].map((st) => (
              <button
                key={st.id}
                onClick={() => {
                  onUpdateConfig({ SpectrumStyle: String(st.id) });
                  onHudAction({ spectrumStyle: st.id });
                }}
                style={
                  spectrumStyle === st.id
                    ? {
                        backgroundColor: hexWithAlpha(primaryAccent, 0.2),
                        borderColor: primaryAccent,
                        color: '#FFFFFF',
                      }
                    : undefined
                }
                className="l2-pill rounded-lg py-1.5 font-mono text-[11px] font-medium text-[#A3A9B8]"
              >
                {st.label}
              </button>
            ))}
          </div>

          {/* Live HTML5 Canvas Spectrum Preview */}
          <div className="mt-3 overflow-hidden rounded-xl border border-white/10 bg-[#06080D] p-2">
            <canvas
              ref={spectrumCanvasRef}
              width={480}
              height={118}
              className="h-[118px] w-full"
            />
          </div>
        </div>

        {/* Band Count & Smoothing Controls */}
        <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 items-center">
          <div>
            <div className="mb-1 font-mono text-[10px] text-[#A3A9B8]">
              FREKANS BANT SAYISI (FFT BANDS)
            </div>
            <div className="grid grid-cols-3 gap-1.5">
              {[16, 32, 64].map((b) => (
                <button
                  key={b}
                  onClick={() => {
                    onUpdateConfig({ SpectrumBandCount: String(b) });
                    onHudAction({ spectrumBands: b });
                  }}
                  style={
                    spectrumBands === b
                      ? {
                          backgroundColor: hexWithAlpha(primaryAccent, 0.22),
                          borderColor: primaryAccent,
                          color: '#FFFFFF',
                        }
                      : undefined
                  }
                  className="l2-pill rounded-lg py-1 font-mono text-xs font-bold text-[#A3A9B8]"
                >
                  {b} Bant
                </button>
              ))}
            </div>
          </div>

          <div>
            <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
              <span>YUMUŞATMA & GRAVITY DECAY</span>
              <span>%{spectrumSmoothing}</span>
            </div>
            <input
              type="range"
              min={10}
              max={95}
              value={spectrumSmoothing}
              onChange={(e) => {
                const val = Number(e.target.value);
                onUpdateConfig({ SpectrumSmoothing: String(val) });
                onHudAction({ spectrumSmoothing: val });
              }}
              className="cyber-slider w-full"
            />
          </div>
        </div>
      </div>

      {/* POMODORO FOCUS RING CONTROLLER 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Timer className="h-4 w-4" style={{ color: primaryAccent }} />
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              Pomodoro & Derin Odak (Deep Focus) Sayacı
            </h3>
          </div>
          <span
            style={{
              backgroundColor: hexWithAlpha(primaryAccent, 0.18),
              color: primaryAccent,
            }}
            className="rounded-full px-2.5 py-0.5 font-mono text-[10px] font-bold uppercase"
          >
            FAZ: {telemetry.pomodoroPhase === 'Focus' ? 'ODAK (FOCUS)' : 'MOLA (BREAK)'}
          </span>
        </div>

        <div className="my-3 grid grid-cols-1 gap-4 sm:grid-cols-12 items-center">
          {/* SVG Circular Ring Timer */}
          <div className="sm:col-span-5 flex flex-col items-center justify-center">
            <div className="relative flex h-32 w-32 items-center justify-center">
              <svg className="h-full w-full -rotate-90" viewBox="0 0 120 120">
                <circle
                  cx="60"
                  cy="60"
                  r="52"
                  fill="none"
                  stroke="rgba(255,255,255,0.08)"
                  strokeWidth="8"
                />
                <circle
                  cx="60"
                  cy="60"
                  r="52"
                  fill="none"
                  stroke={primaryAccent}
                  strokeWidth="8"
                  strokeLinecap="round"
                  strokeDasharray={ringCircumference}
                  strokeDashoffset={ringCircumference * (1 - pomodoroProgress)}
                  style={{
                    filter: `drop-shadow(0 0 8px ${primaryAccent})`,
                    transition: 'stroke-dashoffset 300ms ease',
                  }}
                />
              </svg>
              <div className="absolute flex flex-col items-center">
                <span className="font-mono text-2xl font-bold tracking-tight text-white">
                  {formatTime(telemetry.pomodoroRemainingSec)}
                </span>
                <div className="mt-1 flex items-center gap-1">
                  {[0, 1, 2, 3].map((dot) => (
                    <span
                      key={dot}
                      style={{
                        backgroundColor:
                          dot < telemetry.pomodoroCompleted
                            ? primaryAccent
                            : 'rgba(255,255,255,0.2)',
                      }}
                      className="h-2 w-2 rounded-full"
                    />
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Controls & Duration Inputs */}
          <div className="sm:col-span-7 flex flex-col gap-3">
            {/* Transport Buttons */}
            <div className="grid grid-cols-3 gap-2">
              <button
                onClick={() => onPomodoroAction('start_pause')}
                style={{
                  backgroundColor: primaryAccent,
                  color: '#07080C',
                }}
                className="flex h-10 items-center justify-center gap-1.5 rounded-xl text-xs font-bold transition-transform active:scale-95"
              >
                {telemetry.pomodoroRunning ? (
                  <>
                    <Pause className="h-4 w-4 fill-current" />
                    <span>Duraklat</span>
                  </>
                ) : (
                  <>
                    <Play className="h-4 w-4 fill-current" />
                    <span>Başlat</span>
                  </>
                )}
              </button>

              <button
                onClick={() => onPomodoroAction('reset', { focusMin })}
                className="l2-pill flex h-10 items-center justify-center gap-1.5 rounded-xl text-xs font-semibold text-[#F2F4F8]"
              >
                <RotateCcw className="h-3.5 w-3.5" />
                <span>Sıfırla</span>
              </button>

              <button
                onClick={() => onPomodoroAction('skip')}
                className="l2-pill flex h-10 items-center justify-center gap-1.5 rounded-xl text-xs font-semibold text-[#F2F4F8]"
              >
                <SkipForward className="h-3.5 w-3.5" />
                <span>Faz Atla</span>
              </button>
            </div>

            {/* Duration Config (Focus / Short / Long) */}
            <div className="grid grid-cols-3 gap-2">
              {[
                { label: 'Odak (dk)', val: focusMin, set: setFocusMin },
                { label: 'Kısa Mola', val: shortMin, set: setShortMin },
                { label: 'Uzun Mola', val: longMin, set: setLongMin },
              ].map((d) => (
                <div key={d.label} className="l2-pill rounded-xl p-2">
                  <div className="font-mono text-[9.5px] text-[#A3A9B8]">
                    {d.label}
                  </div>
                  <input
                    type="number"
                    min={1}
                    max={120}
                    value={d.val}
                    onChange={(e) => {
                      const v = Math.max(1, Math.min(120, Number(e.target.value)));
                      d.set(v);
                    }}
                    onBlur={() =>
                      onPomodoroAction('config', { focusMin, shortMin, longMin })
                    }
                    className="mt-0.5 w-full bg-transparent font-mono text-sm font-bold text-white focus:outline-none"
                  />
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
