# Guia de instalação

[English](INSTALL.md) · **Português (Brasil)**

## Jogadores

1. Baixe `Ossuary_0.16.0_x64-setup.exe` (Windows x64), da página de [releases](https://github.com/Bruno-BRG/ossuary/releases) ou da saída do build.
2. Execute e siga o instalador.
3. Abra o **Ossuary**. O `ossuary.exe` e o motor `ossuary-engine.exe` são instalados lado a lado; não é preciso instalar o .NET.

Requisitos: Windows 10/11 x64 e Microsoft WebView2 (já vem no Windows 11).

## Compilar do código-fonte

Plataforma-alvo: **Windows x64**.

### Pré-requisitos

| Ferramenta | Observação |
|---|---|
| Git | para clonar o repositório |
| PowerShell | os scripts são `.ps1` (use `-ExecutionPolicy Bypass` se estiverem bloqueados) |
| Node.js | 22.12+ ou 24+ |
| Rust | stable, toolchain MSVC (`x86_64-pc-windows-msvc`) |
| Visual Studio Build Tools | "Desenvolvimento para desktop com C++" + SDK do Windows |
| WebView2 | já vem no Windows 11 |

O SDK do .NET 10 é instalado **localmente** em `.tools/` pelo passo de setup; não precisa estar instalado no sistema.

### Passos

```powershell
git clone https://github.com/Bruno-BRG/ossuary.git
cd ossuary

.\desktop.ps1 setup      # instala o SDK .NET 10 local e os pacotes npm
.\desktop.ps1 dev        # abre o app Tauri com recarga do frontend
```

Outros modos:

```powershell
.\desktop.ps1 web        # o mesmo motor real num navegador, em loopback (UI rápida)
.\desktop.ps1 test       # testes do motor, do fluxo desktop, do frontend e do Rust
.\desktop.ps1 build      # ossuary.exe de release + instalador NSIS
```

### Saídas do build

- `desktop/src-tauri/target/release/ossuary.exe`
- `desktop/src-tauri/target/release/bundle/nsis/Ossuary_0.16.0_x64-setup.exe`

Distribua o instalador, ou os dois executáveis juntos.

### Só o motor (sem janela)

```powershell
.\fastcheck.ps1
.\headless.ps1 test
.\headless.ps1 dump panels      # também: level | overworld | town
.\headless.ps1 soak 50 500
.\headless.ps1 fx fireball      # animação de uma magia em ASCII
```

### Problemas comuns

- *"a execução de scripts está desabilitada"*: rode `powershell -ExecutionPolicy Bypass -File .\desktop.ps1 setup`.
- *"No .NET 10 SDK found"*: rode `.\desktop.ps1 setup` antes.
- *Erros de Rust ou do linker*: instale as Build Tools do MSVC com o SDK do Windows.
- *Janela em branco*: instale ou atualize o WebView2.
