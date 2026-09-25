namespace ArcherArcade.Logic.Modes
{
    /// <summary>
    /// Training Range (GAME_DESIGN §7): a board at 10 / 20 / 30 / 40 m, exact wind from the slider, long preview, no
    /// timer, hit statistics. No rewards (badge progress only).
    /// </summary>
    public sealed class TrainingRange
    {
        public static readonly int[] Distances = { 10, 20, 30, 40 };
        public const double NoTimerSeconds = 1e9;
        public const double PreviewShare = 0.45;
        public const double BoardHeight = 1.6;
        public const double BoardRadius = 0.6;
        public const double BullseyeHalfHeight = 0.2;

        public TrainingRange(FighterSpec player, int distance, int wind)
        {
            Distance = distance;
            Wind = wind < -Logic.Wind.MaxBars ? -Logic.Wind.MaxBars : (wind > Logic.Wind.MaxBars ? Logic.Wind.MaxBars : wind);
            var setup = new MatchSetup { Seed = 1UL, FixedWind = Wind };
            setup.Rules.TurnSeconds = NoTimerSeconds;
            setup.Arena.Grounds.Add(Shape.BoxFromTop(0, 0, 6, 2));
            setup.Arena.Grounds.Add(Shape.BoxFromTop(distance, 0, 4, 2));
            setup.Arena.MinX = -30;
            setup.Arena.MaxX = distance + 30;
            // A board on a post facing the player; hits within BullseyeHalfHeight of its centre are bullseyes.
            setup.Props.Add(new PropSpec
            {
                Kind = PropKind.Target, Shape = Shape.Circle(new Vec2(distance, BoardHeight), BoardRadius), Durable = true
            });
            player.Side = 0;
            player.Feet = new Vec2(0, 0);
            player.Facing = 1;
            setup.Fighters.Add(player);
            Match = new MatchState(setup);
        }

        public int Distance { get; }
        public int Wind { get; }
        public MatchState Match { get; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int Bullseyes { get; private set; }

        public double Accuracy => Shots == 0 ? 0.0 : (double)Hits / Shots;

        public ShotResult Shoot(ShotInput input)
        {
            ShotResult r = Match.ApplyShot(input);
            if (!r.Accepted) return r;
            Shots++;
            bool hit = false, bull = false;
            foreach (MatchEvent e in Match.Events)
            {
                if (e.Kind != MatchEventKind.TargetHit) continue;
                hit = true;
                bull |= System.Math.Abs(e.Point.Y - BoardHeight) <= BullseyeHalfHeight;
            }
            if (hit) Hits++;
            if (bull) Bullseyes++;
            return r;
        }
    }
}
