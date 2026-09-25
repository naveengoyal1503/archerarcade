using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// Outcome of MatchState.ApplyShot. Owned and reused by the MatchState: read it (or copy what you need)
    /// before the next ApplyShot / Tick call.
    /// </summary>
    public sealed class ShotResult
    {
        public bool Accepted;
        public ShotRejectReason Reason;
        public int Shooter = -1;
        public ShotInput Input;
        public int Wind;

        /// <summary>Match clock at release (moving targets are placed with it).</summary>
        public double Clock;
        public readonly List<ArrowPath> Arrows = new List<ArrowPath>(8);

        /// <summary>Latest time any arrow of this shot stops (length of the flight animation).</summary>
        public double Duration;

        internal void Reset()
        {
            Accepted = false;
            Reason = ShotRejectReason.None;
            Shooter = -1;
            Input = default;
            Wind = 0;
            Clock = 0.0;
            Arrows.Clear();
            Duration = 0.0;
        }
    }
}
