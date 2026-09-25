namespace ArcherArcade.Logic.Campaign
{
    /// <summary>
    /// One attempt at a campaign level: owns the match and the computer opponents, counts what the goal needs
    /// (targets, apples, rope, arrows), ends the match when the goal is reached or failed, and scores the stars.
    /// The Match screen drives it: <see cref="Shoot"/> on release, <see cref="Tick"/> while aiming,
    /// <see cref="DecideAi"/> on the computer's turn.
    /// </summary>
    public sealed class LevelRun
    {
        public const int PlayerIndex = 0;

        readonly AiPlayer[] _ai;

        public LevelRun(LevelDef level, FighterSpec player, ulong seed, bool assist)
        {
            Level = level;
            Assist = assist;
            Match = new MatchState(LevelBuilder.Build(level, player, seed));
            _ai = new AiPlayer[Match.FighterCount];
            for (int i = 1; i < Match.FighterCount; i++)
            {
                _ai[i] = new AiPlayer(level.Opponents[i - 1].Profile(), Match.DeriveSeed((ulong)i));
            }
            Outcome = LevelOutcome.Playing;
        }

        public LevelDef Level { get; }
        public MatchState Match { get; }
        public bool Assist { get; }
        public LevelOutcome Outcome { get; private set; }

        public int ArrowsUsed { get; private set; }
        public int TargetsHit { get; private set; }
        public int ApplesHit { get; private set; }
        public int Headshots { get; private set; }
        public int Hits { get; private set; }
        public bool DummyHit { get; private set; }
        public bool RopeCut { get; private set; }
        public bool TookDamage { get; private set; }

        public int ArrowsLeft => Level.IsDuelGoal ? -1 : Level.EffectiveArrowLimit - ArrowsUsed;

        public bool IsPlayerTurn => Match.Phase == MatchPhase.Aiming && Match.CurrentSide == 0;

        /// <summary>The computer opponent whose turn it is (null on the player's turn).</summary>
        public AiPlayer CurrentAi => Match.Phase == MatchPhase.Aiming && Match.CurrentSide == 1 ? _ai[Match.CurrentFighter.Index] : null;

        public AiDecision DecideAi()
        {
            AiPlayer ai = CurrentAi;
            if (ai == null) throw new System.InvalidOperationException("It is not the computer's turn.");
            return ai.Decide(Match);
        }

        public ShotResult Shoot(ShotInput input)
        {
            int side = Match.CurrentSide;
            AiPlayer ai = CurrentAi;
            ShotResult r = Match.ApplyShot(input);
            if (!r.Accepted) return r;
            if (side == 0) ArrowsUsed++;
            else ai?.Observe(Match, r);
            Consume();
            CheckGoal();
            return r;
        }

        public bool Tick(double dt)
        {
            bool timedOut = Match.Tick(dt);
            Consume();
            CheckGoal();
            return timedOut;
        }

        void Consume()
        {
            var events = Match.Events;
            for (int i = 0; i < events.Count; i++)
            {
                MatchEvent e = events[i];
                bool byPlayer = e.Source == PlayerIndex;
                switch (e.Kind)
                {
                    case MatchEventKind.TargetHit: if (byPlayer) TargetsHit++; break;
                    case MatchEventKind.AppleHit: if (byPlayer) ApplesHit++; break;
                    case MatchEventKind.DummyHit: if (byPlayer) DummyHit = true; break;
                    case MatchEventKind.RopeCut: if (byPlayer) RopeCut = true; break;
                    case MatchEventKind.Hit:
                        if (byPlayer)
                        {
                            Hits++;
                            if (e.Zone == HitZone.Head) Headshots++;
                        }
                        break;
                }
                if (e.Fighter == PlayerIndex && e.Amount > 0 && IsDamage(e.Kind)) TookDamage = true;
            }
        }

        static bool IsDamage(MatchEventKind k)
        {
            switch (k)
            {
                case MatchEventKind.Hit:
                case MatchEventKind.ChainHit:
                case MatchEventKind.SplashHit:
                case MatchEventKind.ExplosionHit:
                case MatchEventKind.BurnDamage:
                case MatchEventKind.PoisonDamage:
                    return true;
                default:
                    return false;
            }
        }

        void CheckGoal()
        {
            if (Match.Phase != MatchPhase.Over)
            {
                switch (Level.Goal)
                {
                    case GoalKind.Targets:
                    case GoalKind.TrickShot:
                        if (TargetsHit >= Level.TargetCount) Match.EndByGoal(0);
                        else if (ArrowsUsed >= Level.EffectiveArrowLimit) Match.EndByGoal(1);
                        break;
                    case GoalKind.AppleShot:
                        if (DummyHit) Match.EndByGoal(1);
                        else if (ApplesHit >= Level.TargetCount) Match.EndByGoal(0);
                        else if (ArrowsUsed >= Level.EffectiveArrowLimit) Match.EndByGoal(1);
                        break;
                    case GoalKind.Rescue:
                        if (RopeCut) Match.EndByGoal(0);
                        break;
                }
            }
            if (Match.Phase == MatchPhase.Over) Outcome = Match.Winner == 0 ? LevelOutcome.Won : LevelOutcome.Lost;
        }

        public LevelResult Result()
        {
            Fighter p = Match.GetFighter(PlayerIndex);
            bool won = Outcome == LevelOutcome.Won;
            return new LevelResult
            {
                LevelNumber = Level.Number,
                Won = won,
                Stars = StarRules.For(Level, won, p.HpFraction, p.OwnTurns, ArrowsUsed, Assist),
                PlayerTurns = p.OwnTurns,
                ArrowsUsed = ArrowsUsed,
                HpFraction = p.HpFraction,
                Headshots = Headshots,
                Hits = Hits,
                Assist = Assist,
                TookNoDamage = !TookDamage
            };
        }
    }
}
