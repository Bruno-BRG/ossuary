using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ossuary.Core
{
    public enum Lang { En, Pt }

    /// <summary>
    /// Text localisation. English is the source language and the key: <see cref="T"/> returns the
    /// Portuguese entry when the player picked Portuguese and otherwise the text itself, so an
    /// untranslated string degrades to English instead of breaking. Dynamic messages (names, numbers)
    /// are matched by <see cref="Rx"/> patterns. Pure data: no UI, no platform.
    /// </summary>
    public static class Loc
    {
        /// <summary>Live language. Core defaults to English so headless tests stay stable; the app sets it from DisplaySettings.</summary>
        public static Lang Current = Lang.En;

        // Mutations: every name, blurb and the messages that carry them.
        static void AddMutationText()
        {
            var pt = new Dictionary<string, (string, string)>
            {
                ["bone-plating"] = ("Placas de Osso", "Placas de osso crescem sob a pele. CA +2."),
                ["many-eyes"] = ("Muitos Olhos", "Mais dois pares de olhos se abrem. Visão +2."),
                ["marrow-heart"] = ("Coração de Tutano", "Seu coração bate grosso e lento. PV +8."),
                ["grave-whisper"] = ("Sussurro da Cova", "Os mortos murmuram para você. Mp +6, necrótico 20%."),
                ["ashen-skin"] = ("Pele de Cinza", "Sua pele fica cinza e seca. Fogo 25%."),
                ["hollow-step"] = ("Passo Oco", "Seus passos são leves como pó. Evasão +2."),
                ["knuckle-spurs"] = ("Esporões nos Nós", "Esporões brotam dos seus nós dos dedos. Acerto +1, dano +1."),
                ["iron-gut"] = ("Estômago de Ferro", "Nada que você engole o envenena por muito tempo. Veneno 30%."),
                ["third-rib"] = ("Terceira Costela", "Uma costela extra protege, mas aperta. CA +3, Des -1."),
                ["ember-marrow"] = ("Tutano em Brasa", "Seus ossos queimam. Dano +2, e você vive com fome."),
                ["brittle-bones"] = ("Ossos Quebradiços", "Seus ossos são giz. CA -2."),
                ["ravenous"] = ("Voraz", "Algo em você passa fome sempre. Você gasta comida em dobro."),
                ["palsied-hands"] = ("Mãos Trêmulas", "Suas mãos não param quietas. Acerto -2."),
                ["echoing-steps"] = ("Passos Ecoantes", "Seus passos ecoam no escuro. Monstros notam de uma casa mais longe."),
                ["pallid-skin"] = ("Pele Pálida", "O frio morde fundo. Gelo -30%."),
                ["thin-blood"] = ("Sangue Ralo", "Seu sangue corre ralo. PV -6."),
            };
            foreach (var m in Entities.MutationTable.All)
            {
                if (!pt.TryGetValue(m.Id, out var t)) continue;
                Pt[m.Name] = t.Item1; Pt[m.Blurb] = t.Item2;
                Pt[$"Your body twists: {m.Name}. {m.Blurb}"] = $"Seu corpo se retorce: {t.Item1}. {t.Item2}";
                Pt[$"The priest draws out {m.Name} like a splinter."] = $"O padre arranca {t.Item1} como uma farpa.";
            }
        }

        // Trap messages carry the trap's name, so every combination is registered up front.
        static Loc()
        {
            AddMutationText();
            var traps = new[] { ("spike trap", "armadilha de espetos", "uma"), ("hole", "buraco", "um"), ("dart trap", "armadilha de dardos", "uma"),
                                ("teleport trap", "armadilha de teletransporte", "uma"), ("alarm trap", "armadilha de alarme", "uma"),
                                ("fire trap", "armadilha de fogo", "uma"), ("web", "teia", "uma") };
            foreach (var (en, pt, art) in traps)
            {
                string the = art == "uma" ? "a" : "o";
                Pt[$"You spot a {en}."] = $"Você percebe {art} {pt}.";
                Pt[$"You disarm the {en}."] = $"Você desarma {the} {pt}.";
                Pt[$"You fail to disarm the {en}."] = $"Você não consegue desarmar {the} {pt}.";
                Rx.Add(R($@"You find a {en} at \((\d+),(\d+)\)\.", $"Você acha {art} {pt} em ($1,$2)."));
            }
        }

        public static string Code(Lang l) => l == Lang.Pt ? "pt" : "en";
        public static string Name(Lang l) => l == Lang.Pt ? "Português" : "English";

        public static string T(string en)
        {
            if (Current == Lang.En || string.IsNullOrEmpty(en)) return en;
            if (Pt.TryGetValue(en, out var pt)) return pt;
            string u = U(en);
            if (!ReferenceEquals(u, en)) return u;
            string town = TownText.Translate(en);
            if (town != null) return town;
            foreach (var (re, rep) in Rx)
                if (re.IsMatch(en))
                {
                    string r = re.Replace(en, rep, 1);
                    foreach (var kv in Names) r = r.Replace(kv.Key, kv.Value);
                    return r;
                }
            return en;
        }

        public static string F(string en, params object[] args) => string.Format(T(en), args);


        /// <summary>Region and place names: proper nouns, applied inside dynamic messages too.</summary>
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            ["The Verdant Reach"] = "O Alcance Verdejante", ["Ashen Marches"] = "Marcas de Cinza",
            ["The Sunken Vale"] = "O Vale Afundado", ["Gallowmoor"] = "Charneca da Forca",
            ["The Iron Hills"] = "As Colinas de Ferro", ["Whisperfen"] = "Pântano Sussurrante",
            ["The Craglands"] = "As Terras Fendidas", ["Emberdown"] = "Brasa Baixa",
            ["The Hollow Wastes"] = "Os Ermos Ocos",
            ["The Dungeons"] = "As Masmorras", ["The Mines of Dwarfdeep"] = "As Minas de Dwarfdeep",
            ["The Warrens"] = "As Tocas", ["The Sunken Vaults"] = "Os Cofres Afundados", ["The Ashen Spire"] = "A Torre de Cinza",
        };

        static readonly Dictionary<string, string> Ui = new Dictionary<string, string>
        {
            ["Abilities"] = "Habilidades", ["Advancement"] = "Evolução", ["Ahead"] = "À frente", ["Altar"] = "Altar",
            ["Any key begins again."] = "Qualquer tecla recomeça.", ["Any key begins another run."] = "Qualquer tecla inicia outra expedição.",
            ["Armour"] = "Armadura", ["Arrow keys choose a destination."] = "Setas escolhem o destino.",
            ["Attributes "] = "Atributos ", ["BLIND"] = "CEGO", ["BURNING"] = "EM CHAMAS", ["CONFUSED"] = "CONFUSO",
            ["STUNNED"] = "ATORDOADO", ["TRIPPING"] = "ALUCINADO", ["WET"] = "MOLHADO",
            ["Books"] = "Livros", ["Carrying"] = "Carregando", ["Character"] = "Personagem", ["Choose"] = "Escolher",
            ["Class perks (Shift+C) grant them."] = "Os talentos de classe (Shift+C) concedem.",
            ["Class"] = "Classe", ["Commands"] = "Comandos", ["Cost  Reach"] = "Custo Alcance",
            ["DEPTH "] = "NÍVEL ", ["Deepest level"] = "Nível mais fundo", ["Discoveries"] = "Descobertas",
            ["Dislikes: "] = "Odeia: ", ["Enter begin  Esc back"] = "Enter começa  Esc volta",
            ["Enter confirms.  Esc cancels."] = "Enter confirma.  Esc cancela.", ["Enter next  Esc title"] = "Enter ok  Esc título",
            ["Enter rebind   Del clear   R reset   ⇧R reset all"] = "Enter remapeia   Del limpa   R padrão   ⇧R tudo",
            ["Enter/b buy   s sell   hjkl move   Esc leave"] = "Enter/b compra   s vende   hjkl move   Esc sai",
            ["Equipped"] = "Equipado", ["Esc back"] = "Esc volta", ["Esc leaves"] = "Esc sai", ["Esc quits."] = "Esc sai.",
            ["Evade"] = "Esquiva", ["Fed"] = "Alimentado", ["Hungry"] = "Faminto", ["Starving"] = "Definhando",
            ["Food"] = "Comida", ["Game over"] = "Fim de jogo", ["Gear: "] = "Equipo: ", ["Gold carried"] = "Ouro carregado",
            ["Here"] = "Aqui", ["In view"] = "À vista", ["Inventory"] = "Inventário", ["JOURNAL"] = "DIÁRIO",
            ["Jewellery"] = "Joias", ["Kills"] = "Mortes", ["Kit: "] = "Kit: ", ["Landmarks"] = "Marcos",
            ["Levels mapped"] = "Níveis mapeados", ["Likes:    "] = "Gosta:    ", ["Load"] = "Carga",
            ["Lv  School        Cost  Fail"] = "Nv  Escola        Custo Falha",
            ["Map"] = "Mapa", ["Message history"] = "Histórico", ["Name"] = "Nome", ["Nearby"] = "Por perto",
            ["New character"] = "Novo personagem", ["Nothing is available to you right now."] = "Nada disponível para você agora.",
            ["OVERWORLD"] = "MUNDO", ["Other"] = "Outros", ["Perks: "] = "Talentos: ", ["Potions"] = "Poções",
            ["Press the new key.  Esc cancels."] = "Aperte a nova tecla.  Esc cancela.", ["Race"] = "Raça",
            ["Read a spellbook (r) away from enemies to learn from it."] = "Leia um grimório (r) longe de inimigos para aprender.",
            ["Regions seen"] = "Regiões vistas", ["Scrolls"] = "Pergaminhos", ["Seed"] = "Semente", ["Self"] = "Si mesmo",
            ["Shop gold"] = "Ouro da loja", ["Shop"] = "Loja", ["Skills"] = "Perícias", ["Skills: "] = "Perícias: ",
            ["Sold out."] = "Esgotado.", ["Spells"] = "Magias", ["TOWN"] = "CIDADE", ["TRAVEL"] = "VIAGEM",
            ["The road is quiet."] = "A estrada está calma.", ["The shopkeeper is gone."] = "O lojista sumiu.",
            ["Tools"] = "Ferramentas", ["Town"] = "Cidade", ["Traits: "] = "Traços: ",
            ["Up/Down  Enter casts  Esc closes"] = "↑↓  Enter conjura  Esc fecha", ["Up/Down  Enter chooses  Esc leaves"] = "↑↓  Enter escolhe  Esc sai",
            ["Up/Down  Enter takes  Esc closes"] = "↑↓  Enter pega  Esc fecha", ["Up/Down  Enter uses  Esc closes"] = "↑↓  Enter usa  Esc fecha",
            ["Up/Down choose  Enter next  Esc back"] = "↑↓ escolhe  Enter avança  Esc volta",
            ["Victory"] = "Vitória", ["Vitals"] = "Vitais", ["Wands"] = "Varinhas", ["Weapons"] = "Armas", ["Worn"] = "Vestido",
            ["You are carrying nothing."] = "Você não carrega nada.", ["You escape with your life."] = "Você escapa com vida.",
            ["You follow no god."] = "Você não segue nenhum deus.", ["You have died."] = "Você morreu.",
            ["You know no abilities."] = "Você não conhece habilidades.", ["You know no spells."] = "Você não conhece magias.",
            ["Your gold"] = "Seu ouro", ["[DANGEROUS]"] = "[PERIGOSO]", ["[DEADLY]"] = "[MORTAL]", ["[WARY]"] = "[CAUTELOSO]",
            ["ability"] = "habilidade", ["adjacent"] = "adjacente", ["any key closes"] = "qualquer tecla fecha",
            ["apply a tool (pick-axe, lock pick)"] = "usar ferramenta (picareta, gazua)", ["arrows"] = "setas",
            ["average"] = "médio", ["bare hands"] = "mãos nuas", ["  bare hands"] = "  mãos nuas", ["no armour"] = "sem armadura", ["  no armour"] = "  sem armadura",
            ["character sheet / discoveries"] = "ficha / descobertas", ["character"] = "ficha", ["climb"] = "subir",
            ["descend / climb"] = "descer / subir", ["descend"] = "descer", ["diagonals"] = "diagonais",
            ["eat / drink a potion"] = "comer / beber poção", ["enter picks"] = "enter escolhe", ["fire at a target"] = "atirar no alvo",
            ["get"] = "pegar", ["help"] = "ajuda", ["inventory"] = "inventário", ["kick or attack ahead"] = "chutar ou atacar à frente",
            ["log"] = "diário", ["look / inspect / swap with"] = "olhar / inspecionar / trocar com",
            ["message history"] = "histórico de mensagens", ["move (vi keys, arrows, numpad)"] = "mover (teclas vi, setas, numérico)",
            ["move"] = "mover", ["none"] = "nenhum", ["options / CRT strength / colour theme"] = "opções / força do CRT / tema de cor",
            ["options"] = "opções", ["pack"] = "mochila", ["pick up / drop"] = "pegar / largar", ["press a key..."] = "aperte uma tecla...",
            ["put on / remove a ring"] = "pôr / tirar anel", ["range "] = "alcance ",
            ["read a scroll or book / zap a wand / cast a spell"] = "ler pergaminho ou livro / usar varinha / conjurar",
            ["search for traps and doors"] = "procurar armadilhas e portas", ["search"] = "procurar",
            ["self"] = "si", ["text (x3)"] = "texto (x3)", ["this help"] = "esta ajuda", ["toggle the minimap"] = "alternar minimapa",
            ["travel on the overworld"] = "viajar pelo mundo", ["travel"] = "viajar", ["use a key / open a door"] = "usar chave / abrir porta",
            ["use an ability / spend advancements (Shift)"] = "usar habilidade / gastar evolução (Shift)", ["wait one turn"] = "esperar um turno",
            ["wield / wear / take off armour"] = "empunhar / vestir / tirar armadura",
            ["Str"] = "For", ["Dex"] = "Des", ["Con"] = "Con", ["Int"] = "Int", ["Wis"] = "Sab", ["Cha"] = "Car",
            ["Forest"] = "Floresta", ["Grass"] = "Campo", ["Hills"] = "Colinas", ["Mountain"] = "Montanha", ["Swamp"] = "Pântano",
            ["Snow"] = "Neve", ["Ash"] = "Cinza", ["Ruins"] = "Ruínas", ["Road"] = "Estrada", ["Sand"] = "Areia", ["Water"] = "Água",
            ["Shallow"] = "Rasa", ["DeepWater"] = "Água funda",
            ["Adventurer"] = "Aventureiro", ["Fighter"] = "Guerreiro", ["Rogue"] = "Ladino", ["Cleric"] = "Clérigo",
            ["Wizard"] = "Mago", ["Ranger"] = "Patrulheiro", ["Paladin"] = "Paladino", ["Necromancer"] = "Necromante",
            ["Human"] = "Humano", ["Dwarf"] = "Anão", ["Elf"] = "Elfo", ["Halfling"] = "Halfling", ["Orc"] = "Orc", ["Gnome"] = "Gnomo", ["Ashen"] = "Cinzento",
            ["Begin as "] = "Começar como ",
        };

        static Dictionary<string, string> _ui;

        /// <summary>Interface text: whole-string match only, so names and numbers pass through untouched. Used by TextBuilder.</summary>
        public static string U(string s)
        {
            if (Current == Lang.En || string.IsNullOrEmpty(s)) return s;
            if (_ui == null)
            {
                var d = new Dictionary<string, string>();
                foreach (var kv in Pt) d[kv.Key] = kv.Value;
                foreach (var kv in Ui) d[kv.Key] = kv.Value;
                foreach (var kv in TownText.Pt) d[kv.Key] = kv.Value;
                foreach (var kv in Names) d[kv.Key] = kv.Value;
                foreach (var kv in new List<KeyValuePair<string, string>>(d))
                    if (kv.Key.Length <= 26) d[kv.Key.ToUpperInvariant()] = kv.Value.ToUpperInvariant();
                _ui = d;
            }
            return _ui.TryGetValue(s, out var v) ? v : s;
        }

        static (Regex, string) R(string pattern, string replacement) => (new Regex("^" + pattern + "$", RegexOptions.CultureInvariant), replacement);

        static readonly Dictionary<string, string> Pt = new Dictionary<string, string>
        {
            ["The restless shade is laid to rest at last."] = "A sombra inquieta enfim descansa.",
            // ---- difficulty modes
            ["Mode"] = "Modo", ["Normal"] = "Normal", ["Classic"] = "Clássico", ["Hardcore"] = "Hardcore",
            ["The standard game."] = "O jogo padrão.", ["No hunger. Food is only a luxury."] = "Sem fome. Comida é só luxo.",
            ["One save: it is erased when you resume. No quicksave."] = "Um único save: some ao retomar. Sem save rápido.",
            ["Hardcore: the run is saved only when you quit."] = "Hardcore: a run só é salva ao sair.",
            ["◄► mode  Enter begin  Esc back"] = "◄► modo  Enter começa  Esc volta",
            ["Daily board"] = "Placar diário", ["No daily runs yet."] = "Nenhuma run diária ainda.", ["Daily"] = "Diário",
            ["D daily board   Esc back"] = "D placar diário   Esc volta",
            // ---- corruption
            ["drink at a fountain (it may be tainted)"] = "beber numa fonte (pode estar contaminada)",
            ["There is nothing to drink from here."] = "Não há de onde beber aqui.",
            ["A fountain bubbles here. Press Shift+E to drink."] = "Uma fonte borbulha aqui. Aperte Shift+E para beber.",
            ["Mutations"] = "Mutações", ["Corruption"] = "Corrupção", ["Mutant"] = "Mutante", ["Carry three mutations at once."] = "Carregue três mutações ao mesmo tempo.",
            ["Purge the Ossuary from me"] = "Expulse o Ossuário de mim",
            ["The water is dark with something that was not water. You swallow it anyway."] = "A água está escura com algo que não era água. Você engole mesmo assim.",
            ["The water is thick and black. It burns going down, and it does not stop."] = "A água é grossa e negra. Queima ao descer e não para.",
            ["The water tastes of rust and nothing else."] = "A água tem gosto de ferrugem e mais nada.",
            ["It tastes of bone dust and old graves."] = "Tem gosto de pó de osso e de covas velhas.",
            ["Your body has nothing left to give."] = "Seu corpo não tem mais nada a dar.",
            ["The priest drains the worst of it. Nothing unwelcome is left in you."] = "O padre drena o pior. Nada indesejado resta em você.",
            // ---- stealth
            ["Stealth: notice -"] = "Furtividade: aviso -",
            // ---- traps
            ["disarm a trap you have found"] = "desarmar uma armadilha achada",
            ["There is nothing to disarm here."] = "Não há o que desarmar aqui.",
            ["There is no known trap to disarm nearby."] = "Não há armadilha conhecida para desarmar por perto.",
            ["There is no known trap to disarm there."] = "Não há armadilha conhecida para desarmar ali.",
            ["You cannot do that now."] = "Você não pode fazer isso agora.",
            ["You set it off!"] = "Você a disparou!",
            // ---- achievements
            ["Achievements"] = "Conquistas", ["local"] = "locais", ["What you have done across all your runs."] = "O que você já fez em todas as suas runs.",
            ["Achievement: "] = "Conquista: ",
            ["First Blood"] = "Primeiro Sangue", ["Kill something."] = "Mate alguma coisa.",
            ["Slayer"] = "Carniceiro", ["Kill 100 creatures in one run."] = "Mate 100 criaturas em uma run.",
            ["Delver"] = "Escavador", ["Reach depth 5."] = "Chegue ao nível 5.",
            ["Deep Delver"] = "Mergulhador", ["Reach depth 10."] = "Chegue ao nível 10.",
            ["Into the Abyss"] = "No Abismo", ["Reach depth 15."] = "Chegue ao nível 15.",
            ["Veteran"] = "Veterano", ["Reach level 10."] = "Chegue ao nível 10 de personagem.",
            ["Champion"] = "Campeão", ["Reach level 20."] = "Chegue ao nível 20 de personagem.",
            ["Survivor"] = "Sobrevivente", ["Live through 5000 turns."] = "Sobreviva a 5000 turnos.",
            ["Pious"] = "Devoto", ["Reach 100 piety with a god."] = "Chegue a 100 de piedade com um deus.",
            ["Scholar"] = "Erudito", ["Know ten spells."] = "Conheça dez magias.",
            ["Rich"] = "Rico", ["Carry 1000 gold."] = "Carregue 1000 de ouro.",
            ["Relic Hunter"] = "Caçador de Relíquias", ["Hold an artifact."] = "Segure um artefato.",
            ["Shade Breaker"] = "Quebra-Sombras", ["Lay a dead hero's shade to rest."] = "Dê descanso à sombra de um herói morto.",
            ["Cartographer"] = "Cartógrafo", ["See six regions of the world."] = "Veja seis regiões do mundo.",
            ["Light Footed"] = "Pés Leves", ["Reach depth 3 without killing anything."] = "Chegue ao nível 3 sem matar nada.",
            ["Out of the Pit"] = "Fora do Poço", ["Escape with the Amulet."] = "Escape com o Amuleto.",
            ["Iron Will"] = "Vontade de Ferro", ["Escape with the Amulet in Hardcore."] = "Escape com o Amuleto no Hardcore.",
            ["Daily Victor"] = "Vitória Diária", ["Escape with the Amulet in a daily challenge."] = "Escape com o Amuleto em um desafio diário.",
            // ---- past runs
            ["Past runs"] = "Expedições passadas", ["history"] = "histórico", ["Your finished expeditions, newest first."] = "Suas expedições terminadas, as mais novas primeiro.",
            ["No finished runs yet."] = "Nenhuma expedição terminada ainda.", ["Hero"] = "Herói", ["Class"] = "Classe", ["End"] = "Fim",
            ["died"] = "morreu", ["won"] = "venceu", ["abandoned"] = "abandonou",
            // ---- morgue and death
            ["morgue"] = "necrotério", ["level"] = "nível", ["Killed by"] = "Morto por", ["Abandoned the run."] = "Abandonou a expedição.",
            ["Escaped with the Amulet of Yendor."] = "Escapou com o Amuleto de Yendor.",
            ["Deepest level"] = "Nível mais fundo", ["Turns"] = "Turnos", ["Kills"] = "Mortes", ["Score"] = "Pontos",
            ["Ended on"] = "Terminou em", ["Ended in"] = "Terminou em", ["Follower of"] = "Devoto de", ["piety"] = "piedade", ["Seed"] = "Semente",
            ["Attributes"] = "Atributos", ["Skills"] = "Habilidades", ["Perks"] = "Vantagens", ["Spells"] = "Magias", ["Equipment"] = "Equipamento",
            ["Inventory"] = "Mochila", ["Last words"] = "Últimas palavras", ["Wielding"] = "Empunhando", ["Wearing"] = "Vestindo",
            ["Ring"] = "Anel", ["Amulet"] = "Amuleto", ["(empty)"] = "(vazia)", ["gold"] = "ouro",
            ["starvation"] = "fome", ["poison"] = "veneno", ["burning"] = "queimadura", ["a spike trap"] = "uma armadilha de espetos",
            ["a poison dart trap"] = "uma armadilha de dardos", ["a fire trap"] = "uma armadilha de fogo", ["a trap"] = "uma armadilha",
            ["an electric shock"] = "um choque elétrico", ["a potion of acid"] = "uma poção de ácido", ["unknown causes"] = "causas desconhecidas",
            ["an amulet of strangulation"] = "um amuleto de estrangulamento", ["abandoned the run"] = "abandono",
            // ---- auto-explore, stairs travel, rest
            ["search for traps and doors / rest until healed"] = "procurar armadilhas e portas / descansar até curar",
            ["auto-explore / travel to the stairs"] = "explorar sozinho / ir até a escada",
            ["There is nothing to explore here."] = "Não há o que explorar aqui.",
            ["You cannot rest here."] = "Você não pode descansar aqui.",
            ["Not with enemies in sight."] = "Não com inimigos à vista.",
            ["You are already rested."] = "Você já está descansado.",
            ["You are already on the stairs."] = "Você já está na escada.",
            ["You feel rested."] = "Você se sente descansado.",
            ["You stop at some items."] = "Você para diante de alguns itens.",
            ["Nothing left to explore here."] = "Não há mais o que explorar aqui.",
            ["You have seen all there is to see here."] = "Você já viu tudo o que há para ver aqui.",
            ["You have not found any stairs yet."] = "Você ainda não achou nenhuma escada.",
            ["Something is in the way."] = "Algo está no caminho.",
            // ---- menu / settings
            ["Menu"] = "Menu", ["Esc resumes"] = "Esc volta",
            ["Resume"] = "Continuar", ["Back"] = "Voltar", ["Save game"] = "Salvar jogo",
            ["Display"] = "Tela", ["Audio"] = "Áudio", ["Game"] = "Jogo",
            ["Theme"] = "Tema", ["CRT"] = "CRT", ["Text size"] = "Tamanho do texto", ["Language"] = "Idioma",
            ["Master volume"] = "Volume geral", ["Music"] = "Música", ["Effects"] = "Efeitos",
            ["Controls"] = "Controles", ["rebind keys"] = "remapear teclas",
            ["Main menu"] = "Menu principal", ["Quit game"] = "Sair do jogo",
            ["Back."] = "Voltar.",
            ["Writes this run to disk. Dying deletes the save."] = "Grava a expedição no disco. Morrer apaga o save.",
            ["Flat pixels, no scanlines."] = "Pixels chapados, sem linhas de varredura.",
            ["Curvature, scanlines, phosphor glow."] = "Curvatura, linhas de varredura e brilho de fósforo.",
            ["The largest that fits the window."] = "O maior que cabe na janela.",
            ["Saved now; takes effect when sound is added."] = "Salvo; vale quando o som for adicionado.",
            ["Rebind any key."] = "Remapeie qualquer tecla.",
            ["Saves the run and returns to the title."] = "Salva a expedição e volta ao título.",
            ["Saves the run and closes the game."] = "Salva a expedição e fecha o jogo.",
            ["Portuguese (Brazil) or English."] = "Português (Brasil) ou inglês.",
            ["↑↓ select   ←→ change   Enter choose"] = "↑↓ escolher   ←→ mudar   Enter confirma",
            ["Game saved."] = "Jogo salvo.", ["You are already here."] = "Você já está aqui.",
            ["Nothing to save yet."] = "Nada para salvar ainda.",
            ["indigo dark, bone text, full colour"] = "índigo escuro, texto de osso, cores cheias",
            ["IBM 5151 amber phosphor"] = "fósforo âmbar do IBM 5151",
            ["P1 green phosphor"] = "fósforo verde P1",
            ["the sixteen colours of a PC"] = "as dezesseis cores de um PC",
            ["Auto"] = "Auto", ["Off"] = "Desligado", ["Subtle"] = "Sutil", ["Strong"] = "Forte",
            ["Amber"] = "Âmbar", ["Phosphor"] = "Fósforo",

            // ---- opening
            ["Overworld"] = "Mundo",
            ["The Ossuary remembers."] = "O Ossuary lembra.",
            // ---- overworld and towns
            ["You cannot walk away from it. Attack (k) or flee (<)."] = "Você não escapa andando. Ataque (k) ou fuja (<).",
            ["You break away and run."] = "Você se solta e corre.",
            ["You leave town and return to the road."] = "Você deixa a cidade e volta à estrada.",
            ["You step back outside."] = "Você volta para a rua.",
            ["The shopkeeper cannot afford that much gold."] = "O lojista não tem tanto ouro.",
            ["The shopkeeper cannot afford that."] = "O lojista não pode pagar isso.",
            ["You cannot afford that."] = "Você não tem ouro para isso.",
            ["You emerge into the open air."] = "Você emerge ao ar livre.",
            ["You descend the staircase."] = "Você desce a escada.",
            ["You climb the stairs."] = "Você sobe a escada.",
            ["The stairs lead up to daylight."] = "A escada sobe até a luz do dia.",
            ["There is no staircase down here."] = "Não há escada para baixo aqui.",
            ["There is no way up here."] = "Não há como subir aqui.",
            ["The stairs lead no further down. This is the bottom of this branch."] = "A escada não desce mais. Este é o fundo deste ramo.",
            ["You enter the dungeons beneath the earth."] = "Você entra nas masmorras sob a terra.",
            ["The air here feels heavy, as if something precious waits in the dark."] = "O ar aqui pesa, como se algo precioso esperasse no escuro.",
            ["You emerge into the open air, the Amulet of Yendor blazing against your chest."] = "Você emerge ao ar livre, o Amuleto de Yendor ardendo contra o peito.",
            ["Press Ctrl-Q again within 3s to abandon the run."] = "Aperte Ctrl-Q de novo em 3s para abandonar a expedição.",
        };

        static readonly List<(Regex, string)> Rx = new List<(Regex, string)>
        {
            R(@"Seed (\d+)\. Press \? for help\.", "Semente $1. Aperte ? para ajuda."),
            R(@"A cold draught\. Someone died here: (.+) the (.+)\.", "Uma corrente fria. Alguém morreu aqui: $1, $2."),
            R(@"A (.+) blocks your path!", "Algo barra seu caminho: $1!"),
            R(@"Attack it with k, or flee with <\. It is (.+)\.", "Ataque com k ou fuja com <. Parece $1."),
            R(@"You travel (\d+) hours into (.+)\.", "Você viaja $1 horas até $2."),
            R(@"You walk for (\d+) hours\.", "Você caminha por $1 horas."),
            R(@"You arrive in (.+)\. Some (\d+) people live here\.", "Você chega a $1. Cerca de $2 pessoas vivem aqui."),
            R(@"You step into (.+)\.", "Você entra em $1."),
            R(@"  (.+) - (\d+) gold", "  $1 - $2 de ouro"),
            R(@"You buy (\d+) gold pieces for (\d+) gold\.", "Você compra $1 moedas de ouro por $2 de ouro."),
            R(@"You buy (.+) for (\d+) gold\.", "Você compra $1 por $2 de ouro."),
            R(@"You sell (.+) for (\d+) gold\.", "Você vende $1 por $2 de ouro."),
            R(@"You hand over (\d+) gold pieces\.", "Você entrega $1 moedas de ouro."),
            R(@"You enter (.+)\.", "Você entra em $1."),
            R(@"You arrive at (.+)\.", "Você chega a $1."),
            R(@"Escaped after (\d+) turns, (\d+) kills, depth (\d+)\. The Ossuary remembers\.", "Fuga após $1 turnos, $2 mortes, profundidade $3. O Ossuary lembra."),
            R(@"Could not save: (.+)", "Não foi possível salvar: $1"),
        };
    }
}
