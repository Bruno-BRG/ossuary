using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core.Magic
{
    /// <summary>
    /// A timed state a spell leaves on the hero: numbers from <see cref="ItemMods"/> (stats, armour, resistances, extra melee die),
    /// healing each turn, and a retaliation against whoever strikes you. Expressed as data so a new ward is one row.
    /// </summary>
    public sealed class BuffDef
    {
        public string Id, Label, Msg;
        public ItemMods Mods;
        public int Regen;
        public DamageType RetType; public int RetDice, RetSides;
    }

    public static class SpellBuffs
    {
        static BuffDef B(string id, string label, string msg, ItemMods mods, int regen = 0, DamageType ret = DamageType.Physical, int retDice = 0, int retSides = 0)
            => new BuffDef { Id = id, Label = label, Msg = msg, Mods = mods, Regen = regen, RetType = ret, RetDice = retDice, RetSides = retSides };

        public static readonly BuffDef[] All =
        {
            // ---- Arcane
            B("mage-armor", "Mage Armor", "Unseen plates of force settle over you. (AC +4)", new ItemMods { Ac = 4 }),
            B("arcane-shield", "Shield", "A disc of force snaps up before you. (AC +6, briefly)", new ItemMods { Ac = 6 }),
            B("bulls-strength", "Might", "Your muscles swell. (Str +3)", new ItemMods { Str = 3 }),
            B("cats-grace", "Grace", "You feel light and quick. (Dex +3)", new ItemMods { Dex = 3, Evasion = 1 }),
            B("foxs-cunning", "Cunning", "Your thoughts sharpen. (Int +3)", new ItemMods { Int = 3 }),
            B("owls-wisdom", "Insight", "Calm clarity settles on you. (Wis +3)", new ItemMods { Wis = 3 }),
            B("endure", "Endure", "The extremes of heat, cold and storm lose their bite.", new ItemMods { ResFire = 30, ResCold = 30, ResLightning = 20 }),
            B("fire-ward", "Fire Ward", "A cool veil wraps you. (fire 60%)", new ItemMods { ResFire = 60 }),
            B("frost-ward", "Frost Ward", "A warm veil wraps you. (cold 60%)", new ItemMods { ResCold = 60 }),
            B("storm-ward", "Storm Ward", "Your hair lifts, and the lightning slides off. (lightning 60%)", new ItemMods { ResLightning = 60 }),
            B("arcane-focus", "Focus", "The weave of magic comes into focus. (spell power +3, fewer fizzles)", new ItemMods { SpellPower = 3, SpellFocus = 10 }),
            B("blur", "Blur", "Your outline smears. (evasion +4)", new ItemMods { Evasion = 4 }),
            B("displacement", "Displace", "Your image slips a hand's breadth from where you stand.", new ItemMods { Evasion = 3, Ac = 1 }),
            B("enlarge", "Enlarged", "You swell to half again your size. (Str +4, Con +3, +15 HP)", new ItemMods { Str = 4, Con = 3, Hp = 15 }),
            B("sense", "Sense", "You feel every living thing nearby.", default),
            // ---- Dead
            B("grave-ward", "Grave Ward", "Graveyard cold settles around you. (necrotic 50%, cold 30%)", new ItemMods { ResNecrotic = 50, ResCold = 30 }),
            B("unholy-vigor", "Unholy", "Your blows come back warmer than they went out. (+1d4 necrotic, 15% life steal)", new ItemMods { ExtraType = DamageType.Necrotic, ExtraSides = 4, LifeSteal = 15 }),
            B("dark-pact", "Dark Pact", "You sign. Something signs back. (spell power +4)", new ItemMods { SpellPower = 4 }),
            B("bloodlust", "Bloodlust", "Red heat behind the eyes. (+2 to hit, +3 damage, 10% life steal)", new ItemMods { ToHit = 2, Dmg = 3, LifeSteal = 10 }),
            B("death-aura", "Death Aura", "A shroud of cold death hangs about you.", new ItemMods { ResNecrotic = 30 }, 0, DamageType.Necrotic, 1, 6),
            // ---- Sacred
            B("regen", "Regen", "Your wounds begin to close on their own.", default, 2),
            B("sanctuary", "Sanctuary", "A hush falls around you. Few will lift a hand to you.", default),
            B("faith", "Faith", "Faith rises like a wall. (AC +4)", new ItemMods { Ac = 4 }),
            B("favor", "Favor", "Heaven leans on your arm. (+3 to hit, +2 damage, +1d4 holy)", new ItemMods { ToHit = 3, Dmg = 2, ExtraType = DamageType.Holy, ExtraSides = 4 }),
            B("holy-aura", "Holy Aura", "A blazing halo lights you. (AC +3; necrotic 40%; it burns those who strike you)", new ItemMods { Ac = 3, ResNecrotic = 40 }, 0, DamageType.Holy, 1, 6),
            B("prot-evil", "Warded", "A circle of white fire stands about you. (AC +3, necrotic 40%)", new ItemMods { Ac = 3, ResNecrotic = 40 }),
            B("prayer", "Prayer", "The prayer steadies every part of you. (+2 hit, +2 damage, AC +2)", new ItemMods { ToHit = 2, Dmg = 2, Ac = 2, Evasion = 1 }),
            B("fortitude", "Fortitude", "You feel hard to kill. (Con +3, +15 HP)", new ItemMods { Con = 3, Hp = 15 }),
            B("heroism", "Heroism", "Courage is a hot coal in the chest. (+3 to hit, Str +1)", new ItemMods { ToHit = 3, Str = 1 }),
            B("divine-might", "Divine Might", "Strength not your own fills your arms. (Str +4, +2 damage)", new ItemMods { Str = 4, Dmg = 2 }),
            B("angelic", "Angelic", "You are, for a while, a little more than you are. (all stats +2, +20 HP, resist 30%)",
                new ItemMods { Str = 2, Dex = 2, Con = 2, Int = 2, Wis = 2, Hp = 20, ResFire = 30, ResCold = 30, ResLightning = 30, ResPoison = 30, ResNecrotic = 30 }),
            // ---- Nature
            B("barkskin", "Barkskin", "Your skin roughens to bark. (AC +4)", new ItemMods { Ac = 4 }),
            B("thorns", "Thorns", "Thorns push through your skin; the ones who hit you bleed for it.", default, 0, DamageType.Physical, 1, 6),
            B("bears-endurance", "Endurance", "You feel as stubborn as a bear. (Con +4, +10 HP)", new ItemMods { Con = 4, Hp = 10 }),
            B("eagle-eye", "Eagle Eye", "The world sharpens. (+3 to hit)", new ItemMods { ToHit = 3 }),
            B("camouflage", "Camouflage", "You take the colours of the stone. (harder to notice)", new ItemMods { Stealth = 3 }),
            B("wolf-form", "Wolf Form", "You drop onto all fours, long of leg and bright of eye. (Str +3, Dex +3, AC +2)", new ItemMods { Str = 3, Dex = 3, Ac = 2, Evasion = 2 }),
            B("bear-form", "Bear Form", "Your shoulders broaden and your hands become claws. (Str +5, Con +4, +20 HP, AC +3)", new ItemMods { Str = 5, Con = 4, Hp = 20, Ac = 3 }),
            B("eagle-form", "Eagle Form", "The air takes you. (Dex +4, evasion +4)", new ItemMods { Dex = 4, Evasion = 4 }),
            B("flame-blade", "Flame Blade", "Your weapon wears a skin of fire. (+1d6 fire)", new ItemMods { ExtraType = DamageType.Fire, ExtraSides = 6 }),
            B("shillelagh", "Shillelagh", "Your weapon thrums with oak. (+2 to hit, +3 damage)", new ItemMods { ToHit = 2, Dmg = 3 }),
            // ---- Shadow
            B("shadow-cloak", "Shadowed", "Shadows gather to you like cats. (harder to notice, evasion +2)", new ItemMods { Stealth = 3, Evasion = 2 }),
            B("venom-blade", "Venom Blade", "Your weapon beads with venom. (+1d6 poison)", new ItemMods { ExtraType = DamageType.Poison, ExtraSides = 6 }),
            B("shadow-blade", "Shadow Blade", "Your weapon drinks the light. (+1d6 necrotic, +2 to hit)", new ItemMods { ExtraType = DamageType.Necrotic, ExtraSides = 6, ToHit = 2 }),
            B("vampiric-edge", "Vampiric Edge", "Your weapon thirsts. (25% life steal)", new ItemMods { LifeSteal = 25 }),
            B("fade", "Fade", "You are hard to look at, harder to hit. (evasion +5, harder to notice)", new ItemMods { Evasion = 5, Stealth = 2 }),
            B("umbral-shroud", "Umbral Shroud", "A shroud of dark folds around you. (AC +2, necrotic 30%, cold 20%)", new ItemMods { Ac = 2, ResNecrotic = 30, ResCold = 20, Evasion = 1 }),

            // ---- The second round
            B("lightfoot", "Lightfoot", "Your step goes quiet. (harder to notice, evasion +1)", new ItemMods { Stealth = 2, Evasion = 1 }),
            B("keen-edge", "Keen Edge", "Your weapon finds its mark. (+3 to hit, +1 damage)", new ItemMods { ToHit = 3, Dmg = 1 }),
            B("aegis", "Aegis", "A ward settles against everything. (AC +3; fire, cold and lightning 15%)", new ItemMods { Ac = 3, ResFire = 15, ResCold = 15, ResLightning = 15 }),
            B("iron-body", "Iron Body", "You turn to a statue that moves. (AC +5, poison 40%, evasion -1)", new ItemMods { Ac = 5, ResPoison = 40, Evasion = -1 }),
            B("titan", "Titan", "You swell with a giant's strength. (Str +6, Con +4, +25 HP)", new ItemMods { Str = 6, Con = 4, Hp = 25 }),
            B("lich-form", "Lich Form", "Cold settles into your bones, and you like it. (necrotic 80%, cold 50%, poison 80%, spell power +3, 15% life steal)",
                new ItemMods { ResNecrotic = 80, ResCold = 50, ResPoison = 80, SpellPower = 3, LifeSteal = 15 }),
            B("courage", "Courage", "Courage steadies your hand. (+2 to hit, +6 HP)", new ItemMods { ToHit = 2, Hp = 6 }),
            B("holy-weapon", "Holy Weapon", "Your weapon shines. (+1d6 holy, +1 to hit)", new ItemMods { ExtraType = DamageType.Holy, ExtraSides = 6, ToHit = 1 }),
            B("divine-shield", "Divine Shield", "A shield of light stands before you. (AC +6, briefly)", new ItemMods { Ac = 6 }),
            B("benediction", "Benediction", "A full blessing falls on you. (AC +2, +2 to hit, +2 damage, necrotic 30%)", new ItemMods { Ac = 2, ToHit = 2, Dmg = 2, ResNecrotic = 30 }),
            B("stoneform", "Stoneform", "Your skin takes the grain of stone. (AC +5, fire 20%, evasion -1)", new ItemMods { Ac = 5, ResFire = 20, Evasion = -1 }),
            B("vampiric-veil", "Vampiric Veil", "A veil that drinks. (20% life steal, evasion +3, harder to notice)", new ItemMods { LifeSteal = 20, Evasion = 3, Stealth = 2 }),
            B("ambush", "Ambush", "You wait, and you are very good at it. (+4 to hit, +3 damage, well hidden)", new ItemMods { ToHit = 4, Dmg = 3, Stealth = 2 }),
        };

        static Dictionary<string, BuffDef> _byId;

        public static BuffDef Find(string id)
        {
            if (_byId == null)
            {
                var d = new Dictionary<string, BuffDef>();
                foreach (var b in All) d[b.Id] = b;
                _byId = d;
            }
            return id != null && _byId.TryGetValue(id, out var def) ? def : null;
        }
    }
}
