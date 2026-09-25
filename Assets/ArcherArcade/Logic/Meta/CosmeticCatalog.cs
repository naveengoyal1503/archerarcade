using System.Collections.Generic;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// v1.0 cosmetics (GAME_DESIGN §4, §5, §8): 2 skins per archer (default + one earned) and 4 arrow trails
    /// (Classic, Sparkle, Rainbow, Comet). Skins 300–600 coins, trails 200–400, some only from badges or streaks.
    /// </summary>
    public static class CosmeticCatalog
    {
        static readonly CosmeticDef[] Items =
        {
            Skin("skin_ranger_default", "ranger", "Ranger Green", CosmeticSource.Default, 0),
            Skin("skin_ranger_forest", "ranger", "Forest Cloak", CosmeticSource.Shop, 300, chest: true),
            Skin("skin_fire_default", "fire", "Flame Red", CosmeticSource.Default, 0),
            Skin("skin_fire_ember", "fire", "Ember Coat", CosmeticSource.Shop, 450, chest: true),
            Skin("skin_electric_default", "electric", "Volt Yellow", CosmeticSource.Default, 0),
            new CosmeticDef
            {
                Id = "skin_electric_neon", Kind = CosmeticKind.Skin, ArcherId = "electric", Name = "Neon Bolt",
                Source = CosmeticSource.Badge, BadgeId = "thunderstruck", BadgeTier = BadgeTier.Silver
            },
            Skin("skin_bomb_default", "bomb", "Blast Orange", CosmeticSource.Default, 0),
            Skin("skin_bomb_goggles", "bomb", "Blast Goggles", CosmeticSource.Shop, 600, chest: true),
            Trail("trail_classic", "Classic", CosmeticSource.Default, 0),
            Trail("trail_sparkle", "Sparkle", CosmeticSource.Shop, 200, chest: true),
            Trail("trail_rainbow", "Rainbow", CosmeticSource.DailyStreak, 0),
            Trail("trail_comet", "Comet", CosmeticSource.Shop, 400, chest: true)
        };

        static CosmeticDef Skin(string id, string archer, string name, CosmeticSource source, int price, bool chest = false)
        {
            return new CosmeticDef
            {
                Id = id, Kind = CosmeticKind.Skin, ArcherId = archer, Name = name, Source = source, Price = price, InChestPool = chest
            };
        }

        static CosmeticDef Trail(string id, string name, CosmeticSource source, int price, bool chest = false)
        {
            return new CosmeticDef { Id = id, Kind = CosmeticKind.Trail, Name = name, Source = source, Price = price, InChestPool = chest };
        }

        public static IReadOnlyList<CosmeticDef> All => Items;

        public static CosmeticDef ById(string id)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Id == id) return Items[i];
            }
            return null;
        }

        public static string DefaultSkinFor(string archerId) => "skin_" + archerId + "_default";

        public static List<CosmeticDef> SkinsFor(string archerId)
        {
            var list = new List<CosmeticDef>();
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Kind == CosmeticKind.Skin && Items[i].ArcherId == archerId) list.Add(Items[i]);
            }
            return list;
        }

        public static List<CosmeticDef> Trails()
        {
            var list = new List<CosmeticDef>();
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Kind == CosmeticKind.Trail) list.Add(Items[i]);
            }
            return list;
        }

        /// <summary>Chest drops in a fixed order (deterministic).</summary>
        public static List<CosmeticDef> ChestPool()
        {
            var list = new List<CosmeticDef>();
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].InChestPool) list.Add(Items[i]);
            }
            return list;
        }
    }
}
