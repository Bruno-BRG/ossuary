# Idiomas e história de abertura

O jogo fala **português (Brasil)** e **inglês**. O idioma padrão do aplicativo é
português; muda em `F2` → Tela → **Idioma** (também pelo título, em Opções) e vale na hora.
A escolha mora em `DisplaySettings.Language` e é guardada no `localStorage`
junto de tema, CRT e escala; o frontend a reenvia ao motor em cada `new`/`load`/`display`.

## Como funciona (`Loc.cs`)

- O **inglês é a chave**. `Loc.T("texto")` devolve o português quando o idioma é PT e,
  se não houver tradução, o próprio inglês: nada quebra por falta de entrada.
- `Game.Say` já passa tudo por `Loc.T`: mensagens estáticas casam por dicionário e as
  dinâmicas (nomes, números) por padrões `Rx` (`"You buy (.+) for (\d+) gold."`).
- `TextBuilder.Write/WriteClipped` passam por `Loc.U`, só com casamento da string inteira.
  Rótulos, títulos e dicas da UI traduzem sem editar cada tela.
- `Names` traduz nomes próprios (regiões, ramos) até dentro de mensagens dinâmicas.
- O Core nasce em **inglês** (`Loc.Current`); testes headless ficam estáveis. O host define o idioma.
- Texto novo: escreva em inglês, adicione a entrada PT em `Loc.cs`. Strings alinhadas em coluna
  precisam de tradução com largura parecida.

## Estado da tradução

Traduzido: título, menu/opções, HUD, rótulos de painéis, comandos, mensagens de overworld,
cidade, loja e vitória, nomes das regiões/ramos, raças e classes (nomes), intro e abertura.
Ainda em inglês (próxima camada): descrições de raças/classes/magias/habilidades, nomes de
itens e monstros, mensagens de combate e mágicas, textos de deuses e altares.

## Abertura

1. Título → **Nova expedição** → criação do personagem.
2. **Intro animada** (`desktop/src/intro.ts`): a história inteira, da primeira cova até a
   chegada do jogador, em **sete eras** (O Poço, Os Reis e os Arquivos, O Ossuary, O Selo de
   Yendor, A Guerra da Torre, Hoje, Você). Cada página é uma cena em **corte lateral do
   mundo**: o poço onde as vilas enterram os mortos; os andares cavados por reis e
   arquivistas; as cinco camadas com os nomes dos ramos; Yendor descendo com o selo e a
   água negra subindo; a Torre de Cinza queimando o que sobe; as vilas muradas e o quadro de
   recompensas (30 ◆ por cabeça, ★ 5000 ◆ pelo Amuleto); e a estrada até a boca do poço.
   No alto, uma linha do tempo e o título da era ("há trezentos anos"). A **primeira linha
   de cada página** do motor (`Story.Intro()`) é o título; as outras são o texto, digitado.
   Cada página termina e **segue sozinha** após ~4,5 s; `Enter` completa o texto e depois
   avança, `Esc` pula tudo. A última página fala o objetivo (descer, pegar o Amuleto, voltar)
   e espera `Enter`. Com "reduzir movimento" ligado, mostra cada página já pronta.
3. A expedição começa **no overworld**, na região mais calma, ao lado da entrada da
   dungeon (`Game.BeginAtOverworld`), com o primeiro texto no diário. A dungeon só abre
   quando o jogador entra nela.

A história segue `docs/lore.md`. O save guarda `Overworld` para reproduzir o início certo.
