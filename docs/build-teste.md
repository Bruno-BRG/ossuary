# Build e teste — Ossuary

## Preparação

Windows x64, Node.js 22.12+ ou 24+, Rust MSVC, Visual Studio Build Tools com
C++/Windows SDK e WebView2. `desktop.ps1 setup` instala dependências npm e
um SDK .NET 10 local em `.tools/`. O jogador não precisa de SDK: o motor é
distribuído como executável autocontido.

```powershell
.\desktop.ps1 setup
.\desktop.ps1 dev
```

## Validação

```powershell
.\fastcheck.ps1                  # C# e TypeScript
.\headless.ps1 test              # Core + fluxo desktop
.\headless.ps1 dump panels       # UI em ASCII
.\headless.ps1 dump town         # cidade
.\headless.ps1 fx fireball       # a animação de uma magia, passo a passo em ASCII (fx list | fx all)
.\headless.ps1 soak 50 500        # 25 mil turnos
.\check.ps1                      # frontend, IPC, Rust e testes de simulação
```

`test.ps1` e `run-tests.ps1` executam a mesma suite headless. MSBuild sempre
verifica as fontes e usa compilação incremental; não há executável de teste
em cache que ignore alterações. A fonte é copiada para a saída de teste e
seu teste de cobertura falha se o recurso estiver ausente.

## Desenvolvimento e distribuição

```powershell
.\desktop.ps1 web                # navegador, motor real em loopback
.\desktop.ps1 prepare            # publica o processo de simulação
.\desktop.ps1 build              # app Tauri e instalador NSIS
```

O publish gera `ossuary-engine.exe` com runtime embutido. O pipeline cria
`desktop/src-tauri/binaries/ossuary-engine-x86_64-pc-windows-msvc.exe`, o
nome usado por Tauri `externalBin`. O bundler entrega `ossuary.exe` e
`ossuary-engine.exe` lado a lado e monta o instalador Windows x64.

Saídas:

- `desktop/src-tauri/target/release/ossuary.exe`
- `desktop/src-tauri/target/release/ossuary-engine.exe`
- `desktop/src-tauri/target/release/bundle/nsis/Ossuary_0.1.0_x64-setup.exe`

Distribuir o instalador ou ambos os executáveis. O processo de simulação
precisa acompanhar o aplicativo. O pacote atual é Windows x64; outros
sistemas operacionais exigem seus próprios pré-requisitos e pacotes.

Mais detalhes: [desktop.md](desktop.md).

## Inicialização nativa

Após empacotar, `desktop.ps1 build` inicia o executável com `--smoke-test`, em janela oculta. O teste exige fonte bitmap carregada, frame recebido por IPC e pixels desenhados no Canvas do WebView2. O aplicativo retorna código 0 ao confirmar; falha de inicialização retorna 2 após 20 segundos. O pipeline também impõe limite de 30 segundos e recolhe o processo em caso de timeout.
