# Alpha v0.1 — Ossuary

Aplicativo desktop Tauri 2 + Rust, terminal TypeScript/Canvas bitmap e motor
C#/.NET 10 autocontido. Os fontes definitivos estão em `engine/`, `desktop/`
e `assets/`; o pipeline e os testes usam essa estrutura.

## Implementado

- Geração de dungeon: 5 branches, aproximadamente 54 níveis e 6 estilos.
- Combate, FOV, pathfinding, monstros, fome, status, itens e equipamento.
- Overworld, regiões, estradas, relógio, viagens, encontros e cidades verticais (andares, porões, serviços, gente).
- Lojas, economia, seleção de compra/venda, papéis e progressão.
- Missão, amuleto final, morte permanente e vitória.
- HUD, inventário, personagem, histórico, descobertas, ajuda, mira e opções.
- Título com logo em blocos, semente opcional, 4 temas e CRT ajustável.
- Fonte unscii-16 8×16, escala inteira em pixels físicos e preferências persistidas.
- Empacotamento Windows x64 com motor embutido no instalador NSIS.

## Validação

73 testes headless (simulação + protocolo desktop) PASS, 8 testes
frontend/IPC PASS e 2 testes Rust. O protocolo é exercitado com o
executável autocontido em uma pasta vazia. Soak: 50 sementes × 500 turnos.
Capturas em `docs/shots/`.

Executar: `desktop.ps1 dev`. Distribuir: `desktop.ps1 build`.
Ver [build-teste.md](build-teste.md).

## Estado depois da rodada de acompanhamento

Todo o backlog de `a-fazer.md` foi implementado nesta rodada (tecla segurada, auto-explore, morgue/histórico/cemitério, modos e desafios, diário, conquistas, armadilhas, furtividade,
corrupção e mutações, companheiros, crafting, artefatos e conjuntos, magias, deuses, monstros e chefes por branch, facções, reputação/contratos/eventos, cofres e estilos novos, The Annex,
Trained, som, água animada e tiles quadrados). Salvamento: **versão 10**. Suíte headless: 79 testes de simulação + 5 de vitória + 28 de funcionalidades + fluxo desktop; vitest: terminal, áudio e água
(o teste de IPC empacotado só roda no Windows). Itens que ficaram como ideias futuras estão anotados dentro de cada entrada em *Feito* de `a-fazer.md`.

## Evolução de gameplay

Sistema RPG completo (raças, classes, magia, perks, itens, deuses, superfícies; ver rpg.md e balance.md). Save/load completo, novos conteúdos, animação de água e tiles de mapa
quadrados são evoluções futuras. `F5` mostra a semente inicial; não restaura
progresso de uma run. Estas funcionalidades não são requisitos do runtime.
