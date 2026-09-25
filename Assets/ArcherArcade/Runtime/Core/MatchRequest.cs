using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;

namespace ArcherArcade.Core
{
    /// <summary>What the Match scene should play; set by the menus before <see cref="SceneFlow.StartMatch"/>.</summary>
    public sealed class MatchRequest
    {
        public GameMode Mode = GameMode.Campaign;
        /// <summary>Campaign level number (1–20).</summary>
        public int Level = 1;
        /// <summary>Quick Duel difficulty ("easy" / "medium" / "hard") and arena id (or "random").</summary>
        public string Difficulty = "medium";
        public string ArenaId = "random";
        /// <summary>2-Player best-of series (lives across rounds and rematches).</summary>
        public PvpSeries Series;
        /// <summary>Daily Challenge day number (UTC).</summary>
        public int Day;
        /// <summary>Training Range distance (m) and wind.</summary>
        public double TrainingDistance = 20;
        public int TrainingWind;
        /// <summary>Boosters bought for this match (already paid).</summary>
        public BoosterKind[] Boosters = new BoosterKind[0];
        /// <summary>Loss help: preview +15 % (campaign, after 3 losses).</summary>
        public bool Assist;
        public ulong Seed = 1;

        public MatchRequest Clone()
        {
            var r = (MatchRequest)MemberwiseClone();
            r.Boosters = (BoosterKind[])Boosters.Clone();
            return r;
        }
    }
}
