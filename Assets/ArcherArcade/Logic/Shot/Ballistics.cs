namespace ArcherArcade.Logic
{
    /// <summary>
    /// Arrow motion: constant acceleration (gravity + wind) integrated exactly per fixed 120 Hz step, so sampled
    /// positions equal the analytic arc and every device computes identical numbers.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>Power (0..1) from drag distance in dp: clamp01(drag / 180) eased with easeOutQuad.</summary>
        public static double PowerFromDrag(double dragDp, ShotConfig cfg)
        {
            return DetMath.EaseOutQuad(DetMath.Clamp01(dragDp / cfg.PowerDragDp));
        }

        public static double SpeedFromPower(double power, ShotConfig cfg)
        {
            return DetMath.Lerp(cfg.MinSpeed, cfg.MaxSpeed, DetMath.Clamp01(power));
        }

        /// <summary>Launch velocity. <paramref name="facing"/> is +1 (shoots right) or −1 (shoots left).</summary>
        public static Vec2 LaunchVelocity(double angleDeg, double power, int facing, ShotConfig cfg)
        {
            double a = DetMath.Clamp(angleDeg, cfg.MinAngleDeg, cfg.MaxAngleDeg);
            double speed = SpeedFromPower(power, cfg);
            return new Vec2(facing * speed * DetMath.CosDeg(a), speed * DetMath.SinDeg(a));
        }

        /// <summary>Acceleration from gravity (scaled by the tip) and wind (signed, + blows toward +x).</summary>
        public static Vec2 Acceleration(double wind, double gravityScale, ShotConfig cfg)
        {
            return new Vec2(wind * cfg.WindAccelPerUnit, -cfg.Gravity * gravityScale);
        }

        /// <summary>Exact constant-acceleration step.</summary>
        public static void Step(ref Vec2 position, ref Vec2 velocity, Vec2 acceleration, double dt)
        {
            double halfDt2 = 0.5 * dt * dt;
            position = new Vec2(
                position.X + velocity.X * dt + acceleration.X * halfDt2,
                position.Y + velocity.Y * dt + acceleration.Y * halfDt2);
            velocity = new Vec2(velocity.X + acceleration.X * dt, velocity.Y + acceleration.Y * dt);
        }

        /// <summary>Analytic position after <paramref name="t"/> seconds (used by visuals to interpolate).</summary>
        public static Vec2 PositionAt(Vec2 start, Vec2 velocity, Vec2 acceleration, double t)
        {
            double h = 0.5 * t * t;
            return new Vec2(start.X + velocity.X * t + acceleration.X * h, start.Y + velocity.Y * t + acceleration.Y * h);
        }

        public static Vec2 VelocityAt(Vec2 velocity, Vec2 acceleration, double t)
        {
            return new Vec2(velocity.X + acceleration.X * t, velocity.Y + acceleration.Y * t);
        }

        /// <summary>
        /// Time for the arc to come back down to <paramref name="height"/> (0 if never). Used for the trajectory
        /// preview length ("share of the arc").
        /// </summary>
        public static double TimeToHeight(Vec2 start, Vec2 velocity, Vec2 acceleration, double height)
        {
            // start.Y + vy t + 0.5 ay t^2 = height, ay < 0 → larger root.
            double a = 0.5 * acceleration.Y;
            double b = velocity.Y;
            double c = start.Y - height;
            if (a >= 0.0) return 0.0;
            double disc = b * b - 4.0 * a * c;
            if (disc < 0.0) return 0.0;
            double t = (-b - DetMath.Sqrt(disc)) / (2.0 * a);
            return t > 0.0 ? t : 0.0;
        }
    }
}
