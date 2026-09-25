namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// Loadout rules (GAME_DESIGN §5, §5.1): up to 3 special tips, only unlocked ones, no duplicates; at most one of
    /// each booster. Boosters are never required. A level may force its own tips (LEVELS.md: L11 Bomb, L17 Split).
    /// </summary>
    public static class LoadoutRules
    {
        public const int MaxSpecialTips = 3;

        public static LoadoutError Validate(Loadout loadout, bool archerOwned, int highestLevelCleared)
        {
            if (!archerOwned) return LoadoutError.ArcherLocked;
            ArrowTip[] tips = loadout.Tips ?? new ArrowTip[0];
            int special = 0;
            for (int i = 0; i < tips.Length; i++)
            {
                if (tips[i] == ArrowTip.Normal) return LoadoutError.TipNotAllowed;
                if (!TipUnlocks.IsUnlocked(tips[i], highestLevelCleared)) return LoadoutError.TipLocked;
                for (int j = 0; j < i; j++)
                {
                    if (tips[j] == tips[i]) return LoadoutError.DuplicateTip;
                }
                special++;
            }
            if (special > MaxSpecialTips) return LoadoutError.TooManyTips;

            BoosterKind[] boosters = loadout.Boosters ?? new BoosterKind[0];
            for (int i = 0; i < boosters.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (boosters[j] == boosters[i]) return LoadoutError.DuplicateBooster;
                }
            }
            return LoadoutError.None;
        }

        /// <summary>Builds the match entry for the player from a (valid) loadout.</summary>
        public static FighterSpec ToFighterSpec(Loadout loadout, int archerLevel, Vec2 feet, int facing)
        {
            return new FighterSpec
            {
                Def = ArcherTable.Hero(loadout.ArcherId),
                Side = 0,
                Level = archerLevel,
                Feet = feet,
                Facing = facing,
                Tips = loadout.Tips ?? new ArrowTip[0],
                Boosters = loadout.Boosters ?? new BoosterKind[0]
            };
        }
    }
}
