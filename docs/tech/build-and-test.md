# Build and test — Ossuary

## Setup

Windows x64, Node.js 22.12+ or 24+, Rust MSVC, Visual Studio Build Tools with
C++/Windows SDK and WebView2. `desktop.ps1 setup` installs npm dependencies and a
local .NET 10 SDK in `.tools/`. Players need no SDK: the engine ships as a
self-contained executable.

```powershell
.\desktop.ps1 setup
.\desktop.ps1 dev
```

## Validation

```powershell
.\fastcheck.ps1                  # C# and TypeScript
.\headless.ps1 test              # Core + desktop flow
.\headless.ps1 dump panels       # UI as ASCII
.\headless.ps1 dump town         # town
.\headless.ps1 fx fireball       # one spell animation, step by step in ASCII (fx list | fx all)
.\headless.ps1 soak 50 500        # 25k turns
.\check.ps1                      # frontend, IPC, Rust and simulation tests
```

`test.ps1` and `run-tests.ps1` run the same headless suite. MSBuild always
checks the sources and builds incrementally; there is no cached test executable that
ignores changes. The font is copied to the test output and its coverage test fails
if the resource is missing.

## Development and distribution

```powershell
.\desktop.ps1 web                # browser, real engine on loopback
.\desktop.ps1 prepare            # publishes the simulation process
.\desktop.ps1 build              # Tauri app and NSIS installer
```

The publish step produces `ossuary-engine.exe` with the runtime embedded. The pipeline creates
`desktop/src-tauri/binaries/ossuary-engine-x86_64-pc-windows-msvc.exe`, the
name Tauri `externalBin` expects. The bundler ships `ossuary.exe` and
`ossuary-engine.exe` side by side and builds the Windows x64 installer.

Outputs:

- `desktop/src-tauri/target/release/ossuary.exe`
- `desktop/src-tauri/target/release/ossuary-engine.exe`
- `desktop/src-tauri/target/release/bundle/nsis/Ossuary_0.16.0_x64-setup.exe`

Distribute the installer or both executables. The simulation process must travel with
the app. The current package is Windows x64; other operating systems need their own
prerequisites and packages.

More details: [desktop.md](desktop.md).

## Native startup

After packaging, `desktop.ps1 build` launches the executable with `--smoke-test` in a hidden window. The test requires the bitmap font loaded, a frame received over IPC and pixels drawn on the WebView2 Canvas. The app exits with code 0 on confirmation; a startup failure exits with 2 after 20 seconds. The pipeline also enforces a 30-second limit and reaps the process on timeout.
