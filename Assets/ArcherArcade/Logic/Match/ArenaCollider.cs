namespace ArcherArcade.Logic
{
    /// <summary>One solid thing an arrow can hit. Fighters add one collider per hit zone.</summary>
    public struct ArenaCollider
    {
        /// <summary>Shape now (moving props are updated with CollisionWorld.SetTime).</summary>
        public Shape Shape;

        /// <summary>Shape at rest, before continuous motion.</summary>
        public Shape RestShape;
        public PropMotion Motion;

        public ColliderKind Kind;

        /// <summary>Fighter index for fighter zones, prop index for props, otherwise −1.</summary>
        public int Owner;

        public HitZone Zone;

        /// <summary>Bounce pad: arrows reflect instead of stopping.</summary>
        public bool Bounces;
    }
}
