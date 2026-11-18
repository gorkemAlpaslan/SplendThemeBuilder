use serde::{Deserialize, Serialize};
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use std::process::{Child, ChildStdin, ChildStdout, Command, Stdio};
use std::sync::Mutex;
use tauri::{Manager, RunEvent};

#[cfg(windows)]
use std::os::windows::process::CommandExt;

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x08000000;

const FALLBACK_BRIDGE_PATH: &str =
    r"C:\Users\gorke\Documents\Project Icons\Desktop-DoomGreen\DoomControlCenter.exe";

struct BridgeProcess {
    child: Child,
    stdin: ChildStdin,
    stdout: BufReader<ChildStdout>,
}

struct BridgeState(Mutex<Option<BridgeProcess>>);

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct AppInfo {
    version: String,
    product_name: String,
    bridge_path: String,
    bridge_exists: bool,
}

#[derive(Deserialize)]
struct FileFilter {
    name: String,
    extensions: Vec<String>,
}

fn resolve_bridge_path() -> PathBuf {
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() {
            let candidate = dir.join("DoomControlCenter.exe");
            if candidate.exists() {
                return candidate;
            }
            // In `cargo run` / `tauri dev`, exe lives in `src-tauri/target/debug/`
            if let Some(target_dir) = dir.parent() {
                if let Some(src_tauri_dir) = target_dir.parent() {
                    if let Some(root_dir) = src_tauri_dir.parent() {
                        let dev_candidate = root_dir.join("DoomControlCenter.exe");
                        if dev_candidate.exists() {
                            return dev_candidate;
                        }
                    }
                }
            }
        }
    }

    if let Ok(cwd) = std::env::current_dir() {
        let cwd_candidate = cwd.join("DoomControlCenter.exe");
        if cwd_candidate.exists() {
            return cwd_candidate;
        }
        if let Some(parent) = cwd.parent() {
            let parent_candidate = parent.join("DoomControlCenter.exe");
            if parent_candidate.exists() {
                return parent_candidate;
            }
        }
    }

    PathBuf::from(FALLBACK_BRIDGE_PATH)
}

fn spawn_bridge() -> Result<BridgeProcess, String> {
    let bridge_path = resolve_bridge_path();
    if !bridge_path.exists() {
        return Err(format!(
            "DoomControlCenter.exe not found at {}",
            bridge_path.display()
        ));
    }

    let mut cmd = Command::new(&bridge_path);
    cmd.arg("--bridge")
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::null());

    if let Some(parent) = bridge_path.parent() {
        cmd.current_dir(parent);
    }

    #[cfg(windows)]
    cmd.creation_flags(CREATE_NO_WINDOW);

    let mut child = cmd
        .spawn()
        .map_err(|e| format!("Failed to spawn DoomControlCenter.exe --bridge: {e}"))?;

    let stdin = child
        .stdin
        .take()
        .ok_or_else(|| "Failed to open bridge stdin".to_string())?;
    let stdout = child
        .stdout
        .take()
        .ok_or_else(|| "Failed to open bridge stdout".to_string())?;

    Ok(BridgeProcess {
        child,
        stdin,
        stdout: BufReader::new(stdout),
    })
}

fn is_alive(proc: &mut BridgeProcess) -> bool {
    matches!(proc.child.try_wait(), Ok(None))
}

fn send_and_receive(proc: &mut BridgeProcess, payload: &str) -> Result<String, String> {
    let trimmed = payload.trim_end_matches(['\r', '\n']);
    writeln!(proc.stdin, "{trimmed}")
        .and_then(|_| proc.stdin.flush())
        .map_err(|e| format!("Bridge write failed: {e}"))?;

    let mut response = String::new();
    let bytes = proc
        .stdout
        .read_line(&mut response)
        .map_err(|e| format!("Bridge read failed: {e}"))?;

    if bytes == 0 {
        return Err("Bridge closed stdout unexpectedly".to_string());
    }

    Ok(response.trim_end_matches(['\r', '\n']).to_string())
}

#[tauri::command]
fn bridge_call(
    command_json: String,
    state: tauri::State<'_, BridgeState>,
) -> Result<String, String> {
    let mut guard = state
        .0
        .lock()
        .map_err(|_| "Bridge mutex poisoned".to_string())?;

    if let Some(proc) = guard.as_mut() {
        if !is_alive(proc) {
            *guard = None;
        }
    }

    if guard.is_none() {
        *guard = Some(spawn_bridge()?);
    }

    let first_attempt = {
        let proc = guard.as_mut().expect("bridge process initialized");
        send_and_receive(proc, &command_json)
    };

    match first_attempt {
        Ok(res) => Ok(res),
        Err(_) => {
            if let Some(mut old) = guard.take() {
                let _ = old.child.kill();
                let _ = old.child.wait();
            }
            let mut fresh = spawn_bridge()?;
            let retry = send_and_receive(&mut fresh, &command_json);
            if retry.is_ok() {
                *guard = Some(fresh);
            } else {
                let _ = fresh.child.kill();
                let _ = fresh.child.wait();
            }
            retry
        }
    }
}

#[tauri::command]
fn app_info(app: tauri::AppHandle) -> AppInfo {
    let bridge_path = resolve_bridge_path();
    let pkg = app.package_info();
    AppInfo {
        version: pkg.version.to_string(),
        product_name: pkg.name.clone(),
        bridge_exists: bridge_path.exists(),
        bridge_path: bridge_path.to_string_lossy().into_owned(),
    }
}

#[tauri::command]
fn open_file_dialog(
    title: Option<String>,
    filters: Option<Vec<FileFilter>>,
) -> Result<Option<String>, String> {
    let mut dialog = rfd::FileDialog::new();
    if let Some(t) = title {
        dialog = dialog.set_title(&t);
    }
    if let Some(list) = filters {
        for f in list {
            let exts: Vec<&str> = f.extensions.iter().map(String::as_str).collect();
            dialog = dialog.add_filter(&f.name, &exts);
        }
    }
    Ok(dialog.pick_file().map(|p| p.to_string_lossy().into_owned()))
}

#[tauri::command]
fn save_file_dialog(
    title: Option<String>,
    default_name: Option<String>,
    filters: Option<Vec<FileFilter>>,
) -> Result<Option<String>, String> {
    let mut dialog = rfd::FileDialog::new();
    if let Some(t) = title {
        dialog = dialog.set_title(&t);
    }
    if let Some(name) = default_name {
        dialog = dialog.set_file_name(&name);
    }
    if let Some(list) = filters {
        for f in list {
            let exts: Vec<&str> = f.extensions.iter().map(String::as_str).collect();
            dialog = dialog.add_filter(&f.name, &exts);
        }
    }
    Ok(dialog.save_file().map(|p| p.to_string_lossy().into_owned()))
}

pub fn run() {
    let log_path = std::env::temp_dir().join("splend_theme_builder_boot.log");
    let _ = std::fs::write(&log_path, format!("Starting SplendThemeBuilder...\n"));
    let log_path_panic = log_path.clone();
    std::panic::set_hook(Box::new(move |info| {
        let _ = std::fs::OpenOptions::new()
            .create(true)
            .append(true)
            .open(&log_path_panic)
            .and_then(|mut f| writeln!(f, "PANIC: {info}"));
    }));

    let app = tauri::Builder::default()
        .manage(BridgeState(Mutex::new(None)))
        .setup(|app| {
            let log_p = std::env::temp_dir().join("splend_theme_builder_boot.log");
            let _ = std::fs::OpenOptions::new().create(true).append(true).open(&log_p).and_then(|mut f| writeln!(f, "Setup reached, main window: {}", app.get_webview_window("main").is_some()));
            if let Some(w) = app.get_webview_window("main") {
                let _ = w.show();
                let _ = w.set_focus();
            }
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            bridge_call,
            app_info,
            open_file_dialog,
            save_file_dialog
        ])
        .build(tauri::generate_context!())
        .expect("error while building tauri application");

    app.run(|app_handle, event| {
        if let RunEvent::Exit = event {
            if let Some(state) = app_handle.try_state::<BridgeState>() {
                if let Ok(mut guard) = state.0.lock() {
                    if let Some(mut proc) = guard.take() {
                        let _ = proc.child.kill();
                        let _ = proc.child.wait();
                    }
                }
            }
        }
    });
}
