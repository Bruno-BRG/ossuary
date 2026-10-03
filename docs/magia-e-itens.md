# Magia, itens e animações

Este documento descreve o acervo de magias, livros, itens mágicos e itens únicos, e o sistema de
**animações** que faz cada magia aparecer no mundo (a bola de fogo *é* uma bola de fogo que voa e explode).
As tabelas abaixo foram geradas do código; a fonte da verdade são os arquivos citados em cada seção.

## Em números

| | |
|---|---|
| Magias | **262** em **8 escolas** (eram 39 em 6) |
| Livros | **39** (com nível de profundidade 1–5) |
| Varinhas / pergaminhos / poções que lançam magias | **38 / 34 / 18** |
| Armas / armaduras / elmos / luvas / botas / capas / escudos novos | 26 / 14 / 10 / 7 / 6 / 7 / 6 |
| Anéis / amuletos novos (agora **vestíveis**) | 24 / 12 |
| Afixos novos | 33 (18 prefixos, 15 sufixos) |
| Itens únicos novos | **43**, 4 conjuntos novos, 38 deles emprestam magias |
| Criaturas invocáveis novas | 21 |
| Efeitos de animação | 23 peças (`FxLib`) |

## Animações

O motor resolve a magia **na hora** e só *grava* como ela deve parecer; o front-end toca a gravação por cima do
quadro já pronto, sem pedir turno ao motor (mesma ideia da água animada).

```
Game.CastSpell ─▶ PlaySpellFx / Fx(...)   grava FxTimeline (passos de células no mapa)
Session.Draw   ─▶ BuildFx                 mapa → tela (câmera, tiles quadrados, tema), Frame.Fx
protocol.ts    ─▶ Frame.fx / fxMs         um array por passo: [célula, glifo, fg, bg] × N  (bg −1 = manter)
main.ts        ─▶ createFxPlayer          toca ~45 ms por passo; uma tecla nova corta; prefers-reduced-motion desliga
```

- **Puro dado** (`Fx.cs`): nada de Rng nem de estado de jogo; gravar nunca muda o que acontece (saves são replays).
  Efeitos usam `Shapes.Hash(x, y, salt)` para variar o desenho. `Game.FxEnabled` (ligado pelo `Session` fora de replays)
  evita custo em testes e *soak*.
- **Geometria compartilhada** (`Shapes`): a explosão cobre exatamente o quadrado Chebyshev que o dano atinge, o cone é o
  mesmo conjunto de células do cone de dano. O que você vê é o que foi atingido.
- **Cores** vêm de rampas por elemento (`Elem`: Arcane, Fire, Cold, Lightning, Necrotic, Holy, Poison, Nature, Shadow, Earth,
  Blood, Water, Mind, Wind) e passam por `Theme.Remap`, então temas âmbar/fósforo também funcionam.
- **Visibilidade**: só células à vista são desenhadas (`FxTimeline.Show`); sob um painel aberto a animação é descartada.
- **Vocabulário** (`FxLib`; devolve o passo em que termina): `Bolt` (projétil com rastro, glifo direcional com `'\0'`),
  `Beam`, `Zap` (raio denteado), `Chain`, `Drain`, `Wave`, `Cone`, `Burst`, `Nova`, `Implode`, `Cloud`, `Rain`, `Eruption`,
  `Shatter`, `Flash`, `Slash`, `Rise`, `Swirl`, `Pillar`, `Meteor`, `Mark`, `Teleport`, `Summon`.
- **Cada magia declara sua aparência**: `.Look(FxKind.Ball, Elem.Fire, 'o')`. `FxKind.Custom` = o código grava sozinho
  (cadeias e chuvas de golpes). Invocações gravam `Summon` em cada célula ocupada.
- **Além das magias**: varinhas, pergaminhos e poções (são magias), tiro de arco, molotov, armadilhas de fogo/alarme/teleporte,
  explosão de monstros e os ataques dos chefes (pancada do Stone Warden, inundação e raio do Drowned King, dreno do Annex Warden,
  sopro do Ashen Regent, corrente do Gaoler).
- **Ver sem janela**: `headless.ps1 fx fireball` imprime cada passo em ASCII; `fx list` lista as animações; `fx all` conta os passos de todas.
- **Limites conhecidos**: a magia já foi resolvida quando a animação toca (um inimigo morto some antes de o projétil chegar); o desenho
  cobre o glifo da célula (monstros e herói ficam escondidos por um instante). Uma tecla nova corta a animação em curso.

## Como uma magia é definida (`Magic/Spells*.cs`)

Uma magia é uma linha de dados com receita (`SpellDef`); a maioria não precisa de código novo.

```csharp
S("fireball", "Fireball", 3, E, 7, Area, 8, "Bursts where you aim…", 2).Look(FxKind.Ball, Elem.Fire, 'o')          // código próprio (legado)
S("cone-of-cold", "Cone of Cold", 4, E, 10, Cn, 5, "A cone of killing frost…", 5)
    .Dmg(Cold, 4, 6).Ride(Rider.Slow, 70, 8).Look(FxKind.Cone, Elem.Cold)                                       // receita
```

| Peça | O que faz |
|---|---|
| `Dmg(tipo, dados, lados, div, fixo, verbo)` | dano `(dados + nível/div)d lados + rank de Magia`; físico, fogo, gelo, raio, veneno, necrótico ou sagrado |
| `Ride(rider, %, turnos)` | efeito nos atingidos: **Burn, Slow, Fear, Sleep, Confuse, Blind, Stun, Root, Poison, Bleed, Weaken, Charm** (os de mente e corpo podem ser resistidos) |
| `Heal(d, l, fixo)` | cura (mais Sabedoria e nível) |
| `Aura(buff, turnos)` | buff em `SpellBuffs` (stats, CA, resistências, dado extra, roubo de vida, cura por turno, retaliação) |
| `Call(criatura, n, turnos)` | invoca aliados temporários |
| `Surf(superfície, turnos)` | água, gelo, fogo, óleo, grama |
| `Shove(n)` | empurra (n > 0) ou puxa (n < 0); bater na parede machuca |
| `Drain(%)` | cura parte do dano causado |
| `Chain(saltos)` / `Scatter(golpes, animação)` | cadeia de raios / golpes em alvos aleatórios à vista |
| `Taint(n)` | custa corrupção |
| `Spec("nome")` | efeito de código: piscar, trocar de lugar, banir, luz, achar armadilhas, arrombar, identificar, escavar, executar, assassinar, golpe de misericórdia, furtar, desarmar, remover maldição, restauração, cura em massa, comandar mortos-vivos, falar com animais… |

**Formas** (derivadas do alvo): `Single`, `Ball` (raio ao redor de uma célula), `Line` (perfura), `Cone` (novo alvo: mire uma direção),
`Nova` (ao redor de você), `Chain`, `Scatter`. **Riders novos** em monstros (`HeldTurns` perde a vez, `DotTurns`/`DotDmg` dano ao longo do
tempo, `VulnTurns` leva +25%) vivem em `Monster` e são aplicados em `Game.Magic.Recipes.cs`.

**Buffs** (`SpellBuffs.cs`, 47 linhas de dado): são `ItemMods` temporários somados em `Player.Gear` (então atributos, PV, Mp, resistências,
furtividade, poder mágico, roubo de vida e dado extra de arma funcionam como em itens e voltam ao normal quando acabam), mais cura por turno
(`Regen`) e retaliação (`Retaliate`: espinhos, aura sagrada, aura da morte). *Sanctuary* faz o monstro hesitar 70% das vezes; *Sense Life*
mostra criaturas pelas paredes.

**Painel** (`Shift+Z`): abas por escola (`←`/`→`), contagem por escola, ordenado por nível; letras a–z escolhem dentro da aba;
`PageUp`/`PageDown`/`Home`/`End` rolam; `◆` marca magias emprestadas por itens. Descrição em até duas linhas, alvo, dano e efeito.

**Falha e foco**: `FailPct` agora também desconta `ItemMods.SpellFocus`; `SpellPower` soma dano a toda magia.

## Catálogo de magias

Cada escola é de um tipo de personagem (qualquer um pode ler qualquer livro, mas o primeiro livro de cada um vem na bagagem):
**Mago** (Evocação, Conjuração, Alteração, Ilusão), **Necromante** (Necromancia), **Clérigo e Paladino** (Sagrada),
**Patrulheiro** (Natureza, livro inicial *a druid's handbook*) e **Ladino** (Sombra, livro inicial *a cutpurse's primer*).

### Evocação — 29 magias (Mago)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Respingo Ácido** (`acid-splash`) | 3 | criatura | Bolt Poison | Um bocado de ácido. Corrói a armadura: o alvo sofre mais de tudo por um tempo. |
| 1 | **Mãos Flamejantes** (`burning-hands`) | 3 | cone | Cone Fire | Um leque de chamas dos seus dedos, três casas de comprimento. |
| 1 | **Dardo de Brasa** (`ember-dart`) | 2 | criatura | Bolt Fire | Uma centelha de fogo. De vez em quando incendeia o alvo. |
| 1 | **Geladura** (`frostbite`) | 2 | criatura | Bolt Cold | Uma mordida de frio que atrasa o alvo. |
| 1 | **Míssil Mágico** (`magic-missile`) | 2 | criatura | Bolt Arcane | Raios de força que não erram. Os dados crescem com o nível. |
| 1 | **Toque Chocante** (`shocking-grasp`) | 2 | criatura | Zap Lightning | Raio pelo seu toque. Só adjacente; bate forte. |
| 1 | **Faísca** (`spark`) | 2 | criatura | Zap Lightning | Um estalo de raio. Rápido e barato. |
| 2 | **Raio Bifurcado** (`forked-lightning`) | 5 | criatura | Custom Lightning | Um raio que se divide e atinge mais uma criatura por perto. |
| 2 | **Raio de Gelo** (`frost-ray`) | 4 | criatura | Beam Cold | Um feixe de frio. Dano alto, exige linha livre. |
| 2 | **Rajada de Vento** (`gust-of-wind`) | 3 | cone | Wave Wind | Um golpe de ar que fere e empurra as coisas duas casas para trás. |
| 2 | **Lança de Gelo** (`ice-lance`) | 4 | criatura | Bolt Cold | Uma lança de gelo. Resfria o alvo e congela a água em que ele pisa. |
| 2 | **Raio Escaldante** (`scorching-ray`) | 5 | criatura | Beam Fire | Uma agulha de fogo branco. Incendeia o alvo. |
| 2 | **Estilhaço de Pedra** (`stone-shard`) | 4 | criatura | Bolt Earth | Uma pedra pontuda arremessada com força. |
| 2 | **Trovão** (`thunderclap`) | 4 | você | Nova Wind | Uma palma ensurdecedora: fere tudo a 2 casas e pode atordoar. |
| 3 | **Bola de Fogo** (`fireball`) | 7 | área | Ball Fire | Explode onde você mira e queima tudo a 2 casas. |
| 3 | **Dardo de Lava** (`lava-bolt`) | 7 | criatura | Bolt Fire | Um bocado de rocha derretida. Queima e deixa chamas no chão onde cai. |
| 3 | **Relâmpago** (`lightning-bolt`) | 6 | linha | Zap Lightning | Um raio que atravessa toda criatura na linha. |
| 3 | **Nuvem Venenosa** (`poison-cloud`) | 6 | área | Cloud Poison | Uma nuvem verde e podre. Tudo nela fica envenenado por alguns turnos. |
| 3 | **Jato de Vapor** (`steam-burst`) | 6 | área | Cloud Water | Ferve a água em vapor: dano alto em molhados a 2 casas, e a água some. No seco, só chia. |
| 4 | **Chamar Raios** (`call-lightning`) | 9 | você | Custom Lightning | Três raios caem do nada sobre criaturas à vista. |
| 4 | **Raio em Cadeia** (`chain-lightning`) | 10 | criatura | Custom Lightning | Atinge o alvo e salta para até três outros por perto. |
| 4 | **Cone de Gelo** (`cone-of-cold`) | 10 | cone | Cone Cold | Um cone de geada mortal, cinco casas de comprimento. Atrasa quem sobrevive. |
| 4 | **Tempestade de Gelo** (`ice-storm`) | 10 | área | Rain Cold | Granizo do tamanho de punhos, numa área larga. |
| 4 | **Leque Prismático** (`prismatic-spray`) | 10 | cone | Cone Mind | Um leque de luz colorida: fere e atordoa. |
| 4 | **Muralha de Fogo** (`wall-of-fire`) | 9 | área | Eruption Fire | Uma cruz em chamas no chão. Se espalha em mato e óleo; quem está nela pega fogo. |
| 5 | **Desintegrar** (`disintegrate`) | 16 | criatura | Beam Poison | Um raio verde que desfaz o que toca. |
| 5 | **Terremoto** (`earthquake`) | 15 | você | Eruption Earth | O chão sacode por 4 casas ao seu redor. Fere e atordoa tudo que está nele. |
| 5 | **Meteoro** (`meteor`) | 15 | área | Meteor Fire | Uma rocha em chamas vinda do nada. Devasta 3 casas ao redor. |
| 5 | **Explosão Solar** (`sunburst`) | 16 | área | Meteor Holy | Um sol cai: varre tudo a 3 casas e cega o que não matar. |

### Conjuração — 20 magias (Mago)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Criar Óleo** (`create-oil`) | 3 | área | Cloud Shadow | Unta o chão com óleo. Queima por muito tempo e faz os incautos escorregarem. Cuidado com tochas. |
| 1 | **Familiar** (`familiar`) | 3 | você | — | Chama uma fera pequena para lutar por você por um tempo. |
| 1 | **Lâmina Espectral** (`spectral-blade`) | 3 | você | — | Uma espada de luz fria luta ao seu lado por um tempo. |
| 2 | **Criar Água** (`create-water`) | 4 | área | Rain Water | Inunda o chão ao redor de um ponto. Molhados queimam menos e conduzem raios; o gelo congela tudo. |
| 2 | **Nuvem de Névoa** (`fog-cloud`) | 4 | você | Cloud Wind | Uma névoa densa: inimigos a 2 casas tateiam às cegas e você fica mais difícil de acertar. |
| 2 | **Passo de Fase** (`phase-step`) | 3 | célula | Teleport Arcane | Um salto curto através das paredes do mundo. |
| 2 | **Invocar Fera** (`summon-beast`) | 5 | você | — | Chama uma fera mais forte conforme você sobe de nível. |
| 2 | **Invocar Enxame** (`summon-swarm`) | 5 | você | — | Uma nuvem de morcegos atende ao seu chamado. |
| 2 | **Teia** (`web`) | 4 | área | Cloud Wind | Fios grudentos numa área pequena. O que toca fica preso por alguns turnos. |
| 3 | **Piscar** (`blink`) | 5 | célula | Teleport Arcane | Atravessa o espaço até um ponto que você vê. |
| 3 | **Chão Congelado** (`frozen-ground`) | 5 | área | Cloud Cold | O gelo se espalha pelo chão e quem pisa nele fica lento. |
| 3 | **Imagem Espelhada** (`mirror-image`) | 6 | você | — | Dois duplos seus tremeluzem ao lado, atraindo golpes feitos para você. |
| 3 | **Trocar de Lugar** (`swap-places`) | 6 | criatura | Teleport Mind | Você e o alvo trocam de posição num piscar. |
| 4 | **Banir** (`banish`) | 10 | criatura | Implode Shadow | Joga o alvo para longe, em outra parte do nível. |
| 4 | **Porta Dimensional** (`dimension-door`) | 8 | célula | Teleport Arcane | Um passo longo através das paredes do mundo. |
| 4 | **Invocar Elemental de Ar** (`summon-air-elemental`) | 10 | você | — | Um elemental de ar rodopiante atende você. |
| 4 | **Invocar Elemental de Terra** (`summon-earth-elemental`) | 10 | você | — | Um elemental de terra se arranca do chão para te servir. |
| 4 | **Invocar Elemental de Fogo** (`summon-fire-elemental`) | 10 | você | None Fire | Um elemental de fogo rasga o nada para te servir. |
| 4 | **Invocar Elemental de Água** (`summon-water-elemental`) | 10 | você | — | Um elemental de água se ergue, pingando, para te servir. |
| 4 | **Teletransporte** (`teleport`) | 9 | você | — | Joga você num lugar aleatório deste nível. |

### Alteração — 25 magias (Mago)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Achar Armadilhas** (`detect-traps`) | 2 | você | Nova Earth | Mostra toda armadilha a 10 casas. |
| 1 | **Arrombar** (`knock`) | 2 | você | Swirl Arcane | Destranca e abre toda porta trancada a 3 casas. |
| 1 | **Luz** (`light`) | 2 | você | Nova Holy | Um clarão mostra o chão ao redor. |
| 1 | **Armadura Arcana** (`mage-armor`) | 3 | você | Swirl Arcane | Placas invisíveis de força se assentam sobre você. (CA +4) |
| 1 | **Escudo** (`arcane-shield`) | 2 | você | Swirl Arcane | Um disco de força salta à sua frente. (CA +6, por pouco tempo) |
| 1 | **Proteção** (`ward`) | 2 | você | Swirl Arcane | Um escudo cintilante: CA +3 por um tempo. |
| 2 | **Borrão** (`blur`) | 4 | você | Swirl Wind | Seu contorno se desfaz. (evasão +4) |
| 2 | **Força** (`bulls-strength`) | 4 | você | Swirl Blood | Seus músculos incham. (For +3) |
| 2 | **Graça** (`cats-grace`) | 4 | você | Swirl Nature | Você se sente leve e rápido. (Des +3) |
| 2 | **Suportar Elementos** (`endure-elements`) | 4 | você | Swirl Water | Fogo e frio 30%, raio 20%, por bastante tempo. |
| 2 | **Prot. Fogo** (`fire-ward`) | 3 | você | Swirl Fire | Um véu fresco te envolve. (fogo 60%) |
| 2 | **Astúcia** (`foxs-cunning`) | 4 | você | Swirl Mind | Seus pensamentos se afiam. (Int +3) |
| 2 | **Prot. Gelo** (`frost-ward`) | 3 | você | Swirl Cold | Um véu morno te envolve. (frio 60%) |
| 2 | **Identificar** (`identify`) | 4 | você | Swirl Mind | Tudo que você carrega e veste mostra o que é. |
| 2 | **Percepção** (`owls-wisdom`) | 4 | você | Swirl Holy | Uma clareza calma te assenta. (Sab +3) |
| 2 | **Sentir a Vida** (`detect-monsters`) | 3 | você | Nova Mind | Por um tempo você sente toda coisa viva a 12 casas, através das paredes. |
| 2 | **Prot. Raio** (`storm-ward`) | 3 | você | Swirl Lightning | Seu cabelo se ergue e o raio escorrega. (raio 60%) |
| 3 | **Foco** (`arcane-focus`) | 5 | você | Swirl Arcane | O tecido da magia entra em foco. (poder mágico +3, menos falhas) |
| 3 | **Clarividência** (`clairvoyance`) | 7 | você | Nova Mind | O nível inteiro se abre na sua mente. |
| 3 | **Escavar** (`dig`) | 6 | linha | Beam Earth | Abre até seis casas de rocha escavável em linha. |
| 3 | **Aceleração** (`haste`) | 6 | você | Swirl Lightning | Você age duas vezes mais que o resto, por pouco tempo. |
| 3 | **Levitar** (`levitate`) | 5 | você | Rise Wind | Você flutua: armadilhas não te alcançam. |
| 3 | **Lentidão** (`slow`) | 5 | criatura | Mark Water | Reduz à metade a velocidade de uma criatura. As fortes resistem. |
| 4 | **Aumentado** (`enlarge`) | 9 | você | Swirl Blood | Você incha até uma vez e meia seu tamanho. (For +4, Con +3, +15 PV) |
| 4 | **Pele de Pedra** (`stone-skin`) | 9 | você | Swirl Earth | Sua pele endurece: CA +6 por bastante tempo. |

### Ilusão — 14 magias (Mago)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Leque de Cores** (`color-spray`) | 3 | cone | Cone Mind | Um leque de cores tonteantes, três casas de comprimento. |
| 1 | **Ofuscar** (`dazzle`) | 2 | criatura | Mark Holy | Um clarão que deixa o alvo cego e tropeçando. |
| 2 | **Embaralhar** (`befuddle`) | 3 | área | Cloud Mind | Um redemoinho de besteira ao redor de um ponto: quem está nele cambaleia. |
| 2 | **Confundir** (`confuse`) | 4 | criatura | Mark Mind | O alvo cambaleia ao acaso. |
| 2 | **Deslocado** (`displacement`) | 4 | você | Swirl Mind | Sua imagem escorrega um palmo de onde você está. |
| 2 | **Invisibilidade** (`invisibility`) | 5 | você | Swirl Shadow | Inimigos perdem você de vista e acertam pior. |
| 2 | **Sono** (`sleep`) | 4 | criatura | Mark Mind | Põe um inimigo vivo para dormir. Os fortes resistem; dano acorda. |
| 2 | **Sugestão** (`suggestion`) | 5 | criatura | Mark Mind | Uma ideia sussurrada: o alvo te serve por pouco tempo. |
| 3 | **Padrão Hipnótico** (`hypnotic-pattern`) | 7 | área | Cloud Mind | Um tecido de luz que põe para dormir quem está a 2 casas. |
| 3 | **Aterrorizar** (`terrify`) | 6 | você | Nova Shadow | Toda criatura a 4 casas vê seu pior medo. |
| 4 | **Enfeitiçar** (`charm`) | 10 | criatura | Mark Mind | Um inimigo vivo luta por você por um tempo. Difícil nos fortes. |
| 4 | **Pesadelo** (`nightmare`) | 9 | criatura | Mark Shadow | Um pesadelo acordado: fere, e o alvo foge aterrorizado. |
| 4 | **Assassino Fantasmal** (`phantasmal-killer`) | 10 | criatura | Mark Mind | O próprio medo do alvo toma forma e ataca. |
| 5 | **Feitiço em Massa** (`mass-charm`) | 16 | área | Cloud Mind | Uma onda de afeto por você: toda criatura a 3 casas pode se voltar contra os amigos. |

### Necromancia — 42 magias (Necromante)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Estilhaço de Osso** (`bone-shard`) | 3 | criatura | Bolt Earth | Uma lasca de osso lançada com um estalo. |
| 1 | **Toque Gélido** (`chill-touch`) | 2 | criatura | Slash Necrotic | Uma mão fria como a cova. Fere, e o alvo bate pior por um tempo. |
| 2 | **Cão de Ossos** (`bone-hound`) | 5 | você | — | Um cão de ossos costurados corre aos seus pés. |
| 2 | **Maldição da Fraqueza** (`curse-of-weakness`) | 4 | área | Cloud Shadow | Uma maldição murmurada sobre tudo ao redor de um ponto: sofrem mais de cada golpe. |
| 2 | **Enfraquecer** (`enfeeble`) | 4 | criatura | Beam Necrotic | Um raio cinza drena o alvo: ele sofre mais de cada golpe por um bom tempo. |
| 2 | **Frio da Cova** (`grave-chill`) | 4 | você | Swirl Necrotic | O frio do cemitério te envolve: necrótico 50%, gelo 30%. |
| 2 | **Sangrar Vida** (`life-tap`) | 1 | você | Implode Blood | Queima 8 dos seus PV em 8 de mana. |
| 2 | **Toque Vampírico** (`vampiric-touch`) | 4 | criatura | Drain Blood | Um toque que devolve tudo: você cura o que causa. |
| 2 | **Definhar** (`wither`) | 4 | criatura | Bolt Necrotic | Apodrece um membro. Fere e atrasa. |
| 3 | **Animar Carniçal** (`animate-ghoul`) | 7 | você | — | Um carniçal se ergue para te servir. Está sempre faminto. |
| 3 | **Sede de Sangue** (`bloodlust`) | 6 | você | Swirl Blood | Calor vermelho atrás dos olhos. (+2 acerto, +3 dano, 10% de roubo de vida) |
| 3 | **Lança de Osso** (`bone-spear`) | 7 | linha | Beam Earth | Uma lança de osso que atravessa toda criatura em linha. |
| 3 | **Comandar Mortos-Vivos** (`command-undead`) | 8 | criatura | Mark Necrotic | Um morto-vivo dobra o joelho para você por um tempo. |
| 3 | **Pacto Sombrio** (`dark-pact`) | 4 | você | Swirl Blood | Você assina. Algo assina de volta. (poder mágico +4) |
| 3 | **Aura da Morte** (`death-aura`) | 7 | você | Swirl Necrotic | Uma mortalha de morte fria paira em volta de você. |
| 3 | **Drenar Vida** (`drain-life`) | 5 | criatura | Drain Necrotic | Rouba vida: fere o alvo e cura você pela metade. Não funciona nos mortos. |
| 3 | **Pavor** (`dread`) | 7 | você | Nova Shadow | Uma onda de pavor: toda criatura a 5 casas pode fugir. |
| 3 | **Fingir de Morto** (`feign-death`) | 6 | você | Cloud Necrotic | Você fica imóvel e frio. Tudo por perto perde você de vista. |
| 3 | **Amarra da Cova** (`gravebind`) | 6 | área | Eruption Necrotic | Mãos mortas agarram quem está preso ao chão: criaturas a 2 casas ficam presas. |
| 3 | **Hemorragia** (`hemorrhage`) | 6 | criatura | Bolt Blood | Abre as veias do alvo à distância: ele sangra por um tempo. |
| 3 | **Ossificar** (`ossify`) | 5 | você | Swirl Earth | O osso cobre sua pele: CA +4 por um tempo. O Ossuary leva um pouco de você por isso. |
| 3 | **Dardo da Peste** (`plague-bolt`) | 6 | criatura | Bolt Poison | Um dardo verde e gorduroso. O alvo adoece por um tempo. |
| 3 | **Erguer Esqueleto** (`raise-skeleton`) | 6 | você | — | Um esqueleto arranha o chão para te servir. |
| 3 | **Remodelar Carne** (`reshape-flesh`) | 8 | você | Implode Blood | Pede um presente ao Ossuary. Você ganha uma mutação, e ele ganha uma parte de você. |
| 3 | **Explosão Pútrida** (`rotting-burst`) | 7 | área | Ball Necrotic | Uma bolha de podridão estoura ao redor de um ponto. |
| 3 | **Chuva de Crânios** (`skull-barrage`) | 7 | você | Custom Necrotic | Três crânios gritando voam sobre criaturas à vista. |
| 3 | **Profano** (`unholy-vigor`) | 6 | você | Swirl Necrotic | Seus golpes voltam mais quentes do que saíram. (+1d4 necrótico, 15% de roubo de vida) |
| 4 | **Lamento da Banshee** (`banshee-wail`) | 9 | você | Nova Shadow | Um grito que fere e manda os vivos correr. 4 casas. |
| 4 | **Golem de Ossos** (`bone-golem`) | 11 | você | — | Um golem de cem esqueletos, amarrados com arame, te atende. |
| 4 | **Nuvem Mortal** (`cloudkill`) | 9 | área | Cloud Poison | Uma névoa verde assassina, três casas de largura. |
| 4 | **Contágio** (`contagion`) | 9 | área | Cloud Poison | Uma doença que gruda. Tudo perto do ponto fica envenenado por muito tempo. |
| 4 | **Onda da Morte** (`death-wave`) | 10 | você | Nova Necrotic | Um anel de morte varre 3 casas ao seu redor. |
| 4 | **Medo** (`fear`) | 8 | criatura | Mark Shadow | O alvo foge aterrorizado. Mentes vazias não sentem medo. |
| 4 | **Dardo de Tutano** (`marrow-bolt`) | 7 | criatura | Bolt Necrotic | Um espeto de tutano frio como a cova. Bate forte; o Ossuary leva um pouco de você. Não afeta mortos. |
| 4 | **Erguer Aparição** (`raise-wight`) | 10 | você | — | Uma aparição em cota podre sai do escuro e se ajoelha. |
| 4 | **Sugar Alma** (`siphon-soul`) | 10 | criatura | Drain Necrotic | Puxa um pedaço da alma do alvo para dentro de você. |
| 5 | **Exército de Ossos** (`army-of-bones`) | 14 | você | — | Três esqueletos se erguem para te guardar. |
| 5 | **Dedo da Morte** (`finger-of-death`) | 15 | criatura | Beam Necrotic | Desfaz os vivos com um apontar de mão. |
| 5 | **Palavra de Morte** (`power-word-kill`) | 18 | criatura | Mark Necrotic | Uma palavra. O que está muito ferido morre; o que não está leva um golpe enorme. |
| 5 | **Ritual de Sangue** (`ritual-of-blood`) | 8 | você | Implode Blood | Corte-se por um quarto da sua vida; encha a mana até a borda. |
| 5 | **Ceifar Almas** (`soul-reap`) | 15 | você | Nova Necrotic | Tudo vivo a 5 casas entrega um pedaço da alma; você fica com um quarto. |
| 5 | **Invocar Espectro** (`summon-wraith`) | 14 | você | — | Um espectro, preso a você por um tempo, atravessa a parede. |

### Sagrada — 43 magias (Clérigo e Paladino)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Abençoar** (`bless`) | 3 | você | Swirl Holy | Uma mão mais firme: +2 para acertar por bastante tempo. |
| 1 | **Ordem** (`command`) | 2 | criatura | Mark Holy | Uma única palavra, numa voz que não é sua: 'fuja'. |
| 1 | **Curar Ferimentos** (`cure-wounds`) | 3 | você | Rise Holy | Fecha sua carne: 2d6 mais Sabedoria e nível. |
| 1 | **Luz Sagrada** (`holy-light`) | 2 | você | Nova Holy | Uma luz suave mostra o chão ao redor. |
| 1 | **Remendo Menor** (`minor-mending`) | 2 | você | Rise Nature | Uma pequena misericórdia: 1d8 mais um pouco de Sabedoria. |
| 1 | **Chama Sagrada** (`sacred-flame`) | 2 | criatura | Pillar Holy | A luz cai sobre o alvo como uma mão. Os mortos-vivos sofrem o dobro. |
| 1 | **Escudo da Fé** (`shield-of-faith`) | 3 | você | Swirl Holy | A fé se ergue como muralha: CA +4 por um tempo. |
| 2 | **Luz Ofuscante** (`blinding-light`) | 4 | cone | Cone Holy | Um cone de luz branca que deixa as vítimas cegas. |
| 2 | **Purificar Corpo** (`cleanse`) | 3 | você | Rise Water | Queima veneno, confusão, cegueira e visões. |
| 2 | **Favor Divino** (`divine-favor`) | 4 | você | Swirl Holy | +3 para acertar, +2 de dano e um dado de fogo sagrado nos golpes, por um tempo. |
| 2 | **Heroísmo** (`heroism`) | 4 | você | Swirl Blood | A coragem é uma brasa no peito. (+3 acerto, For +1) |
| 2 | **Proteção contra o Mal** (`protection-from-evil`) | 4 | você | Swirl Holy | Um círculo de fogo branco: CA +3 e resistência necrótica 40%. |
| 2 | **Remédio** (`remedy`) | 3 | você | Rise Nature | Tira o veneno do sangue. |
| 2 | **Luz Abrasadora** (`searing-light`) | 4 | criatura | Beam Holy | Uma agulha de luz branca. Os mortos-vivos sofrem o dobro. |
| 2 | **Segundo Fôlego** (`second-wind`) | 3 | você | Rise Wind | Seu fôlego e suas pernas voltam: Vigor ao máximo. |
| 2 | **Punir** (`smite`) | 4 | criatura | Pillar Holy | Ira radiante. Os mortos-vivos sofrem o dobro. |
| 2 | **Arma Espiritual** (`spiritual-weapon`) | 4 | você | — | Um martelo de luz luta ao seu lado por um tempo. |
| 2 | **Expulsar Mortos-Vivos** (`turn-undead`) | 5 | você | Nova Holy | Todo morto-vivo à vista queima e foge. |
| 3 | **Consagrar** (`consecrate`) | 7 | área | Eruption Holy | Santifica um trecho de chão: a luz queima quem pisa nele. |
| 3 | **Fortitude** (`fortitude`) | 6 | você | Swirl Blood | Você se sente difícil de matar. (Con +3, +15 PV) |
| 3 | **Cura Maior** (`greater-heal`) | 8 | você | Rise Nature | Uma grande cura: 4d8 mais Sabedoria e nível. |
| 3 | **Martelo da Ira** (`hammer-of-wrath`) | 6 | criatura | Bolt Holy | Um martelo de luz. Pode atordoar. |
| 3 | **Imobilizar** (`hold-person`) | 6 | criatura | Mark Holy | Um alvo vivo fica rígido por alguns turnos. |
| 3 | **Lança Sagrada** (`holy-lance`) | 7 | linha | Beam Holy | Uma lança de luz que atravessa tudo em linha. |
| 3 | **Oração** (`prayer`) | 7 | você | Swirl Holy | A oração firma cada parte sua. (+2 acerto, +2 dano, CA +2) |
| 3 | **Regeneração** (`regeneration`) | 6 | você | Rise Nature | As feridas se fecham sozinhas: 2 PV por turno por um tempo. |
| 3 | **Remover Maldição** (`remove-curse`) | 6 | você | Rise Holy | Tira a maldição de tudo que você carrega. |
| 3 | **Santuário** (`sanctuary`) | 7 | você | Nova Holy | Cai um silêncio ao redor. Poucos erguerão a mão contra você. |
| 4 | **Poder Divino** (`divine-might`) | 9 | você | Swirl Holy | Uma força que não é sua enche seus braços. (For +4, +2 dano) |
| 4 | **Exorcizar** (`exorcise`) | 10 | você | Nova Holy | Queima e põe em fuga tudo a 5 casas. Os mortos-vivos sofrem o dobro. |
| 4 | **Golpe de Chamas** (`flame-strike`) | 10 | área | Pillar Fire | Uma coluna de fogo do céu, 3 casas de largura. |
| 4 | **Aura Sagrada** (`holy-aura`) | 9 | você | Swirl Holy | Um halo ofuscante te ilumina. (CA +3; necrótico 40%; queima quem te golpeia) |
| 4 | **Cura em Massa** (`mass-cure`) | 11 | você | Rise Holy | Luz curativa em você e em todo aliado a 5 casas. |
| 4 | **Purificar** (`purify`) | 10 | você | Rise Holy | Queima 15 pontos de corrupção do Ossuary em você. As mutações ficam. |
| 4 | **Nova Radiante** (`radiant-nova`) | 10 | você | Nova Holy | Um anel de luz explode 3 casas ao seu redor. |
| 4 | **Restauração** (`restoration`) | 10 | você | Rise Holy | Uma cura profunda: 3d8 mais Sabedoria, e todo mal em você acaba. |
| 4 | **Guardiões Espirituais** (`spirit-guardians`) | 10 | você | Nova Holy | Espíritos pálidos rodopiam ao redor, ferindo e atrasando tudo a 2 casas. |
| 4 | **Raio de Sol** (`sunbeam`) | 9 | linha | Beam Holy | Uma lança de sol em linha; o que não morre, fica cego. |
| 5 | **Bênção Angelical** (`angelic-blessing`) | 14 | você | Pillar Holy | Por bastante tempo você é um pouco mais do que é. |
| 5 | **Intervenção Divina** (`divine-intervention`) | 16 | você | Pillar Holy | O céu estende a mão: vida cheia, todo mal acabado e um silêncio ao redor. |
| 5 | **Anjo da Guarda** (`guardian-angel`) | 15 | você | — | Um guardião alado desce para lutar por você. |
| 5 | **Julgamento** (`judgement`) | 16 | área | Pillar Holy | Um pilar de fogo branco cai: tudo a 3 casas é julgado. |
| 5 | **Reviver** (`revive`) | 15 | você | Pillar Holy | Prende sua alma: a próxima morte em 300 turnos é desfeita. |

### Natureza — 50 magias (Patrulheiro)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Antídoto** (`antidote`) | 2 | você | Rise Nature | Uma raiz amarga que acaba com o veneno. |
| 1 | **Pele de Casca** (`barkskin`) | 3 | você | Swirl Nature | Sua pele se enrijece em casca. (CA +4) |
| 1 | **Emplasto de Ervas** (`herbal-poultice`) | 2 | você | Rise Nature | Folhas amassadas numa ferida: 1d8. |
| 1 | **Marca do Caçador** (`hunters-mark`) | 2 | criatura | Mark Blood | Marca uma presa: ela sofre mais de cada golpe por bastante tempo. |
| 1 | **Enxame de Insetos** (`insect-swarm`) | 3 | área | Cloud Nature | Uma nuvem de moscas que picam. Ferroam e fazem as criaturas se debaterem. |
| 1 | **Cacete** (`shillelagh`) | 2 | você | Swirl Nature | Sua arma vibra como carvalho. (+2 acerto, +3 dano) |
| 1 | **Dardo de Espinho** (`thorn-dart`) | 2 | criatura | Bolt Nature | Um espinho envenenado lançado com força. |
| 1 | **Crescimento Selvagem** (`wild-growth`) | 2 | área | Eruption Nature | Grama brota ao redor de um ponto. Mato seco queima bem. |
| 2 | **Acalmar Feras** (`calm-beasts`) | 3 | você | Nova Nature | Toda fera a 4 casas se deita e dorme. |
| 2 | **Camuflagem** (`camouflage`) | 4 | você | Swirl Earth | Você toma as cores da pedra. (mais difícil de notar) |
| 2 | **Corrida do Guepardo** (`cheetah-sprint`) | 4 | você | Swirl Wind | Uma rajada curta de velocidade. |
| 2 | **Redemoinho de Poeira** (`dust-devil`) | 4 | área | Cloud Earth | Um redemoinho pequeno que bate, cega e empurra. |
| 2 | **Olho de Águia** (`eagle-eye`) | 3 | você | Swirl Wind | O mundo se afia. (+3 acerto) |
| 2 | **Enredar** (`entangle`) | 4 | área | Eruption Nature | Raízes e trepadeiras agarram tudo a 2 casas. |
| 2 | **Sementes de Fogo** (`fire-seeds`) | 5 | área | Ball Fire | Sementes que explodem em chamas onde caem. |
| 2 | **Lâmina de Fogo** (`flame-blade`) | 4 | você | Swirl Fire | Sua arma veste uma pele de fogo. (+1d6 fogo) |
| 2 | **Bagas Boas** (`goodberries`) | 3 | você | Rise Blood | Um punhado de bagas: um pouco de cura e um pouco de comida. |
| 2 | **Geada** (`hoarfrost`) | 4 | área | Rain Cold | Uma crosta de geada mortal num trecho de chão. |
| 2 | **Chicote de Raio** (`lightning-lash`) | 4 | criatura | Zap Lightning | Um chicote de raio vindo do céu. |
| 2 | **Fogo da Lua** (`moonfire`) | 4 | criatura | Pillar Water | Fogo branco e frio vindo de cima. Os mortos-vivos sofrem o dobro. |
| 2 | **Jato de Veneno** (`poison-spray`) | 4 | cone | Cone Poison | Um jato de veneno de três casas. |
| 2 | **Rejuvenescer** (`rejuvenate`) | 4 | você | Rise Nature | Cura 2d6 agora e um pouco a cada turno depois. |
| 2 | **Falar com Animais** (`speak-with-animals`) | 4 | criatura | Mark Nature | Uma fera escuta e te segue por um tempo. |
| 2 | **Espinhos do Mato** (`spike-growth`) | 4 | área | Eruption Nature | Espinhos rasgam quem cruza a área e atrasam. |
| 2 | **Invocar Falcão** (`summon-hawk`) | 4 | você | — | Um falcão espiritual mergulha sobre seus inimigos. |
| 2 | **Espinhos** (`thorns`) | 4 | você | Swirl Nature | Espinhos furam sua pele; quem te acerta sangra por isso. |
| 2 | **Salto Selvagem** (`wild-leap`) | 3 | célula | Teleport Nature | Um pulo que leva você cinco casas. |
| 3 | **Vigor** (`bears-endurance`) | 5 | você | Swirl Earth | Você se sente teimoso como um urso. (Con +4, +10 PV) |
| 3 | **Trepadeiras Sufocantes** (`choking-vines`) | 7 | criatura | Eruption Nature | Uma criatura presa e apertada. |
| 3 | **Comunhão com a Natureza** (`commune`) | 5 | você | Nova Nature | A terra diz o que há por perto: o mapa a 14 casas e toda armadilha nele. |
| 3 | **Vendaval** (`gale`) | 6 | cone | Wave Wind | Um vento uivante que bate e joga criaturas três casas para trás. |
| 3 | **Chuva de Granizo** (`sleet-storm`) | 6 | área | Rain Water | Granizo numa área larga: fere, atrasa e gela o chão. |
| 3 | **Javali Espiritual** (`spirit-boar`) | 7 | você | — | Um javali espiritual avança sobre seus inimigos. |
| 3 | **Aranha Espiritual** (`giant-spider`) | 7 | você | — | Uma aranha espiritual, toda patas e veneno, te serve. |
| 3 | **Lobos Espirituais** (`spirit-wolves`) | 8 | você | — | Dois lobos de luz pálida caçam por você. |
| 3 | **Nuvem de Esporos** (`spore-cloud`) | 6 | área | Cloud Nature | Uma nuvem de esporos: queima, e a mente vai junto. |
| 3 | **Espetos de Pedra** (`stone-spikes`) | 6 | cone | Cone Earth | Espetos de pedra saem num cone de quatro casas. |
| 3 | **Tremor** (`tremor`) | 7 | você | Eruption Earth | O chão estremece 3 casas ao seu redor: fere e pode atordoar. |
| 3 | **Dardo de Veneno** (`venom-bolt`) | 6 | criatura | Bolt Poison | Um dardo de veneno concentrado. O veneno demora a passar. |
| 4 | **Forma de Urso** (`bear-form`) | 10 | você | Swirl Earth | Seus ombros se alargam e as mãos viram garras. (For +5, Con +4, +20 PV, CA +3) |
| 4 | **Muralha de Sarças** (`briar-wall`) | 9 | área | Eruption Nature | Uma muralha de espinhos duros como ferro brota e prende o que pega. |
| 4 | **Forma de Águia** (`eagle-form`) | 9 | você | Swirl Wind | O ar te leva. (Des +4, evasão +4) |
| 4 | **Chuva Curativa** (`healing-rain`) | 10 | você | Rain Water | Uma chuva morna que cura você e todo aliado a 5 casas. |
| 4 | **Deslizamento de Pedras** (`rockslide`) | 9 | área | Rain Earth | Uma chuva de pedregulhos vinda de cima. |
| 4 | **Invocar Urso** (`summon-bear`) | 10 | você | — | Um urso espiritual lumbra para o seu lado. |
| 4 | **Tornado** (`tornado`) | 9 | área | Cloud Wind | Um funil uivante: esmaga, empurra e cega. |
| 4 | **Forma de Lobo** (`wolf-form`) | 9 | você | Swirl Shadow | Você cai de quatro, pernas longas e olhos brilhantes. (For +3, Des +3, CA +2) |
| 5 | **Tempestade de Raios** (`lightning-storm`) | 15 | você | Custom Lightning | Cinco raios em cinco criaturas à vista. |
| 5 | **Chuva de Estrelas** (`starfall`) | 16 | você | Custom Arcane | Quatro estrelas caem sobre quatro criaturas à vista. |
| 5 | **Ente** (`treant`) | 15 | você | — | Uma árvore velha caminha. |

### Sombra — 39 magias (Ladino)

| Nv | Magia | Mp | Alvo | Animação | O que faz |
|---|---|---|---|---|---|
| 1 | **Pó Cegante** (`blinding-powder`) | 2 | cone | Cone Earth | Um punhado de pimenta e cinza na cara de tudo à sua frente. |
| 1 | **Praga** (`hex`) | 2 | criatura | Mark Shadow | Uma praga murmurada: os pés do alvo se arrastam. |
| 1 | **Dardo Envenenado** (`poison-dart`) | 2 | criatura | Bolt Poison | Um dardinho, muito veneno. |
| 1 | **Dardo de Sombra** (`shadow-bolt`) | 2 | criatura | Bolt Shadow | Um dardo de escuridão fria. Não afeta os mortos. |
| 1 | **Chave-Mestra** (`skeleton-key`) | 2 | você | Swirl Wind | Destranca toda porta trancada a 5 casas. |
| 1 | **Facas de Arremesso** (`throwing-knives`) | 2 | criatura | Bolt Wind | Um movimento de pulso: três facas que você não tinha um instante atrás. |
| 1 | **Lâmina Venenosa** (`venom-blade`) | 3 | você | Swirl Poison | Sua arma pinga veneno. (+1d6 veneno) |
| 2 | **Abrolhos** (`caltrops`) | 3 | área | Eruption Earth | Um punhado de espinhos de ferro: fere e atrasa. |
| 2 | **Manto de Sombras** (`cloak-of-shadows`) | 4 | você | Swirl Shadow | A sombra se junta a você: muito mais difícil de notar, evasão +2. |
| 2 | **Aleijar** (`cripple`) | 4 | criatura | Slash Blood | Um corte no tendão: o alvo manca atrás de você. |
| 2 | **Desarmar Armadilhas** (`disarm-traps`) | 3 | você | Nova Earth | Desarma toda armadilha a 3 casas. |
| 2 | **Escuridão** (`gloom`) | 3 | área | Cloud Shadow | Um trecho de breu: criaturas nele lutam cegas e sofrem mais. |
| 2 | **Dedos Leves** (`light-fingers`) | 3 | criatura | Slash Wind | Surrupia algumas moedas de tudo que você toca. |
| 2 | **Marcado para Morrer** (`mark-for-death`) | 3 | criatura | Mark Blood | Uma presa marcada: sofre mais de cada golpe por bastante tempo. |
| 2 | **Espeto Mental** (`mind-spike`) | 4 | criatura | Bolt Mind | Uma agulha de pensamento cravada na cabeça do alvo. |
| 2 | **Golpe de Sombra** (`shade-strike`) | 4 | cone | Cone Shadow | Um cone de sombra viva que morde e assusta. |
| 2 | **Passo de Sombra** (`shadow-step`) | 4 | célula | Teleport Shadow | De uma sombra para outra, a seis casas. |
| 2 | **Bomba de Fumaça** (`smoke-bomb`) | 4 | você | Cloud Shadow | Uma explosão de fumaça preta: inimigos a 2 casas ficam cegos e você some de vista. |
| 2 | **Cuspe Venenoso** (`venom-spit`) | 4 | cone | Cone Poison | Um jato de veneno entre os dentes. |
| 3 | **Olhar Aterrador** (`dread-gaze`) | 6 | criatura | Mark Shadow | Um olhar que mostra ao alvo a própria morte. |
| 3 | **Esvanecido** (`fade`) | 6 | você | Swirl Shadow | Você é difícil de olhar, mais difícil de acertar. (evasão +5) |
| 3 | **Chuva de Facas** (`knife-flurry`) | 7 | você | Custom Wind | Três facas acham, cada uma, uma criatura à vista. |
| 3 | **Chicote da Noite** (`night-whip`) | 6 | linha | Beam Shadow | Um chicote de escuridão que açoita tudo em linha. |
| 3 | **Lâmina de Sombra** (`shadow-blade`) | 5 | você | Swirl Shadow | Sua arma bebe a luz. (+1d6 necrótico, +2 acerto) |
| 3 | **Clone de Sombra** (`shadow-clone`) | 6 | você | — | Sua sombra se levanta do chão e luta. |
| 3 | **Garra Umbral** (`umbral-grasp`) | 6 | criatura | Eruption Shadow | Mãos de sombra se erguem e seguram o alvo. |
| 3 | **Mortalha Umbral** (`umbral-shroud`) | 6 | você | Swirl Shadow | Uma mortalha escura se dobra em volta de você. (CA +2, necrótico 30%, frio 20%) |
| 3 | **Gume Vampírico** (`vampiric-edge`) | 5 | você | Swirl Blood | Sua arma tem sede. (25% de roubo de vida) |
| 3 | **Véu de Escuridão** (`veil-of-darkness`) | 6 | você | Cloud Shadow | O escuro engrossa: tudo por perto perde você de vista. |
| 4 | **Assassinar** (`assassinate`) | 8 | criatura | Slash Blood | Um golpe mortal: dano em dobro em quem dorme ou está desprevenido. |
| 4 | **Tentáculos Negros** (`black-tentacles`) | 9 | área | Eruption Shadow | Tentáculos de escuridão esmagam e prendem tudo a 2 casas. |
| 4 | **Golpe de Misericórdia** (`coup-de-grace`) | 8 | criatura | Slash Blood | Acaba com o que está muito ferido; um golpe firme no resto. |
| 4 | **Acelerar Sombra** (`quicken-shadow`) | 9 | você | Swirl Shadow | Uma rajada curta de velocidade. |
| 4 | **Adaga da Alma** (`soul-dagger`) | 9 | criatura | Bolt Blood | Uma adaga de vida roubada; metade do que ela tira volta para você. |
| 4 | **Desaparecer** (`vanish`) | 9 | você | Cloud Shadow | Fumaça, um truque, e você não está aqui: invisível, e todos te perderam. |
| 4 | **Fenda do Vazio** (`void-rift`) | 10 | área | Implode Shadow | Um pequeno buraco no mundo, puxando tudo perto dele. |
| 5 | **Dominar** (`dominate`) | 16 | criatura | Mark Mind | Uma criatura viva se torna sua por bastante tempo. |
| 5 | **Eclipse** (`eclipse`) | 14 | você | Nova Shadow | A luz morre por 5 casas: tudo nela é ferido e cego. |
| 5 | **Caminhada Sombria** (`shadow-walk`) | 12 | você | Custom Shadow | Entre na escuridão e saia em outro ponto do nível, sem ser visto. |


## Livros

Magias vêm de livros; `r` num livro (longe de inimigos) tenta aprender cada magia que você ainda não sabe. Livros achados no chão respeitam a
profundidade (`LevelBuilder.PickBook`: nível máximo = 1 + profundidade/3); a livraria vende os de nível 1–2. Preço = nível² × 40 + 60.

| Livro | Nível de profundidade | Preço | Ensina |
|---|---|---|---|
| *a book of prayers* | 1 | 100 | Curar Ferimentos, Proteção, Abençoar, Purificar Corpo, Remendo Menor, Chama Sagrada, Escudo da Fé, Luz Sagrada, Ordem |
| *a charnel primer* | 1 | 100 | Toque Gélido, Estilhaço de Osso, Enfraquecer, Definhar, Sangrar Vida, Frio da Cova, Cão de Ossos, Toque Vampírico |
| *a cutpurse's primer* | 1 | 100 | Dardo de Sombra, Facas de Arremesso, Dardo Envenenado, Pó Cegante, Lâmina Venenosa, Praga, Chave-Mestra |
| *a druid's handbook* | 1 | 100 | Dardo de Espinho, Enxame de Insetos, Pele de Casca, Marca do Caçador, Emplasto de Ervas, Antídoto, Cacete, Crescimento Selvagem |
| *a hedge-wizard's notes* | 1 | 100 | Luz, Achar Armadilhas, Arrombar, Identificar, Sentir a Vida, Levitar, Escavar |
| *a primer of embers* | 1 | 100 | Dardo de Brasa, Mãos Flamejantes, Geladura, Faísca, Respingo Ácido, Estilhaço de Pedra, Trovão |
| *a spellbook* | 1 | 100 | Míssil Mágico, Toque Chocante, Proteção, Raio de Gelo, Familiar, Faísca |
| *a book of illusions* | 2 | 220 | Sono, Confundir, Invisibilidade, Enfeitiçar, Ofuscar, Leque de Cores, Embaralhar, Sugestão |
| *a book of shadows* | 2 | 220 | Sono, Piscar, Drenar Vida, Erguer Esqueleto, Medo, Toque Gélido |
| *a book of wards* | 2 | 220 | Proteção, Aceleração, Lentidão, Clarividência, Pele de Pedra, Armadura Arcana, Escudo, Borrão, Foco |
| *a book of whispers* | 2 | 220 | Manto de Sombras, Passo de Sombra, Bomba de Fumaça, Marcado para Morrer, Abrolhos, Aleijar, Espeto Mental, Cuspe Venenoso, Dedos Leves, Desarmar Armadilhas, Golpe de Sombra, Escuridão |
| *a psalter of light* | 2 | 220 | Luz Abrasadora, Favor Divino, Proteção contra o Mal, Heroísmo, Remédio, Segundo Fôlego, Luz Ofuscante, Arma Espiritual |
| *a ranger's almanac* | 2 | 220 | Enredar, Espinhos do Mato, Espinhos, Rejuvenescer, Bagas Boas, Olho de Águia, Camuflagem, Corrida do Guepardo, Lâmina de Fogo, Salto Selvagem |
| *a tome of conjuration* | 2 | 220 | Familiar, Invocar Fera, Criar Água, Criar Óleo, Piscar, Teletransporte, Lâmina Espectral, Passo de Fase, Teia, Nuvem de Névoa |
| *a treatise on resistance* | 2 | 220 | Prot. Fogo, Prot. Gelo, Prot. Raio, Suportar Elementos, Deslocado |
| *a bestiary of the unseen* | 3 | 420 | Invocar Enxame, Imagem Espelhada, Trocar de Lugar, Chão Congelado, Banir |
| *a book of grave-bargains* | 3 | 420 | Profano, Pacto Sombrio, Comandar Mortos-Vivos, Animar Carniçal, Sede de Sangue, Fingir de Morto, Aura da Morte |
| *a book of weather* | 3 | 420 | Geada, Redemoinho de Poeira, Chicote de Raio, Sementes de Fogo, Jato de Veneno, Fogo da Lua, Vendaval, Chuva de Granizo |
| *a breviary of the faithful* | 3 | 420 | Regeneração, Santuário, Oração, Fortitude, Remover Maldição, Martelo da Ira, Lança Sagrada, Consagrar, Imobilizar |
| *a manual of the body* | 3 | 420 | Força, Graça, Astúcia, Percepção, Aumentado, Suportar Elementos |
| *a manual of the knife* | 3 | 420 | Lâmina de Sombra, Gume Vampírico, Clone de Sombra, Garra Umbral, Véu de Escuridão, Chuva de Facas, Esvanecido, Olhar Aterrador, Mortalha Umbral, Chicote da Noite |
| *a tome of evocation* | 3 | 420 | Míssil Mágico, Raio de Gelo, Lança de Gelo, Bola de Fogo, Jato de Vapor, Relâmpago, Muralha de Fogo, Raio Escaldante, Rajada de Vento, Raio Bifurcado, Nuvem Venenosa, Dardo de Lava |
| *the green psalter* | 3 | 420 | Falar com Animais, Acalmar Feras, Invocar Falcão, Lobos Espirituais, Javali Espiritual, Aranha Espiritual |
| *the rotted codex* | 3 | 420 | Dardo da Peste, Explosão Pútrida, Hemorragia, Lança de Osso, Pavor, Maldição da Fraqueza, Amarra da Cova, Chuva de Crânios |
| *a book of mercy* | 4 | 700 | Punir, Expulsar Mortos-Vivos, Cura Maior, Purificar, Reviver, Restauração, Cura em Massa |
| *a codex of beast-shapes* | 4 | 700 | Vigor, Forma de Lobo, Forma de Urso, Forma de Águia, Invocar Urso |
| *a codex of storms* | 4 | 700 | Relâmpago, Bola de Fogo, Muralha de Fogo, Raio em Cadeia, Meteoro, Chamar Raios, Tempestade de Gelo, Cone de Gelo |
| *a folio of glamours* | 4 | 700 | Deslocado, Padrão Hipnótico, Aterrorizar, Assassino Fantasmal, Pesadelo, Feitiço em Massa |
| *a grimoire of the dead* | 4 | 700 | Erguer Esqueleto, Ossificar, Remodelar Carne, Drenar Vida, Dardo de Tutano, Medo, Dedo da Morte, Exército de Ossos |
| *a missal of wrath* | 4 | 700 | Aura Sagrada, Golpe de Chamas, Nova Radiante, Exorcizar, Raio de Sol, Poder Divino, Guardiões Espirituais |
| *the assassin's testament* | 4 | 700 | Assassinar, Tentáculos Negros, Fenda do Vazio, Desaparecer, Golpe de Misericórdia, Adaga da Alma, Acelerar Sombra |
| *the black litany* | 4 | 700 | Contágio, Nuvem Mortal, Sugar Alma, Onda da Morte, Lamento da Banshee, Erguer Aparição, Golem de Ossos |
| *the book of four winds* | 4 | 700 | Invocar Elemental de Fogo, Invocar Elemental de Água, Invocar Elemental de Terra, Invocar Elemental de Ar, Porta Dimensional, Banir |
| *the verdant grimoire* | 4 | 700 | Dardo de Veneno, Nuvem de Esporos, Trepadeiras Sufocantes, Tremor, Comunhão com a Natureza, Espetos de Pedra, Deslizamento de Pedras, Muralha de Sarças, Chuva Curativa, Tornado |
| *the annals of ruin* | 5 | 1060 | Leque Prismático, Desintegrar, Explosão Solar, Terremoto, Cone de Gelo, Chamar Raios, Meteoro |
| *the book of last things* | 5 | 1060 | Julgamento, Anjo da Guarda, Bênção Angelical, Intervenção Divina, Restauração |
| *the book of the long night* | 5 | 1060 | Eclipse, Caminhada Sombria, Dominar, Desaparecer, Assassinar |
| *the last rite* | 5 | 1060 | Ceifar Almas, Palavra de Morte, Invocar Espectro, Ritual de Sangue, Dedo da Morte, Exército de Ossos |
| *the wild hunt* | 5 | 1060 | Tempestade de Raios, Chuva de Estrelas, Ente, Invocar Urso, Tornado |

## Itens

### Bases novas (`Items/Catalogue.More.cs`, efeitos em `Items/ItemEffects.cs`)

- **Armas** (26): club, hand axe, stiletto, main gauche, javelin, ashwood staff, kris, rapier, falchion, bastard sword, morning star, flanged mace, war pick, glaive, pike, halberd, druid's crook, greatsword, great axe, maul, dwarven waraxe, katana, wizard's staff, bone staff, staff of the faithful, elven blade.
- **Armaduras** (14): padded armour, studded leather, brigandine, mage's robe, druid's vestments, priest's vestments, banded mail, necromancer's shroud, shadowsilk tunic, half plate, full plate, archmage's robe, mithril shirt, dragonhide armour.
- **Elmos** (10): coif of mail, circlet, wizard's hat, hood of shadows, horned helm, visored helm, skull cap of the dead, winged helm, laurel of the sage, crown of thorns.
- **Luvas** (7): gloves of dexterity, gloves of spellcasting, thieves' gloves, mage's mitts, gauntlets of the faithful, gauntlets of ogre power, bracers of defence.
- **Botas** (6): sandals of the wind, boots of striding, boots of the mage, boots of elvenkind, boots of the north, boots of fire walking.
- **Capas** (7): wolf pelt, cloak of protection, cloak of the mage, cloak of the bat, cloak of fortitude, cloak of resistance, cloak of shadows.
- **Escudos** (6): kite shield, tower shield, bone shield, rune shield, mirror shield, aegis of the faithful.
- **Anéis** (24): silver band, gold band, bone ring, jade ring, ring of fire resistance, ring of frost resistance, ring of storm resistance, ring of poison resistance, ring of the grave, ring of accuracy, ring of evasion, ring of stealth, ring of might, ring of flames, ring of frost, ring of sparks, ring of the mage, ring of focus, ring of vitality, ring of intellect, ring of insight, ring of spell power, ring of vampirism, ring of the archmage.
- **Amuletos** (12): amulet of stealth, amulet of health, amulet of warding, amulet of vigor, amulet of the wolf, amulet of the hunter, amulet of faith, amulet of the grave, amulet of resistance, amulet of the sage, amulet of the magi, amulet of spell power.

`ItemEffects` dá números a uma base só por ela existir (robe do arquimago: +12 Mp, +2 poder mágico, +10% de foco). Afixos, encantamento e
artefatos somam por cima. O loot respeita **nível** (`LevelBuilder.PickDeep`: nível máximo = 2 + profundidade/3), então as coisas grandes aparecem fundo.

**Anéis e amuletos agora valem.** `Player.AccessoryMods` soma anéis e amuleto em `Gear` (e em acerto, dano, dado extra e CA). Amuletos podem ser
vestidos (`P`, ou `Enter` no inventário) e tirados com `R`; o *amulet of life saving* desfaz uma morte e se desfaz; o *amulet of ESP* mostra criaturas pelas paredes.

### Afixos novos (`Items/Affixes.cs`)

Prefixos novos (18): shocking, radiant, rotting, thundering, searing, holy, draining, masterwork, razor-edged, arcane, mage-woven, shadowed, grave-warded, mithril-lined, rune-etched, troll-hide, dragon-warded, fortified.
Sufixos novos (15): of the wolf, of the lion, of the sphinx, of stealth, of brilliance, of the archmage, of fire, of frost, of storms, of the grave, of vitality, of precision, of might, of the hunter, of slaying.

### Varinhas, pergaminhos e poções que são magias (`Items/ItemSpells.cs`)

Cada uma lança uma **magia de verdade** (com o mesmo efeito e a mesma animação), sem mana e sem falha, com um *poder* mínimo de nível de conjurador
(`CastFromItem`). Varinha gasta uma carga; pergaminho e poção se gastam. Mirar e cancelar **não** gasta nada. As antigas (luz, golpe, frio, fogo, raio,
cavar, teletransporte) mantêm o nome e agora usam a mira e as animações. O nível da tabela governa em que profundidade aparecem.

**Varinhas (38)**

| Item | Lança | Poder | Preço | Nível |
|---|---|---|---|---|
| wand of acid | Respingo Ácido (`acid-splash`) | 5 | 150 | 1 |
| wand of confusion | Confundir (`confuse`) | 6 | 200 | 1 |
| wand of entangling | Enredar (`entangle`) | 6 | 200 | 1 |
| wand of magic missiles | Míssil Mágico (`magic-missile`) | 6 | 150 | 1 |
| wand of searing light | Luz Abrasadora (`searing-light`) | 6 | 200 | 1 |
| wand of shadows | Dardo de Sombra (`shadow-bolt`) | 5 | 150 | 1 |
| wand of sleep | Sono (`sleep`) | 6 | 200 | 1 |
| wand of smiting | Punir (`smite`) | 6 | 200 | 1 |
| wand of sparks | Faísca (`spark`) | 4 | 100 | 1 |
| wand of webs | Teia (`web`) | 6 | 200 | 1 |
| wand of blinking | Piscar (`blink`) | 8 | 300 | 2 |
| wand of bones | Lança de Osso (`bone-spear`) | 7 | 300 | 2 |
| wand of cold | Raio de Gelo (`frost-ray`) | 7 | 200 | 2 |
| wand of digging | Escavar (`dig`) | 6 | 200 | 2 |
| wand of draining | Drenar Vida (`drain-life`) | 7 | 300 | 2 |
| wand of fear | Medo (`fear`) | 7 | 250 | 2 |
| wand of fire | Raio Escaldante (`scorching-ray`) | 7 | 200 | 2 |
| wand of gales | Vendaval (`gale`) | 7 | 250 | 2 |
| wand of holding | Imobilizar (`hold-person`) | 7 | 300 | 2 |
| wand of light | Luz (`light`) | 5 | 100 | 2 |
| wand of lightning | Relâmpago (`lightning-bolt`) | 7 | 200 | 2 |
| wand of mending | Cura Maior (`greater-heal`) | 8 | 300 | 2 |
| wand of rot | Explosão Pútrida (`rotting-burst`) | 7 | 300 | 2 |
| wand of slowness | Lentidão (`slow`) | 6 | 220 | 2 |
| wand of striking | Estilhaço de Pedra (`stone-shard`) | 6 | 200 | 2 |
| wand of teleportation | Teletransporte (`teleport`) | 8 | 200 | 2 |
| wand of thunder | Trovão (`thunderclap`) | 6 | 250 | 2 |
| wand of venom | Dardo de Veneno (`venom-bolt`) | 8 | 300 | 2 |
| wand of banishment | Banir (`banish`) | 9 | 400 | 3 |
| wand of charming | Enfeitiçar (`charm`) | 8 | 350 | 3 |
| wand of fire seeds | Sementes de Fogo (`fire-seeds`) | 7 | 300 | 3 |
| wand of fireballs | Bola de Fogo (`fireball`) | 8 | 400 | 3 |
| wand of frost | Cone de Gelo (`cone-of-cold`) | 8 | 400 | 3 |
| wand of lava | Dardo de Lava (`lava-bolt`) | 8 | 350 | 3 |
| wand of the blizzard | Tempestade de Gelo (`ice-storm`) | 9 | 450 | 4 |
| wand of the storm | Raio em Cadeia (`chain-lightning`) | 9 | 450 | 4 |
| wand of meteors | Meteoro (`meteor`) | 12 | 800 | 5 |
| wand of ruin | Desintegrar (`disintegrate`) | 12 | 800 | 5 |

**Pergaminhos (34)**

| Item | Lança | Poder | Preço | Nível |
|---|---|---|---|---|
| scroll of clarity | Purificar Corpo (`cleanse`) | 6 | 100 | 1 |
| scroll of entangling | Enredar (`entangle`) | 6 | 100 | 1 |
| scroll of knocking | Arrombar (`knock`) | 5 | 50 | 1 |
| scroll of light | Luz (`light`) | 5 | 40 | 1 |
| scroll of protection | Armadura Arcana (`mage-armor`) | 6 | 80 | 1 |
| scroll of resistance | Suportar Elementos (`endure-elements`) | 6 | 100 | 1 |
| scroll of sensing | Sentir a Vida (`detect-monsters`) | 6 | 80 | 1 |
| scroll of trapfinding | Achar Armadilhas (`detect-traps`) | 6 | 60 | 1 |
| scroll of blinking | Piscar (`blink`) | 8 | 150 | 2 |
| scroll of fear | Aterrorizar (`terrify`) | 7 | 140 | 2 |
| scroll of frost | Raio de Gelo (`frost-ray`) | 7 | 120 | 2 |
| scroll of gales | Vendaval (`gale`) | 8 | 140 | 2 |
| scroll of healing | Cura Maior (`greater-heal`) | 8 | 160 | 2 |
| scroll of levitation | Levitar (`levitate`) | 6 | 100 | 2 |
| scroll of lightning | Relâmpago (`lightning-bolt`) | 8 | 140 | 2 |
| scroll of remove curse | Remover Maldição (`remove-curse`) | 6 | 150 | 2 |
| scroll of smiting | Punir (`smite`) | 7 | 120 | 2 |
| scroll of banishment | Banir (`banish`) | 9 | 240 | 3 |
| scroll of beasts | Invocar Fera (`summon-beast`) | 8 | 160 | 3 |
| scroll of bone servants | Erguer Esqueleto (`raise-skeleton`) | 8 | 180 | 3 |
| scroll of darkness | Véu de Escuridão (`veil-of-darkness`) | 8 | 160 | 3 |
| scroll of fireball | Bola de Fogo (`fireball`) | 8 | 180 | 3 |
| scroll of haste | Aceleração (`haste`) | 8 | 180 | 3 |
| scroll of invisibility | Invisibilidade (`invisibility`) | 8 | 180 | 3 |
| scroll of sanctuary | Santuário (`sanctuary`) | 8 | 200 | 3 |
| scroll of slumber | Padrão Hipnótico (`hypnotic-pattern`) | 8 | 180 | 3 |
| scroll of warding | Pele de Pedra (`stone-skin`) | 8 | 180 | 3 |
| scroll of elementals | Invocar Elemental de Fogo (`summon-fire-elemental`) | 10 | 300 | 4 |
| scroll of ice | Tempestade de Gelo (`ice-storm`) | 9 | 240 | 4 |
| scroll of mending | Restauração (`restoration`) | 9 | 240 | 4 |
| scroll of revival | Reviver (`revive`) | 10 | 400 | 4 |
| scroll of the tempest | Chamar Raios (`call-lightning`) | 9 | 260 | 4 |
| scroll of mass charm | Feitiço em Massa (`mass-charm`) | 12 | 500 | 5 |
| scroll of meteors | Meteoro (`meteor`) | 12 | 500 | 5 |

**Poções (18)**

| Item | Lança | Poder | Preço | Nível |
|---|---|---|---|---|
| potion of clarity | Purificar Corpo (`cleanse`) | 6 | 120 | 1 |
| potion of fire protection | Prot. Fogo (`fire-ward`) | 6 | 150 | 1 |
| potion of frost protection | Prot. Gelo (`frost-ward`) | 6 | 150 | 1 |
| potion of might | Força (`bulls-strength`) | 6 | 150 | 1 |
| potion of resistance | Suportar Elementos (`endure-elements`) | 6 | 150 | 1 |
| potion of storm protection | Prot. Raio (`storm-ward`) | 6 | 150 | 1 |
| potion of vigor | Segundo Fôlego (`second-wind`) | 6 | 100 | 1 |
| potion of cunning | Astúcia (`foxs-cunning`) | 6 | 180 | 2 |
| potion of grace | Graça (`cats-grace`) | 6 | 180 | 2 |
| potion of heroism | Heroísmo (`heroism`) | 6 | 180 | 2 |
| potion of mending | Restauração (`restoration`) | 8 | 180 | 2 |
| potion of regeneration | Regeneração (`regeneration`) | 6 | 200 | 2 |
| potion of shadows | Invisibilidade (`invisibility`) | 8 | 220 | 2 |
| potion of the bear | Vigor (`bears-endurance`) | 6 | 180 | 2 |
| potion of wisdom | Percepção (`owls-wisdom`) | 6 | 180 | 2 |
| potion of giants | Aumentado (`enlarge`) | 8 | 250 | 3 |
| potion of sanctuary | Santuário (`sanctuary`) | 8 | 240 | 3 |
| potion of stone skin | Pele de Pedra (`stone-skin`) | 8 | 220 | 3 |


### Itens únicos (`Items/Artifacts.More.cs`)

Cada único mora em um nível de um ramo, com uma **chance** (vários podem dividir um nível; cada um rola a sua; os 13 antigos continuam sempre lá).
Muitos **emprestam magias** enquanto estão em uso (`ArtifactDef.Grants`, aparecem com `◆` na lista de magias). Os de conjunto somam bônus em 2 e 3 peças
(*Panóplia da Rainha-Lich*, *Regalia do Invocador de Tempestades*, *Corte Verdejante*, *Cabala da Meia-Noite*); contam armas, armaduras, anéis e amuleto.

| Item único | Base | Onde (nível) | Chance | Poderes | Magias que empresta | Conjunto |
|---|---|---|---|---|---|---|
| **Gaoler's Keyring** | silver band | Masmorras 3 | 40% | +1 furtividade, +1 Des | Arrombar, Achar Armadilhas | — |
| **Ratcatcher's Cudgel** | club | Masmorras 2 | 50% | +2 dano, +1d3 veneno | — | — |
| **Mourner's Blade** | stiletto | Masmorras 5 | 45% | +1 Des, +1d4 necrótico | Assassinar | — |
| **Prisoner's Prayer** | amulet of faith | Masmorras 4 | 40% | +10 PV, +1 Sab | Santuário, Curar Ferimentos | — |
| **Heartwood Staff** | druid's crook | Masmorras 6 | 40% | +2 Sab, +8 Mp, +1 furtividade | Espinhos, Espinhos do Mato | Corte Verdejante |
| **Warden's Bulwark** | tower shield | Masmorras 7 | 40% | +1 Con, +1 CA, fogo 15% | — | — |
| **Nightglove** | thieves' gloves | Masmorras 4 | 35% | +1 Des, +2 furtividade | Dardo de Sombra, Praga | Cabala da Meia-Noite |
| **Stairwalker's Cloak** | cloak | Masmorras 10 | 40% | +1 Des, +2 esquiva, +6 Vigor | Piscar | — |
| **Pickaxe of the First Vein** | war pick | Minas de Dwarfdeep 3 | 45% | +1 For, +2 dano | Escavar | — |
| **Delver's Lamp-Helm** | dwarvish helm | Minas de Dwarfdeep 4 | 40% | fogo 20%, +8 PV | Luz | — |
| **Stonebinder's Gauntlets** | gauntlets | Minas de Dwarfdeep 5 | 40% | +2 For, +1 Con | Pele de Pedra | — |
| **Deepmaw Plate** | banded mail | Minas de Dwarfdeep 6 | 35% | +2 Con, +12 PV, frio 20% | — | — |
| **Ore Golem's Heart** | jade ring | Minas de Dwarfdeep 8 | 40% | +15 PV, +2 CA | Tremor | — |
| **Barkhide Vest** | druid's vestments | Minas de Dwarfdeep 2 | 45% | +1 Sab, +8 PV, veneno 20% | Rejuvenescer | Corte Verdejante |
| **Mantle of the Tempest** | cloak of the mage | Minas de Dwarfdeep 7 | 35% | +8 Mp, raio 30%, +1 esquiva | Faísca | Regalia do Invocador de Tempestades |
| **Rat King's Whiskers** | bone ring | Tocas 3 | 40% | +1 Des, +2 furtividade, veneno 30% | Invocar Enxame | — |
| **Scrap-King's Cleaver** | falchion | Tocas 4 | 40% | +1 For, +3 dano, -1 acerto | — | — |
| **Burrower's Boots** | boots of striding | Tocas 6 | 35% | +2 esquiva, +1 Des | Passo de Fase | — |
| **Mantle of Many Teeth** | cloak | Tocas 2 | 45% | +1 Con, +6 PV | Espinhos | — |
| **Gloves of Static** | gloves of spellcasting | Tocas 7 | 35% | +10 foco, raio 20% | Chicote de Raio | Regalia do Invocador de Tempestades |
| **Antlered Crown** | circlet | Tocas 8 | 35% | +1 Sab, +6 Mp, +1 furtividade | Pele de Casca, Enredar | Corte Verdejante |
| **Tidecaller's Trident** | trident | Cofres Afundados 3 | 45% | frio 25%, +1d6 frio | Chão Congelado | — |
| **Pearl of the Drowned** | amulet of resistance | Cofres Afundados 4 | 40% | +1 Sab, +10 Mp, frio 40% | Criar Água, Lança de Gelo | — |
| **Saltwhite Mail** | scale mail | Cofres Afundados 6 | 35% | frio 30%, +10 PV | Prot. Gelo | — |
| **Lantern of the Deep** | gold band | Cofres Afundados 7 | 40% | +1 furtividade, +8 foco | Sentir a Vida, Luz | — |
| **Midnight Hood** | hood of shadows | Cofres Afundados 8 | 35% | +2 furtividade, +2 esquiva | Esvanecido | Cabala da Meia-Noite |
| **Weeper's Hands** | leather gloves | Cofres Afundados 9 | 40% | +1 Des, +1 esquiva | Imobilizar | — |
| **Sunken Sceptre** | wizard's staff | Cofres Afundados 11 | 45% | +2 Int, +12 Mp, +2 poder mágico | Raio em Cadeia, Jato de Vapor | — |
| **Lich-Queen's Crown** | crown of thorns | Cofres Afundados 12 | 60% | +2 Int, +2 poder mágico, necrótico 30% | Enfraquecer, Definhar | Panóplia da Rainha-Lich |
| **Cindercrown** | circlet | Torre de Cinza 4 | 40% | +8 Mp, fogo 25%, +1 poder mágico | Dardo de Brasa, Prot. Fogo | — |
| **Stormcaller's Staff** | wizard's staff | Torre de Cinza 5 | 35% | +2 Int, +10 Mp, +2 poder mágico, raio 30% | Relâmpago, Chamar Raios | Regalia do Invocador de Tempestades |
| **Pyre-Knight's Sword** | bastard sword | Torre de Cinza 6 | 40% | fogo 30%, +1 For, +1d6 fogo | — | — |
| **Ashmonk's Beads** | amulet of the grave | Torre de Cinza 7 | 40% | necrótico 40%, +1 Sab, +6 Mp | Expulsar Mortos-Vivos, Luz Abrasadora | — |
| **Lich-Queen's Shroud** | necromancer's shroud | Torre de Cinza 10 | 40% | +1 Int, +10 Mp, +1 poder mágico | Frio da Cova, Aura da Morte | Panóplia da Rainha-Lich |
| **Wraithwalkers** | boots of elvenkind | Torre de Cinza 9 | 35% | +2 furtividade, +2 esquiva, necrótico 20% | Passo de Sombra | — |
| **Spire-Lord's Staff** | wizard's staff | Torre de Cinza 12 | 45% | +3 Int, +3 poder mágico, +14 Mp | Bola de Fogo, Meteoro | — |
| **Lich-Queen's Phylactery** | amulet of the grave | Torre de Cinza 13 | 40% | +14 Mp, +1 poder mágico, necrótico 30% | Sugar Alma | Panóplia da Rainha-Lich |
| **Regent's Tithe** | gold band | Torre de Cinza 14 | 40% | +12 PV, +2 poder mágico, fogo 30% | Muralha de Fogo | — |
| **Final Ember** | rune shield | Torre de Cinza 15 | 45% | +10 Mp, fogo 40%, +1 CA | Golpe de Chamas | — |
| **Auditor's Spectacles** | circlet | Anexo 1 | 50% | +2 Int, +1 Sab | Identificar, Achar Armadilhas | — |
| **Ledger of Debts** | bone ring | Anexo 2 | 45% | +15 foco, +6 Mp | Marcado para Morrer, Maldição da Fraqueza | — |
| **Dusk Daggers** | stiletto | Anexo 2 | 45% | +1 Des, +1 furtividade, +1d4 necrótico | Facas de Arremesso, Chuva de Facas | Cabala da Meia-Noite |
| **Plaguebearer's Mask** | skull cap of the dead | Tocas 5 | 35% | veneno 60%, necrótico 30%, +1 Con (relíquia: corrompe) | Dardo da Peste, Nuvem Mortal | — |

## Como adicionar

1. **Magia**: uma linha em `Magic/Spells.*.cs` (receita + `.Look`), a tradução em `Loc.Spells.cs`, e o id em algum livro (`Spells.Books.cs`).
   Um teste varre o catálogo: toda magia está num livro, tem animação, tem tradução, lança sem erro e muda alguma coisa.
2. **Buff**: uma linha em `SpellBuffs.cs` (+ tradução do rótulo e da mensagem).
3. **Criatura invocada**: `Ally(...)` no fim de `Entities/Bestiary.cs` (o ramo `~summon` impede que ela apareça sozinha).
4. **Varinha/pergaminho/poção**: uma linha em `ItemSpells.cs`.
5. **Item**: base em `Catalogue.More.cs`, números em `ItemEffects.cs`; **único**: uma linha em `Artifacts.More.cs`.
6. **Animação nova**: uma peça em `FxLib` e, se for de uma magia só, `FxKind.Custom` e gravar em `Game.Fx(...)`. Veja o resultado com `headless fx <id>`.

## Testes (`ArsenalTests.cs`, `desktop/src/fx.test.ts`)

Catálogo ligado (livros, buffs, invocações, tradução), **toda magia lança, muda o mundo e anima** com glifos do conjunto, geometria das animações,
animação chegando ao quadro (inclusive tiles quadrados e sob painel), riders, buffs, retaliação, especiais, painel por escola, tabelas de itens,
afixos, loot por profundidade, únicos (achados, conjuntos, magias emprestadas), varinhas/pergaminhos/poções (gastam, cancelar não gasta, poder mínimo).
No front-end, `applyFx` e o player de animações (tempo injetável).
