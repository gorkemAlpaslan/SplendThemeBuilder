import type { HarmonyMode } from '../types/theme';

export interface RgbColor {
  r: number;
  g: number;
  b: number;
}

export interface HsvColor {
  h: number; // 0..360
  s: number; // 0..100
  v: number; // 0..100
}

export interface OklchColor {
  l: number; // 0..1
  c: number; // 0..0.4
  h: number; // 0..360
}

export function normalizeHex(hex: string, fallback = '#2EE6A6'): string {
  if (!hex) return fallback;
  const clean = hex.trim().replace(/^#/, '');
  if (/^[0-9a-fA-F]{3}$/.test(clean)) {
    return (
      '#' +
      clean
        .split('')
        .map((ch) => ch + ch)
        .join('')
        .toUpperCase()
    );
  }
  if (/^[0-9a-fA-F]{6}$/.test(clean)) {
    return '#' + clean.toUpperCase();
  }
  return fallback;
}

export function hexToRgb(hex: string): RgbColor {
  const clean = normalizeHex(hex).slice(1);
  const num = parseInt(clean, 16);
  return {
    r: (num >> 16) & 255,
    g: (num >> 8) & 255,
    b: num & 255,
  };
}

export function rgbToHex({ r, g, b }: RgbColor): string {
  const clamp = (n: number) => Math.max(0, Math.min(255, Math.round(n)));
  return (
    '#' +
    [clamp(r), clamp(g), clamp(b)]
      .map((n) => n.toString(16).padStart(2, '0'))
      .join('')
      .toUpperCase()
  );
}

export function rgbToHsv({ r, g, b }: RgbColor): HsvColor {
  const rn = r / 255;
  const gn = g / 255;
  const bn = b / 255;
  const max = Math.max(rn, gn, bn);
  const min = Math.min(rn, gn, bn);
  const d = max - min;

  let h = 0;
  if (d !== 0) {
    if (max === rn) {
      h = ((gn - bn) / d) % 6;
    } else if (max === gn) {
      h = (bn - rn) / d + 2;
    } else {
      h = (rn - gn) / d + 4;
    }
    h = Math.round(h * 60);
    if (h < 0) h += 360;
  }

  const s = max === 0 ? 0 : (d / max) * 100;
  const v = max * 100;
  return {
    h: Math.round(h),
    s: Math.round(s),
    v: Math.round(v),
  };
}

export function hsvToRgb({ h, s, v }: HsvColor): RgbColor {
  const hn = ((h % 360) + 360) % 360;
  const sn = Math.max(0, Math.min(100, s)) / 100;
  const vn = Math.max(0, Math.min(100, v)) / 100;

  const c = vn * sn;
  const x = c * (1 - Math.abs(((hn / 60) % 2) - 1));
  const m = vn - c;

  let r1 = 0;
  let g1 = 0;
  let b1 = 0;

  if (hn < 60) {
    r1 = c;
    g1 = x;
  } else if (hn < 120) {
    r1 = x;
    g1 = c;
  } else if (hn < 180) {
    g1 = c;
    b1 = x;
  } else if (hn < 240) {
    g1 = x;
    b1 = c;
  } else if (hn < 300) {
    r1 = x;
    b1 = c;
  } else {
    r1 = c;
    b1 = x;
  }

  return {
    r: Math.round((r1 + m) * 255),
    g: Math.round((g1 + m) * 255),
    b: Math.round((b1 + m) * 255),
  };
}

export function hexToHsv(hex: string): HsvColor {
  return rgbToHsv(hexToRgb(hex));
}

export function hsvToHex(hsv: HsvColor): string {
  return rgbToHex(hsvToRgb(hsv));
}

export function hexToOklchApprox(hex: string): OklchColor {
  const { r, g, b } = hexToRgb(hex);
  const hsv = rgbToHsv({ r, g, b });
  const lin = (v: number) => {
    const s = v / 255;
    return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
  };
  const lum = 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b);
  const l = Math.cbrt(lum);
  const c = (hsv.s / 100) * (hsv.v / 100) * 0.32;
  return {
    l: Number(l.toFixed(2)),
    c: Number(c.toFixed(3)),
    h: hsv.h,
  };
}

export function shiftHue(hex: string, deg: number, satScale = 1, valScale = 1): string {
  const hsv = hexToHsv(hex);
  return hsvToHex({
    h: (hsv.h + deg + 360) % 360,
    s: Math.max(10, Math.min(100, hsv.s * satScale)),
    v: Math.max(15, Math.min(100, hsv.v * valScale)),
  });
}

export function generateColorHarmony(baseHex: string, mode: HarmonyMode): string[] {
  const hsv = hexToHsv(baseHex);
  const make = (hOff: number, sMult = 1, vMult = 1) =>
    hsvToHex({
      h: (hsv.h + hOff + 360) % 360,
      s: Math.max(12, Math.min(100, Math.round(hsv.s * sMult))),
      v: Math.max(16, Math.min(100, Math.round(hsv.v * vMult))),
    });

  switch (mode) {
    case 'Mono':
      return [
        make(0, 1.0, 1.0),
        make(0, 0.75, 1.15),
        make(0, 0.5, 0.9),
        make(0, 0.9, 0.65),
        make(0, 0.85, 0.35),
      ];
    case 'Analog':
      return [
        make(0, 1.0, 1.0),
        make(-28, 0.92, 0.98),
        make(28, 0.92, 0.98),
        make(-52, 0.82, 0.82),
        make(52, 0.82, 0.82),
      ];
    case 'Complement':
      return [
        make(0, 1.0, 1.0),
        make(180, 0.95, 0.98),
        make(0, 0.65, 1.15),
        make(180, 0.7, 0.75),
        make(0, 0.9, 0.38),
      ];
    case 'Triad':
      return [
        make(0, 1.0, 1.0),
        make(120, 0.9, 0.95),
        make(240, 0.9, 0.95),
        make(60, 0.65, 0.85),
        make(0, 0.85, 0.36),
      ];
    case 'Split':
      return [
        make(0, 1.0, 1.0),
        make(150, 0.92, 0.95),
        make(210, 0.92, 0.95),
        make(0, 0.6, 1.12),
        make(180, 0.75, 0.42),
      ];
    case 'Tetrad':
    default:
      return [
        make(0, 1.0, 1.0),
        make(90, 0.9, 0.94),
        make(180, 0.95, 0.94),
        make(270, 0.9, 0.94),
        make(0, 0.8, 0.38),
      ];
  }
}

export function relativeLuminance(hex: string): number {
  const { r, g, b } = hexToRgb(hex);
  const toLinear = (c: number) => {
    const s = c / 255;
    return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * toLinear(r) + 0.7152 * toLinear(g) + 0.0722 * toLinear(b);
}

export function calculateContrastRatio(hexA: string, hexB: string): number {
  const lumA = relativeLuminance(hexA);
  const lumB = relativeLuminance(hexB);
  const brightest = Math.max(lumA, lumB);
  const darkest = Math.min(lumA, lumB);
  return Number(((brightest + 0.05) / (darkest + 0.05)).toFixed(2));
}

export function autoFixContrast(fgHex: string, bgHex: string, minRatio = 4.5): string {
  let current = normalizeHex(fgHex);
  if (calculateContrastRatio(current, bgHex) >= minRatio) {
    return current;
  }
  const bgLum = relativeLuminance(bgHex);
  const hsv = hexToHsv(current);
  for (let step = 1; step <= 25; step++) {
    const nextV = bgLum < 0.35 ? Math.min(100, hsv.v + step * 4) : Math.max(8, hsv.v - step * 4);
    const nextS = bgLum < 0.35 && nextV >= 96 ? Math.max(15, hsv.s - step * 3) : hsv.s;
    const candidate = hsvToHex({ h: hsv.h, s: nextS, v: nextV });
    if (calculateContrastRatio(candidate, bgHex) >= minRatio) {
      return candidate;
    }
    current = candidate;
  }
  return current;
}

export function kelvinToWarmthOverlay(kelvin: number): {
  hex: string;
  alpha: number;
  label: string;
} {
  const clamped = Math.max(2700, Math.min(6500, kelvin));
  const ratio = (6500 - clamped) / (6500 - 2700); // 0 at 6500K, 1 at 2700K
  const r = 255;
  const g = Math.round(215 - ratio * 75);
  const b = Math.round(170 - ratio * 135);
  const hex = rgbToHex({ r, g, b });
  const alpha = Number((ratio * 0.36).toFixed(3));
  let label = 'Nötr Gün Işığı (6500K)';
  if (clamped <= 3000) label = 'Derin Gece Kehribarı';
  else if (clamped <= 4000) label = 'Sıcak Gece Okuma';
  else if (clamped <= 5200) label = 'Yumuşak Stüdyo';
  return { hex, alpha, label };
}

export function hexWithAlpha(hex: string, alpha: number): string {
  const { r, g, b } = hexToRgb(hex);
  const a = Math.max(0, Math.min(1, alpha));
  return `rgba(${r}, ${g}, ${b}, ${a})`;
}
