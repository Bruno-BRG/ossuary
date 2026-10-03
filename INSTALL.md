# Installation guide

**English** · [Português (Brasil)](INSTALL.pt-BR.md)

## Players

1. Get `Ossuary_0.12.0_x64-setup.exe` (Windows x64) from the [releases page](https://github.com/Bruno-BRG/ossuary/releases) or the build output.
2. Run it and follow the installer.
3. Launch **Ossuary**. Both `ossuary.exe` and its engine `ossuary-engine.exe` are installed
   side by side; no .NET install is needed.

Requirements: Windows 10/11 x64 and Microsoft WebView2 (already present on Windows 11).

## Building from source

Target platform: **Windows x64**.

### Prerequisites

| Tool | Notes |
|---|---|
| Git | to clone the repository |
| PowerShell | scripts are `.ps1` (run with `-ExecutionPolicy Bypass` if scripts are blocked) |
| Node.js | 22.12+ or 24+ |
| Rust | stable, MSVC toolchain (`x86_64-pc-windows-msvc`) |
| Visual Studio Build Tools | "Desktop development with C++" + Windows SDK |
| WebView2 runtime | preinstalled on Windows 11 |

The .NET 10 SDK is installed locally into `.tools/` by the setup step; you do not need it globally.

### Steps

```powershell
git clone https://github.com/Bruno-BRG/ossuary.git
cd ossuary

.\desktop.ps1 setup      # installs the local .NET 10 SDK and npm packages
.\desktop.ps1 dev        # opens the Tauri app with frontend hot reload
```

Other modes:

```powershell
.\desktop.ps1 web        # same real engine in a browser on loopback (fast UI work)
.\desktop.ps1 test       # engine, desktop flow, frontend and Rust tests
.\desktop.ps1 build      # release ossuary.exe + NSIS installer
```

### Build outputs

- `desktop/src-tauri/target/release/ossuary.exe`
- `desktop/src-tauri/target/release/bundle/nsis/Ossuary_0.12.0_x64-setup.exe`

Distribute the installer, or both executables together.

### Engine-only development (no window)

```powershell
.\fastcheck.ps1
.\headless.ps1 test
.\headless.ps1 dump panels      # also: level | overworld | town
.\headless.ps1 soak 50 500
.\headless.ps1 fx fireball      # a spell animation in ASCII
```

### Troubleshooting

- *"running scripts is disabled"*: run `powershell -ExecutionPolicy Bypass -File .\desktop.ps1 setup`.
- *"No .NET 10 SDK found"*: run `.\desktop.ps1 setup` first.
- *Rust or linker errors*: install the MSVC Build Tools with the Windows SDK.
- *Blank window*: install/update the WebView2 runtime.
