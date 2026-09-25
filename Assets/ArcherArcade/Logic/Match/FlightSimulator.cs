using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// Flies arrows through a <see cref="CollisionWorld"/> at the fixed step until each one hits something, falls
    /// into the void, leaves the arena or times out. Split arrows spawn their children at the apex; children are
    /// appended to the output and simulated in order, so results never depend on timing or frame rate.
    /// </summary>
    public static class FlightSimulator
    {
        public static void Simulate(Vec2 origin, Vec2 velocity, Vec2 acceleration, int splitCount, double splitSpreadDeg,
            ShotConfig cfg, CollisionWorld world, ArenaLayout arena, int ignoreOwner, List<ArrowPath> output)
        {
            int first = output.Count;
            output.Add(ArrowPath.Launch(origin, velocity, acceleration, 0.0, -1, splitCount > 1 ? splitCount : 0));
            for (int i = first; i < output.Count; i++)
            {
                ArrowPath path = output[i];
                Run(ref path, i, splitSpreadDeg, cfg, world, arena, ignoreOwner, output);
                output[i] = path;
            }
        }

        static void Run(ref ArrowPath path, int index, double spreadDeg, ShotConfig cfg, CollisionWorld world,
            ArenaLayout arena, int ignoreOwner, List<ArrowPath> output)
        {
            double dt = cfg.StepSeconds;
            Vec2 pos = path.StartPosition;
            Vec2 vel = path.StartVelocity;
            Vec2 acc = path.Acceleration;
            double time = path.StartTime;
            int maxSteps = (int)(cfg.MaxFlightSeconds * cfg.SimHz);
            int startStep = (int)(path.StartTime * cfg.SimHz + 0.5);

            for (int step = startStep; step < maxSteps; step++)
            {
                Vec2 nextPos = pos;
                Vec2 nextVel = vel;
                Ballistics.Step(ref nextPos, ref nextVel, acc, dt);

                double t;
                int ci;
                if (world != null && world.SweepFirst(pos, nextPos, ignoreOwner, out t, out ci))
                {
                    ArenaCollider c = world.Colliders[ci];
                    path.EndTime = time + t * dt;
                    path.EndPosition = Vec2.Lerp(pos, nextPos, t);
                    path.EndVelocity = Vec2.Lerp(vel, nextVel, t);
                    path.ColliderIndex = ci;
                    switch (c.Kind)
                    {
                        case ColliderKind.Fighter:
                            path.Contact = ContactKind.Fighter;
                            path.HitFighter = c.Owner;
                            path.Zone = c.Zone;
                            break;
                        case ColliderKind.Wall:
                            path.Contact = ContactKind.Wall;
                            break;
                        default:
                            path.Contact = ContactKind.Ground;
                            break;
                    }
                    return;
                }

                if (path.SplitCount > 1 && vel.Y > 0.0 && nextVel.Y <= 0.0)
                {
                    SpawnSplit(index, path.SplitCount, spreadDeg, nextPos, nextVel, acc, time + dt, output);
                    path.SplitCount = 0;
                }

                pos = nextPos;
                vel = nextVel;
                time += dt;

                if (pos.Y < arena.KillY || pos.X < arena.MinX || pos.X > arena.MaxX)
                {
                    path.EndTime = time;
                    path.EndPosition = pos;
                    path.EndVelocity = vel;
                    path.Contact = ContactKind.OutOfBounds;
                    return;
                }
            }

            path.EndTime = time;
            path.EndPosition = pos;
            path.EndVelocity = vel;
            path.Contact = ContactKind.Timeout;
        }

        /// <summary>The parent keeps flying as the middle arrow; children fan out evenly around it (odd counts).</summary>
        static void SpawnSplit(int parent, int count, double spreadDeg, Vec2 pos, Vec2 vel, Vec2 acc, double time,
            List<ArrowPath> output)
        {
            int half = (count - 1) / 2;
            for (int k = 1; k <= half; k++)
            {
                output.Add(ArrowPath.Launch(pos, vel.Rotated(spreadDeg * k), acc, time, parent, 0));
                output.Add(ArrowPath.Launch(pos, vel.Rotated(-spreadDeg * k), acc, time, parent, 0));
            }
        }
    }
}
