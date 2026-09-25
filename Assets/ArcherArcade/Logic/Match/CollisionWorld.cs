using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>All colliders for one shot. Rebuilt per shot; lists are reused to avoid allocations.</summary>
    public sealed class CollisionWorld
    {
        public readonly List<ArenaCollider> Colliders = new List<ArenaCollider>(32);

        public void Clear() => Colliders.Clear();

        public void Add(Shape shape, ColliderKind kind, int owner = -1, HitZone zone = HitZone.None)
        {
            Colliders.Add(new ArenaCollider { Shape = shape, Kind = kind, Owner = owner, Zone = zone });
        }

        public void AddArena(ArenaLayout arena)
        {
            for (int i = 0; i < arena.Grounds.Count; i++) Add(arena.Grounds[i], ColliderKind.Ground);
            for (int i = 0; i < arena.Walls.Count; i++) Add(arena.Walls[i], ColliderKind.Wall);
        }

        /// <summary>Adds head, body and legs for a fighter (head first, so it wins exact ties).</summary>
        public void AddFighter(int index, Vec2 feet, BodyConfig body)
        {
            Add(body.ZoneShape(HitZone.Head, feet), ColliderKind.Fighter, index, HitZone.Head);
            Add(body.ZoneShape(HitZone.Body, feet), ColliderKind.Fighter, index, HitZone.Body);
            Add(body.ZoneShape(HitZone.Legs, feet), ColliderKind.Fighter, index, HitZone.Legs);
        }

        /// <summary>
        /// Earliest collider crossed by the segment p0→p1, skipping fighter <paramref name="ignoreOwner"/>
        /// (the shooter). Ties go to the collider added first.
        /// </summary>
        public bool SweepFirst(Vec2 p0, Vec2 p1, int ignoreOwner, out double t, out int colliderIndex)
        {
            t = double.MaxValue;
            colliderIndex = -1;
            for (int i = 0; i < Colliders.Count; i++)
            {
                ArenaCollider c = Colliders[i];
                if (c.Kind == ColliderKind.Fighter && c.Owner == ignoreOwner) continue;
                double tc;
                if (Sweep.Segment(p0, p1, c.Shape, out tc) && tc < t)
                {
                    t = tc;
                    colliderIndex = i;
                }
            }
            if (colliderIndex < 0) t = 0.0;
            return colliderIndex >= 0;
        }
    }
}
