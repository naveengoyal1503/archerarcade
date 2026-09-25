using ArcherArcade.Archers;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;

namespace ArcherArcade.Match
{
    /// <summary>
    /// One game of any mode behind one interface for the Match scene: campaign / daily levels (LevelRun), Quick
    /// Duel and Survival (MatchState + AiPlayer), 2-Player rounds (PvpSeries, both sides human) and the Training
    /// Range. The scene only calls <see cref="Shoot"/>, <see cref="Tick"/>, <see cref="DecideAi"/>.
    /// </summary>
    public sealed class MatchSession
    {
        public readonly MatchRequest Request;
        public GameMode Mode => Request.Mode;
        public MatchState Match { get; private set; }
        public LevelRun Level { get; private set; }
        public LevelDef LevelDef { get; private set; }
        public TrainingRange Training { get; private set; }
        public SurvivalRun Survival { get; private set; }
        public PvpSeries Series => Request.Series;
        public QuickDuel.Setup Quick { get; private set; }
        public TimeOfDay Time { get; private set; } = TimeOfDay.Day;
        public string ArenaId { get; private set; } = "meadow";

        /// <summary>Look (exported character art) per fighter index.</summary>
        public string[] Looks { get; private set; }
        public int ArrowsUsed { get; private set; }
        public int Headshots { get; private set; }
        public int Hits { get; private set; }
        public bool TookDamage { get; private set; }
        public int RoundNumber { get; private set; } = 1;

        AiPlayer _ai;
        readonly FighterSpec _player;
        readonly string _playerSkin;

        public MatchSession(MatchRequest request)
        {
            Request = request;
            Profile profile = ServiceLocator.Profile;
            Loadout loadout = profile.CurrentLoadout();
            loadout.Boosters = request.Boosters ?? new BoosterKind[0];
            _player = profile.PlayerSpec(loadout);
            _playerSkin = loadout.SkinId;
            Start();
        }

        void Start()
        {
            ArrowsUsed = Headshots = Hits = 0;
            TookDamage = false;
            _ai = null;
            Level = null;
            Training = null;
            switch (Mode)
            {
                case GameMode.Campaign:
                    LevelDef = WorldOne.Level(Request.Level);
                    Level = new LevelRun(LevelDef, Copy(_player), Request.Seed, Request.Assist);
                    Match = Level.Match;
                    Time = LevelDef.Time;
                    ArenaId = "level" + LevelDef.Number;
                    break;
                case GameMode.Daily:
                    DailyChallenge.Daily d = DailyChallenge.ForDay(Request.Day);
                    LevelDef = d.Level;
                    Level = new LevelRun(LevelDef, Copy(_player), (ulong)Request.Day, false);
                    Match = Level.Match;
                    Time = LevelDef.Time;
                    break;
                case GameMode.QuickDuel:
                    Quick = QuickDuel.Build(Copy(_player), Request.Difficulty, Request.ArenaId, Request.Seed);
                    Match = new MatchState(Quick.Match);
                    _ai = new AiPlayer(Quick.Ai, Match.DeriveSeed(3UL));
                    ArenaId = Quick.ArenaId;
                    break;
                case GameMode.TwoPlayer:
                    Match = new MatchState(Series.NextRound(Request.Seed + (ulong)RoundNumber * 7919UL));
                    ArenaId = Series.Settings.Arena;
                    break;
                case GameMode.Training:
                    Training = new TrainingRange(Copy(_player), (int)Request.TrainingDistance, Request.TrainingWind);
                    Match = Training.Match;
                    break;
                case GameMode.Survival:
                    if (Survival == null) Survival = new SurvivalRun(Copy(_player), Request.Seed);
                    Match = Survival.StartWave();
                    _ai = Survival.Ai;
                    ArenaId = Survival.ArenaId;
                    Time = Survival.Wave % 6 == 5 ? TimeOfDay.Dusk : Survival.Wave % 6 == 0 ? TimeOfDay.Night : TimeOfDay.Day;
                    break;
            }
            BuildLooks();
        }

        static FighterSpec Copy(FighterSpec s) => new FighterSpec
        {
            Def = s.Def, Side = s.Side, Level = s.Level, Feet = s.Feet, Facing = s.Facing, Tips = s.Tips, NormalArrows = s.NormalArrows,
            HpScale = s.HpScale, BonusHp = s.BonusHp, StandOnProp = s.StandOnProp, StandOnTower = s.StandOnTower, Boosters = s.Boosters,
            StartHp = s.StartHp
        };

        void BuildLooks()
        {
            Looks = new string[Match.FighterCount];
            int twins = 0;
            for (int i = 0; i < Match.FighterCount; i++)
            {
                Fighter f = Match.GetFighter(i);
                string skin = null;
                if (i == 0 && Mode != GameMode.TwoPlayer) skin = _playerSkin;
                else if (Mode == GameMode.TwoPlayer)
                {
                    int player = Series.PlayerOnSide(f.Side);
                    skin = player == 0 ? Series.Settings.Skin1 : Series.Settings.Skin2;
                }
                Looks[i] = ArcherLooks.ForFighter(f.Def, skin, f.Def.Id == "twig_twin" ? twins++ : 0);
            }
        }

        /// <summary>Both sides are human in 2-Player; otherwise side 1 is the computer.</summary>
        public bool IsHuman(int side) => side == 0 || Mode == GameMode.TwoPlayer;

        public bool IsSolo => Match.IsSolo;

        public bool HumanTurn => Match.Phase == MatchPhase.Aiming && IsHuman(Match.CurrentSide);

        public AiDecision DecideAi()
        {
            if (Level != null) return Level.DecideAi();
            return _ai.Decide(Match);
        }

        public ShotResult Shoot(ShotInput input)
        {
            int side = Match.CurrentSide;
            ShotResult r;
            if (Level != null) r = Level.Shoot(input);
            else if (Training != null) r = Training.Shoot(input);
            else
            {
                r = Match.ApplyShot(input);
                if (r.Accepted && side == 1) _ai?.Observe(Match, r);
            }
            if (!r.Accepted) return r;
            if (side == 0 || Mode == GameMode.TwoPlayer) ArrowsUsed += side == 0 ? 1 : 0;
            foreach (MatchEvent e in Match.Events)
            {
                if (e.Kind == MatchEventKind.Hit && e.Source >= 0 && Match.GetFighter(e.Source).Side == 0)
                {
                    Hits++;
                    if (e.Zone == HitZone.Head) Headshots++;
                }
                if (e.Fighter == 0 && e.Amount > 0 && (e.Kind == MatchEventKind.Hit || e.Kind == MatchEventKind.SplashHit ||
                    e.Kind == MatchEventKind.ExplosionHit || e.Kind == MatchEventKind.ChainHit)) TookDamage = true;
            }
            return r;
        }

        /// <summary>Turn timer + moving targets. Returns true when the turn timed out.</summary>
        public bool Tick(double dt) => Level != null ? Level.Tick(dt) : Match.Tick(dt);

        public bool IsOver => Match.Phase == MatchPhase.Over;

        /// <summary>Did side 0 (the player, or the left player in 2-Player) win?</summary>
        public bool PlayerWon => IsOver && Match.Winner == 0;

        /// <summary>Trajectory preview share (GAME_DESIGN §3.1, LEVELS preview column, settings assist).</summary>
        public double PreviewShare(int side)
        {
            bool assist = ServiceLocator.Settings != null && ServiceLocator.Settings.Data.TrajectoryAssist;
            double share;
            switch (Mode)
            {
                case GameMode.Campaign:
                case GameMode.Daily:
                    share = LevelDef.PreviewShare;
                    if (Level.Assist) share += LossHelp.AssistPreviewBonus;
                    if (assist && LevelDef.Tier == LevelTier.Easy) share += 0.15;
                    break;
                case GameMode.TwoPlayer:
                    share = Series.Settings.PreviewShare + (assist ? 0.15 : 0.0);
                    break;
                case GameMode.Training:
                    share = TrainingRange.PreviewShare + (assist ? 0.25 : 0.0);
                    break;
                case GameMode.QuickDuel:
                    share = Request.Difficulty == "hard" ? 0.18 : Request.Difficulty == "easy" ? 0.45 : 0.30;
                    break;
                default:
                    share = Survival != null && Survival.Wave > 5 ? 0.18 : 0.30;
                    break;
            }
            Fighter f = Match.CurrentFighter;
            share *= 1.0 + f.Def.PreviewBonus;
            return share > 1.0 ? 1.0 : share;
        }

        /// <summary>Next 2-Player round (after RecordRound) or Survival wave (after EndWave).</summary>
        public void NextRound()
        {
            RoundNumber++;
            Start();
        }

        /// <summary>Plays the same thing again (Retry / Replay / Rematch).</summary>
        public void Restart()
        {
            RoundNumber = 1;
            if (Mode == GameMode.TwoPlayer) Series.Rematch();
            if (Mode == GameMode.Survival) Survival = null;
            Request.Seed = Request.Seed * 6364136223846793005UL + 1442695040888963407UL;
            Start();
        }
    }
}
