namespace ArcherArcade.Logic
{
    /// <summary>
    /// Something that happened, for visuals, audio, haptics, badges and stats. <see cref="Time"/> is seconds after
    /// release for shot events (so juice fires exactly when the arrow lands), 0 for turn events.
    /// </summary>
    public struct MatchEvent
    {
        public MatchEventKind Kind;

        /// <summary>Fighter affected (target, or the archer whose turn it is).</summary>
        public int Fighter;

        /// <summary>Fighter that caused it (shooter), −1 if none.</summary>
        public int Source;

        public HitZone Zone;
        public int Amount;
        public double Time;
        public Vec2 Point;
        public ArrowTip Tip;

        /// <summary>Arrow index in ShotResult.Arrows, −1 if not from an arrow.</summary>
        public int Arrow;

        /// <summary>Prop involved, −1 if none.</summary>
        public int Prop;
    }
}
