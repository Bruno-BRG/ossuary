using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// Portuguese for the play layer: combat and look messages, the composed lines on the HUD and character sheet,
    /// skills, abilities, perks, tile names and the control labels. Patterns here sit in front of the older ones
    /// in <see cref="Rx"/> so a specific message wins over a general one.
    /// </summary>
    public static partial class Loc
    {
        static void P(string en, string pt) { if (!Pt.ContainsKey(en)) Pt[en] = pt; }

        static void AddGameText()
        {
            // ---- materials in combat (Game.Materials.cs)
            P("The silver bites deep!", "A prata morde fundo!");
            P("The cold iron bites deep!", "O ferro frio morde fundo!");
            P("The obsidian bites deep!", "A obsidiana morde fundo!");

            // ---- words that appear inside composed lines
            foreach (var kv in new[] {
                ("peaceful", "pacífico"), ("weak", "fraco"), ("somewhat dangerous", "um pouco perigoso"), ("dangerous", "perigoso"),
                ("very dangerous", "muito perigoso"), ("extremely dangerous", "extremamente perigoso"),
                ("worse", "pior"), ("better", "melhor"), ("hit", "acerta"), ("hits", "acerta"), ("kill", "mata"), ("kills", "mata"), ("Neutral", "Neutro"), ("ChaoticGood", "Caótico e Bom"), ("NeutralGood", "Neutro e Bom"), ("LawfulGood", "Leal e Bom"),
                ("ChaoticNeutral", "Caótico e Neutro"), ("LawfulNeutral", "Leal e Neutro"), ("ChaoticEvil", "Caótico e Mau"),
                ("NeutralEvil", "Neutro e Mau"), ("LawfulEvil", "Leal e Mau"),
                ("Combat", "Combate"), ("Dodging", "Esquiva"), ("Stealth", "Furtividade"), ("Magic", "Magia"), ("Survival", "Sobrevivência"), ("Search", "Busca"),
                ("Novice", "Novato"), ("Trained", "Treinado"), ("Skilled", "Habilidoso"), ("Expert", "Especialista"), ("Master", "Mestre"),
                ("Fire", "Fogo"), ("Cold", "Frio"), ("Lightning", "Raio"), ("Poison", "Veneno"), ("Necrotic", "Necrótico"), ("Holy", "Sagrado"), ("Acid", "Ácido"),
                ("Mana", "Mana"), ("Regeneration", "Regeneração"), ("Evasion", "Evasão"), ("Movement", "Movimento"), ("Items", "Itens"), ("Windows", "Janelas"),
                // hint bar
                ("arrows", "setas"), ("bump", "esbarrar"), ("move", "mover"), ("get", "pegar"), ("pack", "mochila"), ("character", "ficha"), ("descend", "descer"),
                ("climb", "subir"), ("search", "buscar"), ("help", "ajuda"), ("options", "opções"), ("talk", "falar"), ("travel", "viajar"), ("log", "diário"),
                ("attack", "atacar"), ("flee", "fugir"),
                // status bar and HUD
                ("HP", "PV"), ("Equipped", "Equipado"),
                // character titles
                ("Adventurer", "Aventureiro") })
                P(kv.Item1, kv.Item2);

            // ---- tiles and scenery: they are labels, but a message can name one ("You dig through {a1}"), so the gender is kept
            TN("floor", "chão"); TN("wall", "parede", true); TN("brick wall", "parede de tijolos", true); TN("rock wall", "parede de rocha", true);
            TN("pillar", "pilar"); TN("door", "porta", true); TN("closed door", "porta fechada", true); TN("open door", "porta aberta", true);
            TN("locked door", "porta trancada", true); TN("stairs down", "escada para baixo", true); TN("stairs up", "escada para cima", true);
            TN("ladder down", "escada de mão para baixo", true); TN("magic portal", "portal mágico"); TN("rubble", "escombros");
            TN("altar", "altar"); TN("fountain", "fonte", true); TN("counter", "balcão"); TN("bed", "cama", true); TN("table", "mesa", true);
            TN("barrel", "barril"); TN("shelf", "estante", true); TN("forge", "forja", true); TN("tree", "árvore", true);
            TN("notice board", "quadro de avisos"); TN("grave", "sepultura", true); TN("hearth", "lareira", true);
            TN("the void", "o vazio"); TN("crypt", "cripta", true);

            // ---- plain messages
            foreach (var kv in new[] {
                ("You kick at the air.", "Você chuta o ar."), ("You kick the door open.", "Você arromba a porta com um chute."),
                ("You pick the lock.", "Você arromba a fechadura."), ("You climb over the rubble.", "Você escala os escombros."),
                ("You find nothing out of the ordinary.", "Você não encontra nada fora do comum."),
                ("You have never seen that place.", "Você nunca viu aquele lugar."), ("You have nothing suitable.", "Você não tem nada adequado."),
                ("You die...", "Você morre..."), ("A wall blocks the way.", "Uma parede bloqueia o caminho."), ("There is a wall in the way.", "Há uma parede no caminho."),
                ("There is no lock there.", "Não há fechadura ali."), ("There is nothing here to pick up.", "Não há nada aqui para pegar."),
                ("There is nothing to ascend here.", "Não há nada para subir aqui."), ("There is nothing to descend here.", "Não há nada para descer aqui."),
                ("The fountain water is cold and clean.", "A água da fonte é fria e limpa."),
                ("A cold fire pit, a torn tent, a pack left behind. Whoever was here left in a hurry, or did not leave.",
                    "Uma fogueira fria, uma tenda rasgada, uma mochila deixada para trás. Quem esteve aqui saiu com pressa, ou não saiu."),
                ("A line of carts has stopped on the road. The drivers watch you without hurry. They have rations and potions, and no wish to haggle long.",
                    "Uma fila de carroças parou na estrada. Os condutores te observam sem pressa. Têm rações e poções, e nenhuma vontade de pechinchar muito."),
                ("A fallen wall, a stair going down into the rock, a smell of old stone and older fear.",
                    "Um muro caído, uma escada que desce para dentro da rocha, um cheiro de pedra velha e de medo mais velho."),
                ("A little cairn and a bowl of ash. Someone still comes here to ask for things.",
                    "Um pequeno monte de pedras e uma tigela de cinzas. Alguém ainda vem aqui pedir coisas."),
                ("A traveller, a few days dead. The crows have not finished. The purse is still on the belt.",
                    "Um viajante, morto há alguns dias. Os corvos não terminaram. A bolsa ainda está no cinto."),
                ("An abandoned camp", "Um acampamento abandonado"), ("A caravan", "Uma caravana"), ("A ruin by the road", "Uma ruína à beira da estrada"),
                ("A roadside shrine", "Um santuário na estrada"), ("A toll", "Um pedágio"), ("A body by the road", "Um corpo à beira da estrada"),
                ("Nothing there.", "Nada ali."), ("You have never been there.", "Você nunca esteve ali."),
                ("That was good.", "Estava bom."), ("Exp", "Exp"), ("Time", "Hora"), ("Level", "Nível") })
                P(kv.Item1, kv.Item2);

            // ---- abilities and perks
            foreach (var kv in new[] {
                ("Power Strike", "Golpe Poderoso"), ("Second Wind", "Segundo Fôlego"), ("Cleave", "Golpe Giratório"), ("Shield Bash", "Escudada"),
                ("War Cry", "Grito de Guerra"), ("Lay on Hands", "Imposição de Mãos"), ("Holy Strike", "Golpe Sagrado"), ("Backstab", "Punhalada"),
                ("Vanish", "Sumir"), ("Aimed Shot", "Tiro Mirado"),
                ("A heavy blow: double damage and +2 to hit.", "Um golpe pesado: dano dobrado e +2 para acertar."),
                ("Catch your breath: recover a quarter of your HP.", "Recupere o fôlego: recobra um quarto dos seus PV."),
                ("One sweeping attack on every adjacent foe.", "Um ataque giratório em cada inimigo adjacente."),
                ("Slam with your shield: light damage, and the foe loses its next turns.", "Bata com o escudo: dano leve, e o inimigo perde os próximos turnos."),
                ("A roar that sends the living nearby running.", "Um rugido que faz os vivos por perto saírem correndo."),
                ("Heal half your HP and burn out poison.", "Cura metade dos seus PV e queima o veneno."),
                ("A blow wreathed in light: extra holy damage, double against the dead.", "Um golpe envolto em luz: dano sagrado extra, dobrado contra os mortos."),
                ("Triple damage against a sleeping, fleeing, confused or unaware foe; otherwise double.", "Dano triplo contra inimigo dormindo, fugindo, confuso ou desatento; senão, dobro."),
                ("Slip out of sight for a few turns.", "Suma da vista por alguns turnos."),
                ("Take your time: +4 to hit and double damage at range.", "Sem pressa: +4 para acertar e dano dobrado à distância."),
                ("Agile", "Ágil"), ("Aura of Protection", "Aura de Proteção"), ("Devout", "Devoto"), ("Focus", "Foco"), ("Focused", "Concentrado"),
                ("Gourmand", "Comilão"), ("Hale", "Robusto"), ("Iron Will", "Vontade de Ferro"), ("Keen Eye", "Olho Aguçado"), ("Learned", "Erudito"),
                ("Light Feet", "Pés Leves"), ("Lucky", "Sortudo"), ("Mighty", "Poderoso"), ("Mind Expansion", "Expansão Mental"), ("Quick Learner", "Aprendiz Rápido"),
                ("Quick Recovery", "Recuperação Rápida"), ("Shield Wall", "Muralha de Escudos"), ("Spell Power", "Poder Mágico"), ("Tough", "Resistente"),
                ("Vigorous", "Vigoroso"), ("Weapon Master", "Mestre de Armas"),
                ("+1 Dexterity, +4 Dodging", "+1 Destreza, +4 Esquiva"), ("+1 AC per rank while armoured", "+1 CA por nível enquanto de armadura"),
                ("+1 Wisdom, +5 Survival", "+1 Sabedoria, +5 Sobrevivência"), ("-5% spell failure per rank", "-5% de falha de magia por nível"),
                ("+6 Magic, +4 Search", "+6 Magia, +4 Busca"), ("You get hungry half as fast", "Você sente fome na metade da velocidade"),
                ("+1 Constitution", "+1 Constituição"), ("+20% poison resistance per rank", "+20% de resistência a veneno por nível"),
                ("+2 to hit with missiles per rank", "+2 para acertar com projéteis por nível"), ("+1 Intelligence, +5 Magic", "+1 Inteligência, +5 Magia"),
                ("Foes notice you from 2 cells less per rank", "Inimigos notam você de 2 casas a menos por nível"), ("+2 Evasion per rank", "+2 Evasão por nível"),
                ("+1 Strength", "+1 Força"), ("+6 maximum Mp per rank", "+6 de Mp máximo por nível"), ("+15% experience per rank", "+15% de experiência por nível"),
                ("Mana returns 2 turns sooner per point, per rank", "A mana volta 2 turnos antes por ponto, por nível"), ("+2 AC per rank while carrying a shield", "+2 CA por nível com escudo"),
                ("+2 damage to every damaging spell per rank", "+2 de dano em toda magia de dano por nível"), ("+6 max HP, and heal 6 now", "+6 PV máximos, e cura 6 agora"),
                ("+8 Vigor and faster Vigor recovery per rank", "+8 Vigor e recuperação de Vigor mais rápida por nível"), ("+1 to hit and +1 damage per rank", "+1 para acertar e +1 de dano por nível"),
                ("Ability: a careful shot, +4 to hit, double damage (5 Vigor)", "Habilidade: um tiro cuidadoso, +4 para acertar, dano dobrado (5 Vigor)"),
                ("Ability: triple damage on a sleeping, fleeing or unaware foe (4 Vigor)", "Habilidade: dano triplo em inimigo dormindo, fugindo ou desatento (4 Vigor)"),
                ("Ability: strike every adjacent foe (6 Vigor)", "Habilidade: golpeia todo inimigo adjacente (6 Vigor)"),
                ("Ability: a blow that burns evil; the dead take double (6 Vigor)", "Habilidade: um golpe que queima o mal; os mortos sofrem o dobro (6 Vigor)"),
                ("Ability: heal half your HP and cleanse poison (10 Vigor)", "Habilidade: cura metade dos PV e limpa o veneno (10 Vigor)"),
                ("Ability: a blow for double damage (4 Vigor)", "Habilidade: um golpe de dano dobrado (4 Vigor)"),
                ("Ability: recover a quarter of your HP (8 Vigor)", "Habilidade: recobra um quarto dos PV (8 Vigor)"),
                ("Ability: stagger a foe; needs a shield (4 Vigor)", "Habilidade: abala um inimigo; exige escudo (4 Vigor)"),
                ("Ability: turn invisible for a few turns (8 Vigor)", "Habilidade: fica invisível por alguns turnos (8 Vigor)"),
                ("Ability: frighten the living around you (8 Vigor)", "Habilidade: amedronta os vivos ao redor (8 Vigor)") })
                P(kv.Item1, kv.Item2);

            // ---- key labels (Controls panel)
            foreach (var kv in new[] {
                ("Fire at target", "Atirar no alvo"), ("Inspect", "Inspecionar"), ("Kick / attack ahead", "Chutar / atacar à frente"), ("Look", "Olhar"),
                ("Open door", "Abrir porta"), ("Swap with", "Trocar de lugar com"), ("Use an ability", "Usar uma habilidade"), ("Use key", "Usar chave"),
                ("Apply a tool", "Usar uma ferramenta"), ("Cast a spell", "Conjurar uma magia"), ("Craft", "Criar"), ("Drink a potion", "Beber uma poção"),
                ("Drink at a fountain", "Beber numa fonte"), ("Drop", "Largar"), ("Eat", "Comer"), ("Pick up", "Pegar"), ("Put on ring", "Pôr anel"),
                ("Read scroll", "Ler pergaminho"), ("Remove ring", "Tirar anel"), ("Take off armour", "Tirar armadura"),
                ("Train a skill (Trained mode)", "Treinar perícia (Treinado)"), ("Wear armour", "Vestir armadura"), ("Wield weapon", "Empunhar arma"),
                ("Zap wand", "Usar varinha"), ("Auto-explore", "Explorar sozinho"), ("Climb stairs", "Subir escada"), ("Descend stairs", "Descer escada"),
                ("Disarm a trap", "Desarmar armadilha"), ("Move east", "Mover a leste"), ("Move north", "Mover ao norte"), ("Move north-east", "Mover a nordeste"),
                ("Move north-west", "Mover a noroeste"), ("Move south", "Mover ao sul"), ("Move south-east", "Mover a sudeste"), ("Move south-west", "Mover a sudoeste"),
                ("Move west", "Mover a oeste"), ("Rest until healed", "Descansar até curar"), ("Travel (overworld)", "Viajar (mundo)"),
                ("Travel to altar / fountain", "Ir até altar / fonte"), ("Travel to stairs", "Ir até a escada"), ("Wait a turn", "Esperar um turno"),
                ("Abandon run", "Abandonar expedição"), ("Character sheet", "Ficha do personagem"), ("Cycle CRT", "Alternar CRT"),
                ("Cycle colour theme", "Alternar tema de cor"), ("Help", "Ajuda"), ("Menu / options", "Menu / opções"), ("Message history", "Histórico de mensagens"),
                ("Quest journal", "Diário de missões"), ("Quick save", "Salvar rápido"), ("Spend advancements", "Gastar evoluções"), ("Toggle minimap", "Alternar minimapa") })
                P(kv.Item1, kv.Item2);

            // ---- patterns, most specific first; they go in front of the older list
            var front = new List<(System.Text.RegularExpressions.Regex, string)>
            {
                // combat
                R(@"You miss\.", "Você erra."),
                R(@"You miss (.+)\.", "Você erra {a1}."),
                R(@"You (hit|kill) (.+?) with a critical hit for (\d+) damage\.", "Você $1 {a2} com um golpe crítico por $3 de dano."),
                R(@"You (hit|kill) (.+?) for (\d+) damage\.", "Você $1 {a2} por $3 de dano."),
                R(@"You evade (.+)'s attack\.", "Você esquiva do ataque {d1}."),
                R(@"(.+) misses you\.", "$1 erra você."),
                R(@"(.+) misses (.+)\.", "$1 erra {a2}."),
                R(@"(.+) hits you for (\d+) damage\.", "$1 acerta você por $2 de dano."),
                R(@"(.+) kills you for (\d+) damage\.", "$1 mata você por $2 de dano."),
                // Every damaging effect names what did it: "{verb} the {name} for {dmg} damage." The verb is data on the
                // spell (Magic/Spells.*), so the whole sentence matches here and $1 carries the translated phrase. These
                // sit in front of the general lines below, which would otherwise read the verb as "A spark leaps into
                // the" or leave "You open the throat of" half in English.
                R(@"(.+) the (.+) for (\d+) damage\.", "$1 {a2} por $3 de dano."),
                R(@"(.+) the (.+) for (\d+), and it dies\.", "$1 {a2} por $3, e morre."),
                R(@"(.+) (hits|kills) (.+) for (\d+) damage\.", "$1 $2 {a3} por $4 de dano."),
                R(@"The missile misses\.", "O projétil erra."),
                R(@"The missile misses (.+)\.", "O projétil erra {a1}."),
                R(@"The missile (hits|kills) (.+) for (\d+) damage\.", "O projétil $1 {a2} por $3 de dano."),
                R(@"You have killed (.+)\. \((\d+) experience\)", "Você matou {a1}. ($2 de experiência)"),
                // look
                R(@"It is (peaceful|weak|somewhat dangerous|dangerous|very dangerous|extremely dangerous)\.", "Parece $1."),
                R(@"Fight it \(Enter or K\), or flee \(R or <\)\. It is (.+)\.", "Lute (Enter ou K) ou fuja (R ou <). Parece $1."),
                R(@"(.+), level (\d+) \((\d+) HP, AC (\d+)\)", "$1, nível $2 ($3 PV, CA $4)"),
                R(@"\((\d+),(\d+)\) (.+)", "($1,$2) $3"),
                // Jobs from the notice board (Game.Contracts.cs). A contract is composed ("Hunt 5 giant rats in The
                // Dungeons"), so the deed and the branch ride the patterns as names: the branch takes the article of
                // the sentence and contracts with it ("em As Masmorras" -> "nas Masmorras"). A monster named in the
                // plural keeps the game's own "(s)" convention ("fera(s)").
                R(@"Hunt (\d+) (.+)s in (.+)", "Caçar $1 $2(s) em $3"),
                R(@"Hunt (\d+) (.+?) in (.+)", "Caçar $1 $2 em $3"),
                R(@"Reach depth (\d+) of (.+)", "Chegar ao nível $1 de $2"),
                R(@"You take the job: (.+)\. It pays (\d+) gold\.", "Você aceita o serviço: $1. Paga $2 de ouro."),
                R(@"Take a job: (.+) \(pays (\d+)g\)", "Aceitar serviço: $1 (paga $2 de ouro)"),
                R(@"Report: (.+) \((\d+)/(\d+)\)", "Entregar: $1 ($2/$3)"),
                R(@"\((\d+)/(\d+)\) (.+)", "($1/$2) $3"),
                // The journal row is composed with its label already in Portuguese ("Serviço: "), so the head is either
                // language while the deed after it is always English.
                R(@"(?:Job|Serviço): (.+) \((\d+)/(\d+)\)( ✓)?", "Serviço: $1 ($2/$3)$4"),
                // misc messages
                R(@"You are now wielding (.+)\.", "Você empunha {a1}."),
                R(@"You eat (.+)\. That was good \(\+(\d+) nourishment\)\.", "Você come {a1}. Estava bom (+$2 de nutrição)."),
                R(@"You eat (.+)\. You were not hungry enough to taste it\.", "Você come {a1}. Sem fome demais para sentir o gosto."),
                R(@"You take (.+)\.", "Você pega {a1}."),
                R(@"You (arrive|step) (in|at) (.+)\. Some (\d+) people live here\.", "Você chega a $3. Cerca de $4 pessoas vivem aqui."),
                R(@"(.+) \(\+(\d+)\)\. The Watch of (.+) wants (\d+) gold\.", "$1 (+$2). A Guarda de $3 quer $4 de ouro."),
                R(@"The Watch hears you threatened a citizen\.", "A Guarda soube que você ameaçou um cidadão."),
                R(@"(.+) thinks the (worse|better) of you for (.+)\.", "$1 pensa $2 de você por $3."),
                R("Three men with a rope across the road\\. \"The road is not free,\" says the one with the nicest coat\\. \\(Danger here: (\\d+)\\.\\)",
                    "Três homens com uma corda atravessada na estrada. \"A estrada não é de graça\", diz o de casaco mais bonito. (Perigo aqui: $1.)"),
                R(@"(\s*)\(max (\d+)\)", "$1(máx $2)"),
                // HUD and sheets. The header writes its place with a space on each side (" DEPTH 3 "), so the two
                // patterns that name a place keep whatever padding they were given.
                R(@"(\s*)DEPTH (\d+)(\s*)", "$1NÍVEL $2$3"),
                R(@"(\s*)TOWN (-?\d+)(\s*)", "$1CIDADE $2$3"),
                R(@"Day (\d+)  (.+) \(night\)", "Dia $1  $2 (noite)"),
                R(@"Day (\d+)  (.+)", "Dia $1  $2"),
                R(@"Lv (\d+)", "Nv $1"),
                R(@"Level (\d+)   HP (\d+)/(\d+)(   MP \d+/\d+)?   AC (-?\d+)   XP (\d+)", "Nível $1   PV $2/$3$4   CA $5   XP $6"),
                R(@"Level (\d+)\+", "Nível $1+"),
                R(@"Dlvl (\d+)   (\d+) kills", "Nvl $1   $2 mortes"),
                R(@"Wanted: (\d+) gold", "Procurado: $1 de ouro"),
                R(@"Done (\d+)   Failed (\d+)", "Feitos $1   Falhos $2"),
                R(@"\.\.\.and (\d+) more", "...e mais $1"),
                R(@"\+(\d+) more", "+$1 mais"),
                // the altar panel, and every line that shows where the hero stands with a god
                R(@"God of (.+)\.", "Deus de $1."),
                R(@"Likes:    (.+)", "Gosta:    $1"),
                R(@"Dislikes: (.+)", "Odeia: $1"),
                R(@"Boon \((\d+) piety\): (.+)", "Dádiva ($1 de piedade): $2"),
                R(@"Piety (\d+): (.+)", "Piedade $1: $2"),
                R(@"Your piety (\d+)/(\d+)", "Sua piedade $1/$2"),
                R(@"The last prayer is still fresh \((\d+) turns\)\.", "A última prece ainda está fresca ($1 turnos)."),
                R(@"You follow (.+)\.", "Você segue $1."),
                // the confirmation of the creation screen: the name is the hero's, the race and the class are words
                R(@"Begin as (.+) the (Human|Dwarf|Elf|Halfling|Orc|Gnome|Ashen) (.+)\?  \(Enter\)", "Começar como $1, $3 $2?  (Enter)"),
                R(@"(.+), the (Human|Dwarf|Elf|Halfling|Orc|Gnome|Ashen) (.+)", "$1, $3 $2"),
                R(@"Alignment (.+)", "Alinhamento $1"),
                R(@"Title (.+)", "Título $1"),
                R(@"(\d+) advancement\(s\) unspent", "$1 evolução(ões) por gastar"),
                R(@"(\d+) pick\(s\) to spend", "$1 escolha(s) por gastar"),
                R(@"Perks: (.+)", "Talentos: $1"),
                R(@"Traits: (.+)", "Traços: $1"),
                R(@"(\d+) free advancement at start", "$1 evolução grátis no início"),
                R(@"Mana ([+-]\d+)%", "Mana $1%"),
                R(@"Regeneration (\d+)%", "Regeneração $1%"),
                R(@"Evasion ([+-]\d+)", "Evasão $1"),
                R(@"([A-Z][a-z]+) ([+-]\d+)%", "$1 $2%"),
                R(@"Now: (.+)", "Agora: $1"),
                R(@"Time: (.+)", "Hora: $1"),
                R(@"XP to spend: (\d+)", "XP para gastar: $1"),
                R(@"Gear: (.+)", "Equipo: $1"),
                // The parts of a gear line, joined one by one with ", ": an attribute and a die of elemental damage.
                R(@"([+-]\d+) (Str|Dex|Con|Int|Wis|Mp|HP|Vigor)", "$1 $2"),
                R(@"(\d+)d(\d+) (\w+)", "$1d$2 $3"),
                R(@"Faith: (.+), (.+) - piety (\d+)/(\d+) \(tier (\d+)\)", "Fé: $1, $2 - piedade $3/$4 (nível $5)"),
                R(@"Faith: (.+), (.+) - piety (\d+)/(\d+)", "Fé: $1, $2 - piedade $3/$4"),
                R(@"Faith: (.+)", "Fé: $1"),
                R(@"Companion: (.+), level (\d+), (\d+)/(\d+) HP", "Companheiro: $1, nível $2, $3/$4 PV"),
                R(@"Corruption (\d+)/(\d+)", "Corrupção $1/$2"),
                R(@"Set: (.+) (\d)/3", "Conjunto: $1 $2/3"),
                R(@"Relics worn: (\d+) \(they corrupt\)", "Relíquias vestidas: $1 (elas corrompem)"),
                // The morgue's last line names a branch, so the whole sentence goes through the dictionary and
                // "em As Masmorras" can contract to "nas Masmorras".
                R(@"Ended (in|on) (.+) (\d+)", "Terminou em $2 $3"),
                R(@"(.+) (\d+)/(\d+)", "$1 $2/$3"),
                // What the rebind panel answers (KeyBindings.cs): the key or the label rides the pattern.
                R(@"(.+) is reserved for menus\.", "$1 é reservado para os menus."),
                R(@"Taken from ""(.+)""\.", "Tirado de \"$1\"."),
                R(@"Bound to (.+)\.", "Ligado a $1."),
            };
            // The altar panel opens with the god and its title ("Khorr, the Hammer Beneath"): the name is a proper noun
            // and the title is data, so every god gets the row its own name can start.
            foreach (var god in Entities.Gods.All)
                front.Add(R(System.Text.RegularExpressions.Regex.Escape(god.Name) + @", (.+)", god.Name + ", $1"));
            Rx.InsertRange(0, front);
        }
    }
}
