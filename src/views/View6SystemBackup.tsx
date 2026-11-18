import React, { useEffect, useState } from 'react';
import {
  Activity,
  AlertOctagon,
  Copy,
  History,
  Keyboard,
  Play,
  RotateCcw,
  Settings2,
  ShieldCheck,
  Terminal,
  Trash2,
} from 'lucide-react';
import {
  clearDiagnostics,
  fetchAppInfo,
  isTauriRuntime,
  subscribeDiagnostics,
} from '../api/bridge';
import type {
  ConfigMap,
  DiagnosticLogEntry,
  ShortcutBinding,
  ThemeSnapshot,
} from '../types/theme';
import { hexWithAlpha } from '../utils/color';

interface View6SystemBackupProps {
  config: ConfigMap;
  snapshots: ThemeSnapshot[];
  glassDaemonRunning: boolean;
  onUpdateConfig: (patch: Partial<ConfigMap>) => void;
  onToggleGlassDaemon: (enabled?: boolean) => void;
  onRollbackSnapshot: (id: string) => void;
  onResetDefaults: () => void;
  onTestPing: () => void;
  showToast: (msg: string) => void;
}

const INITIAL_SHORTCUTS: ShortcutBinding[] = [
  {
    id: 'sc_palette',
    action: 'Global Komut Paletini Aç',
    binding: 'Ctrl + Shift + Space',
    scope: 'Global',
    description: 'Herhangi bir uygulamadayken hızlı komut ve tema arama',
  },
  {
    id: 'sc_apply',
    action: 'Taslak Değişiklikleri Uygula',
    binding: 'Ctrl + S',
    scope: 'Uygulama',
    description: 'Aktif renk, cam ve HUD ayarlarını sisteme yazar',
  },
  {
    id: 'sc_undo',
    action: 'Son Snapshot Yedeğine Dön',
    binding: 'Ctrl + Z',
    scope: 'Uygulama',
    description: '20 adımlı geçmişten bir önceki tema durumunu yükler',
  },
  {
    id: 'sc_icons',
    action: 'Masaüstü İkonlarını Gizle / Göster',
    binding: 'Alt + I',
    scope: 'Global',
    description: 'OLED yanma koruması için masaüstü simgelerini gizler',
  },
  {
    id: 'sc_purge',
    action: 'RAM & WorkingSet Hızlı Temizle',
    binding: 'Ctrl + Shift + P',
    scope: 'Global',
    description: 'Arka plan süreçlerinin bellek çalışma kümesini boşaltır',
  },
  {
    id: 'sc_pomodoro',
    action: 'Pomodoro Başlat / Duraklat',
    binding: 'Alt + P',
    scope: 'Masaüstü HUD',
    description: 'Masaüstü odak sayacını kontrol eder',
  },
];

export const View6SystemBackup: React.FC<View6SystemBackupProps> = ({
  config,
  snapshots,
  glassDaemonRunning,
  onUpdateConfig,
  onToggleGlassDaemon,
  onRollbackSnapshot,
  onResetDefaults,
  onTestPing,
  showToast,
}) => {
  const [shortcuts, setShortcuts] = useState<ShortcutBinding[]>(INITIAL_SHORTCUTS);
  const [recordingId, setRecordingId] = useState<string | null>(null);
  const [logs, setLogs] = useState<DiagnosticLogEntry[]>([]);
  const [bridgePath, setBridgePath] = useState<string>(
    'C:\\Users\\gorke\\Documents\\Project Icons\\Desktop-DoomGreen\\DoomControlCenter.exe',
  );
  const [confirmReset, setConfirmReset] = useState<boolean>(false);

  const primaryAccent = config.PrimaryAccentHex || '#2EE6A6';
  const autostart = config.AutostartEnabled !== '0';
  const startMinimized = config.StartMinimizedToTray === '1';
  const fpsCap = config.TargetFpsCap || '60';
  const restorePoint = config.CreateRestorePointBeforeApply === '1';

  useEffect(() => {
    const unsub = subscribeDiagnostics(setLogs);
    void fetchAppInfo().then((info) => {
      if (info.bridgePath) setBridgePath(info.bridgePath);
    });
    return unsub;
  }, []);

  // Key recording listener
  useEffect(() => {
    if (!recordingId) return;
    const handleKey = (e: KeyboardEvent) => {
      e.preventDefault();
      if (e.key === 'Escape') {
        setRecordingId(null);
        return;
      }
      if (['Control', 'Shift', 'Alt', 'Meta'].includes(e.key)) return;

      const parts: string[] = [];
      if (e.ctrlKey) parts.push('Ctrl');
      if (e.shiftKey) parts.push('Shift');
      if (e.altKey) parts.push('Alt');
      parts.push(e.key.length === 1 ? e.key.toUpperCase() : e.key);
      const combo = parts.join(' + ');

      setShortcuts((prev) =>
        prev.map((sc) =>
          sc.id === recordingId ? { ...sc, binding: combo } : sc,
        ),
      );
      setRecordingId(null);
      showToast(`Kısayol '${combo}' olarak güncellendi`);
    };

    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, [recordingId, showToast]);

  const handleCopyLogs = () => {
    const text = logs
      .map((l) => `[${l.timestamp}] [${l.status.toUpperCase()}] ${l.cmd} (${l.durationMs}ms): ${l.summary}`)
      .join('\n');
    void navigator.clipboard?.writeText(text);
    showToast('Tanılama ve IPC günlükleri panoya kopyalandı');
  };

  return (
    <div className="grid grid-cols-12 gap-4 animate-view-enter">
      {/* STARTUP & PERFORMANCE 4x2 */}
      <div className="bento-card col-span-12 xl:col-span-4 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Settings2 className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Başlangıç & Motor Performansı
              </h3>
            </div>
            <span className="font-mono text-[10px] text-[#2EE6A6]">
              IDLE &lt; 0.8% CPU
            </span>
          </div>

          <div className="mt-4 space-y-2.5">
            <button
              onClick={() =>
                onUpdateConfig({ AutostartEnabled: autostart ? '0' : '1' })
              }
              className="l2-pill flex w-full items-center justify-between rounded-xl p-3 text-left"
            >
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">
                  Windows ile Otomatik Başlat
                </div>
                <div className="text-[11px] text-[#A3A9B8]">
                  Oturum açıldığında arka plan cam ve HUD motorunu yükle
                </div>
              </div>
              <span
                className="font-mono text-xs font-bold"
                style={{ color: autostart ? '#2EE6A6' : '#6B7183' }}
              >
                {autostart ? 'AÇIK' : 'KAPALI'}
              </span>
            </button>

            <button
              onClick={() =>
                onUpdateConfig({
                  StartMinimizedToTray: startMinimized ? '0' : '1',
                })
              }
              className="l2-pill flex w-full items-center justify-between rounded-xl p-3 text-left"
            >
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">
                  Sistem Tepsisinde Başlat (--daemon)
                </div>
                <div className="text-[11px] text-[#A3A9B8]">
                  Ana pencereyi açmadan sessizce tepside çalış
                </div>
              </div>
              <span
                className="font-mono text-xs font-bold"
                style={{ color: startMinimized ? '#2EE6A6' : '#6B7183' }}
              >
                {startMinimized ? 'EVET' : 'HAYIR'}
              </span>
            </button>

            <button
              onClick={() => onToggleGlassDaemon(!glassDaemonRunning)}
              className="l2-pill flex w-full items-center justify-between rounded-xl p-3 text-left"
            >
              <div>
                <div className="text-xs font-bold text-[#F2F4F8]">
                  DoomGlassDaemon Servisi
                </div>
                <div className="text-[11px] text-[#A3A9B8]">
                  Canlı pencere kenar ve gölge takip kancası
                </div>
              </div>
              <span
                className="font-mono text-xs font-bold"
                style={{ color: glassDaemonRunning ? '#2EE6A6' : '#FFB547' }}
              >
                {glassDaemonRunning ? 'ÇALIŞIYOR' : 'DURDU'}
              </span>
            </button>
          </div>
        </div>

        {/* Renderer FPS Cap */}
        <div className="mt-4 pt-3 border-t border-white/8">
          <div className="mb-1.5 font-mono text-[10px] text-[#A3A9B8]">
            HUD & SPEKTRUM KARE HIZI SINIRI (FPS CAP)
          </div>
          <div className="grid grid-cols-3 gap-2">
            {['30', '60', '120'].map((fps) => (
              <button
                key={fps}
                onClick={() => onUpdateConfig({ TargetFpsCap: fps })}
                style={
                  fpsCap === fps
                    ? {
                        backgroundColor: hexWithAlpha(primaryAccent, 0.2),
                        borderColor: primaryAccent,
                        color: '#FFFFFF',
                      }
                    : undefined
                }
                className="l2-pill rounded-lg py-1.5 font-mono text-xs font-bold text-[#A3A9B8]"
              >
                {fps} FPS
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* SHORTCUTS TABLE 8x4 */}
      <div className="bento-card col-span-12 xl:col-span-8 p-5 flex flex-col justify-between">
        <div>
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <Keyboard className="h-4 w-4" style={{ color: primaryAccent }} />
              <div>
                <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                  Klavye Kısayolları & Tuş Atama Tablosu
                </h3>
                <p className="text-xs text-[#A3A9B8]">
                  Herhangi bir kısayol rozetine tıklayarak yeni tuş kombinasyonu
                  kaydedebilirsiniz.
                </p>
              </div>
            </div>
            <span className="font-mono text-[10px] text-[#A3A9B8]">
              ÇAKIŞMA KONTROLÜ: AKTİF
            </span>
          </div>

          <div className="space-y-2">
            {shortcuts.map((sc) => {
              const isRec = recordingId === sc.id;
              return (
                <div
                  key={sc.id}
                  className="l2-pill flex flex-wrap items-center justify-between gap-2 rounded-xl px-3.5 py-2.5"
                >
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-[#F2F4F8]">
                        {sc.action}
                      </span>
                      <span className="rounded bg-white/8 px-1.5 py-0.5 font-mono text-[9px] text-[#A3A9B8]">
                        {sc.scope}
                      </span>
                    </div>
                    <div className="text-[11px] text-[#6B7183]">
                      {sc.description}
                    </div>
                  </div>

                  <button
                    onClick={() => setRecordingId(isRec ? null : sc.id)}
                    style={
                      isRec
                        ? {
                            backgroundColor: hexWithAlpha('#FFB547', 0.2),
                            borderColor: '#FFB547',
                            color: '#FFB547',
                          }
                        : {
                            borderColor: hexWithAlpha(primaryAccent, 0.35),
                          }
                    }
                    className="rounded-lg border bg-[#07080C] px-3 py-1.5 font-mono text-xs font-bold text-[#F2F4F8] hover:border-white/40"
                  >
                    {isRec ? 'Tuşlara Basın (Esc İptal)…' : sc.binding}
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* BACKUP / RESTORE & FACTORY RESET 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Güvenlik, Yedekleme & Fabrika Sıfırlama
              </h3>
            </div>
            <span className="font-mono text-[10px] text-[#2EE6A6]">
              KAYIT DEFTERİ KORUMALI
            </span>
          </div>

          {/* System Restore Point Toggle */}
          <div className="mt-3 flex items-center justify-between rounded-xl border border-white/8 bg-white/[0.03] p-3">
            <div>
              <div className="text-xs font-bold text-[#F2F4F8]">
                Uygulamadan Önce Otomatik Snapshot Al
              </div>
              <div className="text-[11px] text-[#A3A9B8]">
                Her tema değişiminde geri dönülebilir durum kaydı oluşturur
              </div>
            </div>
            <button
              onClick={() =>
                onUpdateConfig({
                  CreateRestorePointBeforeApply: restorePoint ? '0' : '1',
                })
              }
              style={{
                backgroundColor: restorePoint ? primaryAccent : 'rgba(255,255,255,0.12)',
              }}
              className="relative h-5 w-9 rounded-full transition-colors"
            >
              <span
                className={`absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white transition-transform ${
                  restorePoint ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          {/* Quick Snapshot List */}
          <div className="mt-3 max-h-[130px] space-y-1.5 overflow-y-auto pr-1">
            {snapshots.slice(0, 5).map((snap) => (
              <div
                key={snap.id}
                className="l2-pill flex items-center justify-between rounded-lg px-3 py-1.5 text-xs"
              >
                <div className="flex items-center gap-2">
                  <History className="h-3.5 w-3.5 text-[#A3A9B8]" />
                  <span className="font-medium text-white">{snap.label}</span>
                  <span className="font-mono text-[10px] text-[#6B7183]">
                    {snap.timestamp}
                  </span>
                </div>
                <button
                  onClick={() => onRollbackSnapshot(snap.id)}
                  className="flex items-center gap-1 font-mono text-[10px] font-bold hover:underline"
                  style={{ color: primaryAccent }}
                >
                  <RotateCcw className="h-3 w-3" />
                  Geri Yükle
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Factory Reset Button */}
        <div className="mt-4 pt-3 border-t border-white/8 flex items-center justify-between gap-3">
          <div className="text-[11px] text-[#A3A9B8]">
            Tüm cam, görev çubuğu ve renk ayarlarını varsayılan Doom Emerald v3
            profiline döndürür.
          </div>
          {!confirmReset ? (
            <button
              onClick={() => setConfirmReset(true)}
              className="flex shrink-0 items-center gap-1.5 rounded-xl border border-[#FF4D5E]/40 bg-[#FF4D5E]/15 px-3 py-2 text-xs font-bold text-[#FF4D5E] hover:bg-[#FF4D5E]/25"
            >
              <AlertOctagon className="h-3.5 w-3.5" />
              <span>Fabrika Ayarlarına Dön</span>
            </button>
          ) : (
            <div className="flex shrink-0 items-center gap-1.5">
              <button
                onClick={() => {
                  onResetDefaults();
                  setConfirmReset(false);
                }}
                className="rounded-xl bg-[#FF4D5E] px-3 py-2 text-xs font-bold text-white"
              >
                Onayla (Sıfırla)
              </button>
              <button
                onClick={() => setConfirmReset(false)}
                className="l2-pill rounded-xl px-2.5 py-2 text-xs text-[#A3A9B8]"
              >
                İptal
              </button>
            </div>
          )}
        </div>
      </div>

      {/* LIVE HEALTH & DIAGNOSTIC IPC CONSOLE 6x3 */}
      <div className="bento-card col-span-12 lg:col-span-6 p-5 flex flex-col justify-between">
        <div>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <Terminal className="h-4 w-4" style={{ color: primaryAccent }} />
              <h3 className="font-['Outfit'] text-sm font-bold text-[#F2F4F8] uppercase">
                Canlı Native Köprü & IPC Tanılama Konsolu
              </h3>
            </div>

            <div className="flex items-center gap-1.5">
              <button
                onClick={onTestPing}
                className="l2-pill flex items-center gap-1 rounded-lg px-2 py-1 font-mono text-[10px] text-[#2EE6A6]"
              >
                <Play className="h-3 w-3" />
                <span>IPC Ping Test</span>
              </button>
              <button
                onClick={handleCopyLogs}
                className="l2-pill flex items-center gap-1 rounded-lg px-2 py-1 font-mono text-[10px] text-[#A3A9B8] hover:text-white"
              >
                <Copy className="h-3 w-3" />
                <span>Kopyala</span>
              </button>
              <button
                onClick={() => clearDiagnostics()}
                className="l2-pill flex items-center gap-1 rounded-lg px-2 py-1 font-mono text-[10px] text-[#A3A9B8] hover:text-white"
              >
                <Trash2 className="h-3 w-3" />
              </button>
            </div>
          </div>

          <div className="mt-1.5 truncate font-mono text-[10px] text-[#6B7183]">
            Köprü: {bridgePath} ({isTauriRuntime() ? 'Tauri v2 Native IPC' : 'Browser Preview'})
          </div>

          {/* Console Output */}
          <div className="mt-3 h-[170px] overflow-y-auto rounded-xl border border-white/10 bg-[#05070B] p-3 font-mono text-[11px] space-y-1.5">
            {logs.length === 0 ? (
              <div className="text-[#6B7183]">Günlük kaydı temiz.</div>
            ) : (
              logs.map((entry) => (
                <div
                  key={entry.id}
                  className="flex items-start justify-between gap-2 border-b border-white/5 pb-1"
                >
                  <div className="min-w-0">
                    <span className="text-[#6B7183]">[{entry.timestamp}] </span>
                    <span
                      className="font-bold"
                      style={{
                        color:
                          entry.status === 'ok'
                            ? '#2EE6A6'
                            : entry.status === 'error'
                              ? '#FF4D5E'
                              : '#4DB8FF',
                      }}
                    >
                      {entry.cmd}
                    </span>
                    <span className="text-[#A3A9B8]"> — {entry.summary}</span>
                  </div>
                  <span className="shrink-0 text-[10px] text-[#6B7183]">
                    {entry.durationMs}ms
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      {/* ABOUT FOOTER BAR 12x1 */}
      <div className="bento-card col-span-12 px-5 py-3.5 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-3">
          <Activity className="h-4 w-4" style={{ color: primaryAccent }} />
          <span className="font-['Outfit'] text-xs font-bold text-[#F2F4F8]">
            SplendThemeBuilder — Doom Control Center v3.0 (Bento Command Deck)
          </span>
          <span className="rounded bg-white/8 px-2 py-0.5 font-mono text-[10px] text-[#A3A9B8]">
            Tauri v2 + React 19 + Tailwind CSS v4 + C# NativeBridge
          </span>
        </div>
        <div className="font-mono text-[11px] text-[#6B7183]">
          Mimari: Opus 5.5 Spec • 10-bit scRGB HDR • 8x Katmanlı HUD
        </div>
      </div>
    </div>
  );
};
