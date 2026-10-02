#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde::Deserialize;
use serde_json::Value;
use std::{
    io::{BufRead, BufReader, Write},
    process::{Child, ChildStdin, Command, Stdio},
    sync::{
        atomic::{AtomicBool, Ordering},
        mpsc, Mutex,
    },
    time::Duration,
};
use tauri::Manager;

#[derive(Deserialize)]
#[serde(rename_all = "camelCase", deny_unknown_fields)]
struct Request {
    op: String,
    seed: Option<String>,
    code: Option<String>,
    key: Option<String>,
    shift: Option<bool>,
    ctrl: Option<bool>,
    repeat: Option<bool>,
    cols: Option<u16>,
    rows: Option<u16>,
    theme: Option<u8>,
    crt: Option<u8>,
    scale: Option<u8>,
    create: Option<bool>,
    daily: Option<bool>,
    lang: Option<String>,
}

impl Request {
    fn validate(&self) -> Result<(), String> {
        if !["new", "load", "title", "play", "key", "resize", "display", "frame"].contains(&self.op.as_str()) {
            return Err("Operação desconhecida.".into());
        }
        if let Some(seed) = &self.seed {
            seed.parse::<u64>()
                .map_err(|_| "Semente inválida.".to_string())?;
        }
        if self.code.as_ref().is_some_and(|v| v.len() > 32)
            || self.key.as_ref().is_some_and(|v| v.len() > 32)
        {
            return Err("Tecla inválida.".into());
        }
        if self.lang.as_ref().is_some_and(|v| v != "pt" && v != "en") {
            return Err("Idioma inválido.".into());
        }
        Ok(())
    }
    fn json(&self) -> Value {
        let mut value = serde_json::json!({"op": self.op});
        macro_rules! field {
            ($name:ident) => {
                if let Some(v) = &self.$name {
                    value[stringify!($name)] = serde_json::json!(v);
                }
            };
        }
        field!(seed);
        field!(code);
        field!(key);
        field!(shift);
        field!(ctrl);
        field!(repeat);
        field!(cols);
        field!(rows);
        field!(theme);
        field!(crt);
        field!(scale);
        field!(create);
        field!(daily);
        field!(lang);
        value
    }
}

struct Engine {
    child: Child,
    input: ChildStdin,
    output: mpsc::Receiver<Result<Value, String>>,
    failed: bool,
}

impl Engine {
    fn start() -> Result<Self, String> {
        let dir = std::env::current_exe()
            .map_err(|e| e.to_string())?
            .parent()
            .ok_or("Diretório do aplicativo indisponível.")?
            .to_path_buf();
        let name = if cfg!(windows) {
            "ossuary-engine.exe"
        } else {
            "ossuary-engine"
        };
        Self::start_at(dir.join(name))
    }

    fn start_at(path: std::path::PathBuf) -> Result<Self, String> {
        let mut command = Command::new(path);
        command
            .stdin(Stdio::piped())
            .stdout(Stdio::piped())
            .stderr(Stdio::inherit());
        #[cfg(windows)]
        {
            use std::os::windows::process::CommandExt;
            command.creation_flags(0x08000000); // CREATE_NO_WINDOW
        }
        let mut child = command
            .spawn()
            .map_err(|e| format!("Não foi possível iniciar o motor: {e}"))?;
        let input = child.stdin.take().ok_or("Entrada do motor indisponível.")?;
        let stdout = child.stdout.take().ok_or("Saída do motor indisponível.")?;
        let (tx, output) = mpsc::channel();
        std::thread::spawn(move || {
            for line in BufReader::new(stdout).lines() {
                let value = line
                    .map_err(|e| e.to_string())
                    .and_then(|s| serde_json::from_str(&s).map_err(|e| e.to_string()));
                if tx.send(value).is_err() {
                    break;
                }
            }
        });
        Ok(Self {
            child,
            input,
            output,
            failed: false,
        })
    }

    fn exchange(&mut self, request: &Request) -> Result<Value, String> {
        if self.failed {
            return Err("O motor encerrou. Reinicie o aplicativo.".into());
        }
        let result = (|| {
            writeln!(self.input, "{}", request.json()).map_err(|e| e.to_string())?;
            self.input.flush().map_err(|e| e.to_string())?;
            self.output
                .recv_timeout(Duration::from_secs(10))
                .map_err(|e| format!("O motor não respondeu: {e}"))?
        })();
        if result.is_err() {
            self.failed = true;
            let _ = self.child.kill();
        }
        result
    }
}
impl Drop for Engine {
    fn drop(&mut self) {
        let _ = self.child.kill();
        let _ = self.child.wait();
    }
}

#[derive(Default)]
struct GameState(Mutex<Option<Engine>>);

struct StartupState {
    smoke: bool,
    ready: AtomicBool,
}

#[tauri::command]
fn startup_ready(
    app: tauri::AppHandle,
    cols: usize,
    rows: usize,
    cells: usize,
    font_glyphs: usize,
    painted: bool,
) -> Result<(), String> {
    if cols < 84 || rows < 26 || cells != cols * rows || font_glyphs < 96 || !painted {
        return Err("A inicialização do terminal está incompleta.".into());
    }
    let state = app.state::<StartupState>();
    state.ready.store(true, Ordering::SeqCst);
    if state.smoke {
        app.exit(0);
    }
    Ok(())
}

#[tauri::command]
async fn game_request(app: tauri::AppHandle, request: Request) -> Result<Value, String> {
    request.validate()?;
    tauri::async_runtime::spawn_blocking(move || {
        let state = app.state::<GameState>();
        let mut engine = state.0.lock().map_err(|_| "Estado do jogo indisponível.")?;
        if engine.is_none() {
            *engine = Some(Engine::start()?);
        }
        engine.as_mut().unwrap().exchange(&request)
    })
    .await
    .map_err(|e| e.to_string())?
}

fn main() {
    let smoke = std::env::args().any(|arg| arg == "--smoke-test");
    tauri::Builder::default()
        .manage(GameState::default())
        .manage(StartupState {
            smoke,
            ready: AtomicBool::new(false),
        })
        .setup(move |app| {
            if smoke {
                if let Some(window) = app.get_webview_window("main") {
                    window.hide()?;
                }
                let handle = app.handle().clone();
                std::thread::spawn(move || {
                    std::thread::sleep(Duration::from_secs(20));
                    if !handle.state::<StartupState>().ready.load(Ordering::SeqCst) {
                        handle.exit(2);
                    }
                });
            }
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![game_request, startup_ready])
        .build(tauri::generate_context!())
        .expect("Não foi possível abrir o Ossuary")
        .run(|app, event| {
            if let tauri::RunEvent::Exit = event {
                if let Ok(mut engine) = app.state::<GameState>().0.lock() {
                    engine.take();
                }
            }
        });
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn protocol_rejects_arbitrary_operations_and_invalid_seed() {
        let request: Request = serde_json::from_value(serde_json::json!({"op":"exec"})).unwrap();
        assert!(request.validate().is_err());
        let request: Request =
            serde_json::from_value(serde_json::json!({"op":"new","seed":"18446744073709551616"}))
                .unwrap();
        assert!(request.validate().is_err());
        let request: Request =
            serde_json::from_value(serde_json::json!({"op":"new","lang":"en"})).unwrap();
        assert!(request.validate().is_ok());
        let request: Request =
            serde_json::from_value(serde_json::json!({"op":"new","lang":"xx"})).unwrap();
        assert!(request.validate().is_err());
        assert!(serde_json::from_value::<Request>(
            serde_json::json!({"op":"frame","path":"secret"})
        )
        .is_err());
    }

    #[test]
    fn held_key_flag_reaches_the_engine() {
        let request: Request = serde_json::from_value(
            serde_json::json!({"op":"key","code":"ArrowRight","shift":false,"ctrl":false,"repeat":true}),
        )
        .unwrap();
        assert!(request.validate().is_ok());
        assert_eq!(request.json()["repeat"], true);
    }

    #[test]
    fn rust_bridge_runs_the_packaged_engine_and_reaps_it() {
        let path = std::path::Path::new(env!("CARGO_MANIFEST_DIR"))
            .join("binaries/ossuary-engine-x86_64-pc-windows-msvc.exe");
        let mut engine = Engine::start_at(path).unwrap();
        let request: Request = serde_json::from_value(
            serde_json::json!({"op":"new","seed":"31337","cols":110,"rows":36}),
        )
        .unwrap();
        let response = engine.exchange(&request).unwrap();
        assert_eq!(response["ok"], true);
        assert_eq!(response["frame"]["glyphs"].as_array().unwrap().len(), 3960);
        let request: Request =
            serde_json::from_value(serde_json::json!({"op":"key","code":"Period"})).unwrap();
        assert_eq!(engine.exchange(&request).unwrap()["frame"]["turn"], 1);
        engine.child.kill().unwrap();
        engine.child.wait().unwrap();
        assert!(engine.exchange(&request).is_err());
        assert!(engine.failed);
    }
}
