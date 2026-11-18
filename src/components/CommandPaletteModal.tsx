import React, { useEffect, useMemo, useRef, useState } from 'react';
import {
  Activity,
  CornerDownLeft,
  Layers,
  Palette,
  Rocket,
  Search,
  Sparkles,
  Terminal,
  X,
} from 'lucide-react';
import type { ThemePreset, ViewId } from '../types/theme';
import { hexWithAlpha } from '../utils/color';

export interface PaletteActionItem {
  id: string;
  kind: 'command' | 'theme' | 'widget' | 'view';
  title: string;
  subtitle: string;
  shortcut?: string;
  accentColor?: string;
  run: () => void;
  preview?: () => void;
}

interface CommandPaletteModalProps {
  open: boolean;
  onClose: () => void;
  presets: ThemePreset[];
  accentHex: string;
  onSelectView: (v: ViewId) => void;
  onApplyPreset: (id: string) => void;
  onApplyConfig: () => void;
  onUndo: () => void;
  onPurgeRam: () => void;
  onToggleIcons: () => void;
  onToggleGlass: () => void;
  onQuickChameleon: () => void;
  onToggleWidget: (widgetKey: string) => void;
}

export const CommandPaletteModal: React.FC<CommandPaletteModalProps> = ({
  open,
  onClose,
  presets,
  accentHex,
  onSelectView,
  onApplyPreset,
  onApplyConfig,
  onUndo,
  onPurgeRam,
  onToggleIcons,
  onToggleGlass,
  onQuickChameleon,
  onToggleWidget,
}) => {
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      setQuery('');
      setSelectedIndex(0);
      setTimeout(() => inputRef.current?.focus(), 20);
    }
  }, [open]);

  const allItems = useMemo<PaletteActionItem[]>(() => {
    const commandItems: PaletteActionItem[] = [
      {
        id: 'cmd_apply',
        kind: 'command',
        title: '> Taslak Değişiklikleri Uygula (Apply)',
        subtitle: 'Aktif renk, cam, görev çubuğu ve HUD ayarlarını sisteme yazar',
        shortcut: 'Ctrl+S',
        run: onApplyConfig,
      },
      {
        id: 'cmd_chameleon',
        kind: 'command',
        title: '> Chameleon OKLab: Duvar Kağıdından Tema Üret',
        subtitle: 'Mevcut masaüstü duvar kağıdından 6 baskın renk çıkarır',
        shortcut: 'Ctrl+K',
        run: onQuickChameleon,
      },
      {
        id: 'cmd_purge_ram',
        kind: 'command',
        title: '> RAM & WorkingSet Temizle (Purge Standby)',
        subtitle: 'Boştaki işlem çalışma kümelerini boşaltıp RAM kazandırır',
        shortcut: 'Ctrl+Shift+P',
        run: onPurgeRam,
      },
      {
        id: 'cmd_rollback',
        kind: 'command',
        title: '> Son Snapshot Yedeğine Geri Dön (Undo)',
        subtitle: '20 adımlı geçmişten bir önceki tema durumunu yükler',
        shortcut: 'Ctrl+Z',
        run: onUndo,
      },
      {
        id: 'cmd_toggle_icons',
        kind: 'command',
        title: '> Masaüstü İkonlarını Gizle / Göster',
        subtitle: '1-tıkla masaüstü simge görünürlüğünü değiştirir',
        shortcut: 'Alt+I',
        run: onToggleIcons,
      },
      {
        id: 'cmd_toggle_glass',
        kind: 'command',
        title: '> DWM Cam & Gölge Daemon Durumunu Değiştir',
        subtitle: 'DoomGlassDaemon arka plan efekt servisini başlatır/durdurur',
        shortcut: 'Alt+G',
        run: onToggleGlass,
      },
    ];

    const viewItems: PaletteActionItem[] = [
      {
        id: 'view_0',
        kind: 'view',
        title: 'V0 — Tema Galerisi & Canlı Tuval',
        subtitle: 'Önce/Sonra tuvali, Chameleon OKLab ve hazır temalar',
        shortcut: 'Alt+1',
        run: () => onSelectView(0),
      },
      {
        id: 'view_1',
        kind: 'view',
        title: 'V1 — Renk & Işık Stüdyosu',
        subtitle: '2D HSV renk tekeri, renk uyumu, 360° Bézier eğrisi, WCAG matrisi',
        shortcut: 'Alt+2',
        run: () => onSelectView(1),
      },
      {
        id: 'view_2',
        kind: 'view',
        title: 'V2 — Gezgin & 10-bit HDR Motoru',
        subtitle: 'Explorer cam materyali, scRGB beyaz metin koruması ve kenar fiziği',
        shortcut: 'Alt+3',
        run: () => onSelectView(2),
      },
      {
        id: 'view_3',
        kind: 'view',
        title: 'V3 — Görev Çubuğu & Dock',
        subtitle: 'Floating Dock, Dynamic Island, Segmented ve sistem tepsisi',
        shortcut: 'Alt+4',
        run: () => onSelectView(3),
      },
      {
        id: 'view_4',
        kind: 'view',
        title: 'V4 — Masaüstü HUD & Ses Spektrumu',
        subtitle: '8 masaüstü widget, canlı WASAPI ses spektrumu ve Pomodoro',
        shortcut: 'Alt+5',
        run: () => onSelectView(4),
      },
      {
        id: 'view_5',
        kind: 'view',
        title: 'V5 — Oyun/Render Boost & Monitör Merkezi',
        subtitle: 'Smart Focus, RAM temizleyici, Karartma ve Kelvin gece ışığı',
        shortcut: 'Alt+6',
        run: () => onSelectView(5),
      },
      {
        id: 'view_6',
        kind: 'view',
        title: 'V6 — Sistem, Kısayollar & Yedekleme',
        subtitle: 'Başlangıç, kısayol atamaları, snapshot yedekleme ve IPC konsolu',
        shortcut: 'Alt+7',
        run: () => onSelectView(6),
      },
    ];

    const themeItems: PaletteActionItem[] = presets.map((p) => ({
      id: `theme_${p.id}`,
      kind: 'theme',
      title: `# ${p.displayName}`,
      subtitle: p.subtitle || p.tags,
      accentColor: p.primaryAccent,
      run: () => onApplyPreset(p.id),
      preview: () => onApplyPreset(p.id),
    }));

    const widgetItems: PaletteActionItem[] = [
      { key: 'WidgetClock', name: 'Saat & Tarih HUD' },
      { key: 'WidgetSystemGpu', name: 'CPU / RAM / GPU Telemetri HUD' },
      { key: 'WidgetStorage', name: 'NVMe Depolama Sürücüleri HUD' },
      { key: 'WidgetNetwork', name: 'Ağ Trafiği & Gecikme HUD' },
      { key: 'WidgetMedia', name: 'Şimdi Çalıyor (SMTC Medya) HUD' },
      { key: 'WidgetNotes', name: 'Hızlı Notlar & Uptime HUD' },
      { key: 'WidgetSpectrum', name: 'Canlı Ses Spektrumu HUD (Widget 7)' },
      { key: 'WidgetPomodoro', name: 'Pomodoro Odak Halkası HUD (Widget 8)' },
    ].map((w) => ({
      id: `widget_${w.key}`,
      kind: 'widget',
      title: `@ ${w.name} Aç/Kapat`,
      subtitle: 'Masaüstü katmanlı ARGB HUD widget görünürlüğünü değiştirir',
      run: () => onToggleWidget(w.key),
    }));

    return [...commandItems, ...viewItems, ...themeItems, ...widgetItems];
  }, [
    presets,
    onApplyConfig,
    onQuickChameleon,
    onPurgeRam,
    onUndo,
    onToggleIcons,
    onToggleGlass,
    onSelectView,
    onApplyPreset,
    onToggleWidget,
  ]);

  const filteredItems = useMemo(() => {
    const raw = query.trim().toLowerCase();
    if (!raw) return allItems.slice(0, 14);

    if (raw.startsWith('>')) {
      const sub = raw.slice(1).trim();
      return allItems.filter(
        (i) =>
          (i.kind === 'command' || i.kind === 'view') &&
          (i.title.toLowerCase().includes(sub) ||
            i.subtitle.toLowerCase().includes(sub)),
      );
    }
    if (raw.startsWith('#')) {
      const sub = raw.slice(1).trim();
      return allItems.filter(
        (i) =>
          i.kind === 'theme' &&
          (i.title.toLowerCase().includes(sub) ||
            i.subtitle.toLowerCase().includes(sub)),
      );
    }
    if (raw.startsWith('@')) {
      const sub = raw.slice(1).trim();
      return allItems.filter(
        (i) =>
          i.kind === 'widget' &&
          (i.title.toLowerCase().includes(sub) ||
            i.subtitle.toLowerCase().includes(sub)),
      );
    }

    return allItems.filter(
      (i) =>
        i.title.toLowerCase().includes(raw) ||
        i.subtitle.toLowerCase().includes(raw),
    );
  }, [allItems, query]);

  useEffect(() => {
    setSelectedIndex(0);
  }, [query]);

  if (!open) return null;

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((prev) =>
        filteredItems.length > 0 ? (prev + 1) % filteredItems.length : 0,
      );
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((prev) =>
        filteredItems.length > 0
          ? (prev - 1 + filteredItems.length) % filteredItems.length
          : 0,
      );
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const item = filteredItems[selectedIndex];
      if (item) {
        item.run();
        onClose();
      }
    } else if (e.key === 'Tab') {
      e.preventDefault();
      const item = filteredItems[selectedIndex];
      if (item?.preview) {
        item.preview();
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      onClose();
    }
  };

  return (
    <div
      onClick={onClose}
      className="fixed inset-0 z-50 flex items-start justify-center bg-[#07080C]/70 pt-[10vh] backdrop-blur-md"
    >
      <div
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
        style={{
          borderColor: hexWithAlpha(accentHex, 0.45),
          boxShadow: `0 24px 64px rgba(0,0,0,0.75), 0 0 36px ${hexWithAlpha(accentHex, 0.2)}`,
        }}
        className="w-full max-w-[640px] overflow-hidden rounded-2xl border bg-[#10131C]/95 backdrop-blur-2xl animate-view-enter"
      >
        {/* Search Input Header */}
        <div className="flex items-center gap-3 border-b border-white/10 px-4 py-3.5">
          <Search className="h-4 w-4 shrink-0" style={{ color: accentHex }} />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Komut (>), Tema (#), Widget (@) veya ayar ara…"
            className="w-full bg-transparent text-sm text-[#F2F4F8] placeholder-[#6B7183] focus:outline-none"
          />
          <div className="flex items-center gap-1">
            {['>', '#', '@'].map((prefix) => (
              <button
                key={prefix}
                onClick={() => {
                  setQuery(prefix);
                  inputRef.current?.focus();
                }}
                className="rounded bg-white/8 px-2 py-0.5 font-mono text-xs text-[#A3A9B8] hover:bg-white/15 hover:text-white"
              >
                {prefix}
              </button>
            ))}
            <button
              onClick={onClose}
              className="ml-1 rounded-lg p-1 text-[#6B7183] hover:bg-white/10 hover:text-white"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Results List */}
        <div className="max-h-[380px] overflow-y-auto p-2">
          {filteredItems.length === 0 ? (
            <div className="py-10 text-center text-xs text-[#6B7183]">
              Eşleşen komut, tema veya widget bulunamadı.
            </div>
          ) : (
            filteredItems.map((item, idx) => {
              const isSelected = idx === selectedIndex;
              return (
                <button
                  key={item.id}
                  onMouseEnter={() => setSelectedIndex(idx)}
                  onClick={() => {
                    item.run();
                    onClose();
                  }}
                  style={
                    isSelected
                      ? {
                          backgroundColor: hexWithAlpha(accentHex, 0.16),
                          borderColor: hexWithAlpha(accentHex, 0.4),
                        }
                      : undefined
                  }
                  className={`flex w-full items-center justify-between rounded-xl border px-3 py-2.5 text-left transition-colors ${
                    isSelected ? 'text-white' : 'border-transparent text-[#A3A9B8]'
                  }`}
                >
                  <div className="flex min-w-0 items-center gap-3">
                    {item.kind === 'theme' ? (
                      <span
                        style={{ backgroundColor: item.accentColor || accentHex }}
                        className="h-5 w-5 shrink-0 rounded-full border border-white/30 shadow"
                      />
                    ) : item.kind === 'command' ? (
                      <Terminal
                        className="h-4 w-4 shrink-0"
                        style={{ color: accentHex }}
                      />
                    ) : item.kind === 'widget' ? (
                      <Activity className="h-4 w-4 shrink-0 text-[#4DB8FF]" />
                    ) : (
                      <Layers className="h-4 w-4 shrink-0 text-[#2EE6A6]" />
                    )}

                    <div className="min-w-0">
                      <div className="truncate text-xs font-semibold text-[#F2F4F8]">
                        {item.title}
                      </div>
                      <div className="truncate text-[11px] text-[#6B7183]">
                        {item.subtitle}
                      </div>
                    </div>
                  </div>

                  <div className="ml-3 flex shrink-0 items-center gap-2">
                    {item.shortcut && (
                      <kbd className="rounded bg-white/8 px-2 py-0.5 font-mono text-[10px] text-[#A3A9B8]">
                        {item.shortcut}
                      </kbd>
                    )}
                    {isSelected && (
                      <CornerDownLeft
                        className="h-3.5 w-3.5"
                        style={{ color: accentHex }}
                      />
                    )}
                  </div>
                </button>
              );
            })
          )}
        </div>

        {/* Footer Hints */}
        <div className="flex items-center justify-between border-t border-white/8 bg-[#090B10]/80 px-4 py-2 font-mono text-[10px] text-[#6B7183]">
          <div className="flex items-center gap-3">
            <span>↑↓ Gezin</span>
            <span>↵ Çalıştır</span>
            <span>Tab Önizle</span>
            <span>Esc Kapat</span>
          </div>
          <span className="flex items-center gap-1 text-[#A3A9B8]">
            <Sparkles className="h-3 w-3" style={{ color: accentHex }} />
            Komut Paleti v3.0
          </span>
        </div>
      </div>
    </div>
  );
};
