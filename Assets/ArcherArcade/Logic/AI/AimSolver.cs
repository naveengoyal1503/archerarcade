namespace ArcherArcade.Logic
{
    /// <summary>
    /// Finds the exact (angle, power) that hits a point, including gravity, wind and the tip's gravity scale.
    /// It walks the angles and, for each, bisects power on the same fixed-step flight the real shot uses, so a
    /// solution is exact for the game, not an approximation of it. Used by the AI (before its difficulty noise)
    /// and by the level validation tests.
    /// </summary>
    public static class AimSolver
    {
        const int BisectIterations = 40;

        /// <summary>Solves for the point only (no obstacles).</summary>
        public static bool Solve(in AimRequest request, ShotConfig cfg, out AimSolution solution)
        {
            return SolveInternal(request, cfg, null, null, -1, ColliderKind.Fighter, -1, HitZone.None, out solution);
        }

        /// <summary>
        /// Solves and checks the full flight against the world: the first thing the arrow touches must be
        /// <paramref name="targetFighter"/>'s <paramref name="zone"/> (HitZone.None = any zone of that fighter).
        /// Walls and islands in the way make the solver pick a different arc.
        /// </summary>
        public static bool SolveValidated(in AimRequest request, ShotConfig cfg, CollisionWorld world, ArenaLayout arena,
            int shooter, int targetFighter, HitZone zone, out AimSolution solution)
        {
            return SolveInternal(request, cfg, world, arena, shooter, ColliderKind.Fighter, targetFighter, zone, out solution);
        }

        /// <summary>Like SolveValidated, but the first thing touched must be prop <paramref name="targetProp"/>.</summary>
        public static bool SolveValidatedProp(in AimRequest request, ShotConfig cfg, CollisionWorld world, ArenaLayout arena,
            int shooter, int targetProp, out AimSolution solution)
        {
            return SolveInternal(request, cfg, world, arena, shooter, ColliderKind.Prop, targetProp, HitZone.None, out solution);
        }

        static bool SolveInternal(in AimRequest r, ShotConfig cfg, CollisionWorld world, ArenaLayout arena, int shooter,
            ColliderKind targetKind, int targetOwner, HitZone zone, out AimSolution solution)
        {
            solution = default;
            double step = r.AngleStepDeg > 0.0 ? r.AngleStepDeg : 0.5;
            int count = (int)((cfg.MaxAngleDeg - cfg.MinAngleDeg) / step + 1e-9) + 1;
            Vec2 acc = Ballistics.Acceleration(r.Wind, r.GravityScale, cfg);

            for (int i = 0; i < count; i++)
            {
                double angle = r.PreferHighArc ? cfg.MaxAngleDeg - i * step : cfg.MinAngleDeg + i * step;
                double power, time;
                if (!SolvePowerForAngle(r, cfg, acc, angle, out power, out time)) continue;
                if (world != null && !Validate(r, cfg, acc, angle, power, world, arena, shooter, targetKind, targetOwner, zone)) continue;
                solution = new AimSolution { AngleDeg = angle, Power = power, FlightTime = time };
                return true;
            }
            return false;
        }

        static bool SolvePowerForAngle(in AimRequest r, ShotConfig cfg, Vec2 acc, double angle, out double power, out double time)
        {
            power = 0.0;
            time = 0.0;
            double tLo, tHi;
            double lo = HeightErrorAt(r, cfg, acc, angle, 0.0, out tLo);
            double hi = HeightErrorAt(r, cfg, acc, angle, 1.0, out tHi);
            if (hi < 0.0 || lo > 0.0) return false; // cannot reach even at full power, or overshoots at minimum

            double a = 0.0, b = 1.0;
            double tMid = tHi;
            for (int i = 0; i < BisectIterations; i++)
            {
                double mid = 0.5 * (a + b);
                double e = HeightErrorAt(r, cfg, acc, angle, mid, out tMid);
                if (e < 0.0) a = mid;
                else b = mid;
            }
            power = b;
            HeightErrorAt(r, cfg, acc, angle, power, out time);
            return true;
        }

        /// <summary>
        /// Height of the arc (minus the target's height) where it crosses the target's x. −∞ if it never gets there
        /// (falls into the void first, turned back by headwind, or times out).
        /// </summary>
        static double HeightErrorAt(in AimRequest r, ShotConfig cfg, Vec2 acc, double angle, double power, out double time)
        {
            time = 0.0;
            Vec2 pos = r.Origin;
            Vec2 vel = Ballistics.LaunchVelocity(angle, power, r.Facing, cfg);
            double dt = cfg.StepSeconds;
            int maxSteps = (int)(cfg.MaxFlightSeconds * cfg.SimHz);
            double dir = r.Facing >= 0 ? 1.0 : -1.0;
            double targetX = r.Target.X;

            if ((targetX - pos.X) * dir <= 0.0)
            {
                return double.NegativeInfinity;
            }

            for (int s = 0; s < maxSteps; s++)
            {
                Vec2 prev = pos;
                Ballistics.Step(ref pos, ref vel, acc, dt);
                if ((pos.X - targetX) * dir >= 0.0)
                {
                    double k = (targetX - prev.X) / (pos.X - prev.X);
                    time = (s + k) * dt;
                    return prev.Y + (pos.Y - prev.Y) * k - r.Target.Y;
                }
                if (vel.X * dir <= 0.0) return double.NegativeInfinity;
                if (pos.Y < r.Target.Y - 40.0) return double.NegativeInfinity;
            }
            return double.NegativeInfinity;
        }

        /// <summary>Flies the real (straight-line, no bounce) shot and checks what it touches first.</summary>
        static bool Validate(in AimRequest r, ShotConfig cfg, Vec2 acc, double angle, double power, CollisionWorld world,
            ArenaLayout arena, int shooter, ColliderKind targetKind, int targetOwner, HitZone zone)
        {
            Vec2 pos = r.Origin;
            Vec2 vel = Ballistics.LaunchVelocity(angle, power, r.Facing, cfg);
            double dt = cfg.StepSeconds;
            int maxSteps = (int)(cfg.MaxFlightSeconds * cfg.SimHz);
            double time = 0.0;
            for (int s = 0; s < maxSteps; s++)
            {
                Vec2 next = pos;
                Vec2 nextVel = vel;
                Ballistics.Step(ref next, ref nextVel, acc, dt);
                if (world.HasMotion) world.SetTime(r.Clock + time);
                double t;
                int ci;
                if (world.SweepFirst(pos, next, shooter, out t, out ci))
                {
                    ArenaCollider c = world.Colliders[ci];
                    return c.Kind == targetKind && c.Owner == targetOwner && !c.Bounces && (zone == HitZone.None || c.Zone == zone);
                }
                time += dt;
                pos = next;
                vel = nextVel;
                if (arena != null && (pos.Y < arena.KillY || pos.X < arena.MinX || pos.X > arena.MaxX)) return false;
            }
            return false;
        }
    }
}
