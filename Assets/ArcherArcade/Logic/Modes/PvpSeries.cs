using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Modes
{
    /// <summary>
    /// Same-phone 2-Player series (pass-and-play): best of 1 / 3 / 5, scoreboard, rematch, swap sides. Rewards are
    /// badge progress only. Player 0 = P1, player 1 = P2; a player stands on the left (side 0) unless sides are
    /// swapped.
    /// </summary>
    public sealed class PvpSeries
    {
        readonly int[] _wins = new int[2];

        public PvpSeries(PvpSettings settings)
        {
            Settings = settings;
            if (Settings.BestOf != 1 && Settings.BestOf != 3 && Settings.BestOf != 5) Settings.BestOf = 3;
            Settings.Handicap1 = Clamp(Settings.Handicap1);
            Settings.Handicap2 = Clamp(Settings.Handicap2);
            Settings.Name1 = CleanName(Settings.Name1, "P1");
            Settings.Name2 = CleanName(Settings.Name2, "P2");
        }

        public PvpSettings Settings { get; }
        public int Round { get; private set; }
        public bool Swapped { get; private set; }
        public int WinsNeeded => Settings.BestOf / 2 + 1;
        public int Wins(int player) => _wins[player];
        public bool IsOver => _wins[0] >= WinsNeeded || _wins[1] >= WinsNeeded;

        /// <summary>Series winner (0 = P1, 1 = P2) or −1 while playing.</summary>
        public int Winner => _wins[0] >= WinsNeeded ? 0 : (_wins[1] >= WinsNeeded ? 1 : -1);

        public int PlayerOnSide(int side) => Swapped ? 1 - side : side;

        public string NameOnSide(int side) => PlayerOnSide(side) == 0 ? Settings.Name1 : Settings.Name2;

        /// <summary>Match for the next round: players on their sides, coin flip for the first turn.</summary>
        public MatchSetup NextRound(ulong seed)
        {
            var rng = new Rng(seed, 0x2A11UL);
            string arena = ArenaCatalog.Pick(Settings.Arena, rng, true);
            double distance = rng.RangeInclusive(18, 26);
            var level = new LevelDef
            {
                World = 0, Number = 0, Name = "2-Player", Goal = GoalKind.Duel,
                Opponents = new[] { OpponentSpec.Of(ArcherIdOf(PlayerOnSide(1)), "easy") },
                Wind = Settings.WindOn ? new WindRange(0, 3) : WindRange.Calm,
                BuildArena = a => ArenaCatalog.Build(a, arena, distance)
            };
            int left = PlayerOnSide(0);
            var p = new FighterSpec { Def = ArcherTable.Hero(ArcherIdOf(left)), Level = 1, HpScale = HandicapOf(left) };
            MatchSetup setup = LevelBuilder.Build(level, p, seed);
            setup.FirstTurn = FirstTurnRule.CoinFlip;
            FighterSpec right = setup.Fighters[1];
            right.Def = ArcherTable.Hero(ArcherIdOf(PlayerOnSide(1)));
            right.HpScale = HandicapOf(PlayerOnSide(1));
            return setup;
        }

        public void RecordRound(int winnerSide)
        {
            if (IsOver) return;
            _wins[PlayerOnSide(winnerSide)]++;
            Round++;
        }

        public void Rematch()
        {
            _wins[0] = 0;
            _wins[1] = 0;
            Round = 0;
        }

        public void SwapSides() => Swapped = !Swapped;

        string ArcherIdOf(int player) => player == 0 ? Settings.Archer1 : Settings.Archer2;
        double HandicapOf(int player) => player == 0 ? Settings.Handicap1 : Settings.Handicap2;

        static double Clamp(double h) => h < PvpSettings.MinHandicap ? PvpSettings.MinHandicap : (h > PvpSettings.MaxHandicap ? PvpSettings.MaxHandicap : h);

        static string CleanName(string name, string fallback)
        {
            if (string.IsNullOrEmpty(name)) return fallback;
            string t = name.Trim();
            if (t.Length == 0) return fallback;
            return t.Length > PvpSettings.MaxNameLength ? t.Substring(0, PvpSettings.MaxNameLength) : t;
        }
    }
}
