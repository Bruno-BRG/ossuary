using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Ossuary.Core
{
    /// <summary>
    /// Portuguese names for monsters and items, and the machinery that lets a pattern in <see cref="Rx"/>
    /// carry them: every captured group goes through <see cref="Part"/>, so "You hit the jackal" becomes
    /// "Você acerta o chacal" without a line per monster. Gender is kept so articles agree.
    /// </summary>
    public static partial class Loc
    {
        /// <summary>English name to (Portuguese name, feminine).</summary>
        static readonly Dictionary<string, (string Pt, bool Fem)> Nm = new Dictionary<string, (string, bool)>();

        static void N(string en, string pt, bool fem = false)
        {
            Nm[en] = (pt, fem);
            if (!Pt.ContainsKey(en)) Pt[en] = pt;
        }

        /// <summary>
        /// Tiles and scenery: labels that also appear inside sentences ("You dig through {a1}"), so they carry a gender.
        /// The label itself is the same text either way.
        /// </summary>
        static readonly Dictionary<string, (string Pt, bool Fem)> Scenery = new Dictionary<string, (string, bool)>();
        static void TN(string en, string pt, bool fem = false)
        {
            Scenery[en] = (pt, fem);
            if (!Pt.ContainsKey(en)) Pt[en] = pt;
        }

        // ------------------------------------------------------------------ monsters
        static void AddMonsterText()
        {
            N("grid bug", "percevejo de grade"); N("newt", "tritão"); N("jackal", "chacal"); N("giant rat", "rato gigante");
            N("kobold", "kobold"); N("dwarf", "anão"); N("gnome", "gnomo"); N("hobbit", "hobbit"); N("gnome lord", "lorde gnomo");
            N("orc", "orc"); N("orc shaman", "xamã orc"); N("orc chieftain", "chefe orc"); N("brown mold", "mofo marrom");
            N("yellow mold", "mofo amarelo"); N("gray mold", "mofo cinzento"); N("gas spore", "esporo de gás");
            N("little lizard", "lagartinho"); N("floating eye", "olho flutuante"); N("cave spider", "aranha das cavernas", true);
            N("centipede", "centopeia", true); N("human zombie", "zumbi humano"); N("skeleton", "esqueleto");
            N("viper", "víbora", true); N("giant python", "píton gigante");
            N("gnome mummy", "múmia gnoma", true); N("jackal warden", "guardião chacal"); N("stalker", "espreitador");
            N("troll", "troll"); N("ogre", "ogro"); N("ogre lord", "lorde ogro"); N("hill giant", "gigante da colina");
            N("werenothing", "lobisnada"); N("wood nymph", "ninfa do bosque", true); N("dwarf lord", "lorde anão");
            N("ice troll", "troll de gelo"); N("wandering wraith", "espectro errante"); N("lich", "lich");
            N("dread knight", "cavaleiro do pavor"); N("fire giant", "gigante de fogo"); N("tiamat", "Tiamat", true);
            N("guardian of the deep", "guardião das profundezas"); N("gaol hound", "cão do cárcere"); N("cave bat", "morcego das cavernas");
            N("ore golem", "golem de minério"); N("plague rat", "rato da peste"); N("rat swarm", "enxame de ratos");
            N("drowned dead", "morto afogado"); N("tide wraith", "espectro da maré"); N("ember wisp", "centelha de brasa", true);
            N("ash wraith", "espectro de cinzas"); N("air elemental", "elemental do ar"); N("arcane hound", "cão arcano");
            N("bone golem", "golem de ossos"); N("bone hound", "cão de ossos"); N("earth elemental", "elemental da terra");
            N("fire elemental", "elemental do fogo"); N("gargoyle", "gárgula", true); N("ghoul", "carniçal");
            N("guardian angel", "anjo da guarda"); N("imp", "diabrete"); N("mirror image", "imagem espelhada", true);
            N("shadow double", "duplo sombrio"); N("spectral blade", "lâmina espectral", true); N("spirit bear", "urso espiritual");
            N("spirit boar", "javali espiritual"); N("spirit hawk", "falcão espiritual"); N("spirit spider", "aranha espiritual", true);
            N("spirit wolf", "lobo espiritual"); N("spiritual weapon", "arma espiritual", true); N("stone sentinel", "sentinela de pedra", true);
            N("storm sprite", "espírito da tempestade"); N("summoned bat", "morcego invocado"); N("treant", "ent");
            N("water elemental", "elemental da água"); N("wight", "aparição", true); N("wraith thrall", "servo espectral");
            N("stray dog", "cão vira-lata");   // the town's pet (Town.cs): a creature, so it takes an article like any other
            N("cat", "gato");
            // The hired companions (Game.Companions.cs) are named by what they are, in the sidebar and before every line.
            N("sellsword", "mercenário"); N("shield-bearer", "porta-escudo"); N("cutthroat", "degolador");
            // The kings of the branches: a rumour names them, so they are names like any other.
            N("Gaoler", "Carcereiro"); N("Stone Warden", "Guardião de Pedra"); N("Rat King", "Rei dos Ratos");
            N("Drowned King", "Rei Afogado"); N("Ashen Regent", "Regente de Cinzas"); N("Annex Warden", "Guardião do Anexo");
        }

        // ------------------------------------------------------------------ items
        /// <summary>"of X" tails shared by potions, scrolls, wands, rings, amulets and affixes: the whole phrase after the noun.</summary>
        static readonly Dictionary<string, string> OfPt = new Dictionary<string, string>();

        static void O(string en, string pt) { OfPt[en] = pt; }

        static void AddItemText()
        {
            // Tails.
            foreach (var kv in new[] {
                ("ESP", "de ESP"), ("faith", "da fé"), ("health", "da saúde"), ("life saving", "de salva-vidas"), ("resistance", "de resistência"),
                ("spell power", "de poder mágico"), ("stealth", "de furtividade"), ("strangulation", "de estrangulamento"),
                ("the berserker", "do berserker"), ("the glacier", "da geleira"), ("the grave", "da cova"), ("the hunter", "do caçador"),
                ("the magi", "dos magos"), ("the oracle", "do oráculo"), ("the phoenix", "da fênix"), ("the sage", "do sábio"),
                ("the shadow", "da sombra"), ("the tempest", "da tempestade"), ("the wolf", "do lobo"), ("vigor", "de vigor"), ("warding", "de proteção"),
                ("agility", "de agilidade"), ("brilliance", "de brilhantismo"), ("fire", "de fogo"), ("fortitude", "de fortitude"), ("frost", "de gelo"),
                ("life", "de vida"), ("might", "de poder"), ("precision", "de precisão"), ("reaping", "da ceifa"), ("ruin", "da ruína"),
                ("shadows", "de sombras"), ("slaying", "de abate"), ("storms", "de tempestades"), ("the archmage", "do arquimago"),
                ("the bear", "do urso"), ("the fox", "da raposa"), ("the lion", "do leão"), ("the mage", "do mago"), ("the owl", "da coruja"),
                ("the sentinel", "da sentinela"), ("the sphinx", "da esfinge"), ("the storm", "da tormenta"), ("vitality", "de vitalidade"),
                ("accuracy", "de precisão"), ("aggravate monster", "de provocar monstros"), ("constitution", "de constituição"), ("dexterity", "de destreza"),
                ("evasion", "de evasão"), ("fire resistance", "de resistência ao fogo"), ("flames", "de chamas"), ("focus", "de foco"),
                ("frost resistance", "de resistência ao gelo"), ("insight", "de intuição"), ("intellect", "de intelecto"), ("invisibility", "de invisibilidade"),
                ("mana", "de mana"), ("poison resistance", "de resistência a veneno"), ("protection", "de proteção"), ("searching", "de busca"),
                ("slow digestion", "de digestão lenta"), ("sparks", "de faíscas"), ("storm resistance", "de resistência a tempestade"), ("strength", "de força"),
                ("sustain ability", "de atributos firmes"), ("the assassin", "do assassino"), ("the grave-knight", "do cavaleiro da cova"),
                ("vampirism", "de vampirismo"), ("warning", "de aviso"),
                ("banishment", "de banimento"), ("beasts", "de feras"), ("blinking", "de piscar"), ("bone servants", "de servos de osso"), ("charging", "de recarga"),
                ("clarity", "de clareza"), ("confuse monster", "de confundir monstro"), ("darkness", "de escuridão"), ("destroy armor", "de destruir armadura"),
                ("earth", "de terra"), ("elementals", "de elementais"), ("enchant armour", "de encantar armadura"), ("enchant weapon", "de encantar arma"),
                ("entangling", "de enredar"), ("fear", "de medo"), ("fireball", "de bola de fogo"), ("gales", "de vendavais"), ("genocide", "de genocídio"),
                ("haste", "de pressa"), ("healing", "de cura"), ("ice", "de gelo"), ("identify", "de identificar"), ("knocking", "de bater à porta"),
                ("levitation", "de levitação"), ("light", "de luz"), ("lightning", "de relâmpago"), ("mapping", "de mapeamento"), ("mass charm", "de enfeitiçar em massa"),
                ("mending", "de remendo"), ("meteors", "de meteoros"), ("punishment", "de castigo"), ("remove curse", "de remover maldição"),
                ("revival", "de revivência"), ("sanctuary", "de santuário"), ("sensing", "de sentir"), ("slumber", "de torpor"), ("smiting", "de golpear"),
                ("teleportation", "de teletransporte"), ("trapfinding", "de achar armadilhas"),
                ("acid", "de ácido"), ("bones", "de ossos"), ("charming", "de enfeitiçar"), ("cold", "de frio"), ("confusion", "de confusão"),
                ("create monster", "de criar monstro"), ("digging", "de cavar"), ("draining", "de drenar"), ("fire seeds", "de sementes de fogo"),
                ("fireballs", "de bolas de fogo"), ("holding", "de prender"), ("lava", "de lava"), ("locking", "de trancar"), ("magic missiles", "de mísseis mágicos"),
                ("nothing", "de nada"), ("opening", "de abrir"), ("rot", "de podridão"), ("searing light", "de luz escaldante"), ("sleep", "de sono"),
                ("slowness", "de lentidão"), ("striking", "de golpe"), ("the blizzard", "da nevasca"), ("thunder", "de trovão"), ("venom", "de veneno"), ("webs", "de teias"),
                ("cunning", "de astúcia"), ("extra healing", "de cura maior"), ("fire protection", "de proteção contra fogo"), ("frost protection", "de proteção contra gelo"),
                ("full healing", "de cura total"), ("gain ability", "de ganho de atributo"), ("gain level", "de ganho de nível"), ("giants", "de gigantes"),
                ("grace", "de graça"), ("hallucination", "de alucinação"), ("heroism", "de heroísmo"), ("mutation", "de mutação"), ("oil", "de óleo"),
                ("poison", "de veneno"), ("regeneration", "de regeneração"), ("see invisible", "de ver o invisível"), ("sleeping", "de sono"),
                ("speed", "de velocidade"), ("stone skin", "de pele de pedra"), ("storm protection", "de proteção contra tempestade"),
                ("the mind", "da mente"), ("wisdom", "de sabedoria"), ("acid resistance", "de resistência a ácido"),
                ("deep stone", "da pedra profunda"), ("the north", "do norte"), ("the wind-walker", "do andarilho do vento"), ("the bat", "do morcego"),
                ("elvenkind", "élficos"), ("the wind", "do vento"), ("the sun", "do sol"), ("the faithful", "dos fiéis"), ("ogre power", "do poder do ogro"),
                ("the healer", "do curandeiro"), ("spellcasting", "de conjuração"), ("flame", "de chama"), ("defence", "de defesa") })
                O(kv.Item1, kv.Item2);

            // Head nouns that take an "of X" tail.
            foreach (var h in new[] {
                ("potion", "poção", true), ("scroll", "pergaminho", false), ("wand", "varinha", true), ("ring", "anel", false), ("amulet", "amuleto", false),
                ("boots", "botas", true), ("cloak", "capa", true), ("gauntlets", "manoplas", true), ("gloves", "luvas", true), ("bracers", "braçadeiras", true),
                ("shield", "escudo", false), ("staff", "cajado", false), ("mantle", "manto", false), ("sandals", "sandálias", true), ("aegis", "égide", true),
                ("charm", "amuleto", false), ("quarterstaff", "bordão", false), ("robe", "manto", false), ("crown", "coroa", true), ("laurel", "louro", false) })
                Head[h.Item1] = (h.Item2, h.Item3);

            // Whole names.
            foreach (var w in new[] {
                ("amulet versus poison", "amuleto contra veneno", false), ("charm", "berloque", false), ("locket", "medalhão", false), ("pendant", "pingente", false), ("talisman", "talismã", false),
                ("archmage's robe", "manto do arquimago", false), ("banded mail", "cota de talas", true), ("battle-priest's mail", "cota do sacerdote de guerra", true),
                ("brigandine", "brigantina", true), ("chain mail", "cota de malha", true), ("dragonhide armour", "armadura de couro de dragão", true),
                ("druid's vestments", "vestes de druida", true), ("elven leather", "couro élfico", false), ("full plate", "armadura completa", true),
                ("half plate", "meia armadura", true), ("lamellar", "armadura lamelar", true), ("leather armour", "armadura de couro", true),
                ("mage's robe", "manto de mago", false), ("mithril shirt", "camisa de mithril", true), ("necromancer's shroud", "mortalha do necromante", true),
                ("padded armour", "armadura acolchoada", true), ("plate mail", "armadura de placas", true), ("priest's vestments", "vestes de sacerdote", true),
                ("quilted gambeson", "gibão acolchoado", false), ("ring mail", "cota de anéis", true), ("scale mail", "cota de escamas", true),
                ("shadowsilk tunic", "túnica de seda sombria", true), ("shaman's furs", "peles de xamã", true), ("splint mail", "armadura de talas", true),
                ("storm-cloth robe", "manto de pano de tempestade", false), ("studded leather", "couro cravejado", false),
                ("ghoul-leather boots", "botas de couro de carniçal", true), ("iron boots", "botas de ferro", true), ("leather boots", "botas de couro", true),
                ("boots of elvenkind", "botas élficas", true), ("boots of fire walking", "botas de caminhar sobre fogo", true), ("boots of striding", "botas de passadas largas", true),
                ("boots of deep stone", "botas da pedra profunda", true), ("cloak", "capa", true), ("cloak of elvenkind", "capa élfica", true),
                ("feathered cloak", "capa de penas", true), ("mantle of the grave", "manto da cova", false), ("wolf pelt", "pele de lobo", true),
                ("human corpse", "cadáver humano", false), ("small corpse", "cadáver pequeno", false),
                ("apple", "maçã", true), ("carrot", "cenoura", true), ("cram ration", "ração cram", true), ("food ration", "ração de comida", true),
                ("lemon", "limão", false), ("orange", "laranja", true), ("pear", "pera", true), ("tripe ration", "ração de tripa", true), ("piece of jade", "pedaço de jade", false),
                ("gauntlets", "manoplas", true), ("gloves of dexterity", "luvas de destreza", true), ("gloves of spellcasting", "luvas de conjuração", true),
                ("gloves of the healer", "luvas do curandeiro", true), ("leather gloves", "luvas de couro", true), ("mage's mitts", "mitenes de mago", true),
                ("thieves' gloves", "luvas de ladrão", true), ("witch's gloves", "luvas de bruxa", true),
                ("gold piece", "moeda de ouro", true), ("silver piece", "moeda de prata", true),
                ("bone crown", "coroa de osso", true), ("cat's-eye circlet", "diadema olho-de-gato", false), ("circlet", "diadema", false), ("coif of mail", "capuz de malha", false),
                ("crown of thorns", "coroa de espinhos", true), ("dwarvish helm", "elmo anão", false), ("great helm", "grande elmo", false), ("hood of shadows", "capuz de sombras", false),
                ("horned helm", "elmo com chifres", false), ("iron halo", "halo de ferro", false), ("laurel of the sage", "louro do sábio", false), ("leather cap", "gorro de couro", false),
                ("orcish helm", "elmo orc", false), ("plague doctor's mask", "máscara de médico da peste", true), ("skull cap of the dead", "solidéu dos mortos", false),
                ("visored helm", "elmo com viseira", false), ("winged helm", "elmo alado", false), ("wizard's hat", "chapéu de mago", false),
                ("gemstone", "gema", true), ("gold locket", "medalhão de ouro", false), ("pearls", "pérolas", true), ("valuable necklace", "colar valioso", false),
                ("bone ring", "anel de osso", false), ("gold band", "aliança de ouro", true), ("jade ring", "anel de jade", false), ("silver band", "aliança de prata", true),
                ("rock", "pedra", true),
                ("bone shield", "escudo de osso", false), ("buckler", "broquel", false), ("duelist's buckler", "broquel de duelista", false), ("kite shield", "escudo em gota", false),
                ("large shield", "escudo grande", false), ("mirror shield", "escudo espelhado", false), ("rampart", "baluarte", false), ("rune shield", "escudo rúnico", false),
                ("shield", "escudo", false), ("small shield", "escudo pequeno", false), ("tower shield", "escudo de torre", false),
                ("bag of holding", "bolsa de contenção", true), ("blindfold", "venda", true), ("candle", "vela", true), ("chest", "baú", false), ("large box", "caixa grande", true),
                ("lock pick", "gazua", true), ("magic lamp", "lâmpada mágica", true), ("mirror", "espelho", false), ("oilskin sack", "saco impermeável", false),
                ("pick-axe", "picareta", true), ("tinning kit", "kit de estanhar", false), ("unicorn horn", "chifre de unicórnio", false),
                ("bandage", "atadura", true),
                ("ashwood staff", "cajado de freixo", false), ("axe", "machado", false), ("bardiche", "bardiche", true), ("bastard sword", "espada bastarda", true),
                ("battle axe", "machado de batalha", false), ("bone staff", "cajado de osso", false), ("claymore", "claymore", true), ("club", "clava", true), ("crossbow", "besta", true),
                ("crystal staff", "cajado de cristal", false), ("cutlass", "alfanje", false), ("dagger", "adaga", true), ("druid's crook", "cajado do druida", false),
                ("dwarven waraxe", "machado de guerra anão", false), ("elven blade", "lâmina élfica", true), ("elven bow", "arco élfico", false), ("estoc", "estoque", false),
                ("falchion", "falchion", false), ("flail", "mangual", false), ("flanged mace", "maça flangeada", true), ("glaive", "glaive", true), ("great axe", "grande machado", false),
                ("greatsword", "montante", false), ("halberd", "alabarda", true), ("hand axe", "machadinha", true), ("javelin", "azagaia", true), ("katana", "katana", true),
                ("kris", "kris", true), ("long sword", "espada longa", true), ("lucerne hammer", "martelo lucerna", false), ("mace", "maça", true), ("main gauche", "main gauche", true),
                ("maul", "malho", false), ("morning star", "estrela da manhã", true), ("pike", "pique", false), ("quarterstaff", "bordão", false), ("rapier", "rapieira", true),
                ("runed dagger", "adaga rúnica", true), ("sabre", "sabre", false), ("sacrificial knife", "faca sacrificial", true), ("scimitar", "cimitarra", true),
                ("short bow", "arco curto", false), ("short sword", "espada curta", true), ("sling", "funda", true), ("spear", "lança", true), ("spiked club", "clava com espinhos", true),
                ("staff", "cajado", false),
                ("stiletto", "estilete", false), ("thornwood staff", "cajado de espinheiro", false), ("trident", "tridente", false), ("war axe", "machado de guerra", false),
                ("war hammer", "martelo de guerra", false), ("war pick", "picareta de guerra", true), ("whip", "chicote", false), ("witch's wand", "varinha de bruxa", true),
                ("wizard's staff", "cajado de mago", false) })
                N(w.Item1, w.Item2, w.Item3);

            // The keys, remains and papers the hero can pick up outside the catalogues: they are read out like any item.
            N("brass key", "chave de latão", true); N("skeleton corpse", "cadáver de esqueleto"); N("amulet of Yendor", "amuleto de Yendor");
            N("warden's ledger page", "página do livro do carcereiro", true); N("dwarf council's tally", "contagem do conselho dos anões", true);
            N("archivist's last entry", "última anotação do arquivista", true); N("spire order roll", "rol de ordens da Torre");

            // Crafted gear: registered with its gender, so "You are now wearing {a1}" says "a armadura".
            N("bone blade", "lâmina de osso", true);
            N("bone-studded armour", "armadura cravejada de osso", true);

            // Books carry their article in the name.
            foreach (var b in new[] {
                ("a bestiary of the unseen", "um bestiário do invisível"), ("a book of grave-bargains", "um livro de barganhas da cova"), ("a book of illusions", "um livro de ilusões"),
                ("a book of mercy", "um livro da misericórdia"), ("a book of prayers", "um livro de preces"), ("a book of shadows", "um livro de sombras"),
                ("a book of stone lore", "um livro do saber da pedra"), ("a book of wards", "um livro de proteções"), ("a book of weather", "um livro do clima"),
                ("a manual of strikes", "um manual de golpes"),
                ("a book of whispers", "um livro de sussurros"), ("a breviary of the faithful", "um breviário dos fiéis"), ("a charnel primer", "uma cartilha do ossuário"),
                ("a codex of beast-shapes", "um códice de formas de fera"), ("a codex of storms", "um códice de tempestades"), ("a cutpurse's primer", "uma cartilha do batedor de carteiras"),
                ("a druid's handbook", "um manual do druida"), ("a folio of flames", "um fólio de chamas"), ("a folio of glamours", "um fólio de encantos"),
                ("a footpad's tricks", "os truques do assaltante"), ("a forester's lore", "o saber do guarda-florestal"), ("a grimoire of the dead", "um grimório dos mortos"),
                ("a guidebook to the deep", "um guia das profundezas"), ("a hedge-wizard's notes", "as notas de um mago de estrada"), ("a lay-brother's psalms", "os salmos de um irmão leigo"),
                ("a manual of the body", "um manual do corpo"), ("a manual of the knife", "um manual da faca"), ("a menagerie of the lesser planes", "uma coleção dos planos menores"),
                ("a missal of wrath", "um missal da ira"), ("a mummer's folio", "o fólio de um comediante"), ("a primer of embers", "uma cartilha de brasas"),
                ("a psalter of light", "um saltério de luz"), ("a ranger's almanac", "o almanaque do patrulheiro"), ("a spellbook", "um grimório"),
                ("a tome of conjuration", "um tomo de conjuração"), ("a tome of evocation", "um tomo de evocação"), ("a treatise on resistance", "um tratado sobre resistência"),
                ("a ward-smith's ledger", "o registro do forjador de proteções"), ("the annals of ruin", "os anais da ruína"), ("the assassin's testament", "o testamento do assassino"),
                ("the black litany", "a litania negra"), ("the bonewright's notes", "as notas do ossuarista"), ("the book of four winds", "o livro dos quatro ventos"),
                ("the book of last things", "o livro das últimas coisas"), ("the book of the long night", "o livro da longa noite"), ("the canticle of dawn", "o cântico da aurora"),
                ("the dreamless book", "o livro sem sonhos"), ("the giant's manual", "o manual do gigante"), ("the green psalter", "o saltério verde"), ("the last rite", "o último rito"),
                ("the lich's catechism", "o catecismo do lich"), ("the night-blade's creed", "o credo da lâmina noturna"), ("the pyrelord's treatise", "o tratado do senhor da pira"),
                ("the rotted codex", "o códice apodrecido"), ("the stone and the gate", "a pedra e o portal"), ("the tide and the stone", "a maré e a pedra"),
                ("the verdant grimoire", "o grimório verdejante"), ("the wild hunt", "a caçada selvagem") })
                N(b.Item1, b.Item2);

            // Affix adjectives (masculine form; the feminine is derived).
            foreach (var a in new[] {
                ("arcane", "arcano"), ("balanced", "balanceado"), ("bladeturning", "desviador de lâminas"), ("bloodthirsty", "sedento de sangue"), ("brutal", "brutal"),
                ("dragon-warded", "protegido contra dragões"), ("draining", "drenante"), ("fire-warded", "protegido contra fogo"), ("flaming", "flamejante"),
                ("fortified", "fortificado"), ("frost-warded", "protegido contra gelo"), ("frozen", "congelado"), ("ghostly", "fantasmagórico"),
                ("grave-warded", "protegido contra a morte"), ("hallowed", "consagrado"), ("holy", "sagrado"), ("keen", "afiado"), ("mage-woven", "tecido por magos"),
                ("masterwork", "de mestre"), ("mithril-lined", "forrado de mithril"), ("radiant", "radiante"), ("razor-edged", "de fio navalha"), ("rotting", "podre"),
                ("rune-etched", "gravado com runas"), ("sage's", "do sábio"), ("sanctified", "santificado"), ("searing", "escaldante"), ("serrated", "serrilhado"),
                ("shadowed", "sombrio"), ("shocking", "elétrico"), ("storm-warded", "protegido contra tempestade"), ("stormforged", "forjado na tempestade"),
                ("stormproof", "à prova de tempestade"), ("sturdy", "robusto"), ("thundering", "trovejante"), ("troll-hide", "de couro de troll"), ("vampiric", "vampírico"),
                ("venom-proof", "à prova de veneno"), ("venomous", "venenoso"), ("vital", "vital"), ("wintry", "invernal"),
                ("blessed", "abençoado"), ("cursed", "amaldiçoado"), ("magical", "mágico"), ("enchanted", "encantado") })
                Adj[a.Item1] = a.Item2;

            // Wear (Items/Item.cs ConditionWord): it leads the name like blessed and cursed.
            foreach (var a in new[] { ("blunted", "cego"), ("chipped", "lascado"), ("dented", "amassado"), ("battered", "surrado") })
                Adj[a.Item1] = a.Item2;

            // Materials (Items/Materials.cs): an invariable "de X" after the noun ("espada longa de aço").
            foreach (var m in new[] { ("copper", "cobre"), ("bronze", "bronze"), ("iron", "ferro"), ("steel", "aço"), ("silver", "prata"),
                ("cold iron", "ferro frio"), ("mithril", "mithril"), ("adamantine", "adamantina"), ("obsidian", "obsidiana"), ("bone", "osso"), ("wood", "madeira") })
            {
                MatPt[m.Item1] = "de " + m.Item2;
                if (!Pt.ContainsKey(m.Item1)) Pt[m.Item1] = m.Item2;
            }
            foreach (var o in new[] { ("copper ore", "minério de cobre"), ("iron ore", "minério de ferro"), ("silver ore", "minério de prata"),
                ("mithril ore", "minério de mithril"), ("adamantine ore", "minério de adamantina") })
                N(o.Item1, o.Item2);
        }

        // ------------------------------------------------------------------ artifacts and sets
        /// <summary>
        /// Every unique the branches can give you, and every set they belong to. A relic is named aloud when it is
        /// carried, dropped or worn, so its name is a name like any other and keeps its gender; the lore is a whole
        /// sentence read out when the relic is identified, so it is registered as a plain entry. One source of truth:
        /// the names of <c>Items/Affixes.cs</c> and <c>Items/Artifacts.More.cs</c>.
        /// </summary>
        static void AddArtifactText()
        {
            foreach (var a in new[] {
                // The first thirteen: the relics of the five branches.
                ("Veil of the First Cell", "Véu da Primeira Cela", false),
                ("Dwarfdeep Cleaver", "Cutelo de Dwarfdeep", false),
                ("Rat King's Tooth", "Dente do Rei dos Ratos", false),
                ("Crown of the Drowned King", "Coroa do Rei Afogado", true),
                ("Ashfall", "Queda de Cinzas", true),
                ("Tidecaller's Gauntlets", "Manoplas do Chamador de Marés", true),
                ("Brinewalkers", "Andarilhos da Salmoura", false),
                ("Mantle of Ash", "Manto de Cinza", false),
                ("Cinder Plate", "Placa de Brasa", true),
                ("Tithe-Collector's Mantle", "Manto do Cobrador de Dízimos", false),
                ("Hollow Ribs", "Costelas Ocas", true),
                ("Gravedigger's Spade", "Pá do Coveiro", true),
                ("Gnawed Cowl", "Capuz Roído", false),
                // The Dungeons.
                ("Gaoler's Keyring", "Chaveiro do Carcereiro", false),
                ("Ratcatcher's Cudgel", "Porrete do Caçador de Ratos", false),
                ("Mourner's Blade", "Lâmina do Enlutado", true),
                ("Prisoner's Prayer", "Prece do Prisioneiro", true),
                ("Heartwood Staff", "Cajado do Cerne", false),
                ("Warden's Bulwark", "Baluarte do Guardião", false),
                ("Nightglove", "Luva da Noite", true),
                ("Stairwalker's Cloak", "Capa do Andarilho das Escadas", true),
                // The Mines of Dwarfdeep.
                ("Pickaxe of the First Vein", "Picareta da Primeira Veia", true),
                ("Delver's Lamp-Helm", "Elmo-Lanterna do Escavador", false),
                ("Stonebinder's Gauntlets", "Manoplas do Vinculador de Pedra", true),
                ("Deepmaw Plate", "Placa da Goela Funda", true),
                ("Ore Golem's Heart", "Coração do Golem de Minério", false),
                ("Barkhide Vest", "Colete de Casca", false),
                ("Mantle of the Tempest", "Manto da Tormenta", false),
                // The Warrens.
                ("Rat King's Whiskers", "Bigodes do Rei dos Ratos", false),
                ("Scrap-King's Cleaver", "Cutelo do Rei da Sucata", false),
                ("Plaguebearer's Mask", "Máscara do Portador da Peste", true),
                ("Burrower's Boots", "Botas do Cavador", true),
                ("Mantle of Many Teeth", "Manto de Muitos Dentes", false),
                ("Gloves of Static", "Luvas da Estática", true),
                ("Antlered Crown", "Coroa Galhada", true),
                // The Sunken Vaults.
                ("Tidecaller's Trident", "Tridente do Chamador de Marés", false),
                ("Pearl of the Drowned", "Pérola dos Afogados", true),
                ("Saltwhite Mail", "Cota Branca de Sal", true),
                ("Lantern of the Deep", "Lanterna das Profundezas", true),
                ("Midnight Hood", "Capuz da Meia-Noite", false),
                ("Weeper's Hands", "Mãos da Carpideira", true),
                ("Sunken Sceptre", "Cetro Afundado", false),
                ("Lich-Queen's Crown", "Coroa da Rainha-Lich", true),
                // The Ashen Spire.
                ("Cindercrown", "Coroa de Brasa", true),
                ("Stormcaller's Staff", "Cajado do Chamador de Tempestades", false),
                ("Pyre-Knight's Sword", "Espada do Cavaleiro da Pira", true),
                ("Ashmonk's Beads", "Contas do Monge de Cinzas", true),
                ("Lich-Queen's Shroud", "Mortalha da Rainha-Lich", true),
                ("Wraithwalkers", "Andarilhos Espectrais", false),
                ("Spire-Lord's Staff", "Cajado do Senhor da Torre", false),
                ("Lich-Queen's Phylactery", "Filactério da Rainha-Lich", false),
                ("Regent's Tithe", "Dízimo do Regente", false),
                ("Final Ember", "Brasa Final", true),
                // The Annex.
                ("Auditor's Spectacles", "Óculos do Auditor", false),
                ("Ledger of Debts", "Registro das Dívidas", false),
                ("Dusk Daggers", "Adagas do Crepúsculo", true) })
                N(a.Item1, a.Item2, a.Item3);

            // The lore of every relic, read out when it is identified.
            foreach (var kv in new[] {
                ("Woven by the first prisoner, from the shirts of the ones who never came back.",
                 "Tecido pelo primeiro prisioneiro, com as camisas dos que nunca voltaram."),
                ("Left behind when the dwarves abandoned the black water. It still remembers the stone.",
                 "Deixado para trás quando os anões abandonaram a água negra. Ainda se lembra da pedra."),
                ("Gnawed, not forged. It drinks.", "Roído, não forjado. Bebe."),
                ("Cold as the water that took him. It still wants a head to wear it.",
                 "Fria como a água que o levou. Ainda quer uma cabeça para usá-la."),
                ("Forged to burn the dead. It could not tell the difference.",
                 "Forjada para queimar os mortos. Não distinguia os vivos deles."),
                ("They close like a tide. Whoever wore them once held the water back.",
                 "Fecham-se como a maré. Quem as vestiu um dia segurou a água de volta."),
                ("They never dry. They never slip, either.", "Nunca secam. Também nunca escorregam."),
                ("Grey flakes fall from it as you walk, and every flake is still warm.",
                 "Flocos cinzentos caem dele enquanto você anda, e cada floco ainda está morno."),
                ("Black plate, glowing at the seams. The last owner is still inside, in a way.",
                 "Placa negra, brilhando nas costuras. O último dono ainda está dentro, de certo modo."),
                ("Woven from the receipts of everything the Annex ever took. It has no weight. It has a great deal of memory.",
                 "Tecido com os recibos de tudo o que o Anexo já tomou. Não tem peso. Tem muita memória."),
                ("A cage of ribs, hollowed out to be worn. It holds you like it held something else.",
                 "Uma gaiola de costelas, ocadas para serem vestidas. Segura você como segurou outra coisa."),
                ("It has dug more graves than anything alive. It wants to keep digging.",
                 "Abriu mais covas que qualquer coisa viva. Quer continuar cavando."),
                ("The Rat King's thoughts still crawl in the lining. They are clever. They are hungry.",
                 "Os pensamentos do Rei dos Ratos ainda rastejam no forro. São espertos. Estão com fome."),
                ("A ring of keys melted into one band. Every lock gave way to it but the one on the way out.",
                 "Um molho de chaves derretido numa só aliança. Toda fechadura cedeu a ele, menos a da saída."),
                ("Knotted oak, black with old blood. The rats never learned to fear it, and the ones who tried are in the walls.",
                 "Carvalho nodoso, negro de sangue velho. Os ratos nunca aprenderam a temê-lo, e os que tentaram estão nas paredes."),
                ("Made for one purpose, which was to be quiet. It has outlived the reason.",
                 "Feita para um único propósito: o silêncio. Sobreviveu ao motivo."),
                ("Scratched into a coin with a spoon by someone who did not expect to be heard. Someone heard.",
                 "Riscada numa moeda com uma colher por alguém que não esperava ser ouvido. Alguém ouviu."),
                ("Grown, not cut, in a cellar where nothing should grow. It still leans toward a light that is not there.",
                 "Crescido, não cortado, num porão onde nada deveria crescer. Ainda se inclina para uma luz que não existe."),
                ("Dented by a thousand prisoners and not one of them got past.",
                 "Amassado por mil prisioneiros e nenhum deles passou."),
                ("Cut from a single black glove, the other of which was never found. It likes to be alone with the dark.",
                 "Cortada de uma única luva negra, cuja outra nunca foi achada. Gosta de ficar sozinha com o escuro."),
                ("Frayed at the hem from every stair it has ever climbed. It knows the way up better than you do.",
                 "Desfiada na barra por cada escada que já subiu. Conhece o caminho de volta melhor que você."),
                ("The pick that struck the first seam, and has not been quiet since.",
                 "A picareta que atingiu a primeira veia, e que não ficou quieta desde então."),
                ("A miner's lamp hammered into a helm. The flame is not oil and has never needed trimming.",
                 "Uma lanterna de mineiro martelada num elmo. A chama não é óleo e nunca precisou ser aparada."),
                ("They close on a hand like a vault door. The stone listens.",
                 "Fecham-se na mão como a porta de um cofre. A pedra escuta."),
                ("Beaten from the hide of something the dwarves dug into and could not dig out of.",
                 "Batida do couro de algo em que os anões cavaram e de onde não conseguiram sair."),
                ("Cold, heavy, and it beats once a minute. The golem was not finished with it.",
                 "Frio, pesado, e bate uma vez por minuto. O golem não tinha terminado com ele."),
                ("Bark, stitched to bark, over a heart nobody has found.",
                 "Casca costurada em casca, sobre um coração que ninguém achou."),
                ("It crackles when you take it off. The dwarves called a storm in the deep, once, and this is what was left.",
                 "Crepita quando você o tira. Os anões chamaram uma tempestade nas profundezas, uma vez, e isso é o que sobrou."),
                ("Twisted into a ring that quivers when anything moves behind you. A great many things do.",
                 "Torcidos num anel que estremece quando algo se move atrás de você. Muitas coisas se movem."),
                ("Sharpened on everything. It has opinions about what you should cut.",
                 "Afiado em tudo. Tem opiniões sobre o que você deve cortar."),
                ("Wear it and the sickness walks beside you instead of in you. It will want a favour.",
                 "Use-a e a doença anda ao seu lado em vez de dentro de você. Ela vai querer um favor."),
                ("Every step a little further than it should be. The tunnels are not where they were.",
                 "Cada passo um pouco mais longe do que deveria. Os túneis não estão mais onde estavam."),
                ("Sewn from a hundred small hides, every one of which bit someone.",
                 "Costurado com cem couros pequenos, cada um deles mordeu alguém."),
                ("Rub them together and the hair stands up on everything alive within ten feet.",
                 "Esfregue uma na outra e os pelos de tudo que vive por perto se arrepiam."),
                ("Antlers grown through bronze. They are, by some measure, still growing.",
                 "Galhadas crescidas através do bronze. Por alguma medida, ainda crescem."),
                ("The tide came when it was raised, and went when it was set down. No one has set it down in a long time.",
                 "A maré vinha quando ele era erguido, e ia embora quando era baixado. Ninguém o baixa há muito tempo."),
                ("Pried from the throat of someone who swallowed it on purpose.",
                 "Arrancada da garganta de alguém que a engoliu de propósito."),
                ("Crusted white, and it will not rust. Salt remembers every sea it has been.",
                 "Branca de crosta, e não enferruja. O sal lembra de cada mar por onde passou."),
                ("A ring that glows a little less than the water it came out of.",
                 "Um anel que brilha um pouco menos que a água de onde saiu."),
                ("It is always the hour just after the lamps go out.",
                 "É sempre a hora logo depois que as lâmpadas se apagam."),
                ("Gloves of someone who held on to something for a very long time.",
                 "Luvas de alguém que se agarrou a algo por muito tempo."),
                ("A king's sceptre that was never allowed to be a king's staff. It has been waiting for a better hand.",
                 "Um cetro de rei que nunca pôde ser cajado de rei. Espera por uma mão melhor."),
                ("The first of the three pieces, and the only one that is angry.",
                 "A primeira das três peças, e a única que está furiosa."),
                ("A circlet of ash that has been burning for three hundred years, with no fuel but you.",
                 "Um diadema de cinzas que arde há trezentos anos, sem combustível além de você."),
                ("The Spire's tower-wizards called lightning to quench the fires. It has a grudge against both.",
                 "Os magos da Torre chamaram raios para apagar o incêndio. Ele guarda rancor dos dois."),
                ("A sword that rode into the Spire to put out a fire. It came out on fire.",
                 "Uma espada que entrou na Torre para apagar um incêndio. Saiu em chamas."),
                ("Prayer beads of bone and cinder. Each one is a name.",
                 "Contas de oração de osso e brasa. Cada uma é um nome."),
                ("She wore it to her own funeral and then declined to be buried.",
                 "Ela a vestiu no próprio funeral e depois recusou ser enterrada."),
                ("Boots of someone who is, strictly, no longer using them.",
                 "Botas de alguém que, a rigor, não as usa mais."),
                ("The last staff in the Spire still lit. The fire in it is not a flame but a decision.",
                 "O último cajado ainda aceso na Torre. O fogo dentro dele não é chama, é uma decisão."),
                ("The last of the three. Whatever she put in it is not what you would expect, and it is not finished.",
                 "O último dos três. O que ela pôs nele não é o que você espera, e não está terminado."),
                ("Gold, warm, a little heavy. The Regent paid his debts in fire.",
                 "Ouro, morno, um pouco pesado. O Regente pagou suas dívidas em fogo."),
                ("The last coal of the Spire, held in an iron fist that learned not to let go.",
                 "A última brasa da Torre, presa num punho de ferro que aprendeu a não soltar."),
                ("Wire and two lenses. They show you what everything cost, and who paid.",
                 "Arame e duas lentes. Mostram quanto custou cada coisa, e quem pagou."),
                ("A ring stamped with a number that increases whenever you are not looking.",
                 "Um anel marcado com um número que aumenta sempre que você não olha."),
                ("A pair that were never found together, and have been waiting to be.",
                 "Um par que nunca foi achado junto, e que esperava por isso.") })
                P(kv.Item1, kv.Item2);

            // The sets: two pieces grant the first bonus, three the second as well.
            foreach (var s in new[] {
                ("The Drowned Court", "A Corte Afogada", true),
                ("The Ashen Regalia", "A Regalia de Cinza", true),
                ("The Lich-Queen's Panoply", "A Panóplia da Rainha-Lich", true),
                ("The Stormcaller's Regalia", "A Regalia do Chamador de Tempestades", true),
                ("The Verdant Court", "A Corte Verdejante", true),
                ("The Midnight Cabal", "A Cabala da Meia-Noite", true) })
                N(s.Item1, s.Item2, s.Item3);
        }

        // ------------------------------------------------------------------ rival parties
        /// <summary>
        /// The parties that race the hero down the Dungeons. They are groups, so a rumour names them inside a
        /// sentence ("$1 are buying rope"): the names live here with the monsters and the items.
        /// </summary>
        static void AddRivalText()
        {
            N("the Ash Company", "a Companhia de Cinzas", true);
            N("Maren's Blades", "as Lâminas de Maren", true);
            N("the Grey Lanterns", "as Lanternas Cinzentas", true);
            N("the Penny Knives", "as Facas de Tostão", true);
            N("Brother Voss and his five", "Irmão Voss e seus cinco");
            N("the Hollow Crows", "os Corvos Ocos");
        }

        static readonly Dictionary<string, (string Pt, bool Fem)> Head = new Dictionary<string, (string, bool)>();
        static readonly Dictionary<string, string> Adj = new Dictionary<string, string>();
        /// <summary>Material word to its Portuguese "de X", longest names first when matching.</summary>
        static readonly Dictionary<string, string> MatPt = new Dictionary<string, string>();
        /// <summary>Words that lead an item name before its enchantment: blessing, curse, unknown magic and wear.</summary>
        static readonly string[] LeadTags = { "blessed ", "cursed ", "magical ", "enchanted ", "blunted ", "chipped ", "dented ", "battered " };

        static string Feminine(string adj)
        {
            if (StartsWithPreposition(adj)) return adj;
            int sp = adj.IndexOf(' ');
            string first = sp < 0 ? adj : adj.Substring(0, sp), rest = sp < 0 ? "" : adj.Substring(sp);
            if (first.EndsWith("o")) first = first.Substring(0, first.Length - 1) + "a";
            return first + rest;
        }

        static bool StartsWithPreposition(string adj) =>
            adj.StartsWith("de ") || adj.StartsWith("do ") || adj.StartsWith("da ") || adj.StartsWith("à ");

        /// <summary>Plural of a composed adjective ("robusto" to "robustos"); a phrase that begins with a preposition is invariable.</summary>
        static string Plural(string adj)
        {
            if (StartsWithPreposition(adj)) return adj;
            int sp = adj.IndexOf(' ');
            string first = sp < 0 ? adj : adj.Substring(0, sp), rest = sp < 0 ? "" : adj.Substring(sp);
            if (first.EndsWith("s") || first.EndsWith("z") || first.EndsWith("m")) return adj;
            if (first.EndsWith("o") || first.EndsWith("a") || first.EndsWith("e")) first += "s";
            else if (first.EndsWith("l") || first.EndsWith("r")) first += "es";
            return first + rest;
        }

        /// <summary>The adjective as it must appear after this noun: gender first, then number ("botas robustas").</summary>
        static string Agree(string adj, bool fem, bool plural) => plural ? Plural(fem ? Feminine(adj) : adj) : (fem ? Feminine(adj) : adj);

        static readonly Dictionary<string, (string Pt, bool Fem)?> _itemMemo = new Dictionary<string, (string, bool)?>();

        /// <summary>Portuguese for an item name, composed from its parts ("blessed +1 keen dagger of the fox"), or null if not recognised.</summary>
        static (string Pt, bool Fem)? ItemPt(string en)
        {
            if (Nm.TryGetValue(en, out var direct)) return direct;
            if (_itemMemo.TryGetValue(en, out var memo)) return memo;
            var look = AppearancePt(en);
            if (look != null) return look;
            // A named masterwork: its proper name stays, what it is gets translated ("Ashtooth, espada longa de aço").
            var titled = TitledRx.Match(en);
            if (titled.Success)
            {
                var rest = ItemPt(titled.Groups[2].Value);
                (string, bool)? named = rest == null ? ((string, bool)?)null : (titled.Groups[1].Value + ", " + rest.Value.Pt, rest.Value.Fem);
                _itemMemo[en] = named;
                return named;
            }
            (string, bool)? res = ComposeItem(en);
            _itemMemo[en] = res;
            return res;
        }

        static readonly Regex TitledRx = new Regex(@"^([A-Z][a-z]+), (.+)$", RegexOptions.CultureInvariant);

        static (string Pt, bool Fem)? ComposeItem(string en)
        {
            string s = en;
            var tags = new List<string>();
            string enchant = null;
            // Leading blessed/cursed/magical/enchanted and "+N".
            while (true)
            {
                bool took = false;
                foreach (string w in LeadTags)
                    if (s.StartsWith(w)) { tags.Add(w.Trim()); s = s.Substring(w.Length); took = true; }
                var m = Regex.Match(s, @"^([+-]\d+) ");
                if (m.Success) { enchant = m.Groups[1].Value; s = s.Substring(m.Length); took = true; }
                if (!took) break;
            }
            // One prefix affix.
            string pre = null;
            foreach (var kv in Adj)
                if (System.Array.IndexOf(LeadTags, kv.Key + " ") < 0 && s.StartsWith(kv.Key + " ")) { pre = kv.Key; s = s.Substring(kv.Key.Length + 1); break; }
            // Base, with an optional "of X" suffix.
            (string Pt, bool Fem)? bas = null; string suffix = null, baseEn = null;
            void ParseBase(string t)
            {
                if (Nm.TryGetValue(t, out var whole)) { bas = whole; baseEn = t; return; }
                int at = 0;
                while ((at = t.IndexOf(" of ", at, System.StringComparison.Ordinal)) >= 0)
                {
                    string head = t.Substring(0, at), tail = t.Substring(at + 4);
                    if (OfPt.TryGetValue(tail, out var tp))
                    {
                        if (Nm.TryGetValue(head, out var hb)) { bas = hb; suffix = tp; baseEn = head; break; }
                        if (Head.TryGetValue(head, out var hh)) { bas = hh; suffix = tp; baseEn = head; break; }
                    }
                    at += 4;
                }
            }
            ParseBase(s);
            // A material in front of the base ("steel long sword"), tried only when the whole did not parse, so "iron boots" stays one name.
            string mat = null;
            if (bas == null)
                foreach (var kv in MatPt)
                    if (s.StartsWith(kv.Key + " ") && (mat == null || kv.Key.Length > mat.Length))
                    {
                        suffix = null; ParseBase(s.Substring(kv.Key.Length + 1));
                        if (bas != null) { mat = kv.Key; break; }
                    }
            if (bas == null) return null;
            bool fem = bas.Value.Fem;
            // "boots" is plural in both languages ("botas"): the adjective must follow the noun's number too.
            bool plural = baseEn.EndsWith("s", System.StringComparison.Ordinal) && baseEn != bas.Value.Pt && bas.Value.Pt.EndsWith("s", System.StringComparison.Ordinal);
            var sb = new StringBuilder(bas.Value.Pt);
            if (mat != null) sb.Append(' ').Append(MatPt[mat]);
            if (pre != null) sb.Append(' ').Append(Agree(Adj[pre], fem, plural));
            if (suffix != null) sb.Append(' ').Append(suffix);
            if (enchant != null) sb.Append(' ').Append(enchant);
            foreach (string t in tags) sb.Append(' ').Append(Agree(Adj[t], fem, plural));
            return (sb.ToString(), fem);
        }

        /// <summary>True when the name has a Portuguese entry (some are the same word in both languages).</summary>
        public static bool KnowsName(string en) => Nm.ContainsKey(en) || ItemPt(en) != null || Pt.ContainsKey(en);

        // ------------------------------------------------------------------ capture handling
        // A phrase that already carries an article takes no second one: "Uma lança de gelo perfura" is not "um Uma ...".
        static bool HasArticle(string pt)
        {
            string low = pt.ToLowerInvariant();
            return low.StartsWith("o ") || low.StartsWith("a ") || low.StartsWith("os ") || low.StartsWith("as ")
                || low.StartsWith("um ") || low.StartsWith("uma ");
        }

        /// <summary>Resolves a captured fragment to (Portuguese text, feminine, hasName). Handles "the jackal", "a dagger", lists and plain words.</summary>
        static (string Pt, bool Fem, bool Known) Resolve(string g)
        {
            string core = g.Trim();
            if (core.Length == 0) return (g, false, false);
            string art = null;
            foreach (string a in new[] { "the ", "an ", "a " })
                if (core.StartsWith(a, System.StringComparison.OrdinalIgnoreCase)) { art = a; break; }
            var direct = Lookup(core);
            if (direct != null) return (direct.Value.Pt, direct.Value.Fem, true);
            if (art != null)
            {
                var rest = Lookup(core.Substring(art.Length));
                if (rest != null) return (rest.Value.Pt, rest.Value.Fem, true);
            }
            if (Pt.TryGetValue(core, out var p)) return (p, false, true);
            return (g, false, false);
        }

        static (string Pt, bool Fem)? Lookup(string s)
        {
            if (Nm.TryGetValue(s, out var n)) return n;
            if (Scenery.TryGetValue(s, out var t)) return t;
            if (s.IndexOf(' ') > 0 || s.Length > 2) { var it = ItemPt(s); if (it != null) return it; }
            return null;
        }

        /// <summary>Translates a captured fragment with no article: lists are split, unknown text is left alone.</summary>
        static string Part(string g)
        {
            // A whole phrase that is registered as one entry wins over the list split: "kills, above all of foes
            // stronger than you" is one line of a god's altar, not a list of two things.
            var whole = Resolve(g);
            if (whole.Known) return StripArticle(whole.Pt, g);
            if (g.IndexOf(", ", System.StringComparison.Ordinal) > 0 && g.IndexOf('(') < 0)
            {
                var bits = g.Split(new[] { ", " }, System.StringSplitOptions.None);
                for (int i = 0; i < bits.Length; i++) bits[i] = Part(bits[i]);
                return string.Join(", ", bits);
            }
            var rk = Regex.Match(g, @"^(.+?) ([+-]?\d+|\(x\d+\))$");
            if (rk.Success && Resolve(rk.Groups[1].Value).Known) return Part(rk.Groups[1].Value) + " " + rk.Groups[2].Value;
            return UCore(g) ?? ApplyRx(g) ?? g;
        }

        // The English fragment said "the jackal": keep an article in Portuguese. A bare "jackal" stays bare.
        static string StripArticle(string pt, string en)
        {
            string low = en.TrimStart().ToLowerInvariant();
            bool hadArticle = low.StartsWith("the ") || low.StartsWith("a ") || low.StartsWith("an ");
            if (!hadArticle || HasArticle(pt)) return pt;
            var r = Resolve(en);
            bool def = low.StartsWith("the ");
            string a = def ? (r.Fem ? "a " : "o ") : (r.Fem ? "uma " : "um ");
            return a + pt;
        }

        static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        static readonly Regex Token = new Regex(@"\$(\d)|\{([adu])(\d)\}", RegexOptions.CultureInvariant);

        /// <summary>$n is the translated capture; {a n} adds the article ("a adaga"), {d n} the contraction with "de" ("da adaga"), {u n} the indefinite ("uma adaga").</summary>
        static string Expand(Match m, string rep)
        {
            string r = Token.Replace(rep, t =>
            {
                if (t.Groups[1].Success) return Part(m.Groups[int.Parse(t.Groups[1].Value)].Value);
                string kind = t.Groups[2].Value;
                string src = m.Groups[int.Parse(t.Groups[3].Value)].Value;
                var res = Resolve(src);
                string pt = res.Known ? res.Pt : src;
                if (!res.Known) pt = UCore(src) ?? src;
                if (HasArticle(pt) || !res.Known) return pt;
                // Proper names and named people take no article.
                switch (kind)
                {
                    case "a": return (res.Fem ? "a " : "o ") + pt;
                    case "u": return (res.Fem ? "uma " : "um ") + pt;
                    default: return (res.Fem ? "da " : "do ") + pt;
                }
            });
            return Capital(r);
        }

        static readonly (Regex, string)[] Contractions =
        {
            (new Regex(@"\bde o\b", RegexOptions.CultureInvariant), "do"), (new Regex(@"\bde a\b", RegexOptions.CultureInvariant), "da"),
            (new Regex(@"\bde os\b", RegexOptions.CultureInvariant), "dos"), (new Regex(@"\bde as\b", RegexOptions.CultureInvariant), "das"),
            (new Regex(@"\bem o\b", RegexOptions.CultureInvariant), "no"), (new Regex(@"\bem a\b", RegexOptions.CultureInvariant), "na"),
            (new Regex(@"\bem os\b", RegexOptions.CultureInvariant), "nos"), (new Regex(@"\bem as\b", RegexOptions.CultureInvariant), "nas"),
            (new Regex(@"\bem um\b", RegexOptions.CultureInvariant), "num"), (new Regex(@"\bem uma\b", RegexOptions.CultureInvariant), "numa"),
            (new Regex(@"\bpor o\b", RegexOptions.CultureInvariant), "pelo"), (new Regex(@"\bpor a\b", RegexOptions.CultureInvariant), "pela"),
            (new Regex(@"\bpor os\b", RegexOptions.CultureInvariant), "pelos"), (new Regex(@"\bpor as\b", RegexOptions.CultureInvariant), "pelas"),
            (new Regex(@"\ba o\b", RegexOptions.CultureInvariant), "ao"), (new Regex(@"\ba os\b", RegexOptions.CultureInvariant), "aos"),
            (new Regex(@"\ba as\b", RegexOptions.CultureInvariant), "às"),
            (new Regex(@"\bem O\b", RegexOptions.CultureInvariant), "no"), (new Regex(@"\bem A\b", RegexOptions.CultureInvariant), "na"),
            (new Regex(@"\bem Os\b", RegexOptions.CultureInvariant), "nos"), (new Regex(@"\bem As\b", RegexOptions.CultureInvariant), "nas"),
            (new Regex(@"\bde O\b", RegexOptions.CultureInvariant), "do"), (new Regex(@"\bde A\b", RegexOptions.CultureInvariant), "da"),
            (new Regex(@"\bde Os\b", RegexOptions.CultureInvariant), "dos"), (new Regex(@"\bde As\b", RegexOptions.CultureInvariant), "das"),
            (new Regex(@"\ba O\b", RegexOptions.CultureInvariant), "ao"), (new Regex(@"\ba As\b", RegexOptions.CultureInvariant), "às"),
            (new Regex(@"\ba Os\b", RegexOptions.CultureInvariant), "aos"), (new Regex(@"\ba A\b", RegexOptions.CultureInvariant), "à"),
        };

        static string Contract(string s)
        {
            foreach (var (re, rep) in Contractions) s = re.Replace(s, rep);
            return s;
        }
    }
}
