using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>All colliders for one shot. Rebuilt per shot; lists are reused to avoid allocations.</summary>
    public sealed class CollisionWorld
    {
        public readonly List<ArenaCollider> Colliders = new List<ArenaCollider>(32);

        /// <summary>True when any collider moves with the clock (call SetTime before sweeping).</summary>
        public bool HasMotion { get; private set; }

        public void Clear()
        {
            Colliders.Clear();
            HasMotion = false;
        }

        public void Add(Shape shape, ColliderKind kind, int owner = -1, HitZone zone = HitZone.None)
        {
            Colliders.Add(new ArenaCollider { Shape = shape, RestShape = shape, Kind = kind, Owner = owner, Zone = zone });
        }

        public void AddProp(int index, Shape restShape, PropMotion motion, bool bounces, double clock)
        {
            Colliders.Add(new ArenaCollider
            {
                Shape = motion.Apply(restShape, clock),
                RestShape = restShape,
                Motion = motion,
                Kind = ColliderKind.Prop,
                Owner = index,
                Bounces = bounces
            });
            if (motion.Kind != MotionKind.None) HasMotion = true;
        }

        /// <summary>Moves every moving prop to where it is at <paramref name="clock"/>.</summary>
        public void SetTime(double clock)
        {
            if (!HasMotion) return;
            for (int i = 0; i < Colliders.Count; i++)
            {
                ArenaCollider c = Colliders[i];
                if (c.Motion.Kind == MotionKind.None) continue;
                c.Shape = c.Motion.Apply(c.RestShape, clock);
                Colliders[i] = c;
            }
        }

        public void AddArena(ArenaLayout arena)
        {
            for (int i = 0; i < arena.Grounds.Count; i++) Add(arena.Grounds[i], ColliderKind.Ground);
            for (int i = 0; i < arena.Walls.Count; i++) Add(arena.Walls[i], ColliderKind.Wall);
        }

        /// <summary>Adds head, body and legs for a fighter (head first, so it wins exact ties).</summary>
        public void AddFighter(Fighter f, BodyConfig body)
        {
            if (f.Def.HasWeakSpot) Add(body.ZoneShape(HitZone.WeakSpot, f), ColliderKind.Fighter, f.Index, HitZone.WeakSpot);
            Add(body.ZoneShape(HitZone.Head, f), ColliderKind.Fighter, f.Index, HitZone.Head);
            Add(body.ZoneShape(HitZone.Body, f), ColliderKind.Fighter, f.Index, HitZone.Body);
            Add(body.ZoneShape(HitZone.Legs, f), ColliderKind.Fighter, f.Index, HitZone.Legs);
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
