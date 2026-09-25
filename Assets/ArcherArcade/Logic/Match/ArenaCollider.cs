namespace ArcherArcade.Logic
{
    /// <summary>One solid thing an arrow can hit. Fighters add one collider per hit zone.</summary>
    public struct ArenaCollider
    {
        public Shape Shape;
        public ColliderKind Kind;

        /// <summary>Fighter index for fighter zones, otherwise −1.</summary>
        public int Owner;

        public HitZone Zone;
    }
}
