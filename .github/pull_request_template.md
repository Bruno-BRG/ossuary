## O quê e por quê

<!-- O que muda e qual problema resolve. Link para a issue, se houver. -->

## Tipo

- [ ] Correção de bug
- [ ] Mecânica / conteúdo (magia, item, monstro…)
- [ ] Visual / animação
- [ ] Documentação / tradução
- [ ] Ferramentas / testes

## Como testei

- [ ] `.\headless.ps1 test`
- [ ] `.\check.ps1`
- [ ] `.\headless.ps1 fx <magia>` / `dump panels` / captura de tela (se mexeu em visual)

## Checklist

- [ ] O Core continua sem depender de UI/plataforma (regra de ouro)
- [ ] Nada não determinístico na simulação (usei o `Rng` do jogo; efeitos visuais usam hash)
- [ ] Se a mudança altera o resultado de sementes, subi `SaveData.Version` e avisei aqui
- [ ] Campo novo no protocolo JSON atualizado nos três lados (se houver)
- [ ] Textos novos têm tradução PT (`Loc.cs` / `Loc.Spells.cs`)
- [ ] Atualizei `docs/` e `docs/a-fazer.md`
