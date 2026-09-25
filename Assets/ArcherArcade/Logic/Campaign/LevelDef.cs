namespace ArcherArcade.Logic.Campaign
{
    /// <summary>One campaign level, exactly as its row in LEVELS.md. Built by <see cref="WorldOne"/>.</summary>
    public sealed class LevelDef
    {
        public int World = 1;
        public int Number;
        public string Name = "";
        public LevelTier Tier;
        public GoalKind Goal;

        /// <summary>Targets / apples / hidden targets to hit (target-type goals).</summary>
        public int TargetCount;

        /// <summary>Enemies in order (a Gauntlet has several; they fight one after another).</summary>
        public OpponentSpec[] Opponents = new OpponentSpec[0];

        /// <summary>LEVELS.md "Dist" column (m), for the level card.</summary>
        public double DistanceMin;
        public double DistanceMax;

        public WindRange Wind;

        /// <summary>Par: turns for duel-type goals, arrows for target-type goals.</summary>
        public int Par;

        /// <summary>Trajectory preview length as a share of the arc (0.6 = 60 %).</summary>
        public double PreviewShare;

        public int RewardCoins;

        /// <summary>Chest earned on first clear (1–4), 0 = none.</summary>
        public int Chest;

        public ArrowTip? UnlockTip;
        public string UnlockArcherId;

        public IdeaCard[] Ideas = new IdeaCard[0];
        public TimeOfDay Time;

        /// <summary>Tips given for this level instead of the player's picks (L11 Bomb ×2, L17 Split ×2), or null.</summary>
        public ArrowTip[] ForcedTips;

        /// <summary>Arrows allowed on target-type goals (0 = 2 × par + 2).</summary>
        public int ArrowLimit;

        /// <summary>Only this tip, with unlimited ammo (daily "Split only" / "Heavy only"), or null.</summary>
        public ArrowTip? OnlyTip;

        /// <summary>Hint shown after 2 losses (string id).</summary>
        public string LossTip = "tip_aim_wind";

        /// <summary>Builds islands, props and spawn points.</summary>
        public System.Action<LevelArena> BuildArena;

        public string Id => "w" + World + "_l" + Number.ToString("00");

        public bool IsDuelGoal => Goal == GoalKind.Duel || Goal == GoalKind.Gauntlet || Goal == GoalKind.Boss;

        public int EffectiveArrowLimit => ArrowLimit > 0 ? ArrowLimit : Par * 2 + 2;
    }
}
