using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Held-key walking. The host repeats a movement key only while this says the way is calm, so a held
    /// direction never walks you into trouble. It reads state only: it never advances a turn or uses the Rng.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>True when walking on one more step on its own is reasonable.</summary>
        public bool CanKeepWalking()
        {
            if (Mode == GameMode.GameOver || Mode == GameMode.Won) return false;
            if (ActiveEncounter || InShop || PendingChoice.Active) return false;
            if (Player.HP <= 0 || Player.Asleep) return false;
            if (Mode == GameMode.Overworld) return true;
            if (Map == null) return false;
            if (HostileInView()) return false;
            var here = GroundItems.At(Map.Number, Player.X, Player.Y);
            if (here != null && here.Count > 0) return false;
            switch (Map.Get(Player.X, Player.Y))
            {
                case TileKind.StairsDown: case TileKind.StairsUp: case TileKind.LadderDown:
                case TileKind.Portal: case TileKind.Altar: case TileKind.Fountain:
                case TileKind.OpenDoor: case TileKind.ClosedDoor:
                    return false;
            }
            return true;
        }

        /// <summary>Where the player stands on the map that is being walked (the world map on the overworld).</summary>
        public (int X, int Y) WalkPosition() => Mode == GameMode.Overworld ? (World.PlayerX, World.PlayerY) : (Player.X, Player.Y);

        /// <summary>An awake enemy the player can see right now (allies, townsfolk and the other people of the dungeon never count).</summary>
        public bool HostileInView()
        {
            for (int i = 0; i < Monsters.Count; i++)
            {
                var m = Monsters[i];
                if (m.Ally || m.Townsperson || m.Dormant || m.HP <= 0 || m.Folk != FolkKind.None) continue;
                if (Map.IsCurrentlyVisible(m.X, m.Y)) return true;
            }
            return false;
        }
    }
}
