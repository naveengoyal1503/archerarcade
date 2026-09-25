using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Modes
{
    /// <summary>
    /// Daily Challenge (LEVELS.md): the UTC day number seeds a base template (duel / targets / apple / trick) and a
    /// twist, so every phone gets the same level the same day, offline. Opponents are Medium, arenas use World 1
    /// props. Reward and streak are handled by <see cref="Meta.Profile.RecordDaily"/>.
    /// </summary>
    public static class DailyChallenge
    {
        static readonly System.DateTime Epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

        public static int DayNumber(System.DateTime utc) => (int)(utc.Date - Epoch.Date).TotalDays;

        public sealed class Daily
        {
            public int Day;
            public DailyTemplate Template;
            public DailyTwist Twist;
            public LevelDef Level;
        }

        public static Daily ForDay(int day)
        {
            var rng = new Rng((ulong)day * 0x9E3779B97F4A7C15UL + 0xDA11UL, 0xDA17UL);
            var template = (DailyTemplate)rng.NextInt(4);
            DailyTwist[] twists = TwistsFor(template);
            DailyTwist twist = twists[rng.NextInt(twists.Length)];

            var level = new LevelDef
            {
                World = 0, Number = 0, Name = "Daily Challenge", Tier = LevelTier.Medium, PreviewShare = 0.30,
                Wind = twist == DailyTwist.StrongWind ? new WindRange(3, 5) : new WindRange(0, 2),
                Time = twist == DailyTwist.Night ? TimeOfDay.Night : TimeOfDay.Day
            };
            if (twist == DailyTwist.SplitOnly) level.OnlyTip = ArrowTip.Split;
            if (twist == DailyTwist.HeavyOnly) level.OnlyTip = ArrowTip.Heavy;
            double radius = twist == DailyTwist.TinyTargets ? 0.28 : 0.45;

            switch (template)
            {
                case DailyTemplate.Duel:
                {
                    double distance = twist == DailyTwist.LongDuel ? 35 : rng.RangeInclusive(18, 28);
                    int props = rng.NextInt(3);
                    level.Goal = GoalKind.Duel;
                    level.Par = 6;
                    level.Opponents = new[] { OpponentSpec.Of("bandit", "medium") };
                    level.DistanceMin = level.DistanceMax = distance;
                    level.BuildArena = a =>
                    {
                        a.Island(distance, 6);
                        if (props == 1)
                        {
                            a.Island(distance * 0.5, 3);
                            a.Wall(distance * 0.5, 0, 2.0);
                        }
                        else if (props == 2)
                        {
                            a.Island(distance * 0.5, 3);
                            a.Tower(0, distance * 0.5, 0, 3, 1);
                        }
                        a.Opponent(0, distance);
                    };
                    break;
                }
                case DailyTemplate.Targets:
                {
                    var xs = new double[4];
                    var ys = new double[4];
                    for (int i = 0; i < 4; i++)
                    {
                        xs[i] = 14 + i * 4 + rng.Range(-1.0, 1.0);
                        ys[i] = rng.Range(1.0, 3.0);
                    }
                    level.Goal = GoalKind.Targets;
                    level.TargetCount = 4;
                    level.Par = 6;
                    level.DistanceMin = 14;
                    level.DistanceMax = 27;
                    level.BuildArena = a =>
                    {
                        a.Island(21, 16);
                        for (int i = 0; i < 4; i++) a.Target(xs[i], ys[i], radius);
                    };
                    break;
                }
                case DailyTemplate.Apple:
                {
                    double gap = rng.Range(3.5, 5.0);
                    level.Goal = GoalKind.AppleShot;
                    level.TargetCount = 3;
                    level.Par = 4;
                    level.DistanceMin = 14;
                    level.DistanceMax = 14 + gap * 2;
                    level.BuildArena = a =>
                    {
                        a.Island(14 + gap, gap * 2 + 6);
                        for (int i = 0; i < 3; i++)
                        {
                            double x = 14 + i * gap;
                            a.Dummy(x, 0);
                            int apple = a.Apple(x, 0);
                            if (radius < 0.45) a.Props[apple].Shape = Shape.Circle(a.Props[apple].Shape.A, 0.12);
                        }
                    };
                    break;
                }
                default:
                {
                    LevelDef trick = WorldOne.Level(12);
                    level.Goal = GoalKind.TrickShot;
                    level.TargetCount = trick.TargetCount;
                    level.Par = trick.Par;
                    level.DistanceMin = trick.DistanceMin;
                    level.DistanceMax = trick.DistanceMax;
                    level.BuildArena = trick.BuildArena;
                    break;
                }
            }
            return new Daily { Day = day, Template = template, Twist = twist, Level = level };
        }

        static DailyTwist[] TwistsFor(DailyTemplate t)
        {
            switch (t)
            {
                case DailyTemplate.Duel:
                    return new[] { DailyTwist.StrongWind, DailyTwist.SplitOnly, DailyTwist.HeavyOnly, DailyTwist.LongDuel, DailyTwist.Night };
                case DailyTemplate.Targets:
                    return new[] { DailyTwist.StrongWind, DailyTwist.SplitOnly, DailyTwist.HeavyOnly, DailyTwist.TinyTargets, DailyTwist.Night };
                case DailyTemplate.Apple:
                    return new[] { DailyTwist.StrongWind, DailyTwist.HeavyOnly, DailyTwist.TinyTargets, DailyTwist.Night };
                default:
                    return new[] { DailyTwist.Night };
            }
        }
    }
}
