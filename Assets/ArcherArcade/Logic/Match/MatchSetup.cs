using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>Everything needed to start a match. Same setup + same shot inputs = same match.</summary>
    public sealed class MatchSetup
    {
        public ulong Seed = 1UL;
        public ShotConfig Shot = new ShotConfig();
        public DamageConfig Damage = new DamageConfig();
        public TipTable Tips = TipTable.CreateDefault();
        public BodyConfig Body = new BodyConfig();
        public MatchConfig Rules = new MatchConfig();
        public PropConfig PropRules = new PropConfig();
        public AbilityTable Abilities = AbilityTable.CreateDefault();
        public BoosterConfig Boosters = new BoosterConfig();
        public ArenaLayout Arena = new ArenaLayout();
        public WindRange Wind = WindRange.Calm;
        public FirstTurnRule FirstTurn = FirstTurnRule.SideZero;

        /// <summary>
        /// Fighters in order. Each side (0 and 1) fights with its first fighter still standing; later fighters of
        /// a side enter one after another (Gauntlet). Side 1 may be empty: a solo level (targets, apples, trick
        /// shots) where every turn is side 0's.
        /// </summary>
        public readonly List<FighterSpec> Fighters = new List<FighterSpec>();

        /// <summary>Props in the arena (walls, crates, towers, pads, targets, shields…).</summary>
        public readonly List<PropSpec> Props = new List<PropSpec>();
    }
}
