using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Modes
{
    /// <summary>
    /// Survival (design `modes` "Survival", pulled forward from ROADMAP 1.1): endless waves of World 1 archers.
    /// Each wave is its own duel; the player's HP carries over (+<see cref="HealBetweenWaves"/>, never above max),
    /// and opponents, AI skill, wind and distance grow with the wave. Same seed → same waves (tested).
    /// </summary>
    public sealed class SurvivalRun
    {
        public const int HealBetweenWaves = 20;
        public const int MaxWaves = 99;

        static readonly string[] Pool =
        {
            "bandit", "scout_pip", "crossbow_scout", "hunter_moss", "shield_bearer", "twig_twin", "healer_druid",
            "tower_sniper", "ranger_bramble", "captain_thorn"
        };

        readonly FighterSpec _player;
        readonly ulong _seed;

        public SurvivalRun(FighterSpec player, ulong seed)
        {
            _player = player;
            _seed = seed;
        }

        /// <summary>Current wave (1-based; 0 before the first).</summary>
        public int Wave { get; private set; }
        /// <summary>Waves won so far (the score).</summary>
        public int WavesCleared { get; private set; }
        public MatchState Match { get; private set; }
        public AiPlayer Ai { get; private set; }
        public OpponentSpec Opponent { get; private set; }
        public string ArenaId { get; private set; }
        /// <summary>The player's HP going into the next wave (0 = full).</summary>
        public int CarriedHp { get; private set; }
        public bool IsOver { get; private set; }

        public static string AiFor(int wave) => wave <= 2 ? "easy" : wave <= 5 ? "medium" : wave <= 9 ? "hard" : "boss";

        public static int HpFor(int wave) => wave * 8 + 52 > 200 ? 200 : wave * 8 + 52;

        public MatchState StartWave()
        {
            if (IsOver) return Match;
            Wave++;
            var rng = new Rng(_seed, 0x5EED00UL + (ulong)Wave);
            int tier = (Wave - 1) / 2;
            int pick = tier + rng.NextInt(2);
            if (pick >= Pool.Length) pick = Pool.Length - 1 - rng.NextInt(3);
            Opponent = OpponentSpec.Of(Pool[pick], AiFor(Wave), HpFor(Wave));
            string[] arenas = ArenaCatalog.QuickDuelArenas;
            ArenaId = arenas[rng.NextInt(arenas.Length)];
            double distance = 16 + (Wave < 12 ? Wave : 12);
            WindRange wind = Wave <= 2 ? new WindRange(0, 1) : Wave <= 5 ? new WindRange(0, 3) : new WindRange(1, 4);
            string arena = ArenaId;
            var level = new LevelDef
            {
                World = 0, Number = 0, Name = "Survival", Goal = GoalKind.Duel, Opponents = new[] { Opponent }, Wind = wind,
                BuildArena = a => ArenaCatalog.Build(a, arena, distance)
            };
            var player = new FighterSpec
            {
                Def = _player.Def, Side = 0, Level = _player.Level, Facing = 1, Tips = _player.Tips, NormalArrows = _player.NormalArrows,
                HpScale = _player.HpScale, BonusHp = _player.BonusHp, Boosters = Wave == 1 ? _player.Boosters : new BoosterKind[0],
                StartHp = CarriedHp
            };
            MatchSetup setup = LevelBuilder.Build(level, player, _seed * 31UL + (ulong)Wave);
            setup.FirstTurn = FirstTurnRule.SideZero;
            Match = new MatchState(setup);
            Ai = new AiPlayer(Opponent.Profile(), Match.DeriveSeed(7UL));
            return Match;
        }

        /// <summary>Call when the wave's match is over: carries HP into the next wave or ends the run.</summary>
        public void EndWave()
        {
            if (Match == null || Match.Phase != MatchPhase.Over) return;
            Fighter me = Match.GetFighter(0);
            if (Match.Winner != 0 || !me.IsAlive || Wave >= MaxWaves)
            {
                if (Match.Winner == 0) WavesCleared = Wave;
                IsOver = true;
                return;
            }
            WavesCleared = Wave;
            int hp = me.Hp + HealBetweenWaves;
            CarriedHp = hp >= me.MaxHp ? 0 : hp;
        }
    }
}
