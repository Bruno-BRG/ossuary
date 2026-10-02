using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Every player verb lives here as one method taking a raw key. Keeping commands
    /// outside the Game class makes the whole verb set headlessly testable and lets
    /// the input layer stay a dumb key-to-command translator.
    /// </summary>
    public sealed class Commands
    {
        readonly Game _g;
        public bool Handled;

        public Commands(Game g) { _g = g; }

        /// <summary>Returns false when the key is not a command (the caller should ignore it).</summary>
        public bool Execute(string cmd)
        {
            Handled = true;
            if (string.IsNullOrEmpty(cmd)) return false;
            _g.ResetNoise();   // noise belongs to the action that makes it, never to a refused one before it

            // Inside the walls nothing is thrown, shot, zapped or cast at anyone.
            if (_g.Mode == GameMode.TownMap && (cmd == "f" || cmd == "z" || cmd == "Z" || cmd == "V" || cmd == "k"))
            {
                _g.Say("Not inside the walls. The Watch frowns on that.", MessageKind.Warn);
                return true;
            }

            switch (cmd)
            {
                case "move-n": return DoMove(0, -1);
                case "move-s": return DoMove(0, 1);
                case "move-e": return DoMove(1, 0);
                case "move-w": return DoMove(-1, 0);
                case "move-ne": return DoMove(1, -1);
                case "move-nw": return DoMove(-1, -1);
                case "move-se": return DoMove(1, 1);
                case "move-sw": return DoMove(-1, 1);

                case ">": return DoDescend();
                case "<": return DoAscend();
                case ".": _g.Wait(); return true;

                case "i": _g.PushInventory(); return true;
                case "g": return DoPickup();
                case "d": return DoDrop();
                case "a": return DoApply();
                case "f": return DoShoot();
                case "w": return DoWield();
                case "W": return DoWear();
                case "T": return DoRemoveArmor();
                case "P": return DoPutOnRing();
                case "R": return DoRemoveRing();
                case "r": return DoRead();
                case "z": return DoZap();
                case "Z": _g.PushSpells(); return true;
                case "V": _g.UiRequests.Abilities = true; return true;
                case "C": _g.UiRequests.Advance = true; return true;
                case "e": return DoEat();
                case "q": return DoQuaff();
                case "k": return DoKick();
                case "u": return DoUseKey();
                case "D": return DoOpenDoor();
                case "s": return DoSearch();
                case "disarm": return DoDisarm();
                case "craft": return DoCraft();
                case "drink": return DoDrinkFountain();
                case "x": _g.PushTargeting(TargetingMode.Inspect); return true;
                case "l": _g.PushTargeting(TargetingMode.Look); return true;
                case "X": return DoSwapWith();

                case "explore": return DoAutoWalk(AutoWalk.Explore);
                case "stairs": return DoAutoWalk(AutoWalk.Stairs);
                case "rest": return DoAutoWalk(AutoWalk.Rest);
                case "feature": return DoAutoWalk(AutoWalk.Feature);

                case "O": _g.PushTravelMode(); return true;
                case "m": _g.ToggleMinimap(); return true;
                case "c": _g.PushCharacter(); return true;
                case "D2": _g.PushDiscoveries(); return true;
                case "H": _g.PushHistory(); return true;
                case "?": _g.PushHelp(); return true;
                case "save": _g.Say("The seed is the save: " + _g.Rng.Seed, MessageKind.Info); return true;
                case "settings": _g.PushSettings(); return true;
                case "crt": DisplaySettings.Current.CycleCrt(1); return true;
                case "theme": DisplaySettings.Current.CycleTheme(1); return true;
                case "quit": _g.Mode = GameMode.GameOver; _g.Abandoned = true; _g.DeathCause = "abandoned the run"; _g.Say("You abandon the run."); return true;
                case "shop-buy": return BuyShopCursor();
                case "shop-sell": return BeginSellToShop();
                default: Handled = false; return false;
            }
        }

        // ------------------------------------------------------------ auto-walk

        enum AutoWalk { Explore, Stairs, Rest, Feature }
        const int AutoWalkLimit = 600, RestLimit = 3000;

        /// <summary>
        /// Explore, travel to stairs and rest: each is a loop of ordinary turns that stops the moment anything
        /// happens (an enemy in sight, damage, any message, stairs or items underfoot). Refusals cost no turn.
        /// </summary>
        bool DoAutoWalk(AutoWalk kind)
        {
            var p = _g.Player;
            if (_g.Mode != GameMode.Dungeon || _g.Map == null)
            {
                _g.Say(kind == AutoWalk.Rest ? "You cannot rest here." : "There is nothing to explore here.", MessageKind.Info);
                return true;
            }
            if (p.Asleep || p.Stunned) { _g.Say(p.Asleep ? "You are asleep." : "You are stunned."); return true; }
            if (_g.HostileInView()) { _g.Say("Not with enemies in sight.", MessageKind.Warn); return true; }
            if (kind == AutoWalk.Rest && !_g.NeedsRest()) { _g.Say("You are already rested.", MessageKind.Info); return true; }
            if (kind == AutoWalk.Feature && _g.IsFeatureSpot(p.X, p.Y)) { _g.Say("You are already there.", MessageKind.Info); return true; }
            if (kind == AutoWalk.Stairs)
            {
                var here = _g.Map.Get(p.X, p.Y);
                if (here == TileKind.StairsDown || here == TileKind.StairsUp || here == TileKind.LadderDown)
                { _g.Say("You are already on the stairs.", MessageKind.Info); return true; }
            }

            int map = _g.Map.Number, hp = p.HP, stuck = 0;
            for (int n = 0; n < (kind == AutoWalk.Rest ? RestLimit : AutoWalkLimit); n++)
            {
                long said = _g.Said;
                var from = _g.WalkPosition();
                if (kind == AutoWalk.Rest)
                {
                    if (!_g.NeedsRest()) { _g.Say("You feel rested.", MessageKind.Good); break; }
                    _g.Wait();
                }
                else
                {
                    int dx, dy;
                    if (kind == AutoWalk.Explore)
                    {
                        _g.ExploreMarkHere();
                        var pile = GroundItems.At(map, p.X, p.Y);
                        if (n > 0 && pile != null && pile.Count > 0) { _g.Say("You stop at some items.", MessageKind.Info); break; }
                        if (!_g.AutoStep(_g.ExploreGoal, out dx, out dy))
                        { _g.Say(n == 0 ? "Nothing left to explore here." : "You have seen all there is to see here.", MessageKind.Info); break; }
                    }
                    else if (kind == AutoWalk.Feature)
                    {
                        if (!_g.FeatureStep(out dx, out dy)) { _g.Say("You have not found a fountain or an altar yet.", MessageKind.Info); break; }
                    }
                    else if (!_g.StairsStep(out dx, out dy, out _))
                    { _g.Say("You have not found any stairs yet.", MessageKind.Info); break; }
                    DoMove(dx, dy);
                    stuck = _g.WalkPosition() == from ? stuck + 1 : 0;
                    if (stuck >= 2) { _g.Say("Something is in the way.", MessageKind.Info); break; }
                }
                if (_g.Mode != GameMode.Dungeon || _g.Map == null || _g.Map.Number != map) break;
                if (p.HP < hp || p.HP <= 0) break;
                if (_g.Said != said || (kind == AutoWalk.Stairs && ArrivedAtStairs()) || (kind == AutoWalk.Feature && _g.IsFeatureSpot(p.X, p.Y))) break;
                if (_g.HostileInView()) break;
                if (kind == AutoWalk.Rest) hp = p.HP;
            }
            return true;
        }

        bool ArrivedAtStairs()
        {
            var t = _g.Map.Get(_g.Player.X, _g.Player.Y);
            return t == TileKind.StairsDown || t == TileKind.StairsUp || t == TileKind.LadderDown;
        }

        // ------------------------------------------------------------ movement

        bool DoMove(int dx, int dy)
        {
            var p = _g.Player;
            if (_g.Mode == GameMode.Overworld)
            {
                _g.OverworldMove(dx, dy);
                return true;
            }
            if (_g.Mode == GameMode.TownMap)
            {
                if (!TownStep(dx, dy)) return true;
                return true;
            }
            if (_g.Map == null) { _g.Say("You cannot move that way here."); return true; }

            _g.FacingX = dx; _g.FacingY = dy;
            if (p.Asleep) { _g.Say("You are asleep."); return true; }
            if (p.Stunned) { _g.Say("You are stunned."); return true; }

            if (p.Confused && _g.Rng.Chance(50))
            {
                dx = _g.Rng.Range(-1, 2);
                dy = _g.Rng.Range(-1, 2);
            }

            if (_g.TryMovePlayer(dx, dy))
                HandleStairs(_g.Map.Get(p.X, p.Y));
            return true;
        }

        /// <summary>
        /// Walking in town. Bumping a person talks to them; bumping a counter, notice board or altar
        /// calls whoever works there; everything else is ordinary movement.
        /// </summary>
        bool TownStep(int dx, int dy)
        {
            var map = _g.Map;
            if (map == null) return false;
            var p = _g.Player;
            _g.FacingX = dx; _g.FacingY = dy;
            int nx = p.X + dx, ny = p.Y + dy;
            if (!map.InBounds(nx, ny)) return true;

            var npc = _g.MonsterAt(nx, ny);
            if (npc != null) { _g.TalkTo(npc); return true; }

            var tile = map.Get(nx, ny);
            if ((tile == TileKind.Counter || tile == TileKind.Board || tile == TileKind.Altar) && dx * dy == 0)
            {
                _g.UseCounter(nx, ny);
                return true;
            }

            if (!map.CanStep(p.X, p.Y, nx, ny, true))
            {
                if (tile == TileKind.Wall || tile == TileKind.WallAlt) _g.Say("A wall blocks the way.", MessageKind.Info);
                return true;
            }
            p.X = nx; p.Y = ny;
            _g.UpdateFov();
            HandleStairs(map.Get(nx, ny));
            _g.EndPlayerTurn();

            // A gate returns you to the road.
            if (_g.Town != null && _g.Town.IsGate(p.X, p.Y)) _g.LeaveTown();
            return true;
        }

        bool DoDrinkFountain()
        {
            if (_g.Mode != GameMode.Dungeon || _g.Map == null || _g.Map.Get(_g.Player.X, _g.Player.Y) != TileKind.Fountain)
            { _g.Say("There is nothing to drink from here.", MessageKind.Info); return true; }
            if (_g.Player.Asleep || _g.Player.Stunned) { _g.Say("You cannot do that now."); return true; }
            _g.DrinkFromFountain();
            return true;
        }

        void HandleStairs(TileKind t)
        {
            if (t == TileKind.Fountain && _g.Mode == GameMode.Dungeon) { _g.Say("A fountain bubbles here. Press Shift+E to drink.", MessageKind.Info); return; }
            if (t == TileKind.StairsDown) _g.Say("There is a staircase down here. Press > to descend.", MessageKind.Info);
            else if (t == TileKind.StairsUp) _g.Say("There is a staircase up here. Press < to climb.", MessageKind.Info);
        }

        bool DoDescend()
        {
            if (_g.Mode == GameMode.TownMap) { _g.TownStairs(-1); return true; }
            if (_g.Map == null) { _g.Say("There is nothing to descend here."); return true; }
            if (_g.Descend()) HandleStairs(_g.Map.Get(_g.Player.X, _g.Player.Y));
            return true;
        }

        bool DoAscend()
        {
            if (_g.ActiveEncounter) { _g.FleeEncounter(); return true; }
            if (_g.Mode == GameMode.TownMap) { _g.TownStairs(1); return true; }
            if (_g.Map == null) { _g.Say("There is nothing to ascend here."); return true; }
            _g.Ascend();
            return true;
        }

        // --------------------------------------------------------------- items

        bool DoPickup()
        {
            var p = _g.Player;
            if (_g.Map == null) return true;
            var stack = GroundItems.At(_g.Map.Number, p.X, p.Y);
            if (stack == null || stack.Count == 0) { _g.Say("There is nothing here to pick up."); return true; }
            for (int i = 0; i < stack.Count; i++)
            {
                _g.Say($"You pick up {stack[i].Name}.", MessageKind.Good);
                p.Inventory.Add(stack[i]);
            }
            GroundItems.RemoveCell(_g.Map.Number, p.X, p.Y);
            _g.Map.Version++;
            _g.EndPlayerTurn();
            return true;
        }

        bool DoDrop()
        {
            var p = _g.Player;
            if (p.Inventory.Count == 0) { _g.Say("You are not carrying anything."); return true; }
            var it = p.Inventory[p.Inventory.Count - 1];
            p.Inventory.RemoveAt(p.Inventory.Count - 1);
            GroundItems.Add(_g.Map.Number, p.X, p.Y, it);
            _g.Say($"You drop {it.Name}.", MessageKind.Neutral);
            _g.Map.Version++;
            _g.EndPlayerTurn();
            return true;
        }

        bool DoApply()
        {
            var tool = ChooseItem("Apply what?", it => it.Def.Kind == ItemKind.Tool);
            if (tool == null) return true;
            if (tool.Name == "pick-axe") { _g.PushTargeting(TargetingMode.Dig); return true; }
            if (tool.Name == "lock pick") { _g.PushTargeting(TargetingMode.PickLock); return true; }
            if (tool.Name == "molotov") { _g.UiState.ThrowItem = tool; _g.PushTargeting(TargetingMode.Throw); return true; }
            _g.Say($"You cannot work out how to use {tool.Name}.", MessageKind.Info);
            return true;
        }

        bool DoCraft()
        {
            if (_g.Mode == GameMode.Overworld) { _g.Say("There is no room to work on the road.", MessageKind.Info); return true; }
            var choices = _g.CraftChoices();
            if (choices.Count == 0) { _g.Say("You have nothing you can combine into something better.", MessageKind.Info); return true; }
            _g.PushChoice(Game.CraftPrompt, choices);
            return true;
        }

        bool DoShoot()
        {
            _g.MakeNoise(2);
            _g.PushTargeting(TargetingMode.Shoot);
            return true;
        }

        bool DoQuaff()
        {
            var potion = ChooseItem("Drink what?", it => it.Def.Kind == ItemKind.Potion);
            if (potion == null) return true;
            _g.Quaff(potion);
            return true;
        }

        bool DoEat()
        {
            var food = ChooseItem("Eat what?", it => it.Def.Kind == ItemKind.Food);
            if (food == null) return true;
            _g.EatFood(food);
            _g.EndPlayerTurn();
            return true;
        }

        bool DoWield()
        {
            var w = ChooseItem("Wield what?", it => it.Def.Kind == ItemKind.Weapon);
            if (w == null) return true;
            Wield(w);
            return true;
        }

        void Wield(Item w)
        {
            var p = _g.Player;
            if ((w.Def.Flags & ItemFlags.TwoHanded) != 0 && p.WornShield != null)
            {
                _g.Say($"You cannot wield {w.Name} with a shield on your arm.");
                return;
            }
            if (p.Wielded != null) p.Inventory.Add(p.Wielded);
            p.Wielded = w;
            p.Inventory.Remove(w);
            p.RefreshGear();
            _g.RevealGear(w);
            _g.Say($"You are now wielding {w.Name}.", MessageKind.Good);
            _g.EndPlayerTurn();
        }

        bool DoWear()
        {
            var p = _g.Player;
            var a = ChooseItem("Wear what?", it => it.Def.Kind.IsWearable() && !p.IsWorn(it));
            if (a == null) return true;
            WearItem(a);
            return true;
        }

        /// <summary>Puts on a piece in its own slot (body, shield, helm, gloves, boots, cloak).</summary>
        void WearItem(Item a)
        {
            var p = _g.Player;
            if (a.Def.Kind == ItemKind.Shield && p.Wielded != null && (p.Wielded.Def.Flags & ItemFlags.TwoHanded) != 0)
            {
                _g.Say($"You cannot carry a shield with {p.Wielded.Name} in both hands.");
                return;
            }
            p.Wear(a);
            _g.RevealGear(a);
            _g.Say($"You are now wearing {a.Name} (AC {a.TotalAc}).", MessageKind.Good);
            _g.EndPlayerTurn();
        }

        bool DoRemoveArmor()
        {
            var p = _g.Player;
            var worn = new List<Item>(p.WornPieces());
            if (worn.Count == 0) { _g.Say("You are not wearing armour."); return true; }
            if (worn.Count == 1) { TakeOffItem(worn[0]); return true; }
            _g.PushChoice("Take off what?", worn);
            return true;
        }

        void TakeOffItem(Item it)
        {
            _g.Player.TakeOff(it);
            _g.Say($"You take off {it.Name}.", MessageKind.Neutral);
            _g.EndPlayerTurn();
        }

        bool DoPutOnRing()
        {
            var r = ChooseItem("Put on which ring?", it => it.Def.Kind == ItemKind.Ring);
            if (r == null) return true;
            var p = _g.Player;
            int slot = p.Rings[0] == null ? 0 : (p.Rings[1] == null ? 1 : -1);
            if (slot < 0) { _g.Say("Your hands are already full of rings."); return true; }
            p.Rings[slot] = r;
            p.RingKnown[slot] = false;
            p.Inventory.Remove(r);
            _g.Say($"You slip on {r.Name}. It may not do anything.", MessageKind.Neutral);
            _g.EndPlayerTurn();
            return true;
        }

        bool DoRemoveRing()
        {
            var p = _g.Player;
            for (int i = 0; i < 2; i++)
            {
                if (p.Rings[i] == null) continue;
                _g.Say($"You remove {p.Rings[i].Name}.", MessageKind.Neutral);
                p.Inventory.Add(p.Rings[i]);
                p.Rings[i] = null;
                p.RingKnown[i] = false;
                _g.EndPlayerTurn();
                return true;
            }
            _g.Say("You are not wearing any rings.");
            return true;
        }

        bool DoRead()
        {
            var s = ChooseItem("Read what?", it => it.Def.Kind == ItemKind.Scroll || it.Def.Kind == ItemKind.Book);
            if (s == null) return true;
            if (s.Def.Kind == ItemKind.Book) _g.StudyBook(s); else _g.UseScroll(s);
            return true;
        }

        bool DoZap()
        {
            _g.MakeNoise(2);
            var w = ChooseItem("Zap what?", it => it.Def.Kind == ItemKind.Wand);
            if (w == null) return true;
            _g.UseWand(w);
            return true;
        }

        bool DoUseKey()
        {
            var p = _g.Player;
            if (_g.Map == null) { _g.Say("There is nothing to open here."); return true; }
            int fx = p.X + _g.FacingX, fy = p.Y + _g.FacingY;
            TileKind t = _g.Map.Get(fx, fy);

            if (t == TileKind.LockedDoor && p.FindFirst("lock pick") == null)
            {
                _g.Say("The door is locked and you have nothing to pick it with.");
                return true;
            }
            if (t == TileKind.ClosedDoor || t == TileKind.LockedDoor || t == TileKind.HiddenDoor)
            {
                _g.Map.Set(fx, fy, TileKind.OpenDoor);
                _g.Say("You open the door.", MessageKind.Neutral);
                _g.Map.Version++;
                _g.EndPlayerTurn();
                return true;
            }
            _g.Say("There is no door here.");
            return true;
        }

        bool DoOpenDoor()
        {
            var p = _g.Player;
            if (_g.Map == null) return true;
            int fx = p.X + _g.FacingX, fy = p.Y + _g.FacingY;
            TileKind t = _g.Map.Get(fx, fy);

            if (t == TileKind.ClosedDoor || t == TileKind.LockedDoor)
            {
                if (t == TileKind.LockedDoor && p.FindFirst("lock pick") == null)
                {
                    _g.Say("The door is locked.");
                    return true;
                }
                _g.Map.Set(fx, fy, TileKind.OpenDoor);
                _g.Say("You open the door.");
                _g.Map.Version++;
                _g.EndPlayerTurn();
                return true;
            }
            if (t == TileKind.Rubble)
            {
                _g.Say("You shift the rubble aside.");
                _g.Map.Set(fx, fy, TileKind.Floor);
                _g.Map.Version++;
                _g.EndPlayerTurn();
                return true;
            }
            _g.Say("There is nothing to open.");
            return true;
        }

        bool DoKick()
        {
            var p = _g.Player;
            if (_g.ActiveEncounter) { _g.AttackEncounter(); return true; }
            if (_g.Map == null) return true;
            int fx = p.X + _g.FacingX, fy = p.Y + _g.FacingY;
            var m = _g.MonsterAt(fx, fy);
            if (m != null) return _g.Attack(m);

            TileKind t = _g.Map.Get(fx, fy);
            if (t == TileKind.ClosedDoor)
            {
                _g.Say("You kick the door open.");
                _g.Map.Set(fx, fy, TileKind.OpenDoor);
                _g.Map.Version++;
                _g.EndPlayerTurn();
                return true;
            }
            if (t == TileKind.Rubble)
            {
                _g.Say("You kick the rubble away.");
                _g.Map.Set(fx, fy, TileKind.Floor);
                _g.Map.Version++;
                _g.EndPlayerTurn();
                return true;
            }
            _g.Say("You kick at the air.");
            _g.EndPlayerTurn();
            return true;
        }

        bool DoSearch()
        {
            var p = _g.Player;
            _g.BeQuiet();
            if (_g.Map == null) return true;
            bool found = false;
            for (int y = p.Y - 1; y <= p.Y + 1; y++)
            {
                for (int x = p.X - 1; x <= p.X + 1; x++)
                {
                    if (TrapTable.TryGet(_g.Map.Number, x, y, out var trapKind, out _))
                    {
                        if (TrapTable.Reveal(_g.Map.Number, x, y)) _g.Say($"You find a {TrapTable.Name(trapKind)} at ({x},{y}).", MessageKind.Good);
                        found = true;
                    }
                    if (_g.Map.InBounds(x, y) && _g.Map.Get(x, y) == TileKind.HiddenDoor)
                    {
                        _g.Map.Set(x, y, TileKind.ClosedDoor);
                        _g.Say($"You find a secret door at ({x},{y}).", MessageKind.Good);
                        found = true;
                    }
                }
            }
            if (!found) _g.Say("You find nothing out of the ordinary.");
            p.GainSkill(Skill.Search, 2);
            _g.Map.Version++;
            _g.EndPlayerTurn();
            return true;
        }

        /// <summary>Disarms a found trap underfoot or beside you, preferring the one you face. Refusals cost no turn.</summary>
        bool DoDisarm()
        {
            var p = _g.Player;
            if (_g.Mode != GameMode.Dungeon || _g.Map == null) { _g.Say("There is nothing to disarm here."); return true; }
            int bx = int.MinValue, by = int.MinValue;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = p.X + dx, y = p.Y + dy;
                    if (!TrapTable.TryGet(_g.Map.Number, x, y, out _, out _) || !TrapTable.IsRevealed(_g.Map.Number, x, y)) continue;
                    bool facing = dx == _g.FacingX && dy == _g.FacingY;
                    if (bx == int.MinValue || facing) { bx = x; by = y; }
                }
            if (bx == int.MinValue) { _g.Say("There is no known trap to disarm nearby.", MessageKind.Info); return true; }
            if (p.Asleep || p.Stunned || p.Blinded) { _g.Say("You cannot do that now."); return true; }
            _g.DisarmTrap(bx, by);
            return true;
        }

        bool DoSwapWith()
        {
            _g.PushTargeting(TargetingMode.Swap);
            return true;
        }

        // ---------------------------------------------------------------- shops

        /// <summary>Prompt used for the sell picker, so the input layer can tell a sell choice apart.</summary>
        public const string SellPrompt = "Sell what?";

        /// <summary>Buys the stock entry under the shop cursor. The panel calls this on Enter/b.</summary>
        public bool BuyShopCursor()
        {
            var shop = _g.CurrentShop;
            if (shop == null || !_g.InShop) { _g.Say("There is no one here to trade with."); return true; }
            if (shop.Stock.Count == 0) { _g.Say("The shelves are bare."); return true; }
            int idx = Math.Max(0, Math.Min(_g.UiState.ShopIndex, shop.Stock.Count - 1));
            _g.UiState.ShopIndex = idx;
            var item = shop.Stock[idx];
            if (_g.BuyFromShop(shop, item))
            {
                // Buying removes the entry, so keep the cursor on a valid row.
                if (_g.UiState.ShopIndex >= shop.Stock.Count)
                    _g.UiState.ShopIndex = Math.Max(0, shop.Stock.Count - 1);
            }
            return true;
        }

        /// <summary>Opens a "sell what?" picker over the pack. The choice panel resolves it via CommitSellChoice.</summary>
        public bool BeginSellToShop()
        {
            var shop = _g.CurrentShop;
            if (shop == null || !_g.InShop) { _g.Say("There is no one here to trade with."); return true; }
            var p = _g.Player;
            if (p.Inventory.Count == 0) { _g.Say("You have nothing to sell."); return true; }
            _g.PushChoice(SellPrompt, new List<Item>(p.Inventory));
            _g.UiState.ChoiceIndex = 0;
            return true;
        }

        /// <summary>Applies a sell choice raised by BeginSellToShop.</summary>
        public bool CommitSellChoice(Item chosen)
        {
            var shop = _g.CurrentShop;
            _g.PendingChoice.Clear();
            if (chosen == null) return false;
            if (shop == null || !_g.InShop) { _g.Say("The shopkeeper is gone."); return false; }
            return _g.SellToShop(shop, chosen);
        }

        // ------------------------------------------------------------- helpers

        public Item ChooseItem(string prompt, Func<Item, bool> filter)
        {
            var p = _g.Player;
            var candidates = new List<Item>();
            for (int i = 0; i < p.Inventory.Count; i++) if (filter(p.Inventory[i])) candidates.Add(p.Inventory[i]);
            if (p.Wielded != null && filter(p.Wielded)) candidates.Add(p.Wielded);
            foreach (var piece in p.WornPieces()) if (filter(piece)) candidates.Add(piece);
            for (int i = 0; i < 2; i++) if (p.Rings[i] != null && filter(p.Rings[i])) candidates.Add(p.Rings[i]);

            if (candidates.Count == 0) { _g.Say("You have nothing suitable."); return null; }
            if (candidates.Count == 1) return candidates[0];
            _g.PushChoice(prompt, candidates);
            return null;
        }

        /// <summary>Applies a choice the player just made in the selection panel.</summary>
        public bool CommitChoice(Item chosen)
        {
            string prompt = _g.PendingChoice.Prompt;
            _g.PendingChoice.Clear();
            if (chosen == null) return false;
            if (prompt == Game.OfferPrompt) { _g.OfferItem(chosen); return true; }
            if (prompt == Game.SacrificePrompt) { _g.SacrificeCorpse(chosen); return true; }
            if (prompt == Game.CraftPrompt) { _g.Craft(chosen); return true; }
            if (prompt == Game.AppraisePrompt)
            {
                _g.AppraiseItem(chosen);
                if (_g.Talking != null && _g.TalkBuilding != null) { _g.UiState.Active = Panel.Service; }
                return true;
            }
            if (chosen.Def.Kind == ItemKind.Weapon) { Wield(chosen); return true; }
            if (chosen.Def.Kind.IsWearable())
            {
                if (_g.Player.IsWorn(chosen)) TakeOffItem(chosen); else WearItem(chosen);
                return true;
            }
            if (chosen.Def.Kind == ItemKind.Ring)
            {
                var p = _g.Player;
                int slot = p.Rings[0] == null ? 0 : (p.Rings[1] == null ? 1 : -1);
                if (slot < 0) { _g.Say("Your hands are already full."); return false; }
                p.Rings[slot] = chosen;
                p.RingKnown[slot] = false;
                p.Inventory.Remove(chosen);
                _g.Say($"You slip on {chosen.Name}.", MessageKind.Neutral);
                _g.EndPlayerTurn();
                return true;
            }
            if (chosen.Def.Kind == ItemKind.Food)
            {
                p_Food(chosen);
                return true;
            }
            if (chosen.Def.Kind == ItemKind.Scroll) { _g.UseScroll(chosen); return true; }
            if (chosen.Def.Kind == ItemKind.Potion) { _g.Quaff(chosen); return true; }
            if (chosen.Def.Kind == ItemKind.Book) { _g.StudyBook(chosen); return true; }
            if (chosen.Def.Kind == ItemKind.Wand)
            {
                _g.UseWand(chosen);
                return true;
            }
            if (chosen.Def.Kind == ItemKind.Tool)
            {
                if (chosen.Name == "pick-axe") { _g.PushTargeting(TargetingMode.Dig); return true; }
                if (chosen.Name == "lock pick") { _g.PushTargeting(TargetingMode.PickLock); return true; }
            }
            return false;
        }

        void p_Food(Item food)
        {
            var p = _g.Player;
            _g.EatFood(food);
            _g.EndPlayerTurn();
        }
    }
}