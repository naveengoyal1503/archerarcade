using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>Icon + colour for game things shown in the UI (arrow tips, boosters, idea cards), DESIGN_TOKENS §3.</summary>
    public static class GameVisuals
    {
        public static string TipIcon(ArrowTip t)
        {
            switch (t)
            {
                case ArrowTip.Fire: return Icons.LocalFireDepartment;
                case ArrowTip.Electric: return Icons.ElectricBolt;
                case ArrowTip.Split: return Icons.CallSplit;
                case ArrowTip.Bomb: return Icons.Bomb;
                case ArrowTip.Heavy: return Icons.Weight;
                case ArrowTip.Ice: return Icons.AcUnit;
                case ArrowTip.Poison: return Icons.Science;
                default: return Icons.ArrowForward;
            }
        }

        public static Color TipColor(ArrowTip t)
        {
            switch (t)
            {
                case ArrowTip.Fire: return UiKit.Hex(0xFF8A3D);
                case ArrowTip.Electric: return UiKit.Hex(0xE0A800);
                case ArrowTip.Split: return UiKit.Hex(0x9D86FF);
                case ArrowTip.Bomb: return UiKit.Hex(0xFF5A6E);
                case ArrowTip.Heavy: return UiKit.Hex(0x8E7A5B);
                case ArrowTip.Ice: return UiKit.Hex(0x38BDF8);
                case ArrowTip.Poison: return UiKit.Hex(0x6FBF2F);
                default: return UiKit.Hex(0x2ED3A0);
            }
        }

        public static string BoosterIcon(BoosterKind b)
        {
            switch (b)
            {
                case BoosterKind.ShieldBubble: return Icons.BubbleChart;
                case BoosterKind.MultiArrow: return Icons.CallSplit;
                case BoosterKind.IronHelmet: return Icons.HealthAndSafety;
                default: return Icons.Favorite;
            }
        }

        public static Color BoosterColor(BoosterKind b)
        {
            switch (b)
            {
                case BoosterKind.ShieldBubble: return UiKit.Hex(0x38BDF8);
                case BoosterKind.MultiArrow: return UiKit.Hex(0x9D86FF);
                case BoosterKind.IronHelmet: return UiKit.Hex(0x8A94A6);
                default: return UiKit.Hex(0xE5484D);
            }
        }

        public static string IdeaIcon(IdeaCard c)
        {
            switch (c)
            {
                case IdeaCard.Tutorial: return Icons.TouchApp;
                case IdeaCard.DuelsAndHp: return Icons.Favorite;
                case IdeaCard.Walls: return Icons.Block;
                case IdeaCard.Wind: return Icons.Air;
                case IdeaCard.Headshots: return Icons.Target;
                case IdeaCard.CrateTowers: return Icons.Inventory2;
                case IdeaCard.AppleShot: return Icons.Nutrition;
                case IdeaCard.SwingingTargets: return Icons.TrackChanges;
                case IdeaCard.MovingPlatforms: return Icons.SwapHoriz;
                case IdeaCard.MiniBoss: return Icons.Skull;
                case IdeaCard.TntCrates: return Icons.Explosion;
                case IdeaCard.ShieldsAndBombs: return Icons.Shield;
                case IdeaCard.BouncePads: return Icons.ArrowUpward;
                case IdeaCard.Gauntlet: return Icons.Groups;
                case IdeaCard.Rescue: return Icons.Pets;
                case IdeaCard.ExplosiveBarrels: return Icons.LocalFireDepartment;
                case IdeaCard.Healers: return Icons.Healing;
                case IdeaCard.SplitArrows: return Icons.CallSplit;
                case IdeaCard.ShieldBubbles: return Icons.BubbleChart;
                case IdeaCard.Boss: return Icons.Crown;
                case IdeaCard.TipFire: return TipIcon(ArrowTip.Fire);
                case IdeaCard.TipElectric: return TipIcon(ArrowTip.Electric);
                case IdeaCard.TipSplit: return TipIcon(ArrowTip.Split);
                case IdeaCard.TipBomb: return TipIcon(ArrowTip.Bomb);
                case IdeaCard.TipHeavy: return TipIcon(ArrowTip.Heavy);
                case IdeaCard.TipIce: return TipIcon(ArrowTip.Ice);
                case IdeaCard.TipPoison: return TipIcon(ArrowTip.Poison);
                default: return Icons.Info;
            }
        }

        public static Color IdeaColor(IdeaCard c)
        {
            switch (c)
            {
                case IdeaCard.TipFire: return TipColor(ArrowTip.Fire);
                case IdeaCard.TipElectric: return TipColor(ArrowTip.Electric);
                case IdeaCard.TipSplit: return TipColor(ArrowTip.Split);
                case IdeaCard.TipBomb: return TipColor(ArrowTip.Bomb);
                case IdeaCard.TipHeavy: return TipColor(ArrowTip.Heavy);
                case IdeaCard.TipIce: return TipColor(ArrowTip.Ice);
                case IdeaCard.TipPoison: return TipColor(ArrowTip.Poison);
                case IdeaCard.MiniBoss:
                case IdeaCard.Boss: return UiKit.Hex(0xE5484D);
                case IdeaCard.Wind: return UiKit.Hex(0x1C9AD6);
                case IdeaCard.Headshots: return UiKit.Hex(0xF0641E);
                default: return UiKit.Hex(0x6D4AFF);
            }
        }

        /// <summary>The idea card that introduces an arrow tip.</summary>
        public static IdeaCard TipCard(ArrowTip t)
        {
            switch (t)
            {
                case ArrowTip.Fire: return IdeaCard.TipFire;
                case ArrowTip.Electric: return IdeaCard.TipElectric;
                case ArrowTip.Split: return IdeaCard.TipSplit;
                case ArrowTip.Bomb: return IdeaCard.TipBomb;
                case ArrowTip.Heavy: return IdeaCard.TipHeavy;
                case ArrowTip.Ice: return IdeaCard.TipIce;
                default: return IdeaCard.TipPoison;
            }
        }

        public static string GoalText(LevelDef l)
        {
            switch (l.Goal)
            {
                case GoalKind.Targets: return Core.Loc.F("goal_targets", l.TargetCount);
                case GoalKind.AppleShot: return Core.Loc.F("goal_apple", l.TargetCount);
                case GoalKind.Gauntlet: return Core.Loc.F("goal_gauntlet", l.Opponents.Length);
                case GoalKind.Rescue: return Core.Loc.T("goal_rescue");
                case GoalKind.TrickShot: return Core.Loc.F("goal_trick", l.TargetCount);
                case GoalKind.Boss: return Core.Loc.T("goal_boss");
                default: return Core.Loc.T("goal_duel");
            }
        }
    }
}
