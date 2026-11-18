import React, { useState } from 'react';
import {
  Check,
  Download,
  FolderOpen,
  History,
  PackageCheck,
  RotateCcw,
  Search,
  Sparkles,
  Star,
  Upload,
  Wand2,
} from 'lucide-react';
import { openFileDialog, saveFileDialog } from '../api/bridge';
import { LiveDesktopCanvas } from '../components/MiniDesktopPreviewDock';
import type {
  ChameleonResult,
  ConfigMap,
  TelemetryState,
  ThemePreset,
  ThemeSnapshot,
  ViewId,
} from '../types/theme';
import { hexWithAlpha } from '../utils/color';

interface View0GalleryProps {
  config: ConfigMap;
  presets: ThemePreset[];
  snapshots: ThemeSnapshot[];
  telemetry: TelemetryState;
  chameleonData: ChameleonResult | null;
  onApplyPreset: (id: string, applyWallpaper?: boolean) => void;
  onToggleFavorite: (id: string) => void;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  onRunChameleon: (imagePath?: string, autoApply?: boolean) => void;
  onRollbackSnapshot: (id: string) => void;
  onExportPackage: (path: string) => void;
  onImportPackage: (path: string) => void;
  onSelectView: (view: ViewId) => void;
  showToast: (msg: string) => void;
}

export const View0Gallery: React.FC<View0GalleryProps> = ({
  config,
  presets,
  snapshots,
  telemetry,
  chameleonData,
  onApplyPreset,
  onToggleFavorite,
  onUpdateConfig,
  onRunChameleon,
  onRollbackSnapshot,
  onExportPackage,
  onImportPackage,
  onSelectView,
  showToast,
}) => {
  const [filterTag, setFilterTag] = useState<string>('all');
  const [searchText, setSearchText] = useState<string>('');
  const [autoChameleon, setAutoChameleon] = useState<boolean>(true);
  const [applyWallpaperWithPreset, setApplyWallpaperWithPreset] =
    useState<boolean>(false);
  const [lastPackageStatus, setLastPackageStatus] = useState<string>(
    'Şema v3.0 hazır • SHA-256 doğrulamalı .doomtheme paketleyici',
  );

  const accentHex = config.PrimaryAccentHex || '#2EE6A6';
  const activeProfile = config.ActiveProfile || 'DoomEmerald';

  const defaultSwatches = chameleonData?.swatches?.length
    ? chameleonData.swatches
    : [
        accentHex,
        config.RimHighlightHex || '#6CA881',
        config.ShadowGlowHex || '#267347',
        '#7C5CFF',
        '#38BDF8',
        config.BodyTintHex || '#0D1F16',
      ];

  const filteredPresets = presets.filter((p) => {
    if (filterTag === 'fav' && !p.isFavorite) return false;
    if (
      filterTag !== 'all' &&
      filterTag !== 'fav' &&
      !p.tags.toLowerCase().includes(filterTag.toLowerCase())
    ) {
      return false;
    }
    if (searchText.trim()) {
      const q = searchText.toLowerCase();
      return (
        p.displayName.toLowerCase().includes(q) ||
        p.subtitle.toLowerCase().includes(q) ||
        p.tags.toLowerCase().includes(q)
      );
    }
    return true;
  });

  const handlePickWallpaper = async () => {
    const file = await openFileDialog('Duvar Kağıdı Seç (Chameleon OKLab)', [
      { name: 'Görseller', extensions: ['jpg', 'jpeg', 'png', 'webp', 'bmp'] },
    ]);
    if (file) {
      onRunChameleon(file, autoChameleon);
    }
  };

  const handleExport = async () => {
    const defaultName = `${activeProfile || 'CustomStudio'}.doomtheme`;
    const target = await saveFileDialog(
      '.doomtheme Paketi Dışa Aktar',
      defaultName,
      [{ name: 'Doom Theme Package', extensions: ['doomtheme', 'json'] }],
    );
    if (target) {
      onExportPackage(target);
      setLastPackageStatus(`Dışa aktarıldı: ${target.split('\\').pop()}`);
    }
  };

  const handleImport = async () => {
    const source = await openFileDialog('.doomtheme Paketi İçe Aktar', [
      { name: 'Doom Theme Package', extensions: ['doomtheme', 'json'] },
    ]);
    if (source) {
      onImportPackage(source);
      setLastPackageStatus(`İçe aktarıldı & doğrulandı: ${source.split('\\').pop()}`);
    }
  };

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* HERO 8x4: Live Desktop Canvas with Interactive Before/After Split Slider */}
      <div className="bento-card col-span-12 xl:col-span-8 p-5 flex flex-col justify-between">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="flex items-center gap-2">
              <span
                style={{
                  backgroundColor: hexWithAlpha(accentHex, 0.16),
                  color: accentHex,
                }}
                className="rounded-md px-2 py-0.5 font-mono text-[10px] font-bold uppercase"
              >
                CANLI MASAÜSTÜ SENTEZLEYİCİ
              </span>
              <h2 className="font-['Outfit'] text-base font-bold text-[#F2F4F8]">
                Gerçek Zamanlı DWM & HUD Tuvali
              </h2>
            </div>
            <p className="mt-0.5 text-xs text-[#A3A9B8]">
              Sürgüyü sağa/sola çekerek stok Windows 11 SDR ile Doom Control Center v3.0
              cam ve 360° neon aura farkını canlı inceleyin.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => onSelectView(1)}
              className="l2-pill rounded-xl px-3 py-1.5 text-xs font-medium text-[#F2F4F8] hover:border-white/25"
            >
              Renk Stüdyosu ›
            </button>
          </div>
        </div>

        <LiveDesktopCanvas
          config={config}
          telemetry={telemetry}
          isHero
          heightClass="aspect-[16/9] max-h-[320px]"
          onSelectView={onSelectView}
        />
      </div>

      {/* CHAMELEON ANALYZER 4x4: K-Means OKLab 6-Swatch Extractor */}
      <div className="bento-card col-span-12 xl:col-span-4 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Wand2 className="h-4 w-4" style={{ color: accentHex }} />
              <h3 className="font-['Outfit'] text-sm font-bold tracking-wide text-[#F2F4F8] uppercase">
                Chameleon OKLab Analizör
              </h3>
            </div>
            <span className="rounded-full border border-[#2EE6A6]/40 bg-[#2EE6A6]/15 px-2 py-0.5 font-mono text-[10px] font-semibold text-[#2EE6A6]">
              WCAG {chameleonData?.contrastScore ?? '14.8'}:1
            </span>
          </div>

          <p className="mt-1.5 text-xs text-[#A3A9B8]">
            Aktif duvar kağıdını OKLab algısal renk uzayında tarayarak 6 baskın renk
            kümesini çıkarır ve cam/aura dengesini otomatik kurar.
          </p>

          {/* 6 Swatches Grid */}
          <div className="mt-4">
            <div className="mb-1.5 flex items-center justify-between font-mono text-[10px] text-[#6B7183]">
              <span>ALGISAL OKLAB KÜMELERİ (6 SWATCH)</span>
              <span>TIKLA → VURGU ATA</span>
            </div>
            <div className="grid grid-cols-6 gap-2">
              {defaultSwatches.slice(0, 6).map((hex, i) => (
                <button
                  key={`${hex}-${i}`}
                  onClick={() => {
                    onUpdateConfig({ PrimaryAccentHex: hex });
                    showToast(`Primary Accent ${hex} olarak atandı`);
                  }}
                  title={`${hex} rengini Primary Accent olarak ata`}
                  className="group flex flex-col items-center gap-1"
                >
                  <div
                    style={{
                      backgroundColor: hex,
                      boxShadow: `0 4px 14px ${hexWithAlpha(hex, 0.45)}`,
                    }}
                    className="h-11 w-full rounded-xl border border-white/25 transition-transform duration-150 group-hover:scale-108"
                  />
                  <span className="font-mono text-[9.5px] text-[#A3A9B8] group-hover:text-white">
                    {hex}
                  </span>
                </button>
              ))}
            </div>
          </div>

          {/* Auto-Theme Toggle */}
          <div className="mt-4 flex items-center justify-between rounded-xl border border-white/8 bg-white/4 px-3 py-2.5">
            <div>
              <div className="text-xs font-semibold text-[#F2F4F8]">
                Otomatik Tema Sentezi (Auto-Theme)
              </div>
              <div className="text-[11px] text-[#6B7183]">
                Analiz sonrası cam ve rim tonlarını anında uygula
              </div>
            </div>
            <button
              onClick={() => setAutoChameleon((p) => !p)}
              style={{
                backgroundColor: autoChameleon ? accentHex : 'rgba(255,255,255,0.12)',
              }}
              className="relative h-5 w-9 rounded-full transition-colors"
            >
              <span
                className={`absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white transition-transform ${
                  autoChameleon ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="mt-4 flex flex-col gap-2">
          <button
            onClick={() => onRunChameleon(undefined, autoChameleon)}
            style={{
              backgroundColor: hexWithAlpha(accentHex, 0.2),
              borderColor: hexWithAlpha(accentHex, 0.5),
              color: '#F2F4F8',
            }}
            className="flex h-10 w-full items-center justify-center gap-2 rounded-xl border text-xs font-semibold transition-all hover:brightness-110 active:scale-98"
          >
            <Sparkles className="h-4 w-4" style={{ color: accentHex }} />
            <span>Mevcut Duvar Kağıdından Tema Üret</span>
          </button>

          <button
            onClick={() => void handlePickWallpaper()}
            className="l2-pill flex h-9 w-full items-center justify-center gap-2 rounded-xl text-xs font-medium text-[#A3A9B8] hover:text-white"
          >
            <FolderOpen className="h-3.5 w-3.5" />
            <span>Özel Görsel Seç ve Analiz Et…</span>
          </button>
        </div>
      </div>

      {/* GALLERY GRID 12xN: 3D-Hover Theme Preset Cards */}
      <div className="bento-card col-span-12 p-5">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="font-['Outfit'] text-base font-bold text-[#F2F4F8]">
              Hazır & Özel Tema Koleksiyonu ({filteredPresets.length})
            </h3>
            <p className="text-xs text-[#A3A9B8]">
              Kartların üzerine gelerek 3B speküler yansımayı görün; tek tıkla tüm DWM,
              Gezgin, Görev Çubuğu ve HUD katmanlarına uygulayın.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {/* Filter Tabs */}
            {[
              { id: 'all', label: 'Tümü' },
              { id: 'fav', label: '★ Favoriler' },
              { id: 'hdr', label: '10-bit HDR' },
              { id: 'cyber', label: 'Cyber / Neon' },
              { id: 'minimal', label: 'Minimal' },
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setFilterTag(tab.id)}
                style={
                  filterTag === tab.id
                    ? {
                        backgroundColor: hexWithAlpha(accentHex, 0.2),
                        borderColor: hexWithAlpha(accentHex, 0.5),
                        color: '#F2F4F8',
                      }
                    : undefined
                }
                className="l2-pill rounded-lg px-2.5 py-1 text-xs font-medium text-[#A3A9B8]"
              >
                {tab.label}
              </button>
            ))}

            {/* Wallpaper apply toggle */}
            <label className="l2-pill flex cursor-pointer items-center gap-2 rounded-lg px-2.5 py-1 text-xs text-[#A3A9B8]">
              <input
                type="checkbox"
                checked={applyWallpaperWithPreset}
                onChange={(e) => setApplyWallpaperWithPreset(e.target.checked)}
                className="accent-[#2EE6A6]"
              />
              <span>Duvar Kağıdını da Değiştir</span>
            </label>

            {/* Search Input */}
            <div className="l2-pill flex h-8 items-center gap-1.5 rounded-lg px-2.5">
              <Search className="h-3.5 w-3.5 text-[#6B7183]" />
              <input
                type="text"
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                placeholder="Tema veya etiket ara…"
                className="w-36 bg-transparent text-xs text-white placeholder-[#6B7183] focus:outline-none"
              />
            </div>
          </div>
        </div>

        {/* Cards Grid */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {filteredPresets.map((preset) => {
            const isSelected =
              activeProfile.toLowerCase() === preset.id.toLowerCase();
            const tags = preset.tags
              ? preset.tags.split(',').filter(Boolean).slice(0, 3)
              : ['v3.0', 'hdr'];

            return (
              <div
                key={preset.id}
                onClick={() => onApplyPreset(preset.id, applyWallpaperWithPreset)}
                style={
                  isSelected
                    ? {
                        borderColor: preset.primaryAccent,
                        boxShadow: `0 12px 30px rgba(0,0,0,0.6), 0 0 24px ${hexWithAlpha(
                          preset.primaryAccent,
                          0.28,
                        )}`,
                      }
                    : undefined
                }
                className="bento-card bento-card-interactive group cursor-pointer overflow-hidden p-3.5 flex flex-col justify-between"
              >
                {/* Mini 16:10 Visual Thumbnail */}
                <div>
                  <div
                    style={{
                      background: `radial-gradient(circle at 20% 20%, ${hexWithAlpha(
                        preset.primaryAccent,
                        0.42,
                      )}, transparent 65%), linear-gradient(145deg, ${
                        preset.uiBackground
                      } 0%, ${preset.explorerBodyTint} 65%, ${
                        preset.taskbarTint
                      } 100%)`,
                      borderColor: hexWithAlpha(preset.rimHighlight, 0.45),
                    }}
                    className="relative mb-3 aspect-[16/10] w-full overflow-hidden rounded-xl border p-2.5"
                  >
                    {/* Top badges */}
                    <div className="flex items-center justify-between">
                      <span
                        style={{
                          backgroundColor: hexWithAlpha(preset.primaryAccent, 0.22),
                          borderColor: hexWithAlpha(preset.primaryAccent, 0.5),
                          color: preset.primaryAccent,
                        }}
                        className="rounded border px-1.5 py-0.5 font-mono text-[9px] font-bold"
                      >
                        {isSelected ? 'AKTİF' : preset.isBuiltIn ? 'STÜDYO' : 'ÖZEL'}
                      </span>

                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          onToggleFavorite(preset.id);
                        }}
                        title="Favorilere Ekle / Çıkar"
                        className="rounded-md bg-black/40 p-1 text-[#A3A9B8] hover:text-[#FFB547]"
                      >
                        <Star
                          className="h-3.5 w-3.5"
                          fill={preset.isFavorite ? '#FFB547' : 'none'}
                          stroke={preset.isFavorite ? '#FFB547' : 'currentColor'}
                        />
                      </button>
                    </div>

                    {/* Mini simulated window inside preset card */}
                    <div
                      style={{
                        backgroundColor: hexWithAlpha(preset.uiCard, 0.82),
                        borderColor: preset.rimHighlight,
                        boxShadow: `0 6px 16px ${hexWithAlpha(
                          preset.explorerShadowGlow,
                          0.6,
                        )}`,
                      }}
                      className="mt-3 rounded-lg border p-2"
                    >
                      <div className="flex items-center justify-between">
                        <span
                          className="h-1.5 w-10 rounded-full"
                          style={{ backgroundColor: preset.primaryAccent }}
                        />
                        <span
                          className="h-2 w-2 rounded-full"
                          style={{ backgroundColor: preset.secondaryAccent }}
                        />
                      </div>
                      <div className="mt-1.5 h-1.5 w-16 rounded-full bg-white/15" />
                    </div>
                  </div>

                  {/* Preset Title & Subtitle */}
                  <div className="flex items-center justify-between gap-1">
                    <h4 className="truncate font-['Outfit'] text-sm font-bold text-[#F2F4F8]">
                      {preset.displayName}
                    </h4>
                    {isSelected && (
                      <Check
                        className="h-4 w-4 shrink-0"
                        style={{ color: preset.primaryAccent }}
                      />
                    )}
                  </div>
                  <p className="mt-0.5 line-clamp-2 text-[11px] leading-relaxed text-[#A3A9B8]">
                    {preset.subtitle}
                  </p>
                </div>

                {/* Bottom Color Swatch Bar & Tags */}
                <div className="mt-3 pt-2.5 border-t border-white/8 flex items-center justify-between">
                  <div className="flex items-center -space-x-1">
                    {[
                      preset.primaryAccent,
                      preset.rimHighlight,
                      preset.explorerShadowGlow,
                      preset.explorerBodyTint,
                      preset.secondaryAccent,
                    ].map((col, idx) => (
                      <span
                        key={idx}
                        style={{ backgroundColor: col }}
                        className="h-4 w-4 rounded-full border border-black/60 shadow-sm"
                      />
                    ))}
                  </div>

                  <div className="flex items-center gap-1">
                    {tags.slice(0, 2).map((t) => (
                      <span
                        key={t}
                        className="rounded bg-white/6 px-1.5 py-0.5 font-mono text-[9px] text-[#6B7183] uppercase"
                      >
                        #{t}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* PRESET I/O 6x2: .doomtheme Packager */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <PackageCheck className="h-4 w-4" style={{ color: accentHex }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                .doomtheme Paketleyici & Şema Doğrulayıcı
              </h3>
            </div>
            <span className="font-mono text-[10px] text-[#2EE6A6]">SCHEMA v3.0</span>
          </div>
          <p className="mt-1 text-xs text-[#A3A9B8]">
            Aktif renk paletini, DWM cam yoğunluğunu, 10-bit HDR korumasını ve HUD
            yerleşimini tek bir taşınabilir <code className="text-white">.doomtheme</code>{' '}
            paketinde dışa/içe aktarın.
          </p>
        </div>

        <div className="my-3 rounded-xl border border-dashed border-white/15 bg-white/[0.02] p-3.5 text-center">
          <div className="font-mono text-xs text-[#F2F4F8]">{lastPackageStatus}</div>
          <div className="mt-1 text-[11px] text-[#6B7183]">
            İçe aktarılan paketler otomatik olarak şema ve renk kontrast testinden geçer
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <button
            onClick={() => void handleExport()}
            style={{
              backgroundColor: hexWithAlpha(accentHex, 0.16),
              borderColor: hexWithAlpha(accentHex, 0.45),
            }}
            className="flex h-10 items-center justify-center gap-2 rounded-xl border text-xs font-semibold text-[#F2F4F8] hover:brightness-110"
          >
            <Download className="h-4 w-4" style={{ color: accentHex }} />
            <span>.doomtheme Dışa Aktar</span>
          </button>

          <button
            onClick={() => void handleImport()}
            className="l2-pill flex h-10 items-center justify-center gap-2 rounded-xl text-xs font-semibold text-[#F2F4F8] hover:border-white/25"
          >
            <Upload className="h-4 w-4 text-[#4DB8FF]" />
            <span>.doomtheme İçe Aktar</span>
          </button>
        </div>
      </div>

      {/* RECENT / HISTORY 6x2: 20-Step Snapshot Rollback Timeline */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div className="mb-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <History className="h-4 w-4" style={{ color: accentHex }} />
            <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
              20 Adımlı Snapshot Geri Yükleme Zaman Çizelgesi
            </h3>
          </div>
          <span className="font-mono text-[10px] text-[#A3A9B8]">
            {snapshots.length} / 20 KAYIT
          </span>
        </div>

        <div className="flex max-h-[155px] flex-col gap-2 overflow-y-auto pr-1">
          {snapshots.length === 0 ? (
            <div className="py-6 text-center text-xs text-[#6B7183]">
              Henüz kayıtlı snapshot bulunmuyor.
            </div>
          ) : (
            snapshots.map((snap, idx) => (
              <div
                key={snap.id || idx}
                className="l2-pill flex items-center justify-between rounded-xl px-3 py-2"
              >
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="flex items-center -space-x-1 shrink-0">
                    <span
                      style={{ backgroundColor: snap.primaryAccentHex }}
                      className="h-4 w-4 rounded-full border border-white/20"
                    />
                    <span
                      style={{ backgroundColor: snap.rimHighlightHex }}
                      className="h-4 w-4 rounded-full border border-white/20"
                    />
                    <span
                      style={{ backgroundColor: snap.shadowGlowHex }}
                      className="h-4 w-4 rounded-full border border-white/20"
                    />
                  </div>
                  <div className="min-w-0">
                    <div className="truncate text-xs font-semibold text-[#F2F4F8]">
                      {snap.label || snap.presetId}
                    </div>
                    <div className="font-mono text-[10px] text-[#6B7183]">
                      {snap.timestamp} • {snap.primaryAccentHex}
                    </div>
                  </div>
                </div>

                <button
                  onClick={() => onRollbackSnapshot(snap.id)}
                  className="ml-2 flex shrink-0 items-center gap-1 rounded-lg bg-white/8 px-2.5 py-1 text-[11px] font-medium text-[#F2F4F8] hover:bg-white/15"
                >
                  <RotateCcw className="h-3 w-3" style={{ color: accentHex }} />
                  <span>Yükle</span>
                </button>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};
