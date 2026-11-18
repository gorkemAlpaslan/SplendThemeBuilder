import React, { useEffect, useRef, useState } from 'react';
import {
  CheckCircle2,
  Lock,
  Palette,
  RefreshCw,
  ShieldAlert,
  Sparkles,
  Unlock,
  Wand2,
} from 'lucide-react';
import type { BezierControlPoints, ConfigMap, HarmonyMode } from '../types/theme';
import {
  autoFixContrast,
  calculateContrastRatio,
  generateColorHarmony,
  hexToHsv,
  hexToOklchApprox,
  hexToRgb,
  hexWithAlpha,
  hsvToHex,
  normalizeHex,
  rgbToHex,
} from '../utils/color';

interface View1ColorStudioProps {
  config: ConfigMap;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  showToast: (msg: string) => void;
}

type ColorRoleKey =
  | 'PrimaryAccentHex'
  | 'RimHighlightHex'
  | 'ShadowGlowHex'
  | 'BodyTintHex'
  | 'TaskbarTintHex';

const COLOR_ROLES: Array<{ key: ColorRoleKey; label: string; desc: string }> = [
  {
    key: 'PrimaryAccentHex',
    label: 'Primary Accent',
    desc: 'Ana neon vurgu, aktif kontroller ve birincil aura',
  },
  {
    key: 'RimHighlightHex',
    label: '1px Specular Rim',
    desc: 'Pencere ve kart üst kenar kristal ışık kırılması',
  },
  {
    key: 'ShadowGlowHex',
    label: '360° Shadow Glow',
    desc: 'Aktif pencere dış çevresindeki derin neon yayılımı',
  },
  {
    key: 'BodyTintHex',
    label: 'Explorer Cam Tint',
    desc: 'Dosya Gezgini yarı saydam cam gövde tonu',
  },
  {
    key: 'TaskbarTintHex',
    label: 'Taskbar / Dock Tint',
    desc: 'Görev çubuğu ve Floating Dock taban rengi',
  },
];

const HARMONY_MODES: HarmonyMode[] = [
  'Mono',
  'Analog',
  'Complement',
  'Triad',
  'Split',
  'Tetrad',
];

export const View1ColorStudio: React.FC<View1ColorStudioProps> = ({
  config,
  onUpdateConfig,
  showToast,
}) => {
  const [activeRole, setActiveRole] = useState<ColorRoleKey>('PrimaryAccentHex');
  const [harmonyMode, setHarmonyMode] = useState<HarmonyMode>('Split');
  const [lockedSwatches, setLockedSwatches] = useState<Record<number, string>>({});
  const [rerollSeed, setRerollSeed] = useState<number>(0);

  // 360° Glow Bézier Curve control points
  const [bezier, setBezier] = useState<BezierControlPoints>({
    p1x: 0.24,
    p1y: 0.88,
    p2x: 0.68,
    p2y: 0.96,
  });
  const [draggingNode, setDraggingNode] = useState<'p1' | 'p2' | null>(null);

  const wheelCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const svgCurveRef = useRef<SVGSVGElement | null>(null);

  const currentHex = normalizeHex(config[activeRole] || '#2EE6A6');
  const hsv = hexToHsv(currentHex);
  const rgb = hexToRgb(currentHex);
  const oklch = hexToOklchApprox(currentHex);

  const primaryAccent = normalizeHex(config.PrimaryAccentHex || '#2EE6A6');
  const rimHighlight = normalizeHex(config.RimHighlightHex || '#6CA881');
  const shadowGlow = normalizeHex(config.ShadowGlowHex || '#267347');
  const bodyTint = normalizeHex(config.BodyTintHex || '#0D1F16');

  // Render 2D HSV Color Wheel on HTML5 Canvas
  useEffect(() => {
    const canvas = wheelCanvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const size = canvas.width;
    const cx = size / 2;
    const cy = size / 2;
    const radius = size / 2 - 6;

    ctx.clearRect(0, 0, size, size);

    for (let angle = 0; angle < 360; angle += 1) {
      const startAngle = ((angle - 0.8) * Math.PI) / 180;
      const endAngle = ((angle + 0.8) * Math.PI) / 180;

      const grad = ctx.createRadialGradient(cx, cy, 0, cx, cy, radius);
      grad.addColorStop(0, hsvToHex({ h: angle, s: 0, v: hsv.v }));
      grad.addColorStop(1, hsvToHex({ h: angle, s: 100, v: hsv.v }));

      ctx.beginPath();
      ctx.moveTo(cx, cy);
      ctx.arc(cx, cy, radius, startAngle, endAngle);
      ctx.closePath();
      ctx.fillStyle = grad;
      ctx.fill();
    }

    // Outer specular ring
    ctx.beginPath();
    ctx.arc(cx, cy, radius, 0, Math.PI * 2);
    ctx.strokeStyle = 'rgba(255,255,255,0.22)';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Draw current color handle
    const rad = (hsv.h * Math.PI) / 180;
    const dist = (hsv.s / 100) * radius;
    const px = cx + Math.cos(rad) * dist;
    const py = cy + Math.sin(rad) * dist;

    ctx.beginPath();
    ctx.arc(px, py, 7, 0, Math.PI * 2);
    ctx.fillStyle = currentHex;
    ctx.fill();
    ctx.lineWidth = 2.5;
    ctx.strokeStyle = '#FFFFFF';
    ctx.stroke();
  }, [currentHex, hsv.h, hsv.s, hsv.v]);

  const handleWheelPick = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = wheelCanvasRef.current;
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * canvas.width;
    const y = ((e.clientY - rect.top) / rect.height) * canvas.height;
    const cx = canvas.width / 2;
    const cy = canvas.height / 2;
    const radius = canvas.width / 2 - 6;

    const dx = x - cx;
    const dy = y - cy;
    const dist = Math.min(radius, Math.sqrt(dx * dx + dy * dy));
    let angle = (Math.atan2(dy, dx) * 180) / Math.PI;
    if (angle < 0) angle += 360;

    const nextHex = hsvToHex({
      h: Math.round(angle),
      s: Math.round((dist / radius) * 100),
      v: hsv.v,
    });
    onUpdateConfig({ [activeRole]: nextHex });
  };

  // Harmony Swatches
  const harmonySwatches = React.useMemo(() => {
    const shiftedBase =
      rerollSeed === 0
        ? primaryAccent
        : hsvToHex({
            h: (hexToHsv(primaryAccent).h + rerollSeed * 17) % 360,
            s: hexToHsv(primaryAccent).s,
            v: hexToHsv(primaryAccent).v,
          });
    const raw = generateColorHarmony(shiftedBase, harmonyMode);
    return raw.map((hex, i) => lockedSwatches[i] || hex);
  }, [primaryAccent, harmonyMode, lockedSwatches, rerollSeed]);

  // Dragging SVG Bézier curve handles
  const handleSvgMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
    if (!draggingNode || !svgCurveRef.current) return;
    const rect = svgCurveRef.current.getBoundingClientRect();
    const nx = Math.max(0.05, Math.min(0.95, (e.clientX - rect.left) / rect.width));
    const ny = Math.max(0.05, Math.min(0.95, 1 - (e.clientY - rect.top) / rect.height));
    setBezier((prev) =>
      draggingNode === 'p1'
        ? { ...prev, p1x: Number(nx.toFixed(2)), p1y: Number(ny.toFixed(2)) }
        : { ...prev, p2x: Number(nx.toFixed(2)), p2y: Number(ny.toFixed(2)) },
    );
  };

  // WCAG Contrast Pairs
  const contrastPairs = [
    {
      name: 'Birincil Metin / Gezgin Camı',
      fg: '#F2F4F8',
      bg: bodyTint,
      min: 4.5,
      roleToFix: 'BodyTintHex' as ColorRoleKey,
    },
    {
      name: 'Primary Accent / Bento L1 Yüzey',
      fg: primaryAccent,
      bg: '#161820',
      min: 3.0,
      roleToFix: 'PrimaryAccentHex' as ColorRoleKey,
    },
    {
      name: 'Specular Rim / L0 Obsidyen Boşluk',
      fg: rimHighlight,
      bg: '#07080C',
      min: 3.0,
      roleToFix: 'RimHighlightHex' as ColorRoleKey,
    },
    {
      name: '360° Aura Glow / Arka Plan',
      fg: shadowGlow,
      bg: '#07080C',
      min: 2.2,
      roleToFix: 'ShadowGlowHex' as ColorRoleKey,
    },
  ];

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* HSV COLOR WHEEL 5x5 */}
      <div className="bento-card col-span-12 xl:col-span-5 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Palette className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                2D HSV & OKLCH Renk Tekeri
              </h3>
            </div>
            <span className="font-mono text-xs font-semibold" style={{ color: currentHex }}>
              {currentHex}
            </span>
          </div>

          {/* Role Selector Pills */}
          <div className="mt-3 grid grid-cols-2 gap-1.5 sm:grid-cols-3">
            {COLOR_ROLES.map((role) => {
              const isSel = activeRole === role.key;
              const col = config[role.key] || '#2EE6A6';
              return (
                <button
                  key={role.key}
                  onClick={() => setActiveRole(role.key)}
                  style={
                    isSel
                      ? {
                          backgroundColor: hexWithAlpha(col, 0.2),
                          borderColor: col,
                        }
                      : undefined
                  }
                  className="l2-pill flex items-center gap-2 rounded-xl px-2.5 py-1.5 text-left"
                >
                  <span
                    style={{ backgroundColor: col }}
                    className="h-3.5 w-3.5 shrink-0 rounded-full border border-white/25"
                  />
                  <span className="truncate text-[11px] font-medium text-[#F2F4F8]">
                    {role.label}
                  </span>
                </button>
              );
            })}
          </div>

          {/* Interactive Canvas Wheel + Sliders */}
          <div className="mt-4 flex flex-col items-center gap-4 sm:flex-row">
            <canvas
              ref={wheelCanvasRef}
              width={188}
              height={188}
              onClick={handleWheelPick}
              onMouseMove={(e) => {
                if (e.buttons === 1) handleWheelPick(e);
              }}
              className="cursor-crosshair rounded-full shadow-lg"
            />

            <div className="flex flex-1 flex-col gap-2.5 w-full">
              {/* Value / Brightness Slider */}
              <div>
                <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                  <span>PARLAKLIK (VALUE)</span>
                  <span>{hsv.v}%</span>
                </div>
                <input
                  type="range"
                  min={8}
                  max={100}
                  value={hsv.v}
                  onChange={(e) => {
                    const next = hsvToHex({
                      h: hsv.h,
                      s: hsv.s,
                      v: Number(e.target.value),
                    });
                    onUpdateConfig({ [activeRole]: next });
                  }}
                  className="cyber-slider w-full"
                />
              </div>

              {/* Saturation Slider */}
              <div>
                <div className="mb-1 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                  <span>DOYGUNLUK (SAT)</span>
                  <span>{hsv.s}%</span>
                </div>
                <input
                  type="range"
                  min={0}
                  max={100}
                  value={hsv.s}
                  onChange={(e) => {
                    const next = hsvToHex({
                      h: hsv.h,
                      s: Number(e.target.value),
                      v: hsv.v,
                    });
                    onUpdateConfig({ [activeRole]: next });
                  }}
                  className="cyber-slider w-full"
                />
              </div>

              {/* HEX Input */}
              <div className="flex items-center gap-2">
                <span className="font-mono text-[11px] text-[#A3A9B8]">HEX</span>
                <input
                  type="text"
                  value={currentHex}
                  onChange={(e) => {
                    const val = e.target.value;
                    if (/^#[0-9A-Fa-f]{6}$/.test(val)) {
                      onUpdateConfig({ [activeRole]: val.toUpperCase() });
                    }
                  }}
                  className="l2-pill w-full rounded-lg px-2.5 py-1 font-mono text-xs text-white focus:outline-none"
                />
                <input
                  type="color"
                  value={currentHex}
                  onChange={(e) =>
                    onUpdateConfig({ [activeRole]: e.target.value.toUpperCase() })
                  }
                  className="h-7 w-8 cursor-pointer rounded border-0 bg-transparent"
                />
              </div>

              {/* RGB Numeric Inputs */}
              <div className="grid grid-cols-3 gap-1.5">
                {(['r', 'g', 'b'] as const).map((ch) => (
                  <div key={ch} className="l2-pill rounded-lg px-2 py-1">
                    <div className="font-mono text-[9px] text-[#6B7183] uppercase">
                      {ch}
                    </div>
                    <input
                      type="number"
                      min={0}
                      max={255}
                      value={rgb[ch]}
                      onChange={(e) => {
                        const nextRgb = {
                          ...rgb,
                          [ch]: Math.max(0, Math.min(255, Number(e.target.value))),
                        };
                        onUpdateConfig({ [activeRole]: rgbToHex(nextRgb) });
                      }}
                      className="w-full bg-transparent font-mono text-xs text-white focus:outline-none"
                    />
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* OKLCH Readout Footer */}
        <div className="mt-4 flex items-center justify-between rounded-xl border border-white/8 bg-white/4 px-3 py-2 font-mono text-[11px]">
          <span className="text-[#A3A9B8]">ALGILANAN OKLCH</span>
          <span className="text-[#F2F4F8]">
            L: {oklch.l} • C: {oklch.c} • H: {oklch.h}°
          </span>
        </div>
      </div>

      {/* RIGHT COLUMN: HARMONY GENERATOR (7x3) + 360° GLOW BÉZIER CURVE (7x2) */}
      <div className="col-span-12 xl:col-span-7 flex flex-col gap-4">
        {/* COLOR HARMONY GENERATOR */}
        <div className="bento-card p-5">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Renk Uyumu Sentezleyici (Harmony Generator)
              </h3>
              <p className="text-xs text-[#A3A9B8]">
                Aktif vurgudan matematiksel renk armonileri üretin, beğendiklerinizi
                kilitleyin veya tek tıkla tüm katmanlara atayın.
              </p>
            </div>

            <div className="flex items-center gap-2">
              <button
                onClick={() => setRerollSeed((s) => s + 1)}
                className="l2-pill flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-medium text-[#F2F4F8]"
              >
                <RefreshCw className="h-3.5 w-3.5" style={{ color: primaryAccent }} />
                <span>Varyasyon Üret</span>
              </button>
              <button
                onClick={() => {
                  onUpdateConfig({
                    PrimaryAccentHex: harmonySwatches[0],
                    RimHighlightHex: harmonySwatches[1],
                    ShadowGlowHex: harmonySwatches[2],
                    TaskbarTintHex: harmonySwatches[4],
                  });
                  showToast('5 renk armonisi tüm tema katmanlarına atandı!');
                }}
                style={{
                  backgroundColor: hexWithAlpha(primaryAccent, 0.2),
                  borderColor: hexWithAlpha(primaryAccent, 0.5),
                }}
                className="flex items-center gap-1.5 rounded-lg border px-3 py-1 text-xs font-semibold text-white"
              >
                <Sparkles className="h-3.5 w-3.5" style={{ color: primaryAccent }} />
                <span>Tümünü Ata</span>
              </button>
            </div>
          </div>

          {/* Segmented Harmony Mode Bar */}
          <div className="mt-3 grid grid-cols-3 gap-1.5 sm:grid-cols-6">
            {HARMONY_MODES.map((m) => (
              <button
                key={m}
                onClick={() => setHarmonyMode(m)}
                style={
                  harmonyMode === m
                    ? {
                        backgroundColor: hexWithAlpha(primaryAccent, 0.22),
                        borderColor: primaryAccent,
                        color: '#FFFFFF',
                      }
                    : undefined
                }
                className="l2-pill rounded-lg py-1.5 font-mono text-xs font-medium text-[#A3A9B8]"
              >
                {m}
              </button>
            ))}
          </div>

          {/* 5 Harmony Swatch Cards with Lock & Ata Buttons */}
          <div className="mt-4 grid grid-cols-1 gap-2.5 sm:grid-cols-5">
            {harmonySwatches.map((hex, idx) => {
              const isLocked = Boolean(lockedSwatches[idx]);
              return (
                <div
                  key={idx}
                  className="l2-pill flex flex-col justify-between rounded-xl p-2.5"
                >
                  <div>
                    <div
                      style={{
                        backgroundColor: hex,
                        boxShadow: `0 6px 18px ${hexWithAlpha(hex, 0.4)}`,
                      }}
                      className="relative h-14 w-full rounded-lg border border-white/25"
                    >
                      <button
                        onClick={() =>
                          setLockedSwatches((prev) => {
                            const next = { ...prev };
                            if (next[idx]) delete next[idx];
                            else next[idx] = hex;
                            return next;
                          })
                        }
                        title={isLocked ? 'Kilidi Aç' : 'Bu Rengi Kilitle'}
                        className="absolute top-1.5 right-1.5 rounded bg-black/55 p-1 text-white/80 hover:text-white"
                      >
                        {isLocked ? (
                          <Lock className="h-3 w-3 text-[#FFB547]" />
                        ) : (
                          <Unlock className="h-3 w-3" />
                        )}
                      </button>
                    </div>
                    <div className="mt-2 flex items-center justify-between font-mono text-[11px]">
                      <span className="font-bold text-white">{hex}</span>
                      <span className="text-[9px] text-[#6B7183]">#{idx + 1}</span>
                    </div>
                  </div>

                  {/* Quick Assign ("Ata") Buttons */}
                  <div className="mt-2 grid grid-cols-3 gap-1">
                    <button
                      onClick={() => {
                        onUpdateConfig({ PrimaryAccentHex: hex });
                        showToast(`${hex} -> Primary Accent atandı`);
                      }}
                      className="rounded bg-white/8 py-1 font-mono text-[9px] text-[#E2E8F0] hover:bg-white/20"
                    >
                      Vurgu
                    </button>
                    <button
                      onClick={() => {
                        onUpdateConfig({ RimHighlightHex: hex });
                        showToast(`${hex} -> Specular Rim atandı`);
                      }}
                      className="rounded bg-white/8 py-1 font-mono text-[9px] text-[#E2E8F0] hover:bg-white/20"
                    >
                      Rim
                    </button>
                    <button
                      onClick={() => {
                        onUpdateConfig({ ShadowGlowHex: hex });
                        showToast(`${hex} -> 360° Glow atandı`);
                      }}
                      className="rounded bg-white/8 py-1 font-mono text-[9px] text-[#E2E8F0] hover:bg-white/20"
                    >
                      Gölge
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* 360° GLOW BÉZIER CURVE EDITOR */}
        <div className="bento-card p-5">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div>
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                360° Pencere Aura & Glow Bézier Eğrisi
              </h3>
              <p className="text-xs text-[#A3A9B8]">
                Pencere kenarlarından dışa doğru yayılan 4 katmanlı GDI+ / DWM neon
                ışık sönümleme (falloff) eğrisini şekillendirin.
              </p>
            </div>

            <div className="flex items-center gap-1.5">
              {[
                { name: 'Yumuşak Aura', p: { p1x: 0.25, p1y: 0.85, p2x: 0.65, p2y: 0.95 } },
                { name: 'Keskin Neon', p: { p1x: 0.12, p1y: 0.95, p2x: 0.35, p2y: 0.98 } },
                { name: 'Doğrusal', p: { p1x: 0.33, p1y: 0.33, p2x: 0.66, p2y: 0.66 } },
              ].map((preset) => (
                <button
                  key={preset.name}
                  onClick={() => setBezier(preset.p)}
                  className="l2-pill rounded-lg px-2 py-1 font-mono text-[10px] text-[#A3A9B8] hover:text-white"
                >
                  {preset.name}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-12 items-center">
            {/* Interactive SVG Cubic Bezier Graph */}
            <div className="md:col-span-7">
              <svg
                ref={svgCurveRef}
                viewBox="0 0 240 120"
                onMouseMove={handleSvgMouseMove}
                onMouseUp={() => setDraggingNode(null)}
                onMouseLeave={() => setDraggingNode(null)}
                className="h-32 w-full rounded-xl border border-white/10 bg-[#080A10] select-none"
              >
                {/* Grid lines */}
                {[30, 60, 90].map((y) => (
                  <line
                    key={y}
                    x1={0}
                    y1={y}
                    x2={240}
                    y2={y}
                    stroke="rgba(255,255,255,0.06)"
                    strokeDasharray="3 3"
                  />
                ))}
                {/* Handle Guide Lines */}
                <line
                  x1={10}
                  y1={110}
                  x2={bezier.p1x * 220 + 10}
                  y2={(1 - bezier.p1y) * 100 + 10}
                  stroke="rgba(255,255,255,0.25)"
                  strokeWidth="1"
                />
                <line
                  x1={230}
                  y1={10}
                  x2={bezier.p2x * 220 + 10}
                  y2={(1 - bezier.p2y) * 100 + 10}
                  stroke="rgba(255,255,255,0.25)"
                  strokeWidth="1"
                />
                {/* Cubic Bezier Path */}
                <path
                  d={`M 10 110 C ${bezier.p1x * 220 + 10} ${
                    (1 - bezier.p1y) * 100 + 10
                  }, ${bezier.p2x * 220 + 10} ${
                    (1 - bezier.p2y) * 100 + 10
                  }, 230 10`}
                  fill="none"
                  stroke={primaryAccent}
                  strokeWidth="3"
                />
                {/* P1 Control Point */}
                <circle
                  cx={bezier.p1x * 220 + 10}
                  cy={(1 - bezier.p1y) * 100 + 10}
                  r={6}
                  fill={primaryAccent}
                  stroke="#FFFFFF"
                  strokeWidth="2"
                  onMouseDown={() => setDraggingNode('p1')}
                  className="cursor-grab active:cursor-grabbing"
                />
                {/* P2 Control Point */}
                <circle
                  cx={bezier.p2x * 220 + 10}
                  cy={(1 - bezier.p2y) * 100 + 10}
                  r={6}
                  fill={rimHighlight}
                  stroke="#FFFFFF"
                  strokeWidth="2"
                  onMouseDown={() => setDraggingNode('p2')}
                  className="cursor-grab active:cursor-grabbing"
                />
              </svg>
            </div>

            {/* Animated 360° Ring & Spread/Bloom Sliders */}
            <div className="md:col-span-5 flex flex-col gap-3">
              <div className="flex items-center gap-3">
                <div
                  style={{
                    borderColor: rimHighlight,
                    boxShadow: `0 0 ${Number(config.ShadowSpread ?? 28)}px ${hexWithAlpha(
                      shadowGlow,
                      Number(config.EmeraldShadowIntensity ?? 185) / 255,
                    )}, inset 0 0 12px ${hexWithAlpha(primaryAccent, 0.35)}`,
                  }}
                  className="flex h-16 w-16 shrink-0 items-center justify-center rounded-2xl border-2 bg-[#0B0F17] font-mono text-[10px] font-bold text-white"
                >
                  360°
                </div>

                <div className="flex-1 flex flex-col gap-2">
                  <div>
                    <div className="mb-0.5 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                      <span>YAYILIM (SPREAD)</span>
                      <span>{config.ShadowSpread || 28} px</span>
                    </div>
                    <input
                      type="range"
                      min={8}
                      max={48}
                      value={Number(config.ShadowSpread ?? 28)}
                      onChange={(e) =>
                        onUpdateConfig({ ShadowSpread: e.target.value })
                      }
                      className="cyber-slider w-full"
                    />
                  </div>

                  <div>
                    <div className="mb-0.5 flex justify-between font-mono text-[10px] text-[#A3A9B8]">
                      <span>BLOOM YOĞUNLUĞU</span>
                      <span>{config.EmeraldShadowIntensity || 185}</span>
                    </div>
                    <input
                      type="range"
                      min={0}
                      max={255}
                      value={Number(config.EmeraldShadowIntensity ?? 185)}
                      onChange={(e) =>
                        onUpdateConfig({ EmeraldShadowIntensity: e.target.value })
                      }
                      className="cyber-slider w-full"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* WCAG 2.1 CONTRAST MATRIX 12x2 */}
      <div className="bento-card col-span-12 p-5">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              WCAG 2.1 Erişilebilirlik & Kontrast Matrisi
            </h3>
            <p className="text-xs text-[#A3A9B8]">
              Metin okunabilirliğini ve kenar görünürlüğünü gerçek zamanlı denetler;
              düşük kontrastlı kombinasyonları tek tıkla otomatik onarır.
            </p>
          </div>

          <button
            onClick={() => {
              const fixedAccent = autoFixContrast(primaryAccent, '#161820', 4.5);
              const fixedRim = autoFixContrast(rimHighlight, '#07080C', 4.5);
              onUpdateConfig({
                PrimaryAccentHex: fixedAccent,
                RimHighlightHex: fixedRim,
              });
              showToast('WCAG 2.1 AA/AAA kontrast optimizasyonu uygulandı!');
            }}
            style={{
              backgroundColor: hexWithAlpha(primaryAccent, 0.18),
              borderColor: hexWithAlpha(primaryAccent, 0.45),
            }}
            className="flex items-center gap-1.5 rounded-xl border px-3 py-1.5 text-xs font-semibold text-white hover:brightness-110"
          >
            <Wand2 className="h-3.5 w-3.5" style={{ color: primaryAccent }} />
            <span>Düşük Kontrastları Otomatik Onar</span>
          </button>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {contrastPairs.map((pair) => {
            const ratio = calculateContrastRatio(pair.fg, pair.bg);
            const passedAAA = ratio >= 7.0;
            const passedAA = ratio >= pair.min;

            return (
              <div
                key={pair.name}
                className="l2-pill flex flex-col justify-between rounded-xl p-3"
              >
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-[#F2F4F8]">
                    {pair.name}
                  </span>
                  {passedAA ? (
                    <span className="flex items-center gap-1 rounded bg-[#2EE6A6]/15 px-1.5 py-0.5 font-mono text-[10px] font-bold text-[#2EE6A6]">
                      <CheckCircle2 className="h-3 w-3" />
                      {passedAAA ? 'AAA' : 'AA'}
                    </span>
                  ) : (
                    <span className="flex items-center gap-1 rounded bg-[#FFB547]/20 px-1.5 py-0.5 font-mono text-[10px] font-bold text-[#FFB547]">
                      <ShieldAlert className="h-3 w-3" />
                      DÜŞÜK
                    </span>
                  )}
                </div>

                {/* Sample Preview Strip */}
                <div
                  style={{ backgroundColor: pair.bg, color: pair.fg }}
                  className="my-2.5 flex items-center justify-between rounded-lg border border-white/10 px-3 py-2 font-mono text-xs font-bold"
                >
                  <span>Örnek Aa 123</span>
                  <span>{ratio}:1</span>
                </div>

                <div className="flex items-center justify-between text-[10px] text-[#6B7183]">
                  <span>Hedef ≥ {pair.min}:1</span>
                  {!passedAA && (
                    <button
                      onClick={() => {
                        const fixed = autoFixContrast(pair.fg, pair.bg, pair.min);
                        onUpdateConfig({ [pair.roleToFix]: fixed });
                        showToast(`${pair.name} kontrastı onarıldı (${fixed})`);
                      }}
                      className="font-semibold text-[#FFB547] hover:underline"
                    >
                      Onar ›
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
