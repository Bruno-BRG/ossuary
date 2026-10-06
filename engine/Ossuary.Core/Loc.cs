using System;
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
    public static partial class Loc
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
            AddSpellText();
            AddMonsterText();
            AddItemText();
            AddArtifactText();
            AddRivalText();
            AddMsgText();
            AddGameText();
            AddBodyText();
            AddCraftText();
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
            string u = UCore(en);
            if (u != null) return u;
            string town = TownText.Translate(en);
            if (town != null) return town;
            string body = BodyLine(en);
            if (body != null) return body;
            string rx = ApplyRx(en);
            if (rx != null) return rx;
            Miss(en);
            return en;
        }

        // First matching pattern wins. Captured names (monsters, items, perks) are translated on the way through,
        // and the usual Portuguese contractions are applied to the result.
        static readonly Dictionary<string, string> _rxMemo = new Dictionary<string, string>();
        static string ApplyRx(string en)
        {
            if (_rxMemo.TryGetValue(en, out var hit)) return hit;
            string result = null;
            foreach (var (re, rep) in Rx)
            {
                var m = re.Match(en);
                if (!m.Success) continue;
                result = Finish(Expand(m, rep));
                break;
            }
            if (_rxMemo.Count > 4000) _rxMemo.Clear();
            _rxMemo[en] = result;
            return result;
        }

        /// <summary>
        /// The last step of any translated sentence, wherever its pattern lives (here or in <see cref="TownText"/>):
        /// every proper name becomes Portuguese and the Portuguese contractions are applied, so "Dizem que $1 em
        /// The Iron Hills" cannot leave half a sentence in English.
        /// </summary>
        internal static string Finish(string s)
        {
            foreach (var kv in Names) s = s.Replace(kv.Key, kv.Value);
            return Contract(s);
        }

        /// <summary>
        /// A message matched by a pattern of the town table: its captures go through the same name handling as
        /// <see cref="ApplyRx"/> ({a1}'s articles, "$1" name translation, contractions), so a shop or party name
        /// inside a sentence is translated too.
        /// </summary>
        internal static string TranslateMatch(Match m, string rep) => Finish(Expand(m, rep));
        /// <summary>Dev audit: when set, every string that reached the player in Portuguese without a translation is collected here (`headless loc`).</summary>
        public static HashSet<string> Misses;
        static void Miss(string s) { if (Misses != null && Regex.IsMatch(s, "[A-Za-z]{3,}")) Misses.Add(s); }

        public static string F(string en, params object[] args) => string.Format(T(en), args);


        /// <summary>
        /// Region, place, branch and faction names: proper nouns, applied inside dynamic messages too. Every name the
        /// world generator can put in a sentence belongs here (see <c>World/OverworldGen.cs</c>: <c>DungeonName</c>
        /// and <c>FeatureName</c>), or a rumour would name half a place in English.
        /// </summary>
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            ["The Verdant Reach"] = "O Alcance Verdejante", ["Ashen Marches"] = "Marcas de Cinza",
            ["The Sunken Vale"] = "O Vale Afundado", ["Gallowmoor"] = "Charneca da Forca",
            ["The Iron Hills"] = "As Colinas de Ferro", ["Whisperfen"] = "Pântano Sussurrante",
            ["The Craglands"] = "As Terras Fendidas", ["Emberdown"] = "Brasa Baixa",
            ["The Hollow Wastes"] = "Os Ermos Ocos", ["The Wilds"] = "Os Ermos",
            ["The Dungeons"] = "As Masmorras", ["The Mines of Dwarfdeep"] = "As Minas de Dwarfdeep",
            ["The Mines"] = "As Minas",   // the short name older run records store ("Ended in The Mines 4")
            ["The Warrens"] = "As Tocas", ["The Sunken Vaults"] = "Os Cofres Afundados", ["The Ashen Spire"] = "A Torre de Cinza",
            ["The Annex"] = "O Anexo", ["The Reach League"] = "A Liga do Reach",
            // The four houses that keep a book on the hero: a message can open with one of them.
            ["The Guild"] = "A Guilda", ["The Watch"] = "A Guarda", ["The Temple"] = "O Templo", ["The Cult of the Drowned"] = "O Culto dos Afogados",
            // The mouths of the branches, as the overworld names them.
            ["the Iron Delve"] = "a Escavação de Ferro", ["the Deep Delve"] = "a Escavação Profunda",
            ["the Sunless Vaults"] = "os Cofres sem Sol", ["the Drowned Vaults"] = "os Cofres Afogados",
            ["the Weeping Warren"] = "a Toca Chorosa", ["the Gnawed Warren"] = "a Toca Roída",
            ["the Hollow Deep"] = "o Abismo Oco", ["the Bleak Catacombs"] = "as Catacumbas Desoladas",
            ["the Gilded Galleries"] = "as Galerias Douradas", ["the Gloomhold Dungeons"] = "as Masmorras de Gloomhold",
            ["the Cinder Spire"] = "a Torre de Brasa", ["the Ashen Spire"] = "a Torre de Cinza",
            // Landmarks anyone on the road can point at.
            ["an ancient ruin"] = "uma ruína antiga", ["a cave mouth"] = "uma boca de caverna",
            ["an abandoned mine"] = "uma mina abandonada", ["a ruined keep"] = "um fortim em ruínas",
            ["a wayshrine"] = "um santuário de estrada", ["a stone bridge"] = "uma ponte de pedra",
            ["a landmark"] = "um marco",
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
            ["apply a tool (pick-axe, lock pick, an instrument)"] = "usar ferramenta (picareta, gazua, um instrumento)", ["arrows"] = "setas",
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
            ["Str"] = "For", ["Dex"] = "Des", ["Con"] = "Con", ["Int"] = "Int", ["Wis"] = "Sab", ["Cha"] = "Car", ["AC"] = "CA",
            ["Forest"] = "Floresta", ["Grass"] = "Campo", ["Hills"] = "Colinas", ["Mountain"] = "Montanha", ["Swamp"] = "Pântano",
            ["Snow"] = "Neve", ["Ash"] = "Cinza", ["Ruins"] = "Ruínas", ["Road"] = "Estrada", ["Sand"] = "Areia", ["Water"] = "Água",
            ["Shallow"] = "Rasa", ["DeepWater"] = "Água funda",
            ["Adventurer"] = "Aventureiro", ["Fighter"] = "Guerreiro", ["Rogue"] = "Ladino", ["Cleric"] = "Clérigo",
            ["Wizard"] = "Mago", ["Ranger"] = "Patrulheiro", ["Paladin"] = "Paladino", ["Necromancer"] = "Necromante",
            ["Human"] = "Humano", ["Dwarf"] = "Anão", ["Elf"] = "Elfo", ["Halfling"] = "Halfling", ["Orc"] = "Orc", ["Gnome"] = "Gnomo", ["Ashen"] = "Cinzento",
            ["Begin as "] = "Começar como ",
            // ---- hero titles (Roles.Titles), shown on the character sheet and in the morgue
            ["Explorer"] = "Explorador", ["Legend"] = "Lenda", ["Squire"] = "Escudeiro", ["Warrior"] = "Guerreiro",
            ["Knight"] = "Cavaleiro", ["Footpad"] = "Salteador", ["Thief"] = "Ladrão", ["Master Thief"] = "Mestre Ladrão",
            ["Acolyte"] = "Acólito", ["Priest"] = "Sacerdote", ["Vicar"] = "Vigário", ["High Priest"] = "Sumo Sacerdote",
            ["Apprentice"] = "Aprendiz", ["Conjurer"] = "Conjurador", ["Sorcerer"] = "Feiticeiro", ["Archmage"] = "Arquimago",
            ["Tracker"] = "Rastreador", ["Hunter"] = "Caçador", ["Warden"] = "Guardião", ["Beastmaster"] = "Senhor das Feras",
            ["Gallant"] = "Galante", ["Crusader"] = "Cruzado", ["Templar"] = "Templário", ["Lord Paladin"] = "Senhor Paladino",
            ["Gravewalker"] = "Andarilho de Covas", ["Bonecaller"] = "Chamador de Ossos", ["Deathbinder"] = "Amarrador da Morte",
            ["Lord of Ossuary"] = "Senhor do Ossuário",
        };

        static Dictionary<string, string> _ui;
        static int _uiTownCount;

        /// <summary>Interface text: whole-string match only, so names and numbers pass through untouched. Used by TextBuilder.</summary>
        public static string U(string s)
        {
            if (Current == Lang.En || string.IsNullOrEmpty(s)) return s;
            string r = UCore(s) ?? TownText.Translate(s) ?? ApplyRx(s);
            if (r != null) return r;
            Miss(s);
            return s;
        }

        // Exact match, then the same with surrounding padding peeled off (" IN VIEW ", "Here ").
        static string UCore(string s)
        {
            if (_ui == null || _uiTownCount != TownText.Pt.Count)
            {
                _uiTownCount = TownText.Pt.Count;   // dialogue, quest and rumour texts register their PT as they are first used
                var d = new Dictionary<string, string>();
                foreach (var kv in Pt) d[kv.Key] = kv.Value;
                foreach (var kv in Ui) d[kv.Key] = kv.Value;
                foreach (var kv in TownText.Pt) d[kv.Key] = kv.Value;
                foreach (var kv in Names) d[kv.Key] = kv.Value;
                foreach (var kv in new List<KeyValuePair<string, string>>(d))
                    if (kv.Key.Length <= 26) d[kv.Key.ToUpperInvariant()] = kv.Value.ToUpperInvariant();
                _ui = d;
            }
            if (_ui.TryGetValue(s, out var v)) return v;
            string core = s.Trim();
            if (core.Length > 0 && core.Length != s.Length && _ui.TryGetValue(core, out v))
            {
                int lead = s.IndexOf(core, StringComparison.Ordinal);
                return s.Substring(0, lead) + v + s.Substring(lead + core.Length);
            }
            // A head noun is a word: an item can also begin with its enchantment ("+2 dagger", "-1 adaga").
            if (core.Length > 3 && core.Length < 70 && (char.IsLetter(core[0]) || core[0] == '+' || core[0] == '-'))
            {
                var item = ItemPt(core);
                if (item != null) return item.Value.Pt;
                var qty = Regex.Match(core, @"^(.+?) (\(x\d+\))$");
                if (qty.Success && ItemPt(qty.Groups[1].Value) != null) return ItemPt(qty.Groups[1].Value).Value.Pt + " " + qty.Groups[2].Value;
            }
            return null;
        }

        static (Regex, string) R(string pattern, string replacement) => (new Regex("^" + pattern + "$", RegexOptions.CultureInvariant), replacement);

        static readonly Dictionary<string, string> Pt = new Dictionary<string, string>
        {
            ["The restless shade is laid to rest at last."] = "A sombra inquieta enfim descansa.",
            ["Tiles"] = "Tiles", ["Square"] = "Quadrado", ["Narrow"] = "Estreito",
            ["Each map cell is two columns wide: the world looks square."] = "Cada célula do mapa ocupa duas colunas: o mundo fica quadrado.",
            ["One column per map cell: the world looks tall and narrow."] = "Uma coluna por célula: o mundo fica alto e estreito.",
            // ---- difficulty modes
            ["Mode"] = "Modo", ["Normal"] = "Normal", ["Classic"] = "Clássico", ["Hardcore"] = "Hardcore",
            ["The standard game."] = "O jogo padrão.", ["No hunger. Food is only a luxury."] = "Sem fome. Comida é só luxo.",
            ["One save: it is erased when you resume. No quicksave."] = "Um único save: some ao retomar. Sem save rápido.",
            ["Hardcore: the run is saved only when you quit."] = "Hardcore: a run só é salva ao sair.",
            ["◄► mode  Enter begin  Esc back"] = "◄► modo  Enter começa  Esc volta",
            ["Daily board"] = "Placar diário", ["No daily runs yet."] = "Nenhuma run diária ainda.", ["Daily"] = "Diário",
            ["D daily board   Esc back"] = "D placar diário   Esc volta",
            // ---- challenge modes
            ["Dive"] = "Mergulho", ["Naked"] = "Pelado",
            ["Start on depth 5, a few levels up. Score x2."] = "Começa no nível 5, com alguns níveis a mais. Pontos x2.",
            ["No weapon, armour or shield. One more advancement. Score x2."] = "Sem arma, armadura ou escudo. Um avanço a mais. Pontos x2.",
            // ---- relics (the names and the lore of every artifact live in Loc.Names.cs, with the monsters and the items)
            ["Relics worn: "] = "Relíquias vestidas: ",
            // ---- training
            ["Trained"] = "Treinado", ["Skills rise only when you buy them with XP (Shift+N)."] = "As habilidades só sobem quando você as compra com XP (Shift+N).",
            ["train a skill with XP (Trained mode)"] = "treinar uma habilidade com XP (modo Treinado)",
            ["Your skills grow with use. Training is for the Trained mode."] = "Suas habilidades crescem com o uso. Treinar é do modo Treinado.",
            ["There is nothing left you can train."] = "Não há mais nada que você possa treinar.",
            ["Train what?"] = "Treinar o quê?",
            // ---- crafting
            ["craft: combine what you carry"] = "criar: combinar o que você carrega",
            ["Make what?"] = "Fazer o quê?", ["molotov"] = "molotov", ["bone blade"] = "lâmina de osso", ["bone-studded armour"] = "armadura cravejada de osso",
            ["potion of oil + candle"] = "poção de óleo + vela", ["a blade + remains"] = "uma lâmina + restos", ["armour + two remains"] = "armadura + dois restos",
            ["two potions of healing"] = "duas poções de cura",
            ["There is no room to work on the road."] = "Não há espaço para trabalhar na estrada.",
            ["You have nothing you can combine into something better."] = "Você não tem nada que dê para combinar em algo melhor.",
            ["You no longer have what that needs."] = "Você não tem mais o que isso pede.",
            ["You are not holding that."] = "Você não está segurando isso.", ["You cannot throw it there."] = "Você não pode jogar ali.",
            ["That is too far to throw."] = "Longe demais para jogar.", ["It would shatter on stone. Aim at open floor."] = "Quebraria na pedra. Mire no chão livre.",
            ["The molotov bursts into flame!"] = "O molotov explode em chamas!",
            // ---- vaults
            ["The brass key turns, and the lock lets go. The key crumbles in your hand."] = "A chave de latão gira e a fechadura cede. A chave se desfaz na sua mão.",
            ["The door is locked and you have nothing to pick it with."] = "A porta está trancada e você não tem como abri-la.",
            // ---- reputation, jobs and the road
            ["Standing"] = "Reputação", ["Job"] = "Serviço", ["Jobs done"] = "Serviços feitos",
            ["the Watch"] = "a Guarda", ["the Temple"] = "o Templo", ["the Guild"] = "a Guilda", ["the Cult of the Drowned"] = "o Culto dos Afogados",
            ["revered"] = "reverenciado", ["trusted"] = "de confiança", ["known"] = "conhecido", ["distrusted"] = "malvisto", ["hated"] = "odiado",
            // Why a house changes its mind: the whole phrase after "for", so "<house> thinks the better of you for <reason>" is one sentence.
            ["bribing the Watch"] = "subornar a Guarda",
            ["calling in a favour"] = "cobrar um favor",
            ["killing a priest"] = "matar um sacerdote",
            ["killing a guard"] = "matar um guarda",
            ["putting down road bandits"] = "derrubar bandidos de estrada",
            ["robbing the dead"] = "roubar os mortos",
            ["sharing water with a pilgrim"] = "dividir água com um peregrino",
            ["feeding refugees"] = "alimentar refugiados",
            ["robbing refugees"] = "roubar refugiados",
            ["saving a delver"] = "salvar um escavador",
            ["robbing a wounded delver"] = "roubar um escavador ferido",
            ["burying a stranger"] = "enterrar um desconhecido",
            ["beating a rival party to the bottom"] = "chegar ao fundo antes de um grupo rival",
            ["losing the race"] = "perder a corrida",
            ["catching the thief"] = "pegar o ladrão",
            ["letting the thief go"] = "deixar o ladrão ir",
            ["putting down the toll bandits"] = "derrubar os bandidos do pedágio",
            ["leaving the toll bandits to the roads"] = "deixar os bandidos do pedágio nas estradas",
            ["laying the restless to rest"] = "dar descanso aos inquietos",
            ["carrying the Drowned's vial"] = "carregar o frasco dos Afogados",
            ["putting down a king of the dark"] = "derrubar um rei do escuro",
            // One reason is a whole sentence in English (Game.Ledger.cs): only the phrase after "for" belongs in the message.
            ["The Watch hears you threatened a citizen."] = "ameaçar um cidadão",
            // ---- the rival party that races you down the Dungeons
            ["Reach level 7 of The Dungeons before the rival party does."] = "Chegue ao nível 7 das Masmorras antes do grupo rival.",
            ["The rival party reached level 7 first, and they are not shy about it."] = "O grupo rival chegou ao nível 7 primeiro, e não faz segredo disso.",
            ["Hired Hand"] = "Mão Contratada", ["Finish three Guild jobs."] = "Termine três serviços da Guilda.",
            ["Well Liked"] = "Bem Quisto", ["Be revered by any house."] = "Seja reverenciado por qualquer casa.",
            ["Rest until morning (they know your face)"] = "Descansar até de manhã (conhecem seu rosto)",
            ["A vial from the back room (the Cult sells)"] = "Um frasco dos fundos (o Culto vende)",
            ["A vial of black water, handed over without a word."] = "Um frasco de água negra, entregue sem uma palavra.",
            ["You are already carrying as many jobs as you can finish."] = "Você já carrega tantos serviços quanto consegue terminar.",
            ["An abandoned camp"] = "Um acampamento abandonado", ["A caravan"] = "Uma caravana", ["A ruin by the road"] = "Uma ruína na estrada",
            ["A roadside shrine"] = "Um santuário na estrada", ["A toll"] = "Um pedágio", ["A body by the road"] = "Um corpo na estrada",
            ["Search the camp"] = "Revistar o acampamento", ["Rest here until you are well"] = "Descansar aqui até se curar",
            ["Buy rations (two)"] = "Comprar rações (duas)", ["Buy a healing potion"] = "Comprar uma poção de cura", ["Ask about the vial in the black cart"] = "Perguntar do frasco na carroça preta",
            ["Climb down into the ruin"] = "Descer para a ruína", ["Dig under the fallen wall"] = "Cavar sob o muro caído",
            ["Leave an offering"] = "Deixar uma oferenda", ["Kneel and swear to whoever listens"] = "Ajoelhar e jurar a quem ouvir",
            ["Pay the toll"] = "Pagar o pedágio", ["Refuse and draw"] = "Recusar e sacar", ["Bluff: the Watch is right behind you"] = "Blefar: a Guarda vem logo atrás",
            ["Take the purse"] = "Pegar a bolsa", ["Bury the body"] = "Enterrar o corpo", ["Move on"] = "Seguir viagem",
            // ---- companions
            ["Hire a sellsword"] = "Contratar um mercenário", ["Send my sellsword home"] = "Mandar meu mercenário para casa",
            ["Your sellsword shakes your hand and goes back to the bar."] = "Seu mercenário aperta sua mão e volta para o balcão.",
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
            ["Boss Slayer"] = "Matador de Chefes", ["Kill a branch boss."] = "Mate o chefe de um branch.",
            ["Kingslayer"] = "Matador de Reis", ["Kill three branch bosses in one run."] = "Mate três chefes de branch em uma run.",
            ["Past the Portal"] = "Além do Portal", ["Step through the portal on Dungeons 4."] = "Atravesse o portal em Dungeons 4.",
            ["Debts Paid"] = "Dívidas Pagas", ["Kill the Annex Warden."] = "Mate o Guardião do Anexo.",
            ["The portal takes you, and the world folds."] = "O portal leva você, e o mundo se dobra.", ["You step back through the portal."] = "Você volta pelo portal.",
            ["A portal shimmers here. Press > (Shift + .) to step through."] = "Um portal cintila aqui. Aperte > (Shift + .) para atravessar.",
            ["The portal is dead. Whatever it led to is gone."] = "O portal está morto. O que havia do outro lado se foi.",
            // ---- the gods: every field of a GodDef is read on the altar panel or in a line that names one
            // (Entities/Gods.cs). The name is a proper noun and stays; the title takes the article the panel prints.
            ["the Last Lamp"] = "a Última Lâmpada", ["light and mercy"] = "luz e misericórdia",
            ["+1 Wis and +6 max HP"] = "+1 Sab e +6 PV máx",
            ["killing the undead; sacred magic"] = "matar os mortos-vivos; magia sagrada",
            ["necromancy; killing the harmless"] = "necromancia; matar os indefesos",
            ["full healing, cleansing and a full measure of mana"] = "cura completa, purificação e uma medida cheia de mana",
            ["wounds close faster"] = "feridas fecham mais rápido", ["necrotic resistance 30%"] = "resistência necrótica 30%",
            ["the Hammer Beneath"] = "o Martelo Subterrâneo", ["war and stone"] = "guerra e pedra", ["+1 Str"] = "+1 For",
            ["kills, above all of foes stronger than you"] = "mortes, acima de tudo de inimigos mais fortes que você",
            ["illusions, vanishing and trickery"] = "ilusões, sumir e trapaça",
            ["a +1 enchantment on your weapon"] = "um encantamento +1 na sua arma",
            ["+1 to hit in melee"] = "+1 para acertar no corpo a corpo", ["+2 melee damage"] = "+2 de dano no corpo a corpo",
            ["Mother of Ash"] = "Mãe de Cinzas", ["fire and ruin"] = "fogo e ruína",
            // "Con" and "Int" are the same word in both languages: a gift has to say the whole one to read as Portuguese.
            ["+1 Con"] = "+1 Constituição", ["+1 Int"] = "+1 Inteligência",
            ["killing with fire"] = "matar com fogo", ["killing with cold"] = "matar com frio",
            ["a flame that rides your blows for 300 turns"] = "uma chama que cavalga seus golpes por 300 turnos",
            ["fire resistance 30%"] = "resistência a fogo 30%", ["fire resistance 60%; your blows burn"] = "resistência a fogo 60%; seus golpes queimam",
            ["the Drowned King"] = "o Rei Afogado", ["death and still water"] = "morte e água parada",
            ["killing with death magic; raising the dead"] = "matar com magia da morte; erguer os mortos", ["sacred magic"] = "magia sagrada",
            ["two skeletons to serve you for 200 turns"] = "dois esqueletos para te servir por 200 turnos",
            ["every kill restores 2 HP"] = "cada morte restaura 2 PV",
            ["the Quiet One"] = "o Silencioso", ["shadow and theft"] = "sombra e roubo", ["+1 Dex"] = "+1 Des",
            ["killing the sleeping and unaware; illusions"] = "matar os que dormem e não percebem; ilusões",
            ["roaring war cries"] = "gritos de guerra",
            ["invisibility for 100 turns and a full measure of Vigor"] = "invisibilidade por 100 turnos e uma medida cheia de Vigor",
            ["+2 evasion"] = "+2 de evasão", ["foes notice you from one cell less"] = "inimigos notam você de uma casa a menos",
            ["the Weeping Seam"] = "a Costura Chorosa", ["flesh and change"] = "carne e mudança",
            ["a mutation that is always a gift"] = "uma mutação que é sempre um presente",
            ["every mutation that takes hold; surviving corruption"] = "cada mutação que se firma; sobreviver à corrupção",
            ["purging yourself of the Ossuary"] = "expulsar o Ossuário de você",
            ["mutations are far more often gifts"] = "mutações são muito mais vezes presentes",
            ["poison resistance 30%"] = "resistência a veneno 30%",
            // A god frowns for a reason (Game.Gods.cs): the reason is a phrase inside the sentence.
            ["the killing of the harmless"] = "matar os indefesos", ["the desecration of the dead"] = "a profanação dos mortos",
            ["your purging"] = "sua purificação", ["your necromancy"] = "sua necromancia",
            ["your prayers to another light"] = "suas preces a outra luz", ["your roar"] = "seu rugido", ["your skulking"] = "sua espreita",
            // ---- the altar menu, row by row (Game.Gods.cs); the rows that name a god are patterns in Loc.Msgs.cs
            ["Pray"] = "Rezar", ["Offer gold"] = "Oferecer ouro", ["Offer an item"] = "Oferecer um item",
            ["Sacrifice a corpse"] = "Sacrificar um cadáver", ["Step back"] = "Recuar",
            ["The altar is dead. Nothing answers."] = "O altar está morto. Nada responde.",
            ["Sacrifice what?"] = "Sacrificar o quê?", ["Offer what?"] = "Oferecer o quê?",
            ["You may pray."] = "Você pode rezar.",
            // ---- what a choice prompt asks (Commands.cs)
            ["Drink what?"] = "Beber o quê?", ["Apply what?"] = "Usar o quê?", ["Eat what?"] = "Comer o quê?",
            ["Wield what?"] = "Empunhar o quê?", ["Wear what?"] = "Vestir o quê?", ["Take off what?"] = "Tirar o quê?",
            ["Put on which ring or amulet?"] = "Pôr qual anel ou amuleto?", ["Read what?"] = "Ler o quê?",
            ["Zap what?"] = "Usar qual varinha?", ["Sell what?"] = "Vender o quê?",
            // ---- the bosses, with the article a death cause gives them ("Killed by the Gaoler")
            ["the Gaoler"] = "o Carcereiro", ["the Stone Warden"] = "o Guardião de Pedra", ["the Rat King"] = "o Rei dos Ratos",
            ["the Annex Warden"] = "o Guardião do Anexo", ["the Ashen Regent"] = "o Regente de Cinzas",
            // ---- panels (Ui.cs): the road names a panel, the rest are one-line labels
            ["The Road"] = "A Estrada", ["Nothing known in this school."] = "Nada conhecido nesta escola.",
            ["Dlvl"] = "Nvl",
            // ---- what a tile is wet, frozen or on fire with (Surfaces.cs), read in the look panel and in messages
            ["shallow water"] = "água rasa", ["ice"] = "gelo", ["flames"] = "chamas",
            ["spilled oil"] = "óleo derramado", ["dry brush"] = "mato seco",
            // ---- the mood of a depth, beside DEPTH in the header (Theme.cs)
            ["catacombs"] = "catacumbas", ["flooded vaults"] = "cofres alagados", ["bone pits"] = "fossas de ossos", ["hellmouth"] = "boca do inferno",
            // ---- who a job, a rumour or a line comes from (Quests, Rumours, Game.Main.cs), shown as the speaker
            ["a rumour"] = "um rumor", ["the tavern"] = "a taverna", ["the Captain of the Watch"] = "o Capitão da Guarda",
            ["the gravekeeper"] = "o coveiro", ["The houses"] = "As casas",
            ["A new cycle begins. What you did is remembered."] = "Um novo ciclo começa. O que você fez é lembrado.",
            // ---- the open panel and the mode travel in the frame for a screen reader (Session.Frame)
            ["None"] = "Nenhum", ["History"] = "Histórico", ["Choice"] = "Escolha", ["Travel"] = "Viagem", ["Death"] = "Morte",
            ["Win"] = "Vitória", ["Settings"] = "Opções", ["Create"] = "Criação", ["Advance"] = "Evolução", ["Service"] = "Serviços",
            ["Runs"] = "Expedições", ["Dungeon"] = "Masmorra", ["TownMap"] = "Cidade", ["GameOver"] = "Fim de jogo", ["Won"] = "Vitória",
            // ---- the six endings of the Amulet, and what the house does with it (Game.Main.cs, Dialogues.cs)
            ["The priest buries the Amulet in the rite Yendor meant. The dead settle, the lamps burn lower, and the pit closes."] = "O sacerdote enterra o Amuleto no rito que Yendor quis. Os mortos assentam, as lâmpadas baixam, e o poço se fecha.",
            ["The seal turns, and every vault of the old kingdom opens. The Reach is rich by morning, and it is not the living who collect."] = "O selo gira, e todo cofre do velho reino se abre. O Reach fica rico ao amanhecer, e não são os vivos que recolhem.",
            ["The Watch posts a standing guard and gives you the keys. You are the Warden now, and the seal is yours to keep from everyone."] = "A Guarda põe um posto de vigia e entrega as chaves a você. Agora você é o Carcereiro, e o selo é seu para guardar de todos.",
            ["The Amulet goes to the highest bidder. The Reach eats well for a year, and the Ossuary has a new owner."] = "O Amuleto vai para quem mais pagar. O Reach come bem por um ano, e o Ossuary tem um novo dono.",
            ["The League cannot pay five thousand. It pays what it has, takes the Amulet, and thanks you. The world goes on as it was."] = "A Liga não tem cinco mil. Paga o que tem, leva o Amuleto e agradece. O mundo segue como estava.",
            ["You stamp the seal where Yendor stamped it, and the stone takes it. You are the Archivist now. The Ossuary will not forget you."] = "Você carimba o selo onde Yendor carimbou, e a pedra o aceita. Agora você é o Arquivista. O Ossuary não vai esquecer você.",
            ["The house watches. You are one of theirs now, and everyone can see it."] = "A casa observa. Agora você é um deles, e todos podem ver.",
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
            ["auto-explore / travel to the stairs / to an altar or fountain"] = "explorar sozinho / ir até a escada / até um altar ou fonte",
            ["You have not found a fountain or an altar yet."] = "Você ainda não achou uma fonte ou um altar.",
            ["You are already there."] = "Você já está lá.",
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
            ["Short square-wave bleeps: hits, kills, wounds, warnings."] = "Bipes curtos de onda quadrada: golpes, mortes, ferimentos, avisos.",
            ["Music is not written yet."] = "A música ainda não foi escrita.",
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
            // ---- keyboards: the stairs are symbols, and a Portuguese layout has no key with them alone
            ["> is Shift + . on every keyboard, US or ABNT2."] = "> é Shift + . em qualquer teclado, US ou ABNT2.",
            ["< is Shift + , on every keyboard, US or ABNT2."] = "< é Shift + , em qualquer teclado, US ou ABNT2.",
            ["Shift + . and Shift + , on any keyboard"] = "Shift + . e Shift + , em qualquer teclado",
            ["this help (on ABNT2: AltGr + W)"] = "esta ajuda (no ABNT2: AltGr + W)",
        };

        static readonly List<(Regex, string)> Rx = new List<(Regex, string)>
        {
            R(@"Seed (\d+)\. Press \? for help\.", "Semente $1. Aperte ? para ajuda."),
            R(@"(.+) takes your coin and your word\. They will follow you down\.", "$1 pega seu dinheiro e sua palavra. Vai te seguir lá para baixo."),
            R(@"The (.+) has fallen\.", "{a1} tombou."),
            R(@"You make (.+)\.", "Você faz $1."),
            R(@"Your (.+) shows wear\.", "{a1} mostra desgaste."),
            R(@"Your (.+) shatters!", "{a1} se despedaça!"),
            R(@"A cold draught\. Someone died here: (.+) the (.+)\.", "Uma corrente fria. Alguém morreu aqui: $1, $2."),
            R(@"A (.+) blocks your path!", "Algo barra seu caminho: {u1}!"),
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
            // Spell riders and results.
            R(@"The (.+) is held fast\.", "$1 está preso."),
            R(@"The (.+) reels, stunned\.", "$1 cambaleia, atordoado."),
            R(@"The (.+) slows to a crawl\.", "$1 desacelera até quase parar."),
            R(@"The (.+) flees in terror!", "$1 foge aterrorizado!"),
            R(@"The (.+) slumps, asleep\.", "$1 desaba, dormindo."),
            R(@"The (.+) staggers, confused\.", "$1 cambaleia, confuso."),
            R(@"The (.+) blunders about, blind\.", "$1 tropeça às cegas."),
            R(@"The (.+) sickens\.", "$1 adoece."),
            R(@"The (.+) bleeds freely\.", "$1 sangra muito."),
            R(@"The (.+) is left open to every blow\.", "$1 fica aberto a todo golpe."),
            R(@"The (.+) is yours, for now\.", "$1 é seu, por enquanto."),
            R(@"The (.+) shakes off the spell\.", "$1 se livra da magia."),
            R(@"The (.+) tears free\.", "$1 se solta."),
            R(@"The (.+) stays on its feet\.", "$1 se mantém de pé."),
            R(@"The (.+) is torn away\.", "$1 é arrancado dali."),
            R(@"You lift (\d+) gold off the (.+)\.", "Você surrupia $1 de ouro {d2}."),
            R(@"(\d+) beasts? lie down and sleep\.", "$1 fera(s) se deita(m) e dorme(m)."),
            R(@"(\d+) locks? spring open\.", "$1 fechadura(s) se abre(m)."),
            R(@"The rock crumbles away \((\d+) cells\)\.", "A rocha desmorona ($1 casas)."),
            R(@"You sense (\d+) traps? nearby\.", "Você sente $1 armadilha(s) por perto."),
            R(@"You drink in (\d+) life\.", "Você bebe $1 de vida."),
        };
    }
}
