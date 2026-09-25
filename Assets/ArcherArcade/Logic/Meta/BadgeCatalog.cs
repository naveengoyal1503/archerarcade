using System.Collections.Generic;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>The 22 v1.0 badges, each bronze / silver / gold, exactly as GAME_DESIGN §9.</summary>
    public static class BadgeCatalog
    {
        static readonly BadgeDef[] Items =
        {
            B("sharpshooter", "Sharpshooter", "target", "Headshots", StatKey.Headshots, 10, 50, 200),
            B("bullseye", "Bullseye", "adjust", "Target-level hits", StatKey.TargetHits, 25, 100, 400),
            B("untouchable", "Untouchable", "shield", "Wins without taking damage", StatKey.FlawlessWins, 1, 5, 20),
            B("wind_whisperer", "Wind Whisperer", "air", "Hits in wind 4 or more", StatKey.StrongWindHits, 10, 40, 150),
            B("long_shot", "Long Shot", "straighten", "Hits from 35 m or more", StatKey.LongShotHits, 5, 25, 100),
            B("firestarter", "Firestarter", "local_fire_department", "Burn damage dealt", StatKey.BurnDamage, 100, 500, 2000),
            B("chain_reaction", "Chain Reaction", "electric_bolt", "Electric chains", StatKey.ElectricChains, 10, 50, 200),
            B("demolition", "Demolition", "inventory_2", "Crates and barrels destroyed", StatKey.PropsDestroyed, 10, 50, 200),
            B("triple_threat", "Triple Threat", "call_split", "Triple Shot hits", StatKey.TripleShotHits, 10, 40, 150),
            B("forest_hero", "Forest Hero", "forest", "World 1 stars", StatKey.WorldOneStars, 20, 40, 60),
            new BadgeDef
            {
                Id = "warden_slayer", Name = "Warden Slayer", Icon = "crown", Description = "Beat the Forest Warden",
                Stat = StatKey.WardenWins, Bronze = 1, Silver = 3, Gold = 3, GoldIsBossThreeStars = true
            },
            B("quick_draw", "Quick Draw", "bolt", "Shots released in under 3 s", StatKey.QuickDraws, 20, 100, 400),
            B("comeback", "Comeback", "favorite", "Wins with under 15 % HP", StatKey.Comebacks, 1, 5, 20),
            B("duelist", "Duelist", "group", "2-Player matches played", StatKey.PvpMatches, 3, 15, 50),
            B("friendly_rivals", "Friendly Rivals", "handshake", "2-Player best-of-3 finished", StatKey.PvpSeriesFinished, 1, 5, 20),
            B("daily_devotee", "Daily Devotee", "calendar_month", "Daily streak", StatKey.DailyStreakBest, 3, 7, 30),
            B("collector", "Collector", "groups", "Archers owned", StatKey.ArchersOwned, 2, 3, 4),
            B("upgrader", "Upgrader", "upgrade", "Total archer levels", StatKey.ArcherLevels, 10, 25, 40),
            B("apple_picker", "Apple Picker", "nutrition", "Apples hit", StatKey.ApplesHit, 3, 10, 30),
            B("coin_keeper", "Coin Keeper", "savings", "Coins earned in total", StatKey.CoinsEarned, 1000, 5000, 20000),
            B("thunderstruck", "Thunderstruck", "thunderstorm", "Lightning strikes landed", StatKey.LightningStrikes, 10, 50, 200),
            B("tower_toppler", "Tower Toppler", "domain_disabled", "Crate towers knocked down", StatKey.TowersToppled, 3, 15, 60)
        };

        static BadgeDef B(string id, string name, string icon, string description, StatKey stat, long bronze, long silver, long gold)
        {
            return new BadgeDef
            {
                Id = id, Name = name, Icon = icon, Description = description, Stat = stat, Bronze = bronze, Silver = silver, Gold = gold
            };
        }

        public static IReadOnlyList<BadgeDef> All => Items;

        public static BadgeDef ById(string id)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Id == id) return Items[i];
            }
            return null;
        }
    }
}
