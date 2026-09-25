using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Modes
{
    /// <summary>
    /// Quick Duel (GAME_DESIGN §7): you vs the computer with your archer, a chosen AI difficulty and arena. The
    /// opponent is a random hero archer (seeded); coin rewards come from the economy config.
    /// </summary>
    public static class QuickDuel
    {
        public static readonly string[] Difficulties = { "easy", "medium", "hard" };

        public sealed class Setup
        {
            public MatchSetup Match;
            public AiProfile Ai;
            public string ArenaId;
            public ArcherDef Opponent;
            public double Distance;
        }

        public static Setup Build(FighterSpec player, string difficulty, string arenaId, ulong seed)
        {
            var rng = new Rng(seed, 0x0D0E1UL);
            string arena = ArenaCatalog.Pick(arenaId, rng, false);
            double distance = rng.RangeInclusive(18, 26);
            ArcherDef opponent = ArcherTable.Hero(ArcherTable.HeroIds[rng.NextInt(ArcherTable.HeroIds.Length)]);
            int opponentLevel = difficulty == "hard" ? 3 : (difficulty == "medium" ? 2 : 1);
            WindRange wind = difficulty == "hard" ? new WindRange(1, 4) : (difficulty == "medium" ? new WindRange(0, 3) : new WindRange(0, 2));

            var opp = OpponentSpec.Of(opponent.Id, difficulty);
            var level = new LevelDef
            {
                World = 0, Number = 0, Name = "Quick Duel", Goal = GoalKind.Duel, Opponents = new[] { opp }, Wind = wind,
                BuildArena = a => ArenaCatalog.Build(a, arena, distance)
            };
            MatchSetup setup = LevelBuilder.Build(level, player, seed);
            setup.FirstTurn = FirstTurnRule.CoinFlip;
            FighterSpec enemy = setup.Fighters[1];
            enemy.Def = opponent;
            enemy.Level = opponentLevel;
            return new Setup { Match = setup, Ai = opp.Profile(), ArenaId = arena, Opponent = opponent, Distance = distance };
        }
    }
}
