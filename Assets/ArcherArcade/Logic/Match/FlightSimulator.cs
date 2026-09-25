using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// Flies arrows through a <see cref="CollisionWorld"/> at the fixed step until each one hits something, falls
    /// into the void, leaves the arena or times out. Split arrows spawn their children at the apex and bounce pads
    /// end an arrow with a child that carries on; children are appended to the output and simulated in order, so
    /// results never depend on timing or frame rate. Moving props are placed with the match clock
    /// (<paramref name="clock"/> = clock at release, plus the flight time).
    /// </summary>
    public static class FlightSimulator
    {
        const double BounceLift = 1e-4;

        public static void Simulate(Vec2 origin, Vec2 velocity, Vec2 acceleration, int splitCount, double splitSpreadDeg,
            ShotConfig cfg, PropConfig props, CollisionWorld world, ArenaLayout arena, int ignoreOwner, double clock,
            List<ArrowPath> output, int launchCount = 1, double launchSpreadDeg = 0.0)
        {
            int first = output.Count;
            int split = splitCount > 1 ? splitCount : 0;
            output.Add(ArrowPath.Launch(origin, velocity, acceleration, 0.0, -1, split));

            // Fans (Triple Shot, Multi Arrow): extra launched arrows either side of the aimed one (odd counts).
            for (int k = 1; k <= (launchCount - 1) / 2; k++)
            {
                output.Add(ArrowPath.Launch(origin, velocity.Rotated(launchSpreadDeg * k), acceleration, 0.0, -1, split));
                output.Add(ArrowPath.Launch(origin, velocity.Rotated(-launchSpreadDeg * k), acceleration, 0.0, -1, split));
            }

            for (int i = first; i < output.Count; i++)
            {
                ArrowPath path = output[i];
                Run(ref path, i, splitSpreadDeg, cfg, props, world, arena, ignoreOwner, clock, output);
                output[i] = path;
            }
        }

        static void Run(ref ArrowPath path, int index, double spreadDeg, ShotConfig cfg, PropConfig props,
            CollisionWorld world, ArenaLayout arena, int ignoreOwner, double clock, List<ArrowPath> output)
        {
            double dt = cfg.StepSeconds;
            Vec2 pos = path.StartPosition;
            Vec2 vel = path.StartVelocity;
            Vec2 acc = path.Acceleration;
            double time = path.StartTime;
            double maxTime = cfg.MaxFlightSeconds;
            bool moving = world != null && world.HasMotion;

            while (time < maxTime)
            {
                Vec2 nextPos = pos;
                Vec2 nextVel = vel;
                Ballistics.Step(ref nextPos, ref nextVel, acc, dt);
                if (moving) world.SetTime(clock + time);

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
                        case ColliderKind.Prop:
                            path.HitProp = c.Owner;
                            if (c.Bounces && props != null && path.BounceCount < props.MaxBounces)
                            {
                                path.Contact = ContactKind.Bounce;
                                SpawnBounce(index, ref path, c.Shape, props, output);
                            }
                            else
                            {
                                path.Contact = ContactKind.Prop;
                            }
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
                    SpawnSplit(index, path.SplitCount, spreadDeg, nextPos, nextVel, acc, time + dt, path.BounceCount, output);
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
            int bounces, List<ArrowPath> output)
        {
            int half = (count - 1) / 2;
            for (int k = 1; k <= half; k++)
            {
                output.Add(ArrowPath.Launch(pos, vel.Rotated(spreadDeg * k), acc, time, parent, 0, bounces));
                output.Add(ArrowPath.Launch(pos, vel.Rotated(-spreadDeg * k), acc, time, parent, 0, bounces));
            }
        }

        /// <summary>Mirror the velocity about the pad's surface normal, keep a share of the speed, carry on.</summary>
        static void SpawnBounce(int parent, ref ArrowPath path, Shape pad, PropConfig props, List<ArrowPath> output)
        {
            Vec2 n = pad.NormalAt(path.EndPosition);
            Vec2 v = path.EndVelocity;
            double dot = Vec2.Dot(v, n);
            if (dot > 0.0) dot = 0.0; // already moving away: keep the direction
            Vec2 reflected = (v - n * (2.0 * dot)) * props.BounceSpeedKeep;
            Vec2 start = path.EndPosition + n * BounceLift;
            output.Add(ArrowPath.Launch(start, reflected, path.Acceleration, path.EndTime, parent, path.SplitCount,
                path.BounceCount + 1));
        }
    }
}
