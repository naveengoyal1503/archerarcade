namespace ArcherArcade.Logic
{
    /// <summary>
    /// One arrow's flight, from launch (or split / bounce) to where it stopped. Visuals rebuild the arc exactly with
    /// Ballistics.PositionAt(StartPosition, StartVelocity, Acceleration, t − StartTime).
    /// </summary>
    public struct ArrowPath
    {
        public Vec2 StartPosition;
        public Vec2 StartVelocity;
        public Vec2 Acceleration;
        public double StartTime;

        public double EndTime;
        public Vec2 EndPosition;
        public Vec2 EndVelocity;
        public ContactKind Contact;

        /// <summary>Fighter hit (Contact == Fighter), otherwise −1.</summary>
        public int HitFighter;
        public HitZone Zone;

        /// <summary>Prop hit or bounced off (Contact == Prop or Bounce), otherwise −1.</summary>
        public int HitProp;
        public int ColliderIndex;

        /// <summary>Index of the arrow this one split or bounced from, −1 for a launched arrow.</summary>
        public int Parent;

        /// <summary>Remaining split count (0 = does not split).</summary>
        public int SplitCount;

        /// <summary>Bounces so far in this arrow's chain.</summary>
        public int BounceCount;

        public static ArrowPath Launch(Vec2 position, Vec2 velocity, Vec2 acceleration, double startTime, int parent,
            int splitCount, int bounceCount = 0)
        {
            return new ArrowPath
            {
                StartPosition = position,
                StartVelocity = velocity,
                Acceleration = acceleration,
                StartTime = startTime,
                EndTime = startTime,
                EndPosition = position,
                EndVelocity = velocity,
                Contact = ContactKind.None,
                HitFighter = -1,
                Zone = HitZone.None,
                HitProp = -1,
                ColliderIndex = -1,
                Parent = parent,
                SplitCount = splitCount,
                BounceCount = bounceCount
            };
        }
    }
}
