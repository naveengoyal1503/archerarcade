using System.Collections.Generic;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// Campaign chests (after levels 5/10/15/20): 80–140 coins and a chance of a cosmetic, both seeded by the chest's
    /// id so a chest always holds the same thing — nothing random about money, nothing to buy.
    /// </summary>
    public static class Chests
    {
        public static string IdFor(int world, int chestIndex) => "w" + world + "_chest" + chestIndex;

        public static ChestContents Open(string chestId, EconomyConfig cfg, ICollection<string> owned)
        {
            var rng = new Rng(Hash(chestId), 0xC4E57UL);
            var c = new ChestContents { Coins = rng.RangeInclusive(cfg.ChestCoinsMin, cfg.ChestCoinsMax) };
            bool cosmetic = rng.Chance(cfg.ChestCosmeticChance);
            List<CosmeticDef> pool = CosmeticCatalog.ChestPool();
            int start = rng.NextInt(pool.Count);
            if (!cosmetic) return c;
            for (int i = 0; i < pool.Count; i++)
            {
                CosmeticDef d = pool[(start + i) % pool.Count];
                if (owned != null && owned.Contains(d.Id)) continue;
                c.CosmeticId = d.Id;
                break;
            }
            return c;
        }

        /// <summary>FNV-1a 64 of the id (stable across runtimes, unlike string.GetHashCode).</summary>
        public static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            return h;
        }

        public static int ChestAfterLevel(int levelNumber) => levelNumber % 5 == 0 ? levelNumber / 5 : 0;

        public static int Count => 4;
    }
}
