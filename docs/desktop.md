# Desktop Tauri — Ossuary

O aplicativo é Tauri 2 com Rust, frontend TypeScript e terminal Canvas bitmap.
O motor C# independente roda como executável .NET 10 autocontido, privado e
empacotado junto do aplicativo. Toda a simulação vive em `engine/Ossuary.Core`;
entrada e modais em `engine/Ossuary.Desktop`; validação em `engine/Ossuary.Headless`.

## Estrutura definitiva

```text
engine/
  Ossuary.Core/        simulação e UI como dados
  Ossuary.Desktop/     host, Session, Input, protocolo
  Ossuary.Headless/    console e Tests
desktop/
  src/                renderer bitmap, título, layout, preferências
  src-tauri/          shell Rust, janela, IPC e empacotamento
assets/fonts/         unscii-16.hex e NOTICE.md
docs/                 documentação e capturas
```

## Operação

```powershell
.\desktop.ps1 setup
.\desktop.ps1 dev
.\desktop.ps1 web
.\fastcheck.ps1
.\headless.ps1 test
.\headless.ps1 dump panels
.\headless.ps1 soak 50 500
.\check.ps1
.\desktop.ps1 build
```

O setup instala o SDK .NET 10 local e dependências npm. O publish cria um
motor autocontido: o jogador não instala .NET nem ferramentas de desenvolvimento.
Tauri usa o WebView2 do Windows. O alvo de distribuição validado é Windows x64.

## Contrato

O frontend envia teclas físicas e recebe frames completos. Rust aceita apenas
operações do jogo, valida a semente uint64 e serializa pedidos ao processo.
O protocolo usa JSON UTF-8 por stdin/stdout. O pacote não depende de servidor
HTTP; o endpoint Vite só existe durante desenvolvimento web em loopback.

A grade contém codepoint, fg, bg e negrito. Os tokens de paleta e os parâmetros
CRT vêm do Core. As sementes são strings decimais; o JavaScript não converte
uint64 para Number. Desenho e resize não avançam turnos.

O timeout de transporte é de 10 segundos. Falhas aparecem como erro e não
começam uma nova partida silenciosamente. Encerrar a janela mata e recolhe o
motor. O frontend não recebe permissão de shell ou acesso geral ao filesystem.

## Recursos

Fonte: `assets/fonts/unscii-16.hex`, bitmap 8×16 de viznut, domínio público.
A fonte é copiada para os assets públicos pelo Vite; a suite headless também
recebe uma cópia. Seu teste de cobertura falha se o recurso estiver ausente.
O ícone é gerado do glifo `@` usando tokens da paleta canônica.

## Distribuição

O build produz `ossuary.exe`, `ossuary-engine.exe` e o instalador NSIS em
`desktop/src-tauri/target/release/bundle/nsis/`. Distribuir o instalador ou os
dois executáveis lado a lado. O runtime autocontido pode extrair bibliotecas
nativas no diretório temporário do usuário na primeira execução.

## Verificação

A suite inclui geração, combate, itens, economia, RPG e vitória; fluxo de
painéis, escolhas, mira, viagem, loja, opções e reinício; fonte/DPI/uint64;
IPC do motor publicado e comparação determinística de frames; validação e
transporte Rust. Capturas do frontend real ficam em `docs/shots/`.

Save/load completo e novos sistemas de gameplay são evoluções separadas.
O motor C# é a implementação definitiva das regras, sem cópia concorrente
em outra linguagem. A divisão de responsabilidades está em
[arquitetura.md](arquitetura.md).

## Inicialização nativa

Após empacotar, `desktop.ps1 build` inicia o executável com `--smoke-test`, em janela oculta. O teste exige fonte bitmap carregada, frame recebido por IPC e pixels desenhados no Canvas do WebView2. O aplicativo retorna código 0 ao confirmar; falha de inicialização retorna 2 após 20 segundos. O pipeline também impõe limite de 30 segundos e recolhe o processo em caso de timeout.


## Protocolo: save e título

Operações: `new`, `load`, `key`, `resize`, `display`, `frame`. `load` reconstrói a run
salva (semente + log de teclas) e devolve o frame. O frame traz `started` (há run em
andamento), `hasSave`, `saveInfo`, `toTitle` (um frame só, após "Main menu") e os volumes
`master`/`music`/`effects`. O Rust só repassa; a lógica de save, binds e menu vive no
Core e em `engine/Ossuary.Desktop` (`Session`, `SaveStore`).
