using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Modes
{
    /// <summary>
    /// Quick Duel and 2-Player arenas (LEVELS.md): Meadow (open), Fence (wall), Crates, Platforms, Barrels, and
    /// Mirror Forest (symmetric, no props, pure skill — 2-Player only).
    /// </summary>
    public static class ArenaCatalog
    {
        public static readonly string[] QuickDuelArenas = { "meadow", "fence", "crates", "platforms", "barrels" };
        public static readonly string[] TwoPlayerArenas = { "meadow", "fence", "crates", "platforms", "barrels", "mirror" };

        /// <summary>Fills a level arena with the named layout; the opponent (index 0) stands at <paramref name="distance"/>.</summary>
        public static void Build(LevelArena a, string id, double distance)
        {
            double mid = distance * 0.5;
            switch (id)
            {
                case "fence":
                    a.Island(mid, 3);
                    a.Wall(mid, 0, 2.2);
                    a.Island(distance, 6);
                    a.Opponent(0, distance);
                    break;
                case "crates":
                    a.Island(mid, 3);
                    a.Tower(0, mid, 0, 4, 1);
                    a.Island(distance, 6);
                    a.Opponent(0, distance);
                    break;
                case "platforms":
                {
                    a.PlayerStandOnProp = a.Platform(0, 0.0, 1.4, new Vec2(0, 0), new Vec2(0, 2.0), 4);
                    int p = a.Platform(distance, 0.0, 1.4, new Vec2(0, 2.0), new Vec2(0, 0), 4);
                    a.Opponent(0, distance, 0.0);
                    a.Opponents[0].StandOnProp = p;
                    break;
                }
                case "barrels":
                    a.Island(mid, 4);
                    a.Barrel(mid - 0.8, 0);
                    a.Barrel(mid + 0.8, 0);
                    a.Island(distance, 6);
                    a.Barrel(distance - 1.8, 0);
                    a.Barrel(1.8, 0);
                    a.Opponent(0, distance);
                    break;
                default: // meadow, mirror
                    a.Island(distance, 6);
                    a.Opponent(0, distance);
                    break;
            }
        }

        public static string Pick(string id, Rng rng, bool twoPlayer)
        {
            string[] list = twoPlayer ? TwoPlayerArenas : QuickDuelArenas;
            if (id == "random" || string.IsNullOrEmpty(id)) return list[rng.NextInt(list.Length)];
            return id;
        }
    }
}
