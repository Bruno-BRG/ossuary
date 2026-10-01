# Renderer — Ossuary

Como o texto ASCII vira pixel na tela, e os erros que já custaram tempo.

## Pipeline

`Game.UiState` + `Ui.Draw()` escrevem num `TextBuilder` (chars + cor fg/bg +
bold por célula). `TerminalRenderer.Present(buffer)` transforma isso em dois
meshes — fundo (cores chapadas) e glifos (amostra o atlas) — e a câmera
ortográfica enquadra a grade. `OssuaryTerminal.shader` faz o blend;
`OssuaryCrt.shader` põe scanline/vinheta por cima.

Uma chamada de `Present` = 4 vértices por célula. Turnos são discretos, então
rebuild completo por turno é barato; o dirty-check (`GameMap.Version`) evita
rebuild quando nada mudou.

## Atlas de glifos (`Render/GlyphAtlas.cs`)

**Usa a textura da fonte do próprio Unity.** Não copia pixels para um atlas
próprio. Isso é deliberado, e reverter isso reintroduz uma classe de bug:

A textura de fonte dinâmica é Alpha8, tem tamanho fixo e é **reconstruída (um
objeto novo, layout novo) sempre que esgota espaço**. Copiar os pixels para um
atlas próprio exige que o UV, a página e a ordem dos linhas ainda concordem no
momento da cópia. Quando não concordam, o atlas enche de blocos sólidos ou de
fragmentos de outras letras — e o jogo renderiza texto com os caracteres
errados. Visível num screenshot, invisível numa contagem de cobertura.

Duas regras mantêm os UVs válidos:

1. Pedir **todos** os glifos antes de ler qualquer UV.
2. Capturar a textura **depois** disso, e nunca pedir mais nada (cada
   `RequestCharactersInTexture` pode reconstruir o atlas e invalidar os UVs).

`RequestBatch` + `TextureKey()` existem porque o empacotamento é preguiçoso e
*repetido*: `GetCharacterInfo` de um glifo ainda não guardado também empacota, e
isso pode mover o layout. O laço repete até a textura parar de trocar de
instância.

### Cobertura no shader

`float cov = max(s.a, max(s.r, max(s.g, s.b)));`

O canal que carrega a forma **não é sempre o alpha** — depende da plataforma. E
`max(alpha, luminância)` não resolve: o canal que não carrega a forma é
uniformemente 1, nunca parcialmente aceso, então o `max` dá 1 e o glifo vira
retângulo cheio.

### Filtro Point (obrigatório)

`filterMode = FilterMode.Point` no atlas. O Unity empacota glifos colados e o
retângulo UV inclui esse padding; com bilinear, todo caractere cresce uma
sombra do vizinho e a tela enche de letras corretas cada uma arrastando
fragmentos de outras. Isso não é cosmético.

### Métricas de célula

`CellW` = max do *advance* e da tinta real do glifo mais largo. Só o advance
não basta: `M` e `W` passam da advance em um ou dois pixels, a tinta invade a
célula vizinha e as linhas saem com letras coladas.

O quad é desenhado na **caixa de tinta** (`InkOf`: offsetX, offsetY, w, h em
pixels), não na célula inteira — esticar o UV do glifo sobre a advance
distorce todo caractere.

O UV é **reduzido em meio texel** em cada borda, porque o retângulo empacotado
inclui o padding que se sobrepõe aos texels do vizinho.

## FOV (`Core/Fov.cs`)

Ray casting simétrico com revelação de canto. **Não** é shadowcasting com
aritmética de slope: o shadowcasting recursivo é exatamente a parte do FOV de
roguelike que fica sutilmente assimétrica, porque uma célula em cima de uma
fronteira de setor é decidida pelo setor que a alcançar primeiro. Raio não tem
fronteira — uma célula é visível se algum raio a vê — então a simetria vem por
construção, e vale mais aqui do que as últimas células de velocidade.

36 raios por quadrante, passo de 0.25 célula, e `RevealCorner` mostra a célula
diagonal quando as duas laterais do canto estão abertas (senão o batente de uma
porta fica serrilhado).

## Como verificar

```powershell
.\unity-run.ps1 -Method Ossuary.EditorTools.OssuaryCli.DumpTestPattern -Graphics
```

`unity/Builds/shots/testpattern.png` tem o alfabeto completo em duas_caixas.
Letras legíveis e sem sangramento = atlas e UVs certos. **Precisa de
`-Graphics`**: com `-nographics` não existe device gráfico, e o readback volta
lixo — foi exatamente isso que fez parecer que o shader estava errado quando o
problema era a textura ausente.

`CaptureFrames` renderiza o jogo de verdade em PNG. `RenderDiagnostics`
imprime o atlas, quantos glifos resolveram, e qual textura o material
realmente amostrou (esse último campo é o que transformou "bug de shader" em
"textura nula").